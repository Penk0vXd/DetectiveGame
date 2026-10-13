using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PROTOTYPE CODE: малък runtime прозорец върху съществуващия Computer desktop.
public sealed class NpcChatUI : MonoBehaviour
{
    private GameObject window;
    private TMP_InputField input;
    private TMP_Text transcript;
    private TMP_Text status;
    private Button send;
    private ScrollRect scroll;
    private ConversationSession session;
    private ComputerUIController computer;
    private GameObject launcher;
    private TMP_FontAsset font;

    public static NpcChatUI Create(ComputerUIController owner, GameObject desktop)
    {
        var ui = desktop.AddComponent<NpcChatUI>();
        ui.computer = owner;
        var existingLabel = desktop.GetComponentInChildren<TMP_Text>(true);
        ui.font = existingLabel != null ? existingLabel.font : TMP_Settings.defaultFontAsset;
        ui.Build();
        return ui;
    }

    private void Build()
    {
        launcher = MakeButton(transform, "NpcChatLauncher", "Тестов разговор", .72f, .92f, .96f, .98f, Open).gameObject;
        var root = Rect(transform, "NpcChatWindow", .08f, .12f, .94f, .9f);
        window = root.gameObject;
        root.gameObject.AddComponent<Image>().color = new Color(.16f, .19f, .20f, 1f);
        Label(root, "Title", "Тестов събеседник — неканоничен разговор", .03f, .9f, .79f, .98f);
        MakeButton(root, "Close", "Затвори", .80f, .91f, .97f, .98f, Close);

        var viewport = Rect(root, "TranscriptViewport", .03f, .25f, .97f, .88f);
        viewport.gameObject.AddComponent<Image>().color = new Color(.09f, .11f, .12f);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var content = Rect(viewport, "Transcript", 0, 1, 1, 1);
        content.pivot = new Vector2(.5f, 1);
        transcript = content.gameObject.AddComponent<TextMeshProUGUI>();
        Configure(transcript);
        transcript.margin = new Vector4(12, 10, 12, 10);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        var inputRoot = Rect(root, "Input", .03f, .09f, .76f, .23f);
        var background = inputRoot.gameObject.AddComponent<Image>();
        background.color = new Color(.94f, .94f, .9f);
        var inputViewport = Rect(inputRoot, "TextViewport", .02f, .05f, .98f, .95f);
        inputViewport.gameObject.AddComponent<RectMask2D>();
        var inputText = Label(inputViewport, "Text", "", 0, 0, 1, 1);
        inputText.color = Color.black;
        var placeholder = Label(inputViewport, "Placeholder", "Напишете въпрос…", 0, 0, 1, 1);
        placeholder.color = Color.gray;
        input = inputRoot.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.textViewport = inputViewport;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.fontAsset = font;
        input.characterLimit = NpcPromptBuilder.MaxInputCharacters;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        send = MakeButton(root, "Send", "Изпрати", .78f, .09f, .97f, .23f, Send);
        status = Label(root, "Status", "", .03f, .01f, .97f, .08f);
        status.fontSize = 17;
        window.SetActive(false);
    }

    public void Open()
    {
        if (!computer.IsOpen || !gameObject.activeInHierarchy) return;
        computer.CloseAllDesktopApplications();
        window.SetActive(true);
        window.transform.SetAsLastSibling();
        try
        {
            session = new ConversationSession(Resources.Load<NpcProfile>("NpcPrototype/TestNpc"),
                ConversationChannel.Chat, new LocalNpcModelAdapter());
            transcript.text = "Нов тестов разговор. Репликите са твърдения на събеседника.";
            status.text = "";
            input.text = "";
            send.interactable = true;
        }
        catch (System.Exception)
        {
            status.text = "Липсва валидно досие на тестовия събеседник.";
            send.interactable = false;
        }
    }

    private async void Send()
    {
        var active = session;
        if (active == null || active.IsBusy) return;
        send.interactable = false;
        input.interactable = false;
        status.text = "Изчакване на отговор…";
        var task = active.SendAsync(new ConversationRequest(active.State.PersonId, ConversationChannel.Chat, input.text));
        RefreshTranscript(active);
        var reply = await task;
        // Затворен/нов разговор не приема закъснял UI callback.
        if (this == null || session != active || !window.activeInHierarchy) return;
        RefreshTranscript(active);
        status.text = reply.Success ? "" : reply.Error;
        if (reply.Success) input.text = "";
        send.interactable = true;
        input.interactable = true;
    }

    private void RefreshTranscript(ConversationSession active)
    {
        var text = new StringBuilder();
        foreach (var turn in active.State.Transcript)
        {
            text.Append(turn.SpeakerId == "player" ? "Вие" : active.DisplayName).Append(": ").AppendLine(turn.Text);
            if (turn.Error != null) text.AppendLine("[" + turn.Error + "]");
            text.AppendLine();
        }
        transcript.text = text.ToString();
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0;
    }

    public void Close()
    {
        session?.Dispose();
        session = null;
        if (window != null) window.SetActive(false);
        if (input != null) input.interactable = true;
    }
    private void OnDisable() => Close();
    private void OnDestroy()
    {
        Close();
        if (window != null) Destroy(window);
        if (launcher != null) Destroy(launcher);
    }

    private static RectTransform Rect(Transform parent, string name, float left, float bottom, float right, float top)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    private void Configure(TMP_Text text)
    {
        text.font = font;
        text.fontSize = 22;
        text.color = Color.white;
        text.richText = false;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
    }
    private TMP_Text Label(Transform parent, string name, string value, float left, float bottom, float right, float top)
    {
        var text = Rect(parent, name, left, bottom, right, top).gameObject.AddComponent<TextMeshProUGUI>();
        Configure(text);
        text.text = value;
        return text;
    }
    private Button MakeButton(Transform parent, string name, string value, float left, float bottom, float right, float top,
        UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(parent, name, left, bottom, right, top);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.25f, .3f, .33f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var label = Label(rect, "Label", value, .02f, .02f, .98f, .98f);
        label.alignment = TextAlignmentOptions.Center;
        button.onClick.AddListener(action);
        return button;
    }
}
