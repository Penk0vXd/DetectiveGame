using System;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Контролиран Play Mode тест върху отделно копие; не замества човешки playtest.
public static class NpcChatSmokeTest
{
    private const string Pending = "DetectiveGame.NpcSmoke";
    private static double started;
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Само отделен batch Editor.");
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/DetectiveOffice.unity");
        SessionState.SetBool(Pending, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Resume() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    private static void Subscribe()
    {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }
    private static async void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount < 3)
        {
            if (EditorApplication.timeSinceStartup - started > 90) Finish(1, "Play Mode timeout");
            return;
        }
        EditorApplication.update -= Tick;
        try
        {
            var tool = UnityEngine.Object.FindAnyObjectByType<ComputerInteractable>();
            var computer = UnityEngine.Object.FindAnyObjectByType<ComputerUIController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            tool.Interact();
            computer.ShowDesktop();
            var chat = UnityEngine.Object.FindAnyObjectByType<NpcChatUI>();
            Require(chat != null, "runtime chat created");
            chat.Open();
            var first = Field<ConversationSession>(chat, "session");
            Require(first != null && Field<GameObject>(chat, "window").activeInHierarchy, "profile loaded and window opened");
            Require(player.ModeController.CurrentMode == InteractionModeController.Mode.FocusedUI, "computer owns controls");
            Require(Cursor.visible && Cursor.lockState == CursorLockMode.None, "chat cursor unlocked");
            Require(!Field<TMP_Text>(chat, "transcript").richText, "untrusted markup disabled");
            Require(Field<TMP_InputField>(chat, "input").characterLimit == 1200, "input bounded");
            Require(player.ModeController.TryPause() && player.ModeController.TryResume(), "pause resume");
            Require(ReferenceEquals(first, Field<ConversationSession>(chat, "session")), "pause keeps session");
            var profile = UnityEngine.Object.Instantiate(Resources.Load<NpcProfile>("NpcPrototype/TestNpc"));
            try { await NpcConversationChecks.Boundaries(profile); }
            finally { UnityEngine.Object.Destroy(profile); }
            Field<TMP_InputField>(chat, "input").text = "Какво има върху масата?";
            Field<Button>(chat, "send").onClick.Invoke();
            var deadline = DateTime.UtcNow.AddSeconds(70);
            while (first.IsBusy && DateTime.UtcNow < deadline) await Task.Delay(50);
            Require(!first.IsBusy && first.State.Transcript.Count == 2, "Unity HTTP reply accepted");
            Require(Field<TMP_Text>(chat, "transcript").text.Contains(first.State.Transcript[1].Text), "reply rendered by UI");
            Debug.Log("NPC Unity live reply: " + first.State.Transcript[1].Text);
            computer.OpenMail();
            Require(!Field<GameObject>(chat, "window").activeSelf, "other app closes chat");
            chat.Open();
            Require(!ReferenceEquals(first, Field<ConversationSession>(chat, "session")), "new session on reentry");
            computer.ExitComputer();
            Require(Field<ConversationSession>(chat, "session") == null, "computer exit disposes session");
            Require(player.ModeController.CurrentMode == InteractionModeController.Mode.World, "world restored");
            Require(player.IsWorldInteractionActive && Field<bool>(player.GetComponent<FirstPersonController>(), "controlEnabled"), "world controls restored");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                Require(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked, "world cursor restored");
            else
                Debug.Log("NPC Unity smoke: physical cursor lock SKIPPED (null graphics device)");
            Finish(0, "PASS: boundaries, real model via UI, profile, chat creation, app switch, pause/resume, exit, reentry; cursor lock needs graphical test");
        }
        catch (Exception error) { Debug.LogException(error); Finish(1, error.Message); }
    }
    private static T Field<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Finish(int code, string message)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Pending, false);
        Debug.Log("NPC Unity smoke: " + message);
        EditorApplication.Exit(code);
    }
}
