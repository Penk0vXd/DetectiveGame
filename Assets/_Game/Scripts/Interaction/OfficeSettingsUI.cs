using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// PROTOTYPE CODE: малък Settings екран за единствената Office сцена.
public sealed class OfficeSettingsUI : MonoBehaviour
{
    private static readonly string[] Names = { "Напред", "Наляво", "Назад", "Надясно", "Взаимодействие" };
    private PlayerInputReader reader;
    private Action close;
    private OfficeSettingsData draft;
    private TMP_Text graphicsLabel;
    private TMP_Text qualityLabel;
    private TMP_Text volumeLabel;
    private TMP_Text status;
    private readonly TMP_Text[] bindingLabels = new TMP_Text[5];
    private int capturing = -1;

    public static OfficeSettingsUI Create(Transform parent, TMP_FontAsset font, PlayerInputReader reader, Action close)
    {
        RectTransform panel = Rect(parent, "SettingsPanel", Vector2.zero, new Vector2(780, 790));
        panel.gameObject.AddComponent<Image>().color = new Color(.075f, .09f, .1f, .98f);
        var ui = panel.gameObject.AddComponent<OfficeSettingsUI>();
        ui.reader = reader;
        ui.close = close;
        Label(panel, font, "НАСТРОЙКИ", new Vector2(0, 345), new Vector2(700, 55), 30);
        ui.graphicsLabel = Button(panel, font, new Vector2(0, 270), new Vector2(630, 52), () =>
        { ui.draft.fullscreen = !ui.draft.fullscreen; ui.Refresh(); });
        ui.qualityLabel = Button(panel, font, new Vector2(0, 207), new Vector2(630, 52), () =>
        { ui.draft.quality = (ui.draft.quality + 1) % QualitySettings.names.Length; ui.Refresh(); });
        Button(panel, font, new Vector2(-205, 140), new Vector2(210, 50), () =>
        { ui.draft.volume = Mathf.Max(0f, ui.draft.volume - .1f); ui.Refresh(); }).text = "Звук −";
        ui.volumeLabel = Label(panel, font, "", new Vector2(0, 140), new Vector2(180, 50), 21);
        Button(panel, font, new Vector2(205, 140), new Vector2(210, 50), () =>
        { ui.draft.volume = Mathf.Min(1f, ui.draft.volume + .1f); ui.Refresh(); }).text = "Звук +";
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            ui.bindingLabels[i] = Button(panel, font, new Vector2(0, 62 - i * 62),
                new Vector2(630, 52), () => ui.BeginCapture(index));
        }
        Button(panel, font, new Vector2(-210, -280), new Vector2(190, 52), ui.Apply).text = "Приложи";
        Button(panel, font, new Vector2(0, -280), new Vector2(190, 52), ui.Cancel).text = "Откажи";
        Button(panel, font, new Vector2(210, -280), new Vector2(190, 52), ui.ResetDefaults).text = "По подразбиране";
        ui.status = Label(panel, font, "", new Vector2(0, -347), new Vector2(700, 48), 18);
        panel.gameObject.SetActive(false);
        return ui;
    }

    public void Show()
    {
        draft = OfficeSettingsStore.Current;
        capturing = -1;
        status.text = "";
        Refresh();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        capturing = -1;
        draft = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (capturing < 0 || Keyboard.current == null) return;
        foreach (KeyControl key in Keyboard.current.allKeys)
        {
            if (!key.wasPressedThisFrame) continue;
            string path = "<Keyboard>/" + key.name;
            if (key == Keyboard.current.escapeKey) return; // PauseUIController обработва Esc.
            for (int i = 0; i < draft.bindings.Length; i++)
            {
                if (i != capturing && string.Equals(draft.bindings[i], path, StringComparison.OrdinalIgnoreCase))
                {
                    status.text = "Клавишът вече се използва.";
                    capturing = -1;
                    Refresh();
                    return;
                }
            }
            draft.bindings[capturing] = path;
            capturing = -1;
            status.text = "Клавишът е избран. Натиснете „Приложи“ за запис.";
            Refresh();
            return;
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused || capturing < 0) return;
        capturing = -1;
        if (status != null)
            status.text = "Изборът на клавиш е отменен след загуба на фокус.";
        Refresh();
    }

    private void BeginCapture(int index)
    {
        capturing = index;
        status.text = "Натиснете клавиш. „Откажи“ прекратява избора.";
        Refresh();
    }

    private void ResetDefaults()
    {
        draft = OfficeSettingsStore.Defaults();
        capturing = -1;
        status.text = "Стойностите по подразбиране ще се запишат с „Приложи“.";
        Refresh();
    }

    private void Apply()
    {
        capturing = -1;
        if (OfficeSettingsStore.TryApply(draft, reader, out string message))
        {
            Hide();
            close?.Invoke();
        }
        else status.text = message;
    }

    private void Cancel()
    {
        if (capturing >= 0)
        {
            capturing = -1;
            status.text = "Изборът на клавиш е отменен.";
            Refresh();
            return;
        }
        Hide();
        close?.Invoke();
    }

    private void Refresh()
    {
        if (draft == null) return;
        graphicsLabel.text = "Екран: " + (draft.fullscreen ? "Цял екран" : "Прозорец");
        qualityLabel.text = $"Качество: ниво {draft.quality + 1}/{QualitySettings.names.Length}";
        volumeLabel.text = $"{Mathf.RoundToInt(draft.volume * 100)}%";
        for (int i = 0; i < 5; i++)
            bindingLabels[i].text = Names[i] + ": " + (capturing == i ? "натиснете клавиш" : draft.bindings[i].Substring(11).ToUpperInvariant());
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text Label(Transform parent, TMP_FontAsset font, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(parent, "Label", position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.94f, .92f, .85f);
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private static TMP_Text Button(Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = Rect(parent, "Button", position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.15f, .19f, .19f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        return Label(rect, font, "", Vector2.zero, size, 21);
    }
}
