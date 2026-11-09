using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Контролиран Play Mode тест на state и persistence; не заменя физически UI test.
public static class OfficeStageTwoSmokeTest
{
    private const string Pending = "DetectiveGame.StageTwoSmoke";
    private const string SettingsKey = "office-settings-v1";
    private static double started;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Използвай batch Editor.");
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/DetectiveOffice.unity");
        SessionState.SetBool(Pending, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Reload()
    {
        if (SessionState.GetBool(Pending, false)) Subscribe();
    }

    private static void Subscribe()
    {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount < 3)
        {
            if (EditorApplication.timeSinceStartup - started > 90)
                Finish(1, "Play Mode timeout");
            return;
        }
        EditorApplication.update -= Tick;
        string savePath = Path.Combine(Application.persistentDataPath, "office-save-v1.json");
        bool hadSave = File.Exists(savePath);
        byte[] previousSave = hadSave ? File.ReadAllBytes(savePath) : null;
        bool hadSettings = PlayerPrefs.HasKey(SettingsKey);
        string previousSettings = hadSettings ? PlayerPrefs.GetString(SettingsKey) : null;
        int resultCode = 0;
        string resultMessage = "PASS";
        try
        {
            var interaction = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            var player = interaction.GetComponent<FirstPersonController>();
            var input = interaction.GetComponent<PlayerInputReader>();
            var pause = interaction.GetComponent<PauseUIController>();
            var save = interaction.GetComponent<OfficeSaveController>();
            var power = UnityEngine.Object.FindAnyObjectByType<ComputerPowerController>();
            Require(player != null && input != null && pause != null && save != null && power != null,
                "Office components initialize");
            Require(UnityEngine.Object.FindAnyObjectByType<OfficeSettingsUI>(FindObjectsInactive.Include) != null,
                "Settings runtime view exists");

            Vector3 original = player.transform.position;
            float yaw = player.transform.eulerAngles.y;
            float pitch = player.ViewPitch;
            ComputerPowerState computer = power.StableState;
            Require(save.TrySave(out string saveMessage), "Save succeeds: " + saveMessage);
            player.RestorePose(original + Vector3.right, yaw + 25f, pitch + 10f);
            power.RestoreStableState(ComputerPowerState.On);
            Require(save.TryLoad(out string loadMessage), "Load succeeds: " + loadMessage);
            Require(Vector3.Distance(player.transform.position, original) < .001f, "Player position restored");
            Require(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, yaw)) < .001f,
                "Player yaw restored");
            Require(Mathf.Abs(player.ViewPitch - pitch) < .001f, "View pitch restored");
            Require(power.StableState == computer, "Computer power restored");
            Require(interaction.ModeController.CurrentMode == InteractionModeController.Mode.World,
                "Load returns World mode");

            var phone = UnityEngine.Object.FindAnyObjectByType<PhoneInteractable>();
            phone.Interact();
            Require(interaction.ModeController.ActiveOwner == phone, "Phone owner entered");
            Require(save.TryLoad(out loadMessage), "Load from Phone succeeds: " + loadMessage);
            Require(interaction.ModeController.ActiveOwner == null
                && interaction.ModeController.CurrentMode == InteractionModeController.Mode.World,
                "Load releases Phone owner");

            var computerTool = UnityEngine.Object.FindAnyObjectByType<ComputerInteractable>();
            computerTool.Interact();
            Require(interaction.ModeController.ActiveOwner == computerTool, "Computer owner entered");
            Require(save.TryLoad(out loadMessage) && interaction.ModeController.ActiveOwner == null,
                "Load releases Computer owner");

            var boardTool = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardInteractable>();
            boardTool.Interact();
            Require(interaction.ModeController.ActiveOwner == boardTool, "Board owner entered");
            var boardUI = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardUIController>();
            var boardNotes = (TMP_InputField)typeof(EvidenceBoardUIController)
                .GetField("notesInput", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
            boardNotes.text = "Временна бележка";
            Require(save.TryLoad(out loadMessage) && interaction.ModeController.ActiveOwner == null,
                "Load releases Board owner");
            Require(boardNotes.text == "Временна бележка", "Load keeps session-only Board notes in memory");

            var documentTool = UnityEngine.Object.FindAnyObjectByType<DocumentInteractable>();
            Vector3 documentPosition = documentTool.transform.position;
            documentTool.Interact();
            Require(interaction.ModeController.ActiveOwner == documentTool, "Document owner entered");
            Require(save.TryLoad(out loadMessage) && interaction.ModeController.ActiveOwner == null,
                "Load releases Document owner");
            Require(Vector3.Distance(documentTool.transform.position, documentPosition) < .001f,
                "Load returns document transform");

            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pause, null);
            GameObject pauseRoot = GameObject.Find("RuntimePauseCanvas");
            Require(pauseRoot != null && interaction.ModeController.IsPaused, "Pause runtime view opens");
            pauseRoot.transform.Find("MainPanel/SaveButton").GetComponent<Button>().onClick.Invoke();
            Require(pauseRoot.transform.Find("SaveConfirmationPanel").gameObject.activeSelf,
                "Existing slot asks before overwrite");
            pauseRoot.transform.Find("SaveConfirmationPanel/ConfirmButton").GetComponent<Button>().onClick.Invoke();
            Require(pauseRoot.transform.Find("MainPanel").gameObject.activeSelf, "Save returns to menu");
            pauseRoot.transform.Find("MainPanel/SettingsButton").GetComponent<Button>().onClick.Invoke();
            Require(pauseRoot.transform.Find("SettingsPanel").gameObject.activeSelf,
                "Settings view opens from pause");
            pause.Resume();
            Require(!interaction.ModeController.IsPaused && interaction.ModeController.CurrentMode
                == InteractionModeController.Mode.World, "Esc/Resume path restores World");

            Vector3 beforeCorruptLoad = player.transform.position;
            File.WriteAllText(savePath, "{not valid json");
            Require(!save.TryLoad(out loadMessage), "Corrupt slot is rejected");
            Require(Vector3.Distance(player.transform.position, beforeCorruptLoad) < .001f,
                "Corrupt load does not mutate World");

            OfficeSettingsData originalSettings = OfficeSettingsStore.Current;
            OfficeSettingsData conflict = originalSettings.Copy();
            conflict.bindings[4] = conflict.bindings[0];
            Require(!OfficeSettingsStore.Valid(conflict), "Duplicate binding rejected");
            conflict.bindings[4] = "<Keyboard>/escape";
            Require(!OfficeSettingsStore.Valid(conflict), "Esc binding rejected");
            OfficeSettingsData changed = originalSettings.Copy();
            changed.volume = .3f;
            changed.bindings[4] = "<Keyboard>/f";
            Require(OfficeSettingsStore.TryApply(changed, input, out string settingsMessage),
                "Settings apply succeeds: " + settingsMessage);
            Require(Mathf.Abs(AudioListener.volume - .3f) < .001f
                && input.GetInteractBindingLabel().Contains("F"), "Settings apply to audio and input");
            OfficeSettingsStore.Load();
            Require(Mathf.Abs(OfficeSettingsStore.Current.volume - .3f) < .001f,
                "Settings persist through reload");
            Require(OfficeSettingsStore.TryApply(originalSettings, input, out settingsMessage),
                "Settings restore succeeds: " + settingsMessage);
            Debug.Log("Office Stage 2 checks: save/load pose and power, four tool owners, pause Save/Settings views, corrupt file, settings persistence/conflicts");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            resultCode = 1;
            resultMessage = exception.Message;
        }
        finally
        {
            if (hadSave) File.WriteAllBytes(savePath, previousSave);
            else if (File.Exists(savePath)) File.Delete(savePath);
            if (hadSettings) PlayerPrefs.SetString(SettingsKey, previousSettings);
            else PlayerPrefs.DeleteKey(SettingsKey);
            PlayerPrefs.Save();
        }
        Finish(resultCode, resultMessage);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        Debug.Log("Office Stage 2 smoke: " + message);
        EditorApplication.Exit(code);
    }
}
