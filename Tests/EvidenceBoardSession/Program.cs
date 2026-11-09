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
Console.WriteLine("EvidenceBoardSession checks: PASS");

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
}
