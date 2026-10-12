using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InteractionHUDSetupTool
{
    private const string ScenePath = "Assets/_Game/Scenes/DetectiveOffice.unity";
    private const string FontPath = "Assets/_Game/UI/Fonts/PoliceTerminal SDF.asset";

    [MenuItem("Tools/DetectiveGame/Setup Interaction HUD")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("HUD setup не се изпълнява в Play Mode.");

        // Не затваря мълчаливо незапазена сцена при използване от Editor менюто.
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
        EnsureReadableCyrillicAtlas();

        // Добавя само собствена HUD йерархия; не rebuild-ва Computer UI.
        GameObject rootObject = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "InteractionHUDCanvas");
        if (rootObject == null)
        {
            rootObject = new GameObject("InteractionHUDCanvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Interaction HUD");
        }

        Canvas canvas = GetOrAdd<Canvas>(rootObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(rootObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        CanvasGroup group = GetOrAdd<CanvasGroup>(rootObject);
        group.interactable = false;
        group.blocksRaycasts = false;

        RectTransform indicator = Child(rootObject.transform, "Indicator", Vector2.zero, new Vector2(440, 110));
        RectTransform dot = Child(indicator, "Dot", Vector2.zero, new Vector2(4, 4));
        Image dotImage = GetOrAdd<Image>(dot.gameObject);
        dotImage.color = new Color(0.94f, 0.92f, 0.85f);
        dotImage.raycastTarget = false;
        Shadow dotShadow = GetOrAdd<Shadow>(dot.gameObject);
        dotShadow.effectColor = new Color(0, 0, 0, 0.85f);
        dotShadow.effectDistance = new Vector2(1, -1);

        RectTransform prompt = Child(indicator, "Prompt", new Vector2(0, -48), new Vector2(440, 64));
        TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(prompt.gameObject);
        label.font = font;
        label.fontSize = 20;
        label.alignment = TextAlignmentOptions.Center;
        label.color = dotImage.color;
        label.richText = false;
        label.raycastTarget = false;
        label.text = string.Empty;
        Shadow oldShadow = prompt.GetComponent<Shadow>();
        if (oldShadow != null) Undo.DestroyObjectImmediate(oldShadow);
        RectTransform shadowRect = Child(indicator, "PromptShadow", new Vector2(1.5f, -49.5f), new Vector2(440, 64));
        shadowRect.SetSiblingIndex(1);
        prompt.SetAsLastSibling();
        TextMeshProUGUI shadowLabel = GetOrAdd<TextMeshProUGUI>(shadowRect.gameObject);
        shadowLabel.font = font;
        shadowLabel.fontSize = label.fontSize;
        shadowLabel.alignment = label.alignment;
        shadowLabel.color = new Color(0, 0, 0, 0.95f);
        // По-плътна тъмна граница пази тънките glyphs върху бели повърхности.
        const string shadowMaterialPath = "Assets/_Game/UI/Fonts/InteractionHUDShadow.mat";
        Material shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(shadowMaterialPath);
        if (shadowMaterial == null)
        {
            shadowMaterial = new Material(font.material);
            AssetDatabase.CreateAsset(shadowMaterial, shadowMaterialPath);
        }
        shadowMaterial.SetFloat("_OutlineWidth", 0.4f);
        shadowMaterial.SetFloat("_FaceDilate", 0.3f);
        shadowMaterial.SetColor("_OutlineColor", Color.black);
        shadowMaterial.SetColor("_FaceColor", Color.black);
        EditorUtility.SetDirty(shadowMaterial);
        shadowLabel.fontSharedMaterial = shadowMaterial;
        shadowLabel.richText = false;
        shadowLabel.raycastTarget = false;
        shadowLabel.text = string.Empty;

        InteractionHUD hud = GetOrAdd<InteractionHUD>(rootObject);
        SerializedObject serialized = new SerializedObject(hud);
        serialized.FindProperty("playerInteraction").objectReferenceValue = player;
        serialized.FindProperty("inputReader").objectReferenceValue = player.GetComponent<PlayerInputReader>();
        serialized.FindProperty("indicatorRoot").objectReferenceValue = indicator.gameObject;
        serialized.FindProperty("promptText").objectReferenceValue = label;
        serialized.FindProperty("promptShadow").objectReferenceValue = shadowLabel;
        serialized.ApplyModifiedProperties();
        indicator.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("HUD сцената не беше запазена.");
        AssetDatabase.SaveAssets();
        Debug.Log("Interaction HUD setup: PASS — references, font и сцена са записани.");
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void EnsureReadableCyrillicAtlas()
    {
        const string path = "Assets/_Game/UI/Fonts/PoliceTerminal Cyrillic Fallback SDF.asset";
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (fallback == null) throw new InvalidOperationException("Липсва Cyrillic fallback.");
        bool changed = false;
        foreach (Texture2D atlas in fallback.atlasTextures)
        {
            if (atlas == null || atlas.isReadable) continue;
            // Копираният static atlas е бил направен Dynamic без Read/Write.
            // Запазва съществуващите glyphs, material, GUID и всички references.
            SerializedObject textureData = new SerializedObject(atlas);
            textureData.FindProperty("m_IsReadable").boolValue = true;
            textureData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(atlas);
            changed = true;
        }
        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        const string bulgarian = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЬЮЯабвгдежзийклмнопрстуфхцчшщъьюяѝ";
        // Първият atlas е почти запълнен от оригиналния static font.
        fallback.isMultiAtlasTexturesEnabled = true;
        string toAdd = new string(bulgarian.Where(letter => !fallback.HasCharacter(letter)).ToArray());
        // TMP връща false и когато всички поискани glyphs вече съществуват.
        if (toAdd.Length > 0 && !fallback.TryAddCharacters(toAdd, out string missing))
            throw new InvalidOperationException("Неуспешна подготовка на кирилица: " + missing);
        EditorUtility.SetDirty(fallback);
    }

    private static RectTransform Child(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        if (existing == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create HUD element");
            rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
        }
        else
        {
            rect = (RectTransform)existing;
        }
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
