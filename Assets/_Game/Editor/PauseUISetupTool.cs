using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// PROTOTYPE CODE: еднократен, повторяем scene setup за първия pause екран.
public static class PauseUISetupTool
{
    private const string ScenePath = "Assets/_Game/Scenes/DetectiveOffice.unity";
    private const string FontPath = "Assets/_Game/UI/Fonts/PoliceTerminal SDF.asset";

    [MenuItem("Tools/DetectiveGame/Setup Pause UI")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Pause setup не се изпълнява в Play Mode.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PlayerInteraction player = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerInteraction>(true)).Single();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            throw new InvalidOperationException("Липсва PoliceTerminal SDF font.");

        GameObject canvasObject = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "PauseCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("PauseCanvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Pause Canvas");
        }
        canvasObject.SetActive(true);
        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        GetOrAdd<GraphicRaycaster>(canvasObject);
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform blocker = Child(canvasObject.transform, "Blocker", Vector2.zero, Vector2.zero);
        blocker.anchorMin = Vector2.zero;
        blocker.anchorMax = Vector2.one;
        blocker.offsetMin = blocker.offsetMax = Vector2.zero;
        Image blockerImage = GetOrAdd<Image>(blocker.gameObject);
        blockerImage.color = new Color(0.015f, 0.02f, 0.025f, 0.92f);
        blockerImage.raycastTarget = true;
        blocker.SetAsFirstSibling();

        RectTransform main = Child(canvasObject.transform, "MainPanel", Vector2.zero, new Vector2(560, 420));
        SetPanel(main);
        SetText(Child(main, "Title", new Vector2(0, 130), new Vector2(500, 65)), font, "ПАУЗА", 36);
        Button resume = SetButton(Child(main, "ResumeButton", new Vector2(0, 20), new Vector2(390, 64)), font, "Продължи");
        Button exit = SetButton(Child(main, "ExitButton", new Vector2(0, -76), new Vector2(390, 64)), font, "Изход от играта");

        RectTransform confirm = Child(canvasObject.transform, "ExitConfirmationPanel", Vector2.zero, new Vector2(560, 350));
        SetPanel(confirm);
        SetText(Child(confirm, "Title", new Vector2(0, 90), new Vector2(500, 65)), font, "ИЗХОД ОТ ИГРАТА?", 29);
        Button cancel = SetButton(Child(confirm, "CancelButton", new Vector2(-105, -70), new Vector2(190, 64)), font, "Откажи");
        Button confirmExit = SetButton(Child(confirm, "ConfirmButton", new Vector2(105, -70), new Vector2(190, 64)), font, "Излез");
        confirm.gameObject.SetActive(false);
        main.gameObject.SetActive(true);

        PauseUIController controller = GetOrAdd<PauseUIController>(player.gameObject);
        SerializedObject data = new SerializedObject(controller);
        data.FindProperty("inputReader").objectReferenceValue = player.GetComponent<PlayerInputReader>();
        data.FindProperty("playerInteraction").objectReferenceValue = player;
        data.FindProperty("pauseRoot").objectReferenceValue = canvasObject;
        data.FindProperty("mainPanel").objectReferenceValue = main.gameObject;
        data.FindProperty("exitConfirmationPanel").objectReferenceValue = confirm.gameObject;
        data.FindProperty("resumeButton").objectReferenceValue = resume;
        data.FindProperty("exitButton").objectReferenceValue = exit;
        data.FindProperty("cancelExitButton").objectReferenceValue = cancel;
        data.FindProperty("confirmExitButton").objectReferenceValue = confirmExit;
        data.ApplyModifiedProperties();
        canvasObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Pause сцената не беше запазена.");
        Debug.Log("Pause UI setup: PASS — Main, exit confirmation и references са записани.");
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static RectTransform Child(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        if (existing == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create Pause element");
            rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
        }
        else
            rect = (RectTransform)existing;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void SetPanel(RectTransform rect)
    {
        Image image = GetOrAdd<Image>(rect.gameObject);
        image.color = new Color(0.075f, 0.09f, 0.10f, 0.98f);
        image.raycastTarget = true;
    }

    private static void SetText(RectTransform rect, TMP_FontAsset font, string value, float size)
    {
        TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.94f, 0.92f, 0.85f);
        label.richText = false;
        label.raycastTarget = false;
        label.text = value;
    }

    private static Button SetButton(RectTransform rect, TMP_FontAsset font, string value)
    {
        Image background = GetOrAdd<Image>(rect.gameObject);
        background.color = new Color(0.15f, 0.19f, 0.19f);
        background.raycastTarget = true;
        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = background;
        RectTransform text = Child(rect, "Label", Vector2.zero, rect.sizeDelta);
        SetText(text, font, value, 24);
        return button;
    }
}
