using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// EB-001 presentation: четири четими листа на страница, без местене/изтриване.
public sealed class BoardPlayerNotesView : MonoBehaviour
{
    private const int NotesPerPage = 4;
    private readonly List<PlayerNoteView> views = new List<PlayerNoteView>();
    private PlayerNotesSession session;
    private TMP_FontAsset font;
    private RectTransform surface;
    private TMP_Text pageLabel;
    private Button previous;
    private Button next;
    private int page;

    public static BoardPlayerNotesView Create(Transform parent, PlayerNotesSession session, TMP_FontAsset font)
    {
        RectTransform frame = PlayerNoteView.Rect(parent, "PlayerNotesBoard",
            new Vector2(.04f, .13f), new Vector2(.49f, .90f));
        frame.gameObject.AddComponent<Image>().color = new Color(.25f, .16f, .09f);
        BoardPlayerNotesView view = frame.gameObject.AddComponent<BoardPlayerNotesView>();
        view.session = session;
        view.font = font;
        view.Build(frame);
        return view;
    }

    private void Build(RectTransform frame)
    {
        RectTransform cork = PlayerNoteView.Rect(frame, "Cork", new Vector2(.012f, .012f), new Vector2(.988f, .988f));
        cork.gameObject.AddComponent<Image>().color = new Color(.48f, .34f, .21f);
        CreateButton(frame, "NewPlayerNoteButton", "+ Нова бележка", new Vector2(.04f, .90f),
            new Vector2(.59f, .97f), AddNote);
        RectTransform caption = PlayerNoteView.Rect(frame, "SessionLabel", new Vector2(.62f, .90f), new Vector2(.96f, .97f));
        PlayerNoteView.Label(caption, "Text", "Само за тази сесия", font, 20, TextAlignmentOptions.Center)
            .color = new Color(.96f, .91f, .80f);
        surface = PlayerNoteView.Rect(frame, "PlayerNotesSurface", new Vector2(.025f, .09f), new Vector2(.975f, .88f));
        previous = CreateButton(frame, "PreviousNotesPage", "Предишни", new Vector2(.04f, .025f),
            new Vector2(.31f, .075f), () => ShowPage(page - 1));
        next = CreateButton(frame, "NextNotesPage", "Следващи", new Vector2(.69f, .025f),
            new Vector2(.96f, .075f), () => ShowPage(page + 1));
        RectTransform counter = PlayerNoteView.Rect(frame, "PageLabel", new Vector2(.32f, .025f), new Vector2(.68f, .075f));
        pageLabel = PlayerNoteView.Label(counter, "Text", "", font, 22, TextAlignmentOptions.Center);
        pageLabel.color = new Color(.96f, .91f, .80f);
        foreach (PlayerNote note in session.Notes)
            views.Add(PlayerNoteView.Create(surface, note, font));
        ShowPage(0);
    }

    private void AddNote()
    {
        int slot = session.Notes.Count % NotesPerPage;
        PlayerNote note = session.Create(slot % 2 == 0 ? .25f : .75f, slot < 2 ? .75f : .25f);
        PlayerNoteView view = PlayerNoteView.Create(surface, note, font);
        views.Add(view);
        ShowPage((views.Count - 1) / NotesPerPage);
        view.Focus();
    }

    private void ShowPage(int requestedPage)
    {
        int count = Mathf.Max(1, (views.Count + NotesPerPage - 1) / NotesPerPage);
        page = Mathf.Clamp(requestedPage, 0, count - 1);
        for (int i = 0; i < views.Count; i++)
            views[i].gameObject.SetActive(i / NotesPerPage == page);
        previous.interactable = page > 0;
        next.interactable = page < count - 1;
        pageLabel.text = $"{page + 1} / {count}";
    }

    private Button CreateButton(Transform parent, string name, string title, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.84f, .77f, .60f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);
        PlayerNoteView.Label(rect, "Label", title, font, 24, TextAlignmentOptions.Center);
        return button;
    }
}
