using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// PROTOTYPE CODE: един ръчен Office slot с Player Notes persistence; не сериализира бъдещи case данни.
public sealed class OfficeSaveController : MonoBehaviour
{
    private const int MinSupportedVersion = 1;
    private const int CurrentVersion = 5;

    [Serializable]
    private sealed class SaveData
    {
        public int version = CurrentVersion;
        public string scene;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public ComputerPowerState computer;
        public PlayerNoteSaveDto[] playerNotes;
        public AttachedSourceSaveDto[] attachedSources;
        public TimelineEventSaveDto[] timelineEvents;
        public CaseTheorySaveDto[] caseTheories;
    }

    private const string SceneName = "DetectiveOffice";
    private FirstPersonController player;
    private PlayerInteraction interaction;
    private ComputerPowerController power;
    private ComputerUIController computer;
    private PhoneUIController phone;
    private EvidenceBoardUIController board;

    private string SavePath => Path.Combine(Application.persistentDataPath, "office-save-v1.json");
    public bool HasSave => File.Exists(SavePath);

    private void Awake()
    {
        player = GetComponent<FirstPersonController>();
        interaction = GetComponent<PlayerInteraction>();
        power = FindAnyObjectByType<ComputerPowerController>();
        computer = FindAnyObjectByType<ComputerUIController>();
        phone = FindAnyObjectByType<PhoneUIController>();
        board = FindAnyObjectByType<EvidenceBoardUIController>();
    }

    public bool TrySave(out string message)
    {
        if (!Ready(out message)) return false;
        SaveData data = new SaveData
        {
            version = CurrentVersion,
            scene = SceneName,
            position = player.transform.position,
            yaw = player.transform.eulerAngles.y,
            pitch = player.ViewPitch,
            computer = power.StableState,
            playerNotes = board.CapturePlayerNotesSnapshot(),
            attachedSources = board.CaptureAttachedSourcesSnapshot(),
            timelineEvents = board.CaptureTimelineSnapshot(),
            caseTheories = board.CaptureCaseTheoriesSnapshot()
        };
        if (!Valid(data))
        {
            message = "Играта не може да бъде запазена от тази позиция.";
            return false;
        }

        string path = SavePath;
        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(temporary, JsonUtility.ToJson(data));
            // Подменя валидния файл едва след пълен запис на временния.
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
            message = "Играта е запазена.";
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Office save failed: {exception.Message}", this);
            message = "Записът не успя. Съществуващият save не е променен.";
            return false;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
        }
    }

    public bool TryLoad(out string message)
    {
        if (!Ready(out message)) return false;
        SaveData data;
        try
        {
            if (!File.Exists(SavePath))
            {
                message = "Няма запазена игра.";
                return false;
            }
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Office load failed: {exception.Message}", this);
            message = "Записът не може да бъде прочетен.";
            return false;
        }
        if (!Valid(data))
        {
            message = "Записът е повреден или от друга версия.";
            return false;
        }

        // Валидацията и подготовката на бележките и прикачените източници предхождат промените по UI, owner и world.
        List<PlayerNote> restoredNotes = PlayerNotesSaveMapper.PrepareRestore(data.playerNotes, out int skippedNotes);
        if (skippedNotes > 0)
            Debug.LogWarning($"Office load skipped {skippedNotes} invalid player note(s).", this);
        List<AttachedSource> restoredAttachments = AttachedSourcesSaveMapper.PrepareRestore(data.attachedSources, out int skippedAttachments);
        if (skippedAttachments > 0)
            Debug.LogWarning($"Office load skipped {skippedAttachments} invalid attached source(s).", this);
        List<TimelineEvent> restoredTimeline = TimelineSaveMapper.PrepareRestore(data.timelineEvents, out int skippedTimeline);
        if (skippedTimeline > 0)
            Debug.LogWarning($"Office load skipped {skippedTimeline} invalid timeline event(s).", this);
        List<CaseTheoryDraft> restoredTheories = CaseTheorySaveMapper.PrepareRestore(data.caseTheories, out int skippedTheories);
        if (skippedTheories > 0)
            Debug.LogWarning($"Office load skipped {skippedTheories} invalid case theory draft(s).", this);

        MonoBehaviour owner = interaction.ModeController.ActiveOwner;
        if (owner is DocumentInteractable document) document.Interact();
        else if (owner is ComputerInteractable) computer.ExitComputer();
        else if (owner is PhoneInteractable) phone.ExitPhone();
        else if (owner is EvidenceBoardInteractable) board.ExitBoard();
        if (interaction.ModeController.ActiveOwner != null)
        {
            message = "Активният инструмент не можа да бъде затворен.";
            return false;
        }
        player.RestorePose(data.position, data.yaw, data.pitch);
        power.RestoreStableState(data.computer);
        board.RestorePlayerNotes(restoredNotes);
        board.RestoreAttachedSources(restoredAttachments);
        board.RestoreTimeline(restoredTimeline);
        board.RestoreCaseTheories(restoredTheories);
        message = "Играта е заредена.";
        return true;
    }

    private bool Ready(out string message)
    {
        if (SceneManager.GetActiveScene().name != SceneName || player == null || interaction == null
            || interaction.ModeController == null || power == null || !power.CanRestore || computer == null
            || phone == null || board == null)
        {
            message = "Office save не е достъпен в тази сцена.";
            return false;
        }
        message = null;
        return true;
    }

    private static bool Valid(SaveData data)
    {
        return data != null && data.version >= MinSupportedVersion && data.version <= CurrentVersion
            && data.scene == SceneName
            && Finite(data.position.x) && Finite(data.position.y) && Finite(data.position.z)
            && data.position.sqrMagnitude < 1000000f
            && Finite(data.yaw) && Finite(data.pitch) && data.pitch >= -80f && data.pitch <= 80f
            && (data.computer == ComputerPowerState.Off || data.computer == ComputerPowerState.On
                || data.computer == ComputerPowerState.Sleeping);
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
