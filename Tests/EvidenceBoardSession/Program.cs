using System;

var board = new EvidenceBoardSession(3);
Check(board.TryLink(0, 2), "first link");
Check(board.HasLink(2, 0), "link is undirected");
Check(!board.TryLink(2, 0), "reversed duplicate");
Check(!board.TryLink(1, 1), "self link");
Check(!board.TryLink(-1, 2) && !board.TryLink(0, 3), "out-of-range links");
Check(board.Links.Count == 1, "invalid attempts do not add links");
board.Notes = "Моя хипотеза";
Check(board.Notes == "Моя хипотеза", "notes stay in session");
Check(board.RemoveLink(2, 0) && !board.HasLink(0, 2), "remove in reverse order");
Check(board.Notes == "Моя хипотеза", "removing link keeps notes");
Check(!board.RemoveLink(0, 2), "missing link cannot be removed twice");
var first = board.PlayerNotes.Create(.25f, .75f);
var second = board.PlayerNotes.Create(.75f, .75f);
Check(first.Text == "" && second.Text == "", "new notes start empty");
Check(Guid.TryParse(first.Id, out _) && first.Id != second.Id, "independent stable identities");
string firstId = first.Id;
first.SetText("Хипотеза\n<b>само текст</b>");
second.SetText("Друг въпрос");
first.SetText(first.Text + "?");
Check(second.Text == "Друг въпрос" && first.Text == "Хипотеза\n<b>само текст</b>?",
    "editing is independent and preserves literal multiline text");
Check(first.Id == firstId && first.NormalizedX == .25f && first.NormalizedY == .75f,
    "editing preserves identity and normalized position");
Check(board.Notes == "Моя хипотеза" && board.Links.Count == 0,
    "player notes do not change legacy notes or evidence links");
foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -.1f, 1.1f })
{
    try { board.PlayerNotes.Create(invalid, .5f); throw new Exception("invalid X accepted"); }
    catch (ArgumentOutOfRangeException) { }
    try { board.PlayerNotes.Create(.5f, invalid); throw new Exception("invalid Y accepted"); }
    catch (ArgumentOutOfRangeException) { }
}
Check(board.PlayerNotes.Notes.Count == 2, "invalid positions do not create notes");
var ids = new System.Collections.Generic.HashSet<string> { first.Id, second.Id };
for (int i = 0; i < 100; i++) Check(ids.Add(board.PlayerNotes.Create(0f, 1f).Id), "unique IDs beyond a page");
Check(new EvidenceBoardSession(3).PlayerNotes.Notes.Count == 0, "a new session starts empty");
Console.WriteLine("EvidenceBoardSession checks: PASS");

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
}
