using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Контролиран Play Mode regression, извикан от OfficeFlowSmokeTest.
internal static class BoardPlayerNotesSmokeTest
{
    private const string FirstText = "Моя хипотеза\n<b>не е факт</b>!";
    private const string SecondText = "Друг въпрос?";

    public static async Task Exercise(GameObject root, EvidenceBoardSession session, PauseUIController pause)
    {
        if (session.PlayerNotes.Notes.Count == 0)
        {
            Click(root, "NewPlayerNoteButton");
            await Task.Delay(50);
            TMP_InputField first = Input(root, session.PlayerNotes.Notes[0].Id);
            Require(first.isFocused && first.text == "", "new paper receives keyboard focus");
            foreach (char c in "Моя хипотеза\n<b>не е факт</b>")
                first.ProcessEvent(new Event { type = EventType.KeyDown, character = c });
            Click(root, "NewPlayerNoteButton");
            await Task.Delay(50);
            TMP_InputField second = Input(root, session.PlayerNotes.Notes[1].Id);
            second.text = SecondText;
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            first.ActivateInputField();
            await Task.Delay(50);
            first.stringPosition = first.text.Length;
            first.ProcessEvent(new Event { type = EventType.KeyDown, character = '!' });
            Require(first.text == FirstText && second.text == SecondText, "switching papers edits independently");
            Require(!first.richText && !first.textComponent.richText && !first.restoreOriginalTextOnEscape,
                "literal text and Escape cannot roll back a note");

            // UI Cancel може да достигне input field около Pause; текстът остава.
            first.OnCancel(new BaseEventData(EventSystem.current));
            Require(first.text == FirstText, "UI Cancel preserves typed text");
            first.ActivateInputField();
            await Task.Delay(50);
            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, null);
            await Task.Delay(50);
            Require(Time.timeScale == 0f && session.PlayerNotes.Notes[0].Text == FirstText,
                "Pause preserves the edited paper");
            pause.Resume();
            await Task.Delay(80);
            Require(EventSystem.current.currentSelectedGameObject == first.gameObject && first.isFocused,
                "Resume restores the paper input selection");

            for (int i = 2; i < 5; i++) Click(root, "NewPlayerNoteButton");
            await Task.Delay(50);
            Require(root.GetComponentsInChildren<PlayerNoteView>().Length == 1, "fifth paper opens next page");
            PlayerNoteView fifthView = View(root, session.PlayerNotes.Notes[4].Id);
            Input(root, session.PlayerNotes.Notes[4].Id).text = "Пета бележка";

            // EB-002: изтриване с отказ и потвърждение върху последната бележка на страница.
            ClickChildButton(fifthView.gameObject, "DeleteNoteButton");
            Click(root, "CancelDeleteNoteButton");
            Require(session.PlayerNotes.Notes.Count == 5 && root.GetComponentsInChildren<PlayerNoteView>().Length == 1,
                "canceling delete preserves note and page");
            ClickChildButton(fifthView.gameObject, "DeleteNoteButton");
            Click(root, "ConfirmDeleteNoteButton");
            await Task.Delay(50);
            Require(session.PlayerNotes.Notes.Count == 4 && root.GetComponentsInChildren<PlayerNoteView>().Length == 4,
                "confirming delete on last note of page removes it and returns to previous page");

            // EB-002: преместване на три бележки поотделно с ограничаване в дъската и запазване на текста.
            View(root, session.PlayerNotes.Notes[0].Id).ApplyNormalizedPosition(.31f, .70f);
            View(root, session.PlayerNotes.Notes[1].Id).ApplyNormalizedPosition(.69f, .68f);
            View(root, session.PlayerNotes.Notes[2].Id).ApplyNormalizedPosition(0f, 0f);
            Require(Mathf.Approximately(session.PlayerNotes.Notes[0].NormalizedX, .31f)
                && Mathf.Approximately(session.PlayerNotes.Notes[1].NormalizedX, .69f)
                && session.PlayerNotes.Notes[2].NormalizedX > .20f && session.PlayerNotes.Notes[2].NormalizedY > .20f,
                "moving notes updates normalized positions and clamps inside board");

            // Възстановява петата бележка за проверката на прелистване и запазване между влизанията.
            Click(root, "NewPlayerNoteButton");
            await Task.Delay(50);
            Require(root.GetComponentsInChildren<PlayerNoteView>().Length == 1, "fifth paper opens next page again");
            Input(root, session.PlayerNotes.Notes[4].Id).text = "Пета бележка";
            Click(root, "PreviousNotesPage");
            Require(root.GetComponentsInChildren<PlayerNoteView>().Length == 4, "earlier papers remain accessible");
        }
        AssertPreserved(root, session);
        // Потвърждава pointer hit area; callbacks по-горе не доказват физически input.
        await Task.Delay(50);
        Canvas.ForceUpdateCanvases();
        TMP_InputField target = Input(root, session.PlayerNotes.Notes[0].Id);
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        Require(hits.Count > 0 && (hits[0].gameObject == target.gameObject
            || hits[0].gameObject.transform.IsChildOf(target.transform)),
            "paper input is pointer reachable; hits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)));
    }

    public static void AssertPreserved(GameObject root, EvidenceBoardSession session)
    {
        Require(session.PlayerNotes.Notes.Count == 5, "session retains all papers");
        PlayerNoteView[] views = root.GetComponentsInChildren<PlayerNoteView>(true);
        Require(views.Length == 5 && views.Select(v => v.NoteId).Distinct().Count() == 5,
            "one view per stable note ID, without re-entry duplicates");
        Require(root.GetComponentsInChildren<BoardPlayerNotesView>(true).Length == 1
            && root.GetComponentsInChildren<Button>(true).Count(b => b.name == "NewPlayerNoteButton") == 1,
            "one notes surface and create button");
        string[] expected = { FirstText, SecondText, "", "", "Пета бележка" };
        for (int i = 0; i < expected.Length; i++)
        {
            PlayerNote note = session.PlayerNotes.Notes[i];
            Require(note.Text == expected[i] && Input(root, note.Id).text == expected[i], "text retained for " + note.Id);
        }
        Require(Mathf.Approximately(session.PlayerNotes.Notes[0].NormalizedX, .31f)
            && Mathf.Approximately(session.PlayerNotes.Notes[0].NormalizedY, .70f)
            && Mathf.Approximately(session.PlayerNotes.Notes[1].NormalizedX, .69f)
            && Mathf.Approximately(session.PlayerNotes.Notes[1].NormalizedY, .68f),
            "moved paper positions survive exit and re-entry");
    }

    private static PlayerNoteView View(GameObject root, string id) => root.GetComponentsInChildren<PlayerNoteView>(true)
        .Single(view => view.NoteId == id);

    private static TMP_InputField Input(GameObject root, string id) => View(root, id).GetComponent<TMP_InputField>();

    private static void Click(GameObject root, string name) => root.GetComponentsInChildren<Button>(true)
        .Single(button => button.name == name).onClick.Invoke();

    private static void ClickChildButton(GameObject parent, string name) => parent.GetComponentsInChildren<Button>(true)
        .Single(button => button.name == name).onClick.Invoke();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("EB-001: " + message);
    }
}
