using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// EB-001 / EB-002 presentation: четири четими листа на страница, свободно преместване и изтриване с потвърждение.
public sealed class BoardPlayerNotesView : MonoBehaviour
{
    private const int NotesPerPage = 4;
    private static readonly Vector2[] SlotCenters =
    {
        new Vector2(.25f, .75f),
        new Vector2(.75f, .75f),
        new Vector2(.25f, .25f),
        new Vector2(.75f, .25f)
    };
    private static readonly Vector2 SlotGuideHalfExtent = new Vector2(.224f, .218f);

    private readonly List<PlayerNoteView> views = new List<PlayerNoteView>();
    private PlayerNotesSession session;
    private TMP_FontAsset font;
    private RectTransform surface;
    private GameObject emptyState;
    private GameObject confirmDialog;
    private PlayerNoteView pendingDelete;
    private TMP_Text pageLabel;
    private Button previous;
    private Button next;
    private int page;
    private bool hasExternalItems;

    internal RectTransform Surface => surface;
    internal RectTransform Frame => (RectTransform)transform;

    internal void SetHasExternalItems(bool value)
    {
        hasExternalItems = value;
        if (emptyState != null)
            emptyState.SetActive(views.Count == 0 && !hasExternalItems);
    }

    public static BoardPlayerNotesView Create(Transform parent, PlayerNotesSession session, TMP_FontAsset font)
    {
        RectTransform frame = PlayerNoteView.Rect(parent, "PlayerNotesBoard",
            new Vector2(.03f, .13f), new Vector2(.505f, .90f));
        Image frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.sprite = EvidenceBoardVisuals.WoodFrameSprite;
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;
        frameImage.raycastTarget = true;

        Shadow frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        frameShadow.effectDistance = new Vector2(0f, -6f);

        BoardPlayerNotesView view = frame.gameObject.AddComponent<BoardPlayerNotesView>();
        view.session = session;
        view.font = font;
        view.Build(frame);
        return view;
    }

    private void Build(RectTransform frame)
    {
        // Коркова повърхност с процедурна зърнеста текстура и вдлъбната сянка.
        RectTransform cork = PlayerNoteView.Rect(frame, "Cork", new Vector2(.014f, .014f), new Vector2(.986f, .986f));
        Image corkImage = cork.gameObject.AddComponent<Image>();
        corkImage.sprite = EvidenceBoardVisuals.CorkSprite;
        corkImage.type = Image.Type.Tiled;
        corkImage.pixelsPerUnitMultiplier = 1.15f;
        corkImage.color = Color.white;
        corkImage.raycastTarget = false;

        RectTransform vignette = PlayerNoteView.Rect(frame, "CorkVignette", new Vector2(.014f, .014f), new Vector2(.986f, .986f));
        Image vignetteImage = vignette.gameObject.AddComponent<Image>();
        vignetteImage.sprite = EvidenceBoardVisuals.CorkVignetteSprite;
        vignetteImage.color = Color.white;
        vignetteImage.raycastTarget = false;

        // Горна и долна дървена/тъмна лайсна за последователен детективски интерфейс.
        DecorativeBar(frame, "HeaderRail", new Vector2(.014f, .902f), new Vector2(.986f, .986f), EvidenceBoardVisuals.HeaderRailColor);
        DecorativeBar(frame, "HeaderAccent", new Vector2(.014f, .900f), new Vector2(.986f, .903f), EvidenceBoardVisuals.BrassHairline);
        DecorativeBar(frame, "FooterRail", new Vector2(.014f, .014f), new Vector2(.986f, .084f), EvidenceBoardVisuals.HeaderRailColor);
        DecorativeBar(frame, "FooterAccent", new Vector2(.014f, .083f), new Vector2(.986f, .086f), EvidenceBoardVisuals.BrassHairline);

        AddCornerBrackets(frame);
        AddRightPanelBridge(frame);

        CreateButton(frame, "NewPlayerNoteButton", "+ Нова бележка", new Vector2(.028f, .913f),
            new Vector2(.42f, .974f), AddNote, EvidenceBoardVisuals.ButtonStyle.PrimaryManila, 19f);

        RectTransform caption = PlayerNoteView.Rect(frame, "SessionLabel", new Vector2(.44f, .913f), new Vector2(.972f, .974f));
        TMP_Text sessionText = PlayerNoteView.Label(caption, "Text", "РАБОТНИ БЕЛЕЖКИ — само за тази сесия",
            font, 15f, TextAlignmentOptions.MidlineRight);
        sessionText.color = EvidenceBoardVisuals.MutedLabelText;

        surface = PlayerNoteView.Rect(frame, "PlayerNotesSurface", new Vector2(.022f, .092f), new Vector2(.978f, .894f));
        BuildSlotGuides(surface);
        BuildEmptyState(surface);

        previous = CreateButton(frame, "PreviousNotesPage", "Предишни", new Vector2(.028f, .024f),
            new Vector2(.29f, .075f), () => ShowPage(page - 1), EvidenceBoardVisuals.ButtonStyle.CorkControl, 18f);
        next = CreateButton(frame, "NextNotesPage", "Следващи", new Vector2(.71f, .024f),
            new Vector2(.972f, .075f), () => ShowPage(page + 1), EvidenceBoardVisuals.ButtonStyle.CorkControl, 18f);

        RectTransform counter = PlayerNoteView.Rect(frame, "PageLabel", new Vector2(.31f, .024f), new Vector2(.69f, .075f));
        pageLabel = PlayerNoteView.Label(counter, "Text", "", font, 18f, TextAlignmentOptions.Center);
        pageLabel.color = EvidenceBoardVisuals.ParchmentText;

        foreach (PlayerNote note in session.Notes)
            views.Add(PlayerNoteView.Create(surface, note, font, RequestDeleteNote));
        BuildDeleteConfirmDialog(frame);
        ShowPage(0);
    }

    private void BuildDeleteConfirmDialog(RectTransform frame)
    {
        RectTransform overlay = PlayerNoteView.Rect(frame, "DeleteNoteConfirmDialog",
            new Vector2(.014f, .014f), new Vector2(.986f, .986f));
        Image backdrop = overlay.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.06f, 0.045f, 0.035f, 0.72f);
        backdrop.raycastTarget = true;

        RectTransform card = PlayerNoteView.Rect(overlay, "DialogCard",
            new Vector2(.15f, .35f), new Vector2(.85f, .65f));
        Image cardBg = card.gameObject.AddComponent<Image>();
        cardBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        cardBg.type = Image.Type.Sliced;
        cardBg.color = new Color(0.15f, 0.12f, 0.10f, 0.98f);
        cardBg.raycastTarget = true;

        Outline cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = EvidenceBoardVisuals.BrassAccent;
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        cardShadow.effectDistance = new Vector2(0f, -6f);

        RectTransform promptBox = PlayerNoteView.Rect(card, "DeleteConfirmPrompt",
            new Vector2(.08f, .50f), new Vector2(.92f, .86f));
        TMP_Text prompt = PlayerNoteView.Label(promptBox, "Text", "Да изтрия ли тази бележка?",
            font, 20f, TextAlignmentOptions.Center);
        prompt.color = EvidenceBoardVisuals.ParchmentText;
        prompt.textWrappingMode = TextWrappingModes.Normal;

        CreateButton(card, "ConfirmDeleteNoteButton", "Изтрий", new Vector2(.10f, .14f),
            new Vector2(.46f, .40f), ConfirmDeleteNote, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger, 18f);
        CreateButton(card, "CancelDeleteNoteButton", "Отказ", new Vector2(.54f, .14f),
            new Vector2(.90f, .40f), CancelDeleteNote, EvidenceBoardVisuals.ButtonStyle.CorkControl, 18f);

        confirmDialog = overlay.gameObject;
        confirmDialog.SetActive(false);
    }

    private void BuildSlotGuides(RectTransform parent)
    {
        for (int i = 0; i < SlotCenters.Length; i++)
        {
            Vector2 center = SlotCenters[i];
            RectTransform guide = PlayerNoteView.Rect(parent, "SlotGuide" + i,
                center - SlotGuideHalfExtent, center + SlotGuideHalfExtent);
            Image image = guide.gameObject.AddComponent<Image>();
            image.sprite = EvidenceBoardVisuals.SlotGuideSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }

    private void BuildEmptyState(RectTransform parent)
    {
        RectTransform card = PlayerNoteView.Rect(parent, "EmptyBoardHint", new Vector2(.15f, .37f), new Vector2(.85f, .63f));
        Image bg = card.gameObject.AddComponent<Image>();
        bg.sprite = EvidenceBoardVisuals.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.11f, 0.08f, 0.055f, 0.62f);
        bg.raycastTarget = false;

        RectTransform textBox = PlayerNoteView.Rect(card, "TextBox", new Vector2(.06f, .10f), new Vector2(.94f, .90f));
        TMP_Text hint = PlayerNoteView.Label(textBox, "Text",
            "Няма закачени бележки.\nНатиснете „+ Нова бележка“, за да закачите лист на дъската.",
            font, 18f, TextAlignmentOptions.Center);
        hint.color = EvidenceBoardVisuals.ParchmentText;
        hint.lineSpacing = 6f;
        hint.textWrappingMode = TextWrappingModes.Normal;

        emptyState = card.gameObject;
    }

    private static void AddCornerBrackets(RectTransform frame)
    {
        Vector2 size = new Vector2(14f, 14f);
        PlaceCorner(frame, "CornerTL", new Vector2(0f, 1f), new Vector2(6f, -6f), size);
        PlaceCorner(frame, "CornerTR", new Vector2(1f, 1f), new Vector2(-6f, -6f), size);
        PlaceCorner(frame, "CornerBL", new Vector2(0f, 0f), new Vector2(6f, 6f), size);
        PlaceCorner(frame, "CornerBR", new Vector2(1f, 0f), new Vector2(-6f, 6f), size);
    }

    private static void PlaceCorner(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, anchor, anchor);
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        Image img = rect.gameObject.AddComponent<Image>();
        img.sprite = EvidenceBoardVisuals.ButtonSprite;
        img.type = Image.Type.Sliced;
        img.color = EvidenceBoardVisuals.BrassAccent;
        img.raycastTarget = false;
    }

    private static void AddRightPanelBridge(RectTransform frame)
    {
        // Визуална връзка (панти/свързващ канал) между лявата коркова дъска и десния панел с доказателства.
        DecorativeBar(frame, "BoardBridgeTrack", new Vector2(1.0f, .04f), new Vector2(1.032f, .96f),
            new Color(0.11f, 0.09f, 0.08f, 0.72f));
        DecorativeBar(frame, "BoardBridgeHingeTop", new Vector2(.994f, .73f), new Vector2(1.036f, .79f),
            EvidenceBoardVisuals.BrassAccent);
        DecorativeBar(frame, "BoardBridgeHingeBottom", new Vector2(.994f, .21f), new Vector2(1.036f, .27f),
            EvidenceBoardVisuals.BrassAccent);
    }

    private static void DecorativeBar(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        Image img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void AddNote()
    {
        CancelDeleteNote();
        int slot = session.Notes.Count % NotesPerPage;
        PlayerNote note = session.Create(slot % 2 == 0 ? .25f : .75f, slot < 2 ? .75f : .25f);
        PlayerNoteView view = PlayerNoteView.Create(surface, note, font, RequestDeleteNote);
        views.Add(view);
        ShowPage((views.Count - 1) / NotesPerPage);
        view.Focus();
    }

    private void RequestDeleteNote(PlayerNoteView target)
    {
        if (target == null || !views.Contains(target)) return;
        pendingDelete = target;
        if (confirmDialog != null)
        {
            confirmDialog.transform.SetAsLastSibling();
            confirmDialog.SetActive(true);
        }
    }

    private void ConfirmDeleteNote()
    {
        PlayerNoteView target = pendingDelete;
        CancelDeleteNote();
        if (target == null) return;

        session.Remove(target.NoteId);
        views.Remove(target);
        target.gameObject.SetActive(false);
        target.transform.SetParent(null, false);
        Destroy(target.gameObject);
        ShowPage(page);
    }

    private void CancelDeleteNote()
    {
        pendingDelete = null;
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
    }

    private void OnDisable() => CancelDeleteNote();

    public void SyncNotesFromUI()
    {
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i] != null)
                views[i].FlushPendingText();
        }
    }

    public void RebuildFromSession()
    {
        CancelDeleteNote();
        for (int i = 0; i < views.Count; i++)
        {
            PlayerNoteView view = views[i];
            if (view == null) continue;
            view.gameObject.SetActive(false);
            view.transform.SetParent(null, false);
            Destroy(view.gameObject);
        }
        views.Clear();

        if (session != null)
        {
            foreach (PlayerNote note in session.Notes)
                views.Add(PlayerNoteView.Create(surface, note, font, RequestDeleteNote));
        }

        ShowPage(0);
    }

    private void ShowPage(int requestedPage)
    {
        int count = Mathf.Max(1, (views.Count + NotesPerPage - 1) / NotesPerPage);
        page = Mathf.Clamp(requestedPage, 0, count - 1);
        for (int i = 0; i < views.Count; i++)
            views[i].gameObject.SetActive(i / NotesPerPage == page);
        if (emptyState != null)
            emptyState.SetActive(views.Count == 0 && !hasExternalItems);
        previous.interactable = page > 0;
        next.interactable = page < count - 1;
        pageLabel.text = $"Страница {page + 1} / {count}";
    }

    private Button CreateButton(Transform parent, string name, string title, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction click, EvidenceBoardVisuals.ButtonStyle style, float fontSize)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);
        TMP_Text label = PlayerNoteView.Label(rect, "Label", title, font, fontSize, TextAlignmentOptions.Center);
        EvidenceBoardVisuals.ApplyButtonStyle(button, image, label, style);
        return button;
    }
}
