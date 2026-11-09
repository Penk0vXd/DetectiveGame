using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// PROTOTYPE CODE: един ръчен Office slot; не сериализира бъдещи case данни.
public sealed class OfficeSaveController : MonoBehaviour
{
    [Serializable]
    private sealed class SaveData
    {
        public int version = 1;
        public string scene;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public ComputerPowerState computer;
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
            scene = SceneName,
            position = player.transform.position,
            yaw = player.transform.eulerAngles.y,
            pitch = player.ViewPitch,
            computer = power.StableState
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

        // Валидацията предхожда промените по UI, owner и world.
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
        return data != null && data.version == 1 && data.scene == SceneName
            && Finite(data.position.x) && Finite(data.position.y) && Finite(data.position.z)
            && data.position.sqrMagnitude < 1000000f
            && Finite(data.yaw) && Finite(data.pitch) && data.pitch >= -80f && data.pitch <= 80f
            && (data.computer == ComputerPowerState.Off || data.computer == ComputerPowerState.On
                || data.computer == ComputerPowerState.Sleeping);
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
