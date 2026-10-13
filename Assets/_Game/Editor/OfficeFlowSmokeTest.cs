using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Контролиран scene/runtime тест. Не замества физически input и човешки playtest.
public static class OfficeFlowSmokeTest
{
    private const string Pending = "DetectiveGame.OfficeFlowSmoke";
    private static double started;
    private static PlayerInteraction player;
    private static PauseUIController pause;
    private static readonly List<string> errors = new List<string>();
    private static readonly List<string> overflows = new List<string>();
    private static int editorSearchErrors;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Използвай отделен batch Editor.");
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/DetectiveOffice.unity");
        SessionState.SetBool(Pending, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Reload() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    private static void Subscribe()
    {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
    }
    private static void Log(string message, string stack, LogType type)
    {
        // Известният Editor search-index exception се отчита отделно, не като gameplay failure.
        if (type == LogType.Exception && stack.Contains("UnityEditor.Search.SearchDatabase"))
        {
            editorSearchErrors++;
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message);
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
            player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            pause = UnityEngine.Object.FindAnyObjectByType<PauseUIController>();
            Require(player != null && pause != null, "Player/Pause initialization");
            // Камерата остава в оригиналния office; движението е изключено само за стабилни captures.
            player.GetComponent<FirstPersonController>().enabled = false;
            await Computer();
            await Phone();
            await Document();
            await Board();
            // Свободен ред и повторно влизане без unlock state.
            await Document();
            await Phone();
            await Board();
            await Computer();
            Require(errors.Count == 0, "Unexpected runtime errors: " + string.Join("; ", errors));
            Require(overflows.Count == 0, "Text overflow: " + string.Join("; ", overflows));
            Finish(0, "PASS: Computer/Phone/Document/Board, reverse re-entry, content, pause x10, UI raycast blocking, transforms, power lifecycle; controlled callbacks, not human input");
        }
        catch (Exception error) { Debug.LogException(error); Finish(1, error.Message); }
    }

    private static async Task Computer()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<ComputerInteractable>();
        var ui = UnityEngine.Object.FindAnyObjectByType<ComputerUIController>();
        var power = UnityEngine.Object.FindAnyObjectByType<ComputerPowerController>();
        Aim(tool);
        tool.Interact();
        await Until(() => power.CurrentState == ComputerPowerState.On);
        ui.OpenMail();
        var email = Field<EmailUIController>(ui, "emailUIController");
        var record = Field<List<EmailData>>(email, "emails").First(e => e.Body.Contains("22:41"));
        email.SelectEmail(record);
        Require(Field<TMP_Text>(email, "bodyText").text == record.Body, "Email exact source body");
        var root = Field<GameObject>(ui, "computerRoot");
        await CheckPause(tool, Field<GameObject>(email, "mailWindow"));
        await Capture("computer-mail", root);
        ui.ExitComputer(); World();
        tool.Interact();
        power.Sleep(); World();
        Require(power.CurrentState == ComputerPowerState.Sleeping, "Sleep state");
        tool.Interact();
        Require(power.CurrentState == ComputerPowerState.On, "Wake state");
        power.Restart();
        Require(power.CurrentState == ComputerPowerState.Booting, "Restart boot");
        await Until(() => power.CurrentState == ComputerPowerState.On);
        power.ShutDown();
        await Until(() => power.CurrentState == ComputerPowerState.Off);
        World();
    }
    private static async Task Phone()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<PhoneInteractable>();
        var ui = UnityEngine.Object.FindAnyObjectByType<PhoneUIController>();
        Aim(tool);
        tool.Interact();
        ui.StartCall();
        Require(Field<TMP_Text>(ui, "dialogueText").text.Contains("бюрото"), "Phone document reference");
        await CheckPause(tool, Field<GameObject>(ui, "callPanel"));
        await Capture("phone-call", Field<GameObject>(ui, "phoneRoot"));
        ui.HangUp();
        Require(Field<GameObject>(ui, "contactsPanel").activeInHierarchy, "HangUp contacts");
        ui.ExitPhone(); World();
    }
    private static async Task Document()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<DocumentInteractable>();
        Aim(tool);
        Transform parent = tool.transform.parent;
        Vector3 position = tool.transform.localPosition, scale = tool.transform.localScale;
        Quaternion rotation = tool.transform.localRotation;
        tool.Interact();
        var root = Field<GameObject>(tool, "readingOverlay");
        Require(root != null && root.GetComponentInChildren<TMP_Text>().text.Contains("22:41"), "Document transcript");
        Require(root.transform.Find("ReadingPanel/CloseHint").GetComponent<TMP_Text>().text
            .Contains("[" + player.GetComponent<PlayerInputReader>().GetInteractBindingLabel() + "]"), "Document exit binding hint");
        await CheckPause(tool, root);
        await Capture("document", root);
        tool.Interact(); World();
        Require(tool.transform.parent == parent && Vector3.Distance(position, tool.transform.localPosition) < 0.0001f
            && Quaternion.Angle(rotation, tool.transform.localRotation) < 0.01f && tool.transform.localScale == scale,
            "Document original transform");
        await Task.Delay(50);
        Require(root == null, "Document overlay cleanup");
    }
    private static async Task Board()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardInteractable>();
        var ui = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardUIController>();
        Aim(tool);
        tool.Interact();
        for (int i = 0; i < 3; i++)
        {
            ui.ShowEvidenceDetails(i);
            var title = Field<TMP_Text>(ui, "evidenceTitleText");
            Require(!string.IsNullOrWhiteSpace(title.text), "Board title " + i);
            if (i == 1) Require(Field<TMP_Text>(ui, "evidenceDescriptionText").text.Contains("22:41"), "Board timestamp");
            await CheckPause(tool, Field<GameObject>(ui, "evidenceDetailsPanel"));
            await Capture("board-detail-" + i, Field<GameObject>(ui, "boardRoot"));
            ui.BackToBoard();
            Require(Field<GameObject>(ui, "boardPanel").activeInHierarchy, "Board back");
        }
        ui.ExitBoard(); World();
    }
    private static async Task CheckPause(MonoBehaviour owner, GameObject screen)
    {
        var camera = player.GetComponentInChildren<Camera>();
        Quaternion rotation = camera.transform.rotation;
        float fov = camera.fieldOfView;
        for (int i = 0; i < 10; i++)
        {
            Invoke(pause, "Pause");
            await Task.Delay(50);
            Require(player.ModeController.IsPaused && Time.timeScale == 0 && screen.activeInHierarchy
                && ReferenceEquals(player.ModeController.ActiveOwner, owner), "Pause preserves owner/screen");
            Require(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Pause cursor");
            if (i == 0) await Capture("pause-" + owner.GetType().Name, Field<GameObject>(pause, "pauseRoot"));
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * .8f, Screen.height * .5f) }, hits);
            Require(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(Field<GameObject>(pause, "pauseRoot").transform),
                $"Pause blocks underlying pointer raycast ({Screen.width}x{Screen.height}; hits={string.Join(",", hits.Select(h => h.gameObject.name))}; root={Field<GameObject>(pause, "pauseRoot").name})");
            Field<Button>(pause, "resumeButton").onClick.Invoke();
            Require(!player.ModeController.IsPaused && Time.timeScale == 1 && screen.activeInHierarchy
                && ReferenceEquals(player.ModeController.ActiveOwner, owner), "Resume preserves owner/screen");
            Require(Quaternion.Angle(rotation, camera.transform.rotation) < .01f && camera.fieldOfView == fov, "Pause camera");
            Require(!Field<bool>(player.GetComponent<FirstPersonController>(), "controlEnabled"), "No world movement in tool");
            Require(Cursor.visible == !(owner is DocumentInteractable), "Resume cursor visibility");
        }
    }
    private static void World()
    {
        Require(player.ModeController.CurrentMode == InteractionModeController.Mode.World
            && player.ModeController.ActiveOwner == null && player.IsWorldInteractionActive
            && Field<bool>(player.GetComponent<FirstPersonController>(), "controlEnabled"), "World controls restored");
        Require(!Cursor.visible, "World cursor hidden flag");
        Debug.Log("Office flow: physical cursor lock SKIPPED (batch Editor has no focused Game View)");
    }
    private static void Aim(MonoBehaviour tool)
    {
        var camera = player.GetComponentInChildren<Camera>();
        Vector3 center = tool.GetComponent<Collider>().bounds.center;
        foreach (float distance in new[] { 1.2f, 1.8f, 2.4f })
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI / 8;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            point.y = Mathf.Clamp(center.y + .35f, 1.2f, 1.8f);
            if (Physics.CheckSphere(point, .12f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (!Physics.Raycast(point, center - point, out RaycastHit hit, 3f) || hit.collider.gameObject != tool.gameObject) continue;
            camera.transform.position = point;
            camera.transform.rotation = Quaternion.LookRotation(center - point);
            return;
        }
        throw new Exception("No unobstructed viewpoint: " + tool.name);
    }
    private static Task Capture(string name, GameObject root)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("Office flow: renders/overflow SKIPPED (null graphics device)");
            return Task.CompletedTask;
        }
        Directory.CreateDirectory("Logs/OfficeFlowCaptures");
        var camera = player.GetComponentInChildren<Camera>();
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>()
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        foreach (int width in new[] { 1920, 1280 })
        {
            var texture = new RenderTexture(width, width * 9 / 16, 24);
            texture.Create();
            camera.targetTexture = texture;
            foreach (var canvas in canvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = .5f;
                canvas.scaleFactor = width / 1920f;
            }
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = texture;
            var pixels = new Texture2D(width, width * 9 / 16, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes($"Logs/OfficeFlowCaptures/{name}-{width}.png", pixels.EncodeToPNG());
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                if (text.isTextOverflowing)
                {
                    overflows.Add($"{name}/{text.transform.parent.name}/{text.name}");
                    Debug.LogWarning($"Office text overflow: {name}/{text.transform.parent.name}/{text.name}: {text.text}; rect={text.rectTransform.rect}; size={text.fontSize}");
                }
            RenderTexture.active = null;
            camera.targetTexture = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(pixels);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = true;
            }
        }
        return Task.CompletedTask;
    }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 15;
        while (!condition() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(30);
        Require(condition(), "Power transition timeout");
    }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        if (editorSearchErrors > 0) Debug.LogWarning($"Office flow: {editorSearchErrors} Editor SearchDatabase exception(s), reported separately from gameplay.");
        Debug.Log("Office flow smoke: " + message);
        EditorApplication.Exit(code);
    }
}
