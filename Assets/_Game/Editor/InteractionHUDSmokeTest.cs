using System;
using System.Reflection;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

// Контролиран Play Mode smoke test; не замества визуален или човешки playtest.
public static class InteractionHUDSmokeTest
{
    private const string PendingKey = "DetectiveGame.HUDSmoke.Pending";
    private static double started;

    public static void RunWithVisuals()
    {
        SessionState.SetBool("DetectiveGame.HUDSmoke.Visuals", true);
        Run();
    }

    public static void SetupAndRunWithVisuals()
    {
        InteractionHUDSetupTool.Setup();
        RunWithVisuals();
    }

    public static void SetupPauseAndRun()
    {
        PauseUISetupTool.Setup();
        Run();
    }

    public static void Run()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Изпълнявай smoke test в отделен batch Editor процес.");
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/DetectiveOffice.unity");
        SessionState.SetBool(PendingKey, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void ResumeAfterReload()
    {
        if (SessionState.GetBool(PendingKey, false)) Subscribe();
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
            if (EditorApplication.timeSinceStartup - started > 90) Finish(1, "Play Mode timeout");
            return;
        }
        EditorApplication.update -= Tick;
        try
        {
            CheckScene();
            Finish(0, "PASS: targets, input, mode hiding, re-entry, occlusion, disabled/destroyed, binding override");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1, "FAIL: " + exception.Message);
        }
    }

    private static void CheckScene()
    {
        PlayerInteraction player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
        InteractionHUD hud = UnityEngine.Object.FindAnyObjectByType<InteractionHUD>();
        Require(player != null && hud != null, "Липсва Player/HUD в сцената.");
        Camera camera = player.GetComponentInChildren<Camera>();
        TMP_Text prompt = Field<TMP_Text>(hud, "promptText");
        Require(Field<TMP_Text>(hud, "promptShadow").transform.GetSiblingIndex() < prompt.transform.GetSiblingIndex(),
            "Сянката се рисува пред основния текст.");
        GameObject indicator = Field<GameObject>(hud, "indicatorRoot");
        PlayerInputReader reader = player.GetComponent<PlayerInputReader>();
        // Изолираният batch тест няма фокусиран Game View или физическа клавиатура.
        InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        player.GetComponent<FirstPersonController>().enabled = false;
        camera.transform.rotation = Quaternion.identity;

        MonoBehaviour[] targets = {
            UnityEngine.Object.FindAnyObjectByType<ComputerInteractable>(),
            UnityEngine.Object.FindAnyObjectByType<PhoneInteractable>(),
            UnityEngine.Object.FindAnyObjectByType<EvidenceBoardInteractable>(),
            UnityEngine.Object.FindAnyObjectByType<DocumentInteractable>()
        };
        PauseUIController pause = UnityEngine.Object.FindAnyObjectByType<PauseUIController>();
        Require(pause != null && pause.enabled && Field<GameObject>(pause, "pauseRoot") != null,
            "Pause controller/runtime view не се създаде от Office сцената.");
        if (pause != null)
        {
            InputAction pauseAction = Field<DetectiveGameInput>(reader, "input").asset.FindAction("Player/Pause", false);
            Require(pauseAction != null && pauseAction.bindings.Count == 1
                && pauseAction.bindings[0].effectivePath == "<Keyboard>/escape",
                "Pause action или генерираният input wrapper не съответства на Esc binding.");
        }
        float originalTimeScale = Time.timeScale;
        Require(player.ModeController.TryPause(), "World pause не се отвори.");
        Pump(player, hud, keyboard);
        Require(player.ModeController.IsPaused && Time.timeScale == 0f && !indicator.activeSelf
            && !player.ModeController.TryEnter(targets[0], InteractionModeController.Mode.FocusedUI),
            "Pause не блокира World interaction.");
        Require(player.ModeController.TryResume() && Mathf.Approximately(Time.timeScale, originalTimeScale),
            "World resume не възстанови времето.");
        Pump(player, hud, keyboard);
        if (pause != null)
        {
            GameObject pauseRoot = Field<GameObject>(pause, "pauseRoot");
            Require(!pauseRoot.activeSelf, "Pause root е видим преди Esc.");
            PumpWithPause(player, hud, pause, keyboard, Key.Escape);
            Require(player.ModeController.IsPaused && pauseRoot.activeSelf && Time.timeScale == 0f,
                "Esc не отвори pause от World.");
            PumpWithPause(player, hud, pause, keyboard);
            Field<Button>(pause, "exitButton").onClick.Invoke();
            Require(Field<GameObject>(pause, "exitConfirmationPanel").activeSelf,
                "Липсва потвърждение за изход.");
            PumpWithPause(player, hud, pause, keyboard, Key.Escape);
            Require(!player.ModeController.IsPaused && !pauseRoot.activeSelf
                && Mathf.Approximately(Time.timeScale, originalTimeScale),
                "Esc от потвърждение не продължи играта.");
            PumpWithPause(player, hud, pause, keyboard);
        }
        CheckOriginalSceneTargets(player, hud, camera, keyboard, targets);
        MonoBehaviour previousTarget = null;
        foreach (MonoBehaviour target in targets)
        {
            Require(target != null, "Липсва office target.");
            Vector3 originalPosition = target.transform.position;
            target.transform.position = new Vector3(1000, 1000, 1000);
            Physics.SyncTransforms();
            Collider collider = target.GetComponent<Collider>();
            camera.transform.position = collider.bounds.center - Vector3.forward * 2;
            Pump(player, hud, keyboard);
            Require(ReferenceEquals(player.CurrentTarget, target), "Грешен raycast target: " + target.name);
            Require(prompt.gameObject.activeSelf && prompt.text.Contains(((IInteractable)target).DisplayName), "Липсва име.");
            Require(prompt.text.Contains("[" + reader.GetInteractBindingLabel() + "]"), "Грешен binding.");

            if (pause != null && ReferenceEquals(target, targets[0]))
            {
                PumpWithPause(player, hud, pause, keyboard, Key.Escape, Key.E);
                Require(player.ModeController.IsPaused && player.ModeController.ActiveOwner == null,
                    "Едновременни Esc/E отвориха инструмента вместо pause.");
                PumpWithPause(player, hud, pause, keyboard);
                PumpWithPause(player, hud, pause, keyboard, Key.Escape);
                PumpWithPause(player, hud, pause, keyboard);
                Require(!player.ModeController.IsPaused && player.ModeController.ActiveOwner == null,
                    "Esc/E не възстановиха World режима.");
            }

            target.enabled = false;
            Pump(player, hud, keyboard);
            Require(player.CurrentTarget == null && !prompt.gameObject.activeSelf, "Disabled target остава видим.");
            target.enabled = true;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = camera.transform.position + Vector3.forward;
            wall.transform.localScale = new Vector3(2, 2, 0.1f);
            Physics.SyncTransforms();
            Pump(player, hud, keyboard);
            Require(player.CurrentTarget == null && !prompt.gameObject.activeSelf, "Prompt през преграда.");
            UnityEngine.Object.DestroyImmediate(wall);
            Physics.SyncTransforms();

            Pump(player, hud, keyboard, Key.E);
            Require(!indicator.activeSelf, "HUD остава във focused mode: " + target.name);
            Require(ReferenceEquals(player.ModeController.ActiveOwner, target)
                && player.ModeController.CurrentMode != InteractionModeController.Mode.World,
                "Общият mode controller не пази активния инструмент: " + target.name);
            Require(player.ModeController.TryPause(), "Pause върху инструмент не се отвори.");
            Pump(player, hud, keyboard);
            Require(player.ModeController.IsPaused && Time.timeScale == 0f
                && ReferenceEquals(player.ModeController.ActiveOwner, target)
                && !player.IsWorldInteractionActive,
                "Pause изгуби активния инструмент или пропусна input.");
            Require(player.ModeController.TryResume() && Mathf.Approximately(Time.timeScale, originalTimeScale)
                && ReferenceEquals(player.ModeController.ActiveOwner, target),
                "Resume не върна същия инструмент и времето.");
            if (pause != null)
            {
                PumpWithPause(player, hud, pause, keyboard, Key.Escape);
                Require(player.ModeController.IsPaused
                    && ReferenceEquals(player.ModeController.ActiveOwner, target),
                    "Esc изгуби активния инструмент: " + target.name);
                PumpWithPause(player, hud, pause, keyboard);
                PumpWithPause(player, hud, pause, keyboard, Key.Escape);
                Require(!player.ModeController.IsPaused && ReferenceEquals(player.ModeController.ActiveOwner, target)
                    && Mathf.Approximately(Time.timeScale, originalTimeScale),
                    "Esc не възстанови инструмента: " + target.name);
                PumpWithPause(player, hud, pause, keyboard);
            }
            if (previousTarget != null)
                Require(!player.ModeController.TryExit(previousTarget)
                    && ReferenceEquals(player.ModeController.ActiveOwner, target),
                    "Закъснял exit освободи чужд инструмент.");
            Pump(player, hud, keyboard);
            if (target is ComputerInteractable)
                UnityEngine.Object.FindAnyObjectByType<ComputerUIController>().ExitComputer();
            else if (target is PhoneInteractable)
                UnityEngine.Object.FindAnyObjectByType<PhoneUIController>().ExitPhone();
            else if (target is EvidenceBoardInteractable)
                UnityEngine.Object.FindAnyObjectByType<EvidenceBoardUIController>().ExitBoard();
            else
                Pump(player, hud, keyboard, Key.E);
            Pump(player, hud, keyboard);
            Require(indicator.activeSelf && player.IsWorldInteractionActive, "World не се възстанови.");
            Require(player.ModeController.CurrentMode == InteractionModeController.Mode.World
                && player.ModeController.ActiveOwner == null,
                "Owner остава след exit.");

            // Проверява re-entry и втори exit през същия път.
            Pump(player, hud, keyboard, Key.E);
            Require(!indicator.activeSelf, "Повторното влизане не скри HUD.");
            Pump(player, hud, keyboard);
            if (target is ComputerInteractable)
            {
                ComputerUIController ui = UnityEngine.Object.FindAnyObjectByType<ComputerUIController>();
                Field<GameObject>(ui, "computerRoot").SetActive(false);
                Invoke(ui, "Update");
            }
            else if (target is PhoneInteractable)
            {
                PhoneUIController ui = UnityEngine.Object.FindAnyObjectByType<PhoneUIController>();
                Field<GameObject>(ui, "phoneRoot").SetActive(false);
                Invoke(ui, "Update");
            }
            else if (target is EvidenceBoardInteractable)
            {
                EvidenceBoardUIController ui = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardUIController>();
                Field<GameObject>(ui, "boardRoot").SetActive(false);
                Invoke(ui, "Update");
            }
            else if (target is DocumentInteractable document)
            {
                // Disable по време на inspection трябва да освободи owner и stale reference.
                document.enabled = false;
                Pump(player, hud, keyboard);
                Require(!document.IsInspecting && player.IsWorldInteractionActive
                    && player.ModeController.CurrentMode == InteractionModeController.Mode.World,
                    "Disabled document задържа inspection режима.");
                document.enabled = true;
            }
            Pump(player, hud, keyboard);
            Require(player.ModeController.CurrentMode == InteractionModeController.Mode.World
                && player.ModeController.ActiveOwner == null && player.IsWorldInteractionActive,
                "Външно скрит UI root или disabled document задържа режима: " + target.name);
            camera.transform.position -= Vector3.forward * 10;
            Pump(player, hud, keyboard);
            Require(player.CurrentTarget == null && indicator.activeSelf && !prompt.gameObject.activeSelf, "Prompt извън обсег.");
            target.transform.position = originalPosition;
            previousTarget = target;
        }

        GameObject disposable = GameObject.CreatePrimitive(PrimitiveType.Cube);
        disposable.transform.position = new Vector3(1000, 1000, 1000);
        disposable.AddComponent<TestInteractable>();
        camera.transform.position = disposable.transform.position - Vector3.forward * 2;
        Physics.SyncTransforms();
        Pump(player, hud, keyboard);
        Require(player.CurrentTarget != null, "Липсва disposable target.");

        DetectiveGameInput input = Field<DetectiveGameInput>(reader, "input");
        input.Player.Interact.ApplyBindingOverride(0, "<Keyboard>/f");
        Pump(player, hud, keyboard);
        Require(prompt.text.Contains("[F]"), "Binding override не се показва в HUD.");
        Quaternion before = disposable.transform.rotation;
        Pump(player, hud, keyboard, Key.E);
        Require(disposable.transform.rotation == before, "Старият E binding още действа.");
        Pump(player, hud, keyboard);
        Pump(player, hud, keyboard, Key.F);
        Require(disposable.transform.rotation != before, "Показаният F не изпълнява действието.");
        input.Player.Interact.RemoveAllBindingOverrides();
        Pump(player, hud, keyboard);
        UnityEngine.Object.DestroyImmediate(disposable);
        Require(player.CurrentTarget == null, "Destroyed reference остава достъпен.");
        Pump(player, hud, keyboard);
        Require(!prompt.gameObject.activeSelf, "Destroyed prompt остава видим.");

        if (SessionState.GetBool("DetectiveGame.HUDSmoke.Visuals", false))
        {
            // Render на реалния HUD върху контролирани фонове; не е office screenshot.
            prompt.text = "Компютър\n[" + reader.GetInteractBindingLabel() + "] Използвай";
            prompt.gameObject.SetActive(true);
            TMP_Text shadow = Field<TMP_Text>(hud, "promptShadow");
            shadow.text = prompt.text;
            shadow.gameObject.SetActive(true);
            RenderVisualChecks(hud, prompt);
        }
    }

    private static void CheckOriginalSceneTargets(PlayerInteraction player, InteractionHUD hud, Camera camera,
        Keyboard keyboard, MonoBehaviour[] targets)
    {
        TMP_Text prompt = Field<TMP_Text>(hud, "promptText");
        foreach (MonoBehaviour target in targets)
        {
            Require(target != null, "Липсва office target на оригиналното му място.");
            Collider collider = target.GetComponent<Collider>();
            Require(collider != null && ((IInteractable)target).CanInteract,
                "Office target няма collider или не е достъпен: " + target.name);

            Vector3 center = collider.bounds.center;
            bool found = false;
            // Пробва реалните colliders и препятствия в сцената, без да мести инструментите.
            foreach (float distance in new[] { 1.2f, 1.8f, 2.4f })
            {
                for (int i = 0; i < 16 && !found; i++)
                {
                    float angle = i * Mathf.PI * 2f / 16f;
                    Vector3 viewpoint = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                    viewpoint.y = Mathf.Clamp(center.y + 0.35f, 1.2f, 1.8f);
                    if (Physics.CheckSphere(viewpoint, 0.12f, ~0, QueryTriggerInteraction.Ignore))
                        continue;

                    camera.transform.position = viewpoint;
                    camera.transform.rotation = Quaternion.LookRotation(center - viewpoint);
                    Physics.SyncTransforms();
                    Pump(player, hud, keyboard);
                    if (!ReferenceEquals(player.CurrentTarget, target))
                        continue;

                    Require(prompt.gameObject.activeSelf && prompt.text.Contains(((IInteractable)target).DisplayName),
                        "Office target е видим, но prompt липсва: " + target.name);
                    Debug.Log($"Office HUD original placement: {target.name}, camera={viewpoint}, distance={distance:F1}");
                    found = true;
                }
            }
            Require(found, "Не е намерена чиста видимост към office target на оригиналното му място: " + target.name);
        }
    }

    private static void RenderVisualChecks(InteractionHUD hud, TMP_Text prompt)
    {
        string output = "docs/prototype/evidence/hud";
        Directory.CreateDirectory(output);
        Canvas canvas = hud.GetComponent<Canvas>();
        hud.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
        foreach (Transform child in hud.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
        Camera preview = new GameObject("HUDPreviewCamera").AddComponent<Camera>();
        preview.enabled = false;
        preview.clearFlags = CameraClearFlags.SolidColor;
        preview.cullingMask = 1 << 5;
        preview.transform.position = new Vector3(2000, 2000, 2000);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = preview;
        canvas.planeDistance = 1;
        foreach (int width in new[] { 1920, 1280 })
        foreach (bool light in new[] { false, true })
        {
            int height = width * 9 / 16;
            RenderTexture texture = new RenderTexture(width, height, 24);
            texture.Create();
            preview.targetTexture = texture;
            preview.backgroundColor = light ? Color.white : new Color(0.025f, 0.03f, 0.04f);
            canvas.scaleFactor = width / 1920f;
            Canvas.ForceUpdateCanvases();
            prompt.ForceMeshUpdate();
            Field<TMP_Text>(hud, "promptShadow").ForceMeshUpdate();
            preview.Render();
            RenderTexture.active = texture;
            Texture2D pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes($"{output}/hud-{width}-{(light ? "light" : "dark")}.png", pixels.EncodeToPNG());
            RenderTexture.active = null;
            preview.targetTexture = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
        UnityEngine.Object.DestroyImmediate(preview.gameObject);
        Debug.Log("HUD visual renders: " + output);
    }

    private static void Pump(PlayerInteraction player, InteractionHUD hud, Keyboard keyboard, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
        if (keys.Length > 0)
            Debug.Log($"HUD test input: {keys[0]}, pressed={keyboard[keys[0]].isPressed}, interact={player.GetComponent<PlayerInputReader>().ReadInteract()}, target={player.CurrentTarget?.DisplayName}");
        Invoke(player, "Update");
        Invoke(hud, "LateUpdate");
    }

    private static void PumpWithPause(PlayerInteraction player, InteractionHUD hud, PauseUIController pause,
        Keyboard keyboard, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
        Invoke(pause, "Update");
        Invoke(player, "Update");
        Invoke(hud, "LateUpdate");
    }

    private static void Invoke(object target, string name) => target.GetType()
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static T Field<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Finish(int code, string message)
    {
        SessionState.SetBool(PendingKey, false);
        SessionState.SetBool("DetectiveGame.HUDSmoke.Visuals", false);
        EditorApplication.update -= Tick;
        Debug.Log("Interaction HUD smoke: " + message);
        // Никога не записва runtime преместванията в scene asset-а.
        EditorApplication.Exit(code);
    }
}
