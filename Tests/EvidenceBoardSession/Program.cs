using System;
using System.Collections.Generic;
using System.Text.Json;

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

// EB-002: независимо преместване и изтриване на бележки.
var third = board.PlayerNotes.Create(.25f, .25f);
third.SetText("Трета бележка");
string secondId = second.Id;
string thirdId = third.Id;
first.SetPosition(.33f, .68f);
second.SetPosition(.66f, .62f);
third.SetPosition(.41f, .31f);
Check(first.Id == firstId && first.Text == "Хипотеза\n<b>само текст</b>?"
    && first.NormalizedX == .33f && first.NormalizedY == .68f,
    "moving first note updates normalized position without changing ID or text");
Check(second.Id == secondId && second.Text == "Друг въпрос"
    && second.NormalizedX == .66f && second.NormalizedY == .62f,
    "moving second note is independent");
Check(third.Id == thirdId && third.Text == "Трета бележка"
    && third.NormalizedX == .41f && third.NormalizedY == .31f,
    "moving third note is independent");
foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -.1f, 1.1f })
{
    try { first.SetPosition(invalid, .5f); throw new Exception("invalid move X accepted"); }
    catch (ArgumentOutOfRangeException) { }
    try { first.SetPosition(.5f, invalid); throw new Exception("invalid move Y accepted"); }
    catch (ArgumentOutOfRangeException) { }
}
Check(first.NormalizedX == .33f && first.NormalizedY == .68f, "invalid move preserves previous position");

Check(!board.PlayerNotes.Remove("") && !board.PlayerNotes.Remove("missing-id"), "removing unknown ID returns false");
Check(board.PlayerNotes.Remove(secondId), "removing existing note returns true");
Check(!board.PlayerNotes.Remove(secondId), "removing same note twice returns false");
Check(board.PlayerNotes.Notes.Count == 2
    && board.PlayerNotes.Notes[0].Id == firstId && board.PlayerNotes.Notes[0].Text == "Хипотеза\n<b>само текст</b>?"
    && board.PlayerNotes.Notes[0].NormalizedX == .33f && board.PlayerNotes.Notes[0].NormalizedY == .68f
    && board.PlayerNotes.Notes[1].Id == thirdId && board.PlayerNotes.Notes[1].Text == "Трета бележка"
    && board.PlayerNotes.Notes[1].NormalizedX == .41f && board.PlayerNotes.Notes[1].NormalizedY == .31f,
    "removing a note preserves remaining IDs, texts, and positions");
Check(board.Notes == "Моя хипотеза" && board.Links.Count == 0,
    "moving and removing player notes do not change legacy notes or links");

var ids = new System.Collections.Generic.HashSet<string> { first.Id, third.Id };
for (int i = 0; i < 100; i++) Check(ids.Add(board.PlayerNotes.Create(0f, 1f).Id), "unique IDs beyond a page");
Check(new EvidenceBoardSession(3).PlayerNotes.Notes.Count == 0, "a new session starts empty");

// EB-003: Save/Load snapshot, JSON roundtrip, backward compatibility и load safety.
var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
var saveBoardA = new EvidenceBoardSession(3);
var noteA1 = saveBoardA.PlayerNotes.Create(.25f, .75f);
var noteA2 = saveBoardA.PlayerNotes.Create(.75f, .75f);
var noteA3 = saveBoardA.PlayerNotes.Create(.25f, .25f);
var noteADeleted = saveBoardA.PlayerNotes.Create(.75f, .25f);
noteA1.SetText("Първа бележка\nКирилица + ред 2");
noteA2.SetText("Втора бележка — 22:41");
noteA3.SetText("Трета бележка\n<b>буквален текст</b>");
noteADeleted.SetText("Изтрита преди Save");
noteA1.SetPosition(.28f, .71f);
noteA2.SetPosition(.69f, .64f);
noteA3.SetPosition(.42f, .33f);
Check(saveBoardA.PlayerNotes.Remove(noteADeleted.Id), "EB-003: delete before save");

// 1-4. Save/Load на три бележки с различни ID-та, кирилица, многоредов текст, позиции и ред.
PlayerNoteSaveDto[] dtosA = PlayerNotesSaveMapper.ToSaveDtos(saveBoardA.PlayerNotes);
string jsonSaveA = JsonSerializer.Serialize(new TestSaveEnvelope { version = 2, playerNotes = dtosA }, jsonOptions);
var loadedEnvelopeA = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonSaveA, jsonOptions)!;
List<PlayerNote> restoredA = PlayerNotesSaveMapper.PrepareRestore(loadedEnvelopeA.playerNotes, out int skippedA);
Check(skippedA == 0 && restoredA.Count == 3, "EB-003: valid 3-note snapshot restores cleanly");

var runtimeBoard = new EvidenceBoardSession(3);
runtimeBoard.Notes = "Наследена бележка";
runtimeBoard.PlayerNotes.ReplaceAll(restoredA);
Check(runtimeBoard.PlayerNotes.Notes.Count == 3
    && runtimeBoard.PlayerNotes.Notes[0].Id == noteA1.Id && runtimeBoard.PlayerNotes.Notes[0].Text == noteA1.Text
    && runtimeBoard.PlayerNotes.Notes[0].NormalizedX == .28f && runtimeBoard.PlayerNotes.Notes[0].NormalizedY == .71f
    && runtimeBoard.PlayerNotes.Notes[1].Id == noteA2.Id && runtimeBoard.PlayerNotes.Notes[1].Text == noteA2.Text
    && runtimeBoard.PlayerNotes.Notes[1].NormalizedX == .69f && runtimeBoard.PlayerNotes.Notes[1].NormalizedY == .64f
    && runtimeBoard.PlayerNotes.Notes[2].Id == noteA3.Id && runtimeBoard.PlayerNotes.Notes[2].Text == noteA3.Text
    && runtimeBoard.PlayerNotes.Notes[2].NormalizedX == .42f && runtimeBoard.PlayerNotes.Notes[2].NormalizedY == .33f,
    "EB-003: IDs, texts, positions, and order restored without deleted note");
Check(runtimeBoard.Notes == "Наследена бележка", "EB-003: legacy session notes unaffected");

// 7. Повторен Load на същия файл без дублиране.
runtimeBoard.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEnvelopeA.playerNotes, out _));
Check(runtimeBoard.PlayerNotes.Notes.Count == 3 && runtimeBoard.PlayerNotes.Notes[0].Id == noteA1.Id,
    "EB-003: repeated load does not duplicate notes");

// 8. Последователен Load на два различни Save файла без смесване.
var saveBoardB = new EvidenceBoardSession(3);
var noteB1 = saveBoardB.PlayerNotes.Create(.50f, .50f);
noteB1.SetText("Друг сейв");
string jsonSaveB = JsonSerializer.Serialize(
    new TestSaveEnvelope { version = 2, playerNotes = PlayerNotesSaveMapper.ToSaveDtos(saveBoardB.PlayerNotes) }, jsonOptions);
var loadedEnvelopeB = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonSaveB, jsonOptions)!;
runtimeBoard.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEnvelopeB.playerNotes, out _));
Check(runtimeBoard.PlayerNotes.Notes.Count == 1
    && runtimeBoard.PlayerNotes.Notes[0].Id == noteB1.Id && runtimeBoard.PlayerNotes.Notes[0].Text == "Друг сейв",
    "EB-003: loading second save replaces first save without mixing");

// 5. Стар Save (v1) без playerNotes (липсващо/null поле).
var legacyV1 = JsonSerializer.Deserialize<TestSaveEnvelope>("{\"version\":1}", jsonOptions)!;
List<PlayerNote> restoredLegacy = PlayerNotesSaveMapper.PrepareRestore(legacyV1.playerNotes, out int skippedLegacy);
Check(skippedLegacy == 0 && restoredLegacy.Count == 0, "EB-003: legacy v1 save without playerNotes yields empty list");
runtimeBoard.PlayerNotes.ReplaceAll(restoredLegacy);
Check(runtimeBoard.PlayerNotes.Notes.Count == 0, "EB-003: loading legacy v1 clears previous session notes");

// 6. Нов Save с празна колекция.
string emptyJson = JsonSerializer.Serialize(
    new TestSaveEnvelope { version = 2, playerNotes = Array.Empty<PlayerNoteSaveDto>() }, jsonOptions);
var loadedEmpty = JsonSerializer.Deserialize<TestSaveEnvelope>(emptyJson, jsonOptions)!;
List<PlayerNote> restoredEmpty = PlayerNotesSaveMapper.PrepareRestore(loadedEmpty.playerNotes, out int skippedEmpty);
Check(skippedEmpty == 0 && restoredEmpty.Count == 0, "EB-003: empty collection save loads cleanly");

// 9 & 10. Невалидни ID-та (празен/null/дублиран) и невалидни координати се пропускат безопасно, а валидните се подреждат по order.
var mixedDtos = new[]
{
    new PlayerNoteSaveDto { id = "id-second", text = "Втора", normalizedX = .6f, normalizedY = .4f, order = 2 },
    new PlayerNoteSaveDto { id = "", text = "Празен ID", normalizedX = .5f, normalizedY = .5f, order = 0 },
    new PlayerNoteSaveDto { id = "   ", text = "Whitespace ID", normalizedX = .5f, normalizedY = .5f, order = 0 },
    new PlayerNoteSaveDto { id = null!, text = "Null ID", normalizedX = .5f, normalizedY = .5f, order = 0 },
    new PlayerNoteSaveDto { id = "id-first", text = "Първа", normalizedX = .3f, normalizedY = .7f, order = 1 },
    new PlayerNoteSaveDto { id = "id-first", text = "Дублиран ID", normalizedX = .4f, normalizedY = .6f, order = 3 },
    new PlayerNoteSaveDto { id = "bad-x-neg", text = "x<0", normalizedX = -.1f, normalizedY = .5f, order = 4 },
    new PlayerNoteSaveDto { id = "bad-y-over", text = "y>1", normalizedX = .5f, normalizedY = 1.2f, order = 5 },
    new PlayerNoteSaveDto { id = "bad-nan", text = "NaN", normalizedX = float.NaN, normalizedY = .5f, order = 6 },
    new PlayerNoteSaveDto { id = "bad-inf", text = "Inf", normalizedX = .5f, normalizedY = float.PositiveInfinity, order = 7 }
};
List<PlayerNote> sanitized = PlayerNotesSaveMapper.PrepareRestore(mixedDtos, out int skippedMixed);
Check(skippedMixed == 8 && sanitized.Count == 2
    && sanitized[0].Id == "id-first" && sanitized[1].Id == "id-second",
    "EB-003: invalid IDs, duplicates, and out-of-range coordinates are skipped while preserving valid order");

// 11. Неуспешно зареждане (невалиден JSON / грешка при ReplaceAll) не губи текущото добро състояние.
runtimeBoard.PlayerNotes.ReplaceAll(sanitized);
bool parseFailed = false;
try { JsonSerializer.Deserialize<TestSaveEnvelope>("{broken json", jsonOptions); }
catch (JsonException) { parseFailed = true; }
Check(parseFailed && runtimeBoard.PlayerNotes.Notes.Count == 2 && runtimeBoard.PlayerNotes.Notes[0].Id == "id-first",
    "EB-003: failed load preserves existing good state");

// EB-004: Manual Evidence Attachment System (AttachedSource, duplicate guard, drag/move, remove, Save/Load, backward compat).
var attachBoard = new EvidenceBoardSession(3);
var eb4Note = attachBoard.PlayerNotes.Create(.25f, .75f);
eb4Note.SetText("Лична бележка");

Check(attachBoard.AttachedSources.TryAttach("doc-case0017-incident-report", AttachedSourceType.Document, .75f, .25f, out AttachedSource? docCopy)
    && docCopy != null && Guid.TryParse(docCopy.AttachmentId, out _),
    "EB-004: attach document creates stable attachment ID");
Check(attachBoard.AttachedSources.TryAttach("photo-case0017-parking-ticket", AttachedSourceType.Photo, .75f, .75f, out AttachedSource? photoCopy)
    && photoCopy != null && photoCopy.AttachmentId != docCopy!.AttachmentId,
    "EB-004: attach photo creates independent stable attachment ID");
Check(!attachBoard.AttachedSources.TryAttach("doc-case0017-incident-report", AttachedSourceType.Document, .50f, .50f, out _)
    && attachBoard.AttachedSources.Sources.Count == 2,
    "EB-004: re-attaching same document does not create duplicate");
Check(!attachBoard.AttachedSources.TryAttach("photo-case0017-parking-ticket", AttachedSourceType.Photo, .50f, .50f, out _)
    && attachBoard.AttachedSources.Sources.Count == 2,
    "EB-004: re-attaching same photo does not create duplicate");
Check(!attachBoard.AttachedSources.TryAttach("", AttachedSourceType.Document, .50f, .50f, out _),
    "EB-004: empty source ID rejected");

docCopy!.SetPosition(.62f, .34f);
Check(docCopy.NormalizedX == .62f && docCopy.NormalizedY == .34f
    && eb4Note.NormalizedX == .25f && eb4Note.Text == "Лична бележка",
    "EB-004: moving attached source does not affect PlayerNote");
Check(attachBoard.PlayerNotes.Notes.Count == 1 && attachBoard.PlayerNotes.Notes[0].Id != docCopy.AttachmentId,
    "EB-004: PlayerNote and AttachedSource remain separate collections and identities");

// Remove + Save/Load roundtrip.
Check(attachBoard.AttachedSources.TryAttach("doc-case0012-closure-note", AttachedSourceType.Document, .25f, .25f, out AttachedSource? removedCopy)
    && removedCopy != null, "EB-004: attach third source before removal");
Check(attachBoard.AttachedSources.Remove(removedCopy!.AttachmentId) && !attachBoard.AttachedSources.Remove(removedCopy.AttachmentId),
    "EB-004: remove returns true once and false on repeat");

string jsonV3 = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 3,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(attachBoard.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(attachBoard.AttachedSources)
}, jsonOptions);
var loadedV3 = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonV3, jsonOptions)!;
List<AttachedSource> restoredV3 = AttachedSourcesSaveMapper.PrepareRestore(loadedV3.attachedSources, out int skippedV3);
Check(skippedV3 == 0 && restoredV3.Count == 2, "EB-004: valid v3 snapshot restores 2 attachments without removed item");

var loadedBoard = new EvidenceBoardSession(3);
loadedBoard.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedV3.playerNotes, out _));
loadedBoard.AttachedSources.ReplaceAll(restoredV3);
Check(loadedBoard.AttachedSources.Sources.Count == 2
    && loadedBoard.AttachedSources.Sources[0].AttachmentId == docCopy.AttachmentId
    && loadedBoard.AttachedSources.Sources[0].SourceId == "doc-case0017-incident-report"
    && loadedBoard.AttachedSources.Sources[0].SourceType == AttachedSourceType.Document
    && loadedBoard.AttachedSources.Sources[0].NormalizedX == .62f
    && loadedBoard.AttachedSources.Sources[0].NormalizedY == .34f
    && loadedBoard.AttachedSources.Sources[1].AttachmentId == photoCopy!.AttachmentId
    && loadedBoard.AttachedSources.Sources[1].SourceId == "photo-case0017-parking-ticket"
    && loadedBoard.AttachedSources.Sources[1].SourceType == AttachedSourceType.Photo
    && loadedBoard.PlayerNotes.Notes.Count == 1 && loadedBoard.PlayerNotes.Notes[0].Text == "Лична бележка",
    "EB-004: Save/Load restores attached sources and Player Notes independently");

// Repeated Load does not duplicate.
loadedBoard.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedV3.attachedSources, out _));
Check(loadedBoard.AttachedSources.Sources.Count == 2, "EB-004: repeated Load does not duplicate attached sources");

// Legacy v2 save without attachedSources field clears attachments cleanly while preserving Player Notes.
var legacyV2 = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonSaveA, jsonOptions)!;
List<AttachedSource> restoredLegacyV2 = AttachedSourcesSaveMapper.PrepareRestore(legacyV2.attachedSources, out int skippedLegacyV2);
Check(skippedLegacyV2 == 0 && restoredLegacyV2.Count == 0, "EB-004: legacy v2 save without attachedSources yields empty list");
loadedBoard.AttachedSources.ReplaceAll(restoredLegacyV2);
Check(loadedBoard.AttachedSources.Sources.Count == 0, "EB-004: loading legacy v2 clears previous attached sources");

// Invalid/duplicate DTOs skipped safely.
var mixedAttachDtos = new[]
{
    new AttachedSourceSaveDto { attachmentId = "att-2", sourceId = "photo-case0017-parking-ticket", sourceType = 1, normalizedX = .7f, normalizedY = .3f, order = 2 },
    new AttachedSourceSaveDto { attachmentId = "", sourceId = "doc-x", sourceType = 0, normalizedX = .5f, normalizedY = .5f, order = 0 },
    new AttachedSourceSaveDto { attachmentId = "att-bad-src", sourceId = "   ", sourceType = 0, normalizedX = .5f, normalizedY = .5f, order = 0 },
    new AttachedSourceSaveDto { attachmentId = "att-bad-type", sourceId = "doc-x", sourceType = 99, normalizedX = .5f, normalizedY = .5f, order = 0 },
    new AttachedSourceSaveDto { attachmentId = "att-1", sourceId = "doc-case0017-incident-report", sourceType = 0, normalizedX = .3f, normalizedY = .7f, order = 1 },
    new AttachedSourceSaveDto { attachmentId = "att-1", sourceId = "doc-other", sourceType = 0, normalizedX = .4f, normalizedY = .6f, order = 3 },
    new AttachedSourceSaveDto { attachmentId = "att-dup-src", sourceId = "doc-case0017-incident-report", sourceType = 0, normalizedX = .4f, normalizedY = .6f, order = 4 },
    new AttachedSourceSaveDto { attachmentId = "att-bad-coord", sourceId = "doc-y", sourceType = 0, normalizedX = 1.5f, normalizedY = .5f, order = 5 }
};
List<AttachedSource> sanitizedAttachments = AttachedSourcesSaveMapper.PrepareRestore(mixedAttachDtos, out int skippedMixedAttach);
Check(skippedMixedAttach == 6 && sanitizedAttachments.Count == 2
    && sanitizedAttachments[0].AttachmentId == "att-1" && sanitizedAttachments[1].AttachmentId == "att-2",
    "EB-004: invalid/duplicate attached source DTOs are skipped safely while preserving order");

// EB-006: Manual Investigation Timeline (create, chronological sort, midnight, ties, untimed, validation, edit, delete, Save/Load).
var tlBoard = new EvidenceBoardSession(3);
var tlNote = tlBoard.PlayerNotes.Create(.25f, .75f);
tlNote.SetText("Запазена бележка");
tlBoard.AttachedSources.TryAttach("doc-case0017-incident-report", AttachedSourceType.Document, .75f, .25f, out _);

// 1-4, 11. Три събития извън ред, включително преди и след полунощ + кирилица/многоредов текст.
Check(tlBoard.Timeline.TryCreate("2003-10-15", "00:05", "Напускане на входа", "Човекът излиза след полунощ.", out TimelineEvent evAfterMidnight, out _),
    "EB-006: create post-midnight event");
Check(tlBoard.Timeline.TryCreate("2003-10-14", "23:45", "Неизвестен човек влиза в лабораторията", "Според CCTV записа човек с тъмно яке влиза в коридора.\nВтори ред наблюдение.", out TimelineEvent evBeforeMidnight, out _),
    "EB-006: create pre-midnight event with Cyrillic multiline text");
Check(tlBoard.Timeline.TryCreate("2003-10-14", "22:41", "Отпечатана бележка от паркинг", "Час върху бележката.", out TimelineEvent evEarlier, out _),
    "EB-006: create earlier event");
Check(tlBoard.Timeline.Events.Count == 3
    && tlBoard.Timeline.Events[0].Id == evEarlier.Id
    && tlBoard.Timeline.Events[1].Id == evBeforeMidnight.Id
    && tlBoard.Timeline.Events[2].Id == evAfterMidnight.Id,
    "EB-006: events sort chronologically including before and after midnight");

// 8. Събития с еднакви дата и час пазят предвидим ред (InsertionOrder).
Check(tlBoard.Timeline.TryCreate("2003-10-14", "23:45", "Шум в коридора", "Второ събитие със същия час.", out TimelineEvent evSameTime, out _),
    "EB-006: create second event at identical date and time");
Check(tlBoard.Timeline.Events[1].Id == evBeforeMidnight.Id
    && tlBoard.Timeline.Events[2].Id == evSameTime.Id,
    "EB-006: identical date and time preserves deterministic insertion order");

// 9. Събитие без въведен час се подрежда след уточнените часове за същата дата.
Check(tlBoard.Timeline.TryCreate("2003-10-14", "", "Липсващ ключ от архива", "Не е известен точният час.", out TimelineEvent evUntimed, out _)
    && !evUntimed.HasTime && evUntimed.Time == "",
    "EB-006: create untimed event");
Check(tlBoard.Timeline.Events[3].Id == evUntimed.Id
    && tlBoard.Timeline.Events[4].Id == evAfterMidnight.Id,
    "EB-006: untimed event stays grouped at end of its date before next date");

// 10. Невалидна дата, невалиден час и празно заглавие се отказват без промяна на данните.
int countBeforeInvalid = tlBoard.Timeline.Events.Count;
Check(!tlBoard.Timeline.TryCreate("", "12:00", "Заглавие", "", out _, out string errEmptyDate) && !string.IsNullOrEmpty(errEmptyDate),
    "EB-006: empty date rejected");
Check(!tlBoard.Timeline.TryCreate("2003-02-30", "12:00", "Заглавие", "", out _, out _),
    "EB-006: invalid calendar date rejected");
Check(!tlBoard.Timeline.TryCreate("14-10-2003", "12:00", "Заглавие", "", out _, out _),
    "EB-006: non-YYYY-MM-DD date rejected");
Check(!tlBoard.Timeline.TryCreate("2003-10-14", "24:00", "Заглавие", "", out _, out _),
    "EB-006: 24:00 time rejected");
Check(!tlBoard.Timeline.TryCreate("2003-10-14", "9:30", "Заглавие", "", out _, out _),
    "EB-006: non-HH:mm time rejected");
Check(!tlBoard.Timeline.TryCreate("2003-10-14", "12:60", "Заглавие", "", out _, out _),
    "EB-006: minute 60 rejected");
Check(!tlBoard.Timeline.TryCreate("2003-10-14", "12:00", "   ", "", out _, out _),
    "EB-006: empty title rejected");
Check(tlBoard.Timeline.Events.Count == countBeforeInvalid,
    "EB-006: invalid attempts do not add events");

// 5 & 6. Редактиране на час и описание преподрежда събитието без смяна на ID.
string editedId = evBeforeMidnight.Id;
Check(tlBoard.Timeline.TryUpdate(editedId, "2003-10-14", "23:57", evBeforeMidnight.Title,
    "Коригиран час според отчета (+12 мин).\n<b>буквален текст</b>", out _),
    "EB-006: edit time and description succeeds");
Check(tlBoard.Timeline.Events[1].Id == evSameTime.Id
    && tlBoard.Timeline.Events[2].Id == editedId
    && tlBoard.Timeline.Events[2].Time == "23:57"
    && tlBoard.Timeline.Events[2].Description == "Коригиран час според отчета (+12 мин).\n<b>буквален текст</b>",
    "EB-006: editing time re-sorts event while keeping stable ID and multiline description");

// 7. Изтриване премахва само избраното събитие.
Check(tlBoard.Timeline.Remove(evSameTime.Id) && !tlBoard.Timeline.Remove(evSameTime.Id),
    "EB-006: remove returns true once and false on repeat");
Check(tlBoard.Timeline.Events.Count == 4 && !tlBoard.Timeline.TryGetById(evSameTime.Id, out _),
    "EB-006: deleted event removed without affecting remaining events");
Check(tlBoard.PlayerNotes.Notes.Count == 1 && tlBoard.PlayerNotes.Notes[0].Text == "Запазена бележка"
    && tlBoard.AttachedSources.Sources.Count == 1,
    "EB-006: timeline operations do not mutate PlayerNotes or AttachedSources");

// 16-18. Save/Load v4 JSON roundtrip, стар Save без Timeline, повторен Load без дубликати, невалидни DTO.
string jsonV4 = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(tlBoard.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(tlBoard.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(tlBoard.Timeline)
}, jsonOptions);
var loadedV4 = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonV4, jsonOptions)!;
List<TimelineEvent> restoredTl = TimelineSaveMapper.PrepareRestore(loadedV4.timelineEvents, out int skippedTl);
Check(skippedTl == 0 && restoredTl.Count == 4, "EB-006: valid v4 timeline snapshot restores 4 events");

var loadedTlBoard = new EvidenceBoardSession(3);
loadedTlBoard.Timeline.ReplaceAll(restoredTl);
Check(loadedTlBoard.Timeline.Events.Count == 4
    && loadedTlBoard.Timeline.Events[0].Id == evEarlier.Id
    && loadedTlBoard.Timeline.Events[1].Id == editedId && loadedTlBoard.Timeline.Events[1].Time == "23:57"
    && loadedTlBoard.Timeline.Events[2].Id == evUntimed.Id && !loadedTlBoard.Timeline.Events[2].HasTime
    && loadedTlBoard.Timeline.Events[3].Id == evAfterMidnight.Id,
    "EB-006: Save/Load restores timeline events, order, edited time, untimed flag and Cyrillic text");

// Повторен Load не създава дубликати.
loadedTlBoard.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedV4.timelineEvents, out _));
Check(loadedTlBoard.Timeline.Events.Count == 4, "EB-006: repeated Load does not duplicate timeline events");

// Стар v3 Save без timelineEvents се зарежда чисто и изчиства старите събития.
var legacyV3ForTl = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonV3, jsonOptions)!;
List<TimelineEvent> restoredLegacyV3Tl = TimelineSaveMapper.PrepareRestore(legacyV3ForTl.timelineEvents, out int skippedLegacyTl);
Check(skippedLegacyTl == 0 && restoredLegacyV3Tl.Count == 0, "EB-006: legacy v3 save without timelineEvents yields empty list");
loadedTlBoard.Timeline.ReplaceAll(restoredLegacyV3Tl);
Check(loadedTlBoard.Timeline.Events.Count == 0, "EB-006: loading legacy save clears previous timeline events");

// Невалидни и дублирани DTO се пропускат безопасно.
var mixedTlDtos = new[]
{
    new TimelineEventSaveDto { id = "tl-2", date = "2003-10-15", time = "00:10", title = "Второ", description = "", insertionOrder = 2 },
    new TimelineEventSaveDto { id = "", date = "2003-10-14", time = "10:00", title = "Празен ID", description = "", insertionOrder = 0 },
    new TimelineEventSaveDto { id = "tl-bad-date", date = "2003-99-99", time = "10:00", title = "Лоша дата", description = "", insertionOrder = 0 },
    new TimelineEventSaveDto { id = "tl-bad-time", date = "2003-10-14", time = "25:00", title = "Лош час", description = "", insertionOrder = 0 },
    new TimelineEventSaveDto { id = "tl-no-title", date = "2003-10-14", time = "", title = "   ", description = "", insertionOrder = 0 },
    new TimelineEventSaveDto { id = "tl-1", date = "2003-10-14", time = "23:45", title = "Първо", description = "Описание", insertionOrder = 1 },
    new TimelineEventSaveDto { id = "tl-1", date = "2003-10-14", time = "23:50", title = "Дублиран ID", description = "", insertionOrder = 3 }
};
List<TimelineEvent> sanitizedTl = TimelineSaveMapper.PrepareRestore(mixedTlDtos, out int skippedMixedTl);
Check(skippedMixedTl == 5 && sanitizedTl.Count == 2
    && sanitizedTl[0].Id == "tl-1" && sanitizedTl[1].Id == "tl-2",
    "EB-006: invalid and duplicate timeline DTOs are skipped safely while sorting valid ones");

// EB-007B: Project Orion Stage 1 sources (stable IDs, no duplicates, Player Notes, manual Timeline 23:30/23:45/23:52/00:10, Save/Load).
const string orionSecuritySourceId = "doc-orion-initial-security-report";
const string orionCctvSourceId = "doc-orion-cctv-observation";
var orionBoard = new EvidenceBoardSession(3);
Check(orionBoard.AttachedSources.TryAttach(orionSecuritySourceId, AttachedSourceType.Document, .30f, .70f, out AttachedSource? orionSecCopy)
    && orionSecCopy != null,
    "EB-007B: attach Project Orion Initial Security Report");
Check(orionBoard.AttachedSources.TryAttach(orionCctvSourceId, AttachedSourceType.Document, .70f, .70f, out AttachedSource? orionCctvCopy)
    && orionCctvCopy != null && orionCctvCopy.AttachmentId != orionSecCopy!.AttachmentId,
    "EB-007B: attach Project Orion CCTV Observation with unique attachment ID");
Check(!orionBoard.AttachedSources.TryAttach(orionSecuritySourceId, AttachedSourceType.Document, .50f, .50f, out _)
    && !orionBoard.AttachedSources.TryAttach(orionCctvSourceId, AttachedSourceType.Document, .50f, .50f, out _)
    && orionBoard.AttachedSources.Sources.Count == 2,
    "EB-007B: re-attaching Project Orion sources does not create duplicates");
Check(orionBoard.AttachedSources.TryAttach("doc-case0017-incident-report", AttachedSourceType.Document, .50f, .30f, out _),
    "EB-007B: legacy CASE-0017 source coexists with Project Orion sources");
var orionNote = orionBoard.PlayerNotes.Create(.42f, .48f);
orionNote.SetText("Защо алармата не се е задействала в 23:45?\nЧовекът с качулката излиза в 23:52.");
Check(orionBoard.Timeline.TryCreate("2000-01-02", "00:10", "Установена липса на прототипа", "Охраната открива липсата на Project Orion.", out TimelineEvent orionEv4, out _),
    "EB-007B: manual timeline event 00:10");
Check(orionBoard.Timeline.TryCreate("2000-01-01", "23:45", "Събитие за достъп", "Охранителната система отчита достъп; CCTV показва влизане.", out TimelineEvent orionEv2, out _),
    "EB-007B: manual timeline event 23:45");
Check(orionBoard.Timeline.TryCreate("2000-01-01", "23:52", "Човек напуска коридора", "Неизвестен човек с тъмно яке и качулка напуска коридора.", out TimelineEvent orionEv3, out _),
    "EB-007B: manual timeline event 23:52");
Check(orionBoard.Timeline.TryCreate("2000-01-01", "23:30", "Проверка на лабораторията", "Всичко изглежда нормално.", out TimelineEvent orionEv1, out _),
    "EB-007B: manual timeline event 23:30");
Check(orionBoard.Timeline.Events.Count == 4
    && orionBoard.Timeline.Events[0].Id == orionEv1.Id
    && orionBoard.Timeline.Events[1].Id == orionEv2.Id
    && orionBoard.Timeline.Events[2].Id == orionEv3.Id
    && orionBoard.Timeline.Events[3].Id == orionEv4.Id,
    "EB-007B: manual Project Orion timeline events sort chronologically across midnight");
string orionSaveJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(orionBoard.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(orionBoard.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(orionBoard.Timeline)
}, jsonOptions);
var loadedOrionEnvelope = JsonSerializer.Deserialize<TestSaveEnvelope>(orionSaveJson, jsonOptions)!;
var restoredOrionBoard = new EvidenceBoardSession(3);
restoredOrionBoard.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedOrionEnvelope.playerNotes, out int orionSkippedNotes));
restoredOrionBoard.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedOrionEnvelope.attachedSources, out int orionSkippedSources));
restoredOrionBoard.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedOrionEnvelope.timelineEvents, out int orionSkippedTimeline));
Check(orionSkippedNotes == 0 && orionSkippedSources == 0 && orionSkippedTimeline == 0
    && restoredOrionBoard.AttachedSources.Sources.Count == 3
    && restoredOrionBoard.AttachedSources.ContainsSource(orionSecuritySourceId, AttachedSourceType.Document)
    && restoredOrionBoard.AttachedSources.ContainsSource(orionCctvSourceId, AttachedSourceType.Document)
    && restoredOrionBoard.AttachedSources.ContainsSource("doc-case0017-incident-report", AttachedSourceType.Document)
    && restoredOrionBoard.PlayerNotes.Notes.Count == 1 && restoredOrionBoard.PlayerNotes.Notes[0].Text == orionNote.Text
    && restoredOrionBoard.Timeline.Events.Count == 4,
    "EB-007B: Save/Load roundtrip preserves Project Orion attachments, legacy attachments, Player Notes and Timeline");

// EB-008: Project Orion personnel files (4 employees) + Employee Access Records (Clue 5), no duplicates, manual note, manual timeline 21:40/22:15/23:20, Save/Load.
string[] eb008SourceIds =
{
    "doc-orion-personnel-daniel-brooks",
    "doc-orion-personnel-emily-carter",
    "doc-orion-personnel-michael-reed",
    "doc-orion-personnel-sarah-mitchell",
    "doc-orion-employee-access-records"
};
var eb008Board = new EvidenceBoardSession(3);
Check(eb008Board.AttachedSources.TryAttach(orionSecuritySourceId, AttachedSourceType.Document, .20f, .80f, out _)
    && eb008Board.AttachedSources.TryAttach(orionCctvSourceId, AttachedSourceType.Document, .35f, .80f, out _),
    "EB-008: existing EB-007B sources remain attachable");
for (int i = 0; i < eb008SourceIds.Length; i++)
{
    Check(eb008Board.AttachedSources.TryAttach(eb008SourceIds[i], AttachedSourceType.Document, .25f + i * .10f, .45f, out _),
        "EB-008: attach source " + eb008SourceIds[i]);
    Check(!eb008Board.AttachedSources.TryAttach(eb008SourceIds[i], AttachedSourceType.Document, .50f, .50f, out _),
        "EB-008: duplicate attach rejected for " + eb008SourceIds[i]);
}
Check(eb008Board.AttachedSources.Sources.Count == 7,
    "EB-008: all 7 Project Orion sources attach independently without duplicates");
var eb008Note = eb008Board.PlayerNotes.Create(.52f, .62f);
eb008Note.SetText("Daniel твърди, че е бил вкъщи след 22:00, но е използвал системата за сигурност в 22:15. Трябва да проверя дали достъпът е бил дистанционен.");
Check(eb008Board.Timeline.TryCreate("2000-01-01", "23:20", "Emily използва картата си при напускане", "Отчет за достъп.", out TimelineEvent eb8Ev3, out _)
    && eb008Board.Timeline.TryCreate("2000-01-01", "21:40", "Последно използване на картата на Michael", "Отчет за достъп.", out TimelineEvent eb8Ev1, out _)
    && eb008Board.Timeline.TryCreate("2000-01-01", "22:15", "Достъп на Daniel до системата", "Отчет за достъп.", out TimelineEvent eb8Ev2, out _)
    && eb008Board.Timeline.Events.Count == 3
    && eb008Board.Timeline.Events[0].Id == eb8Ev1.Id
    && eb008Board.Timeline.Events[1].Id == eb8Ev2.Id
    && eb008Board.Timeline.Events[2].Id == eb8Ev3.Id,
    "EB-008: manual Clue 5 timeline events (21:40, 22:15, 23:20) sort chronologically");
string eb008SaveJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(eb008Board.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(eb008Board.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(eb008Board.Timeline)
}, jsonOptions);
var loadedEb008 = JsonSerializer.Deserialize<TestSaveEnvelope>(eb008SaveJson, jsonOptions)!;
var restoredEb008 = new EvidenceBoardSession(3);
restoredEb008.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEb008.playerNotes, out int eb8SkipNotes));
restoredEb008.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb008.attachedSources, out int eb8SkipSrc));
restoredEb008.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedEb008.timelineEvents, out int eb8SkipTl));
Check(eb8SkipNotes == 0 && eb8SkipSrc == 0 && eb8SkipTl == 0
    && restoredEb008.AttachedSources.Sources.Count == 7
    && restoredEb008.AttachedSources.ContainsSource("doc-orion-personnel-daniel-brooks", AttachedSourceType.Document)
    && restoredEb008.AttachedSources.ContainsSource("doc-orion-employee-access-records", AttachedSourceType.Document)
    && restoredEb008.PlayerNotes.Notes.Count == 1 && restoredEb008.PlayerNotes.Notes[0].Text == eb008Note.Text
    && restoredEb008.Timeline.Events.Count == 3,
    "EB-008: Save/Load roundtrip preserves all 7 Project Orion attachments, Player Note and Clue 5 Timeline events");

// EB-009: Clue 6 (3 communications) + Clue 7 (Daniel's deleted computer history), attach/dedupe/delete, manual Timeline 21:15/21:17/22:50/23:05/23:07, Save/Load.
string[] eb009SourceIds =
{
    "doc-orion-comm-daniel-unknown",
    "doc-orion-comm-emily-michael",
    "doc-orion-comm-sarah-unknown",
    "doc-orion-daniel-deleted-history"
};
var eb009Board = new EvidenceBoardSession(3);
AttachedSource? eb9ToDelete = null;
for (int i = 0; i < eb009SourceIds.Length; i++)
{
    Check(eb009Board.AttachedSources.TryAttach(eb009SourceIds[i], AttachedSourceType.Document, .25f + i * .15f, .55f, out AttachedSource? att)
        && att != null,
        "EB-009: attach source " + eb009SourceIds[i]);
    if (i == 2) eb9ToDelete = att;
    Check(!eb009Board.AttachedSources.TryAttach(eb009SourceIds[i], AttachedSourceType.Document, .50f, .50f, out _),
        "EB-009: duplicate attach rejected for " + eb009SourceIds[i]);
}
Check(eb009Board.AttachedSources.Sources.Count == 4, "EB-009: all 4 new sources attach cleanly");
Check(eb009Board.AttachedSources.Remove(eb9ToDelete!.AttachmentId) && eb009Board.AttachedSources.Sources.Count == 3,
    "EB-009: removing one attached copy before save leaves 3 attached sources");
var eb009Note = eb009Board.PlayerNotes.Create(.35f, .35f);
eb009Note.SetText("23:05 Daniel пише, че системата няма да засече нищо.\nСвързано ли е с изтритото търсене?");
Check(eb009Board.Timeline.TryCreate("2000-01-01", "23:07", "Отговор от неизвестен участник", "Комуникация с Daniel.", out TimelineEvent eb9Ev5, out _)
    && eb009Board.Timeline.TryCreate("2000-01-01", "21:15", "Съобщение от Emily", "Бюджет за изследвания.", out TimelineEvent eb9Ev1, out _)
    && eb009Board.Timeline.TryCreate("2000-01-01", "22:50", "Съобщение от Sarah", "Към неизвестен участник.", out TimelineEvent eb9Ev3, out _)
    && eb009Board.Timeline.TryCreate("2000-01-01", "21:17", "Отговор от Michael", "Отговор към Emily.", out TimelineEvent eb9Ev2, out _)
    && eb009Board.Timeline.TryCreate("2000-01-01", "23:05", "Съобщение от Daniel", "Всичко е готово.", out TimelineEvent eb9Ev4, out _)
    && eb009Board.Timeline.Events.Count == 5
    && eb009Board.Timeline.Events[0].Id == eb9Ev1.Id
    && eb009Board.Timeline.Events[1].Id == eb9Ev2.Id
    && eb009Board.Timeline.Events[2].Id == eb9Ev3.Id
    && eb009Board.Timeline.Events[3].Id == eb9Ev4.Id
    && eb009Board.Timeline.Events[4].Id == eb9Ev5.Id,
    "EB-009: manual Clue 6 timeline events (21:15, 21:17, 22:50, 23:05, 23:07) sort chronologically");
string eb009SaveJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(eb009Board.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(eb009Board.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(eb009Board.Timeline)
}, jsonOptions);
var loadedEb009 = JsonSerializer.Deserialize<TestSaveEnvelope>(eb009SaveJson, jsonOptions)!;
var restoredEb009 = new EvidenceBoardSession(3);
restoredEb009.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEb009.playerNotes, out int eb9SkipNotes));
restoredEb009.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb009.attachedSources, out int eb9SkipSrc));
restoredEb009.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedEb009.timelineEvents, out int eb9SkipTl));
Check(eb9SkipNotes == 0 && eb9SkipSrc == 0 && eb9SkipTl == 0
    && restoredEb009.AttachedSources.Sources.Count == 3
    && restoredEb009.AttachedSources.ContainsSource("doc-orion-comm-daniel-unknown", AttachedSourceType.Document)
    && restoredEb009.AttachedSources.ContainsSource("doc-orion-comm-emily-michael", AttachedSourceType.Document)
    && !restoredEb009.AttachedSources.ContainsSource("doc-orion-comm-sarah-unknown", AttachedSourceType.Document)
    && restoredEb009.AttachedSources.ContainsSource("doc-orion-daniel-deleted-history", AttachedSourceType.Document)
    && restoredEb009.PlayerNotes.Notes.Count == 1 && restoredEb009.PlayerNotes.Notes[0].Text == eb009Note.Text
    && restoredEb009.Timeline.Events.Count == 5,
    "EB-009: Save/Load restores remaining attachments without reviving deleted copy, plus Player Note and Timeline");

// EB-010: Stage 3 Motive Investigation (Clues 8-12), unique IDs, duplicate guard, delete without revival, Player Note, Timeline, Save/Load.
string[] eb010SourceIds =
{
    "doc-orion-michael-deleted-email",
    "doc-orion-financial-transfer-orion-solutions",
    "doc-orion-voice-recording-transcript",
    "doc-orion-emily-laboratory-notes",
    "doc-orion-hotel-photograph-description"
};
var eb010Board = new EvidenceBoardSession(3);
Check(eb010Board.AttachedSources.TryAttach("doc-orion-personnel-michael-reed", AttachedSourceType.Document, .20f, .75f, out _),
    "EB-010: attach earlier EB-008 Michael personnel record for comparison");
AttachedSource? eb10ToDelete = null;
for (int i = 0; i < eb010SourceIds.Length; i++)
{
    Check(eb010Board.AttachedSources.TryAttach(eb010SourceIds[i], AttachedSourceType.Document, .30f + i * .12f, .50f, out AttachedSource? att)
        && att != null,
        "EB-010: attach Stage 3 source " + eb010SourceIds[i]);
    if (i == 3) eb10ToDelete = att;
    Check(!eb010Board.AttachedSources.TryAttach(eb010SourceIds[i], AttachedSourceType.Document, .50f, .50f, out _),
        "EB-010: duplicate attach rejected for " + eb010SourceIds[i]);
}
Check(eb010Board.AttachedSources.Sources.Count == 6, "EB-010: 1 earlier + 5 Stage 3 sources attached cleanly");
Check(eb010Board.AttachedSources.Remove(eb10ToDelete!.AttachmentId) && eb010Board.AttachedSources.Sources.Count == 5,
    "EB-010: removing one Stage 3 copy before save leaves 5 attached sources");
var eb010Note = eb010Board.PlayerNotes.Create(.46f, .38f);
eb010Note.SetText("Преводът от 15 000 евро към Orion Solutions и срещата пред хотела — свързани ли са с офертата за 2 милиона евро?");
Check(eb010Board.Timeline.TryCreate("2000-10-11", "", "Среща пред хотел (надпис на фотографията)", "Michael Reed и директорът на Orion Solutions.", out TimelineEvent eb10Ev, out _)
    && eb010Board.Timeline.Events.Count == 1,
    "EB-010: player can manually log Stage 3 timeline event");
string eb010SaveJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(eb010Board.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(eb010Board.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(eb010Board.Timeline)
}, jsonOptions);
var loadedEb010 = JsonSerializer.Deserialize<TestSaveEnvelope>(eb010SaveJson, jsonOptions)!;
var restoredEb010 = new EvidenceBoardSession(3);
restoredEb010.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEb010.playerNotes, out int eb10SkipNotes));
restoredEb010.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb010.attachedSources, out int eb10SkipSrc));
restoredEb010.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedEb010.timelineEvents, out int eb10SkipTl));
Check(eb10SkipNotes == 0 && eb10SkipSrc == 0 && eb10SkipTl == 0
    && restoredEb010.AttachedSources.Sources.Count == 5
    && restoredEb010.AttachedSources.ContainsSource("doc-orion-michael-deleted-email", AttachedSourceType.Document)
    && restoredEb010.AttachedSources.ContainsSource("doc-orion-financial-transfer-orion-solutions", AttachedSourceType.Document)
    && restoredEb010.AttachedSources.ContainsSource("doc-orion-voice-recording-transcript", AttachedSourceType.Document)
    && !restoredEb010.AttachedSources.ContainsSource("doc-orion-emily-laboratory-notes", AttachedSourceType.Document)
    && restoredEb010.AttachedSources.ContainsSource("doc-orion-hotel-photograph-description", AttachedSourceType.Document)
    && restoredEb010.PlayerNotes.Notes.Count == 1 && restoredEb010.PlayerNotes.Notes[0].Text == eb010Note.Text
    && restoredEb010.Timeline.Events.Count == 1 && restoredEb010.Timeline.Events[0].Id == eb10Ev.Id,
    "EB-010: Save/Load restores Stage 3 attachments without reviving deleted copy, plus Player Note and Timeline");
restoredEb010.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb010.attachedSources, out _));
Check(restoredEb010.AttachedSources.Sources.Count == 5, "EB-010: repeated Load does not duplicate Stage 3 attachments");

// EB-011: Stage 4 Contradictions & Alibi Verification (Clues 13-17), unique IDs, duplicate guard, manual timeline non-auto-edit, delete without revival, Save/Load.
string[] eb011SourceIds =
{
    "doc-orion-cctv-clock-discrepancy",
    "doc-orion-daniel-phone-location",
    "doc-orion-emily-parking-message",
    "doc-orion-temp-card-registration",
    "doc-orion-michael-modified-calendar"
};
var eb011Board = new EvidenceBoardSession(3);
Check(eb011Board.AttachedSources.TryAttach("doc-orion-cctv-observation", AttachedSourceType.Document, .20f, .75f, out _),
    "EB-011: attach original Clue 2 CCTV observation");
Check(eb011Board.Timeline.TryCreate("2000-01-01", "23:45", "CCTV влизане (оригинален час)", "Според Clue 2.", out TimelineEvent eb11OrigCctv, out _),
    "EB-011: create player timeline event with original CCTV time 23:45");
AttachedSource? eb11ToDelete = null;
for (int i = 0; i < eb011SourceIds.Length; i++)
{
    Check(eb011Board.AttachedSources.TryAttach(eb011SourceIds[i], AttachedSourceType.Document, .30f + i * .12f, .50f, out AttachedSource? att)
        && att != null,
        "EB-011: attach Stage 4 source " + eb011SourceIds[i]);
    if (i == 1) eb11ToDelete = att;
    Check(!eb011Board.AttachedSources.TryAttach(eb011SourceIds[i], AttachedSourceType.Document, .50f, .50f, out _),
        "EB-011: duplicate attach rejected for " + eb011SourceIds[i]);
}
Check(eb011Board.AttachedSources.Sources.Count == 6
    && eb011Board.Timeline.Events.Count == 1
    && eb011Board.Timeline.Events[0].Id == eb11OrigCctv.Id
    && eb011Board.Timeline.Events[0].Time == "23:45",
    "EB-011: attaching Clue 13 does not automatically alter existing player Timeline events");
Check(eb011Board.AttachedSources.Remove(eb11ToDelete!.AttachmentId) && eb011Board.AttachedSources.Sources.Count == 5,
    "EB-011: removing one Stage 4 copy before save leaves 5 attached sources");
var eb011Note = eb011Board.PlayerNotes.Create(.44f, .36f);
eb011Note.SetText("CCTV часовникът изостава с 12 мин -> 23:45 може да е 23:57.\nСъвпада с активацията на временната карта в 23:56?");
string eb011SaveJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(eb011Board.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(eb011Board.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(eb011Board.Timeline)
}, jsonOptions);
var loadedEb011 = JsonSerializer.Deserialize<TestSaveEnvelope>(eb011SaveJson, jsonOptions)!;
var restoredEb011 = new EvidenceBoardSession(3);
restoredEb011.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEb011.playerNotes, out int eb11SkipNotes));
restoredEb011.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb011.attachedSources, out int eb11SkipSrc));
restoredEb011.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedEb011.timelineEvents, out int eb11SkipTl));
Check(eb11SkipNotes == 0 && eb11SkipSrc == 0 && eb11SkipTl == 0
    && restoredEb011.AttachedSources.Sources.Count == 5
    && restoredEb011.AttachedSources.ContainsSource("doc-orion-cctv-clock-discrepancy", AttachedSourceType.Document)
    && !restoredEb011.AttachedSources.ContainsSource("doc-orion-daniel-phone-location", AttachedSourceType.Document)
    && restoredEb011.AttachedSources.ContainsSource("doc-orion-emily-parking-message", AttachedSourceType.Document)
    && restoredEb011.AttachedSources.ContainsSource("doc-orion-temp-card-registration", AttachedSourceType.Document)
    && restoredEb011.AttachedSources.ContainsSource("doc-orion-michael-modified-calendar", AttachedSourceType.Document)
    && restoredEb011.PlayerNotes.Notes.Count == 1 && restoredEb011.PlayerNotes.Notes[0].Text == eb011Note.Text
    && restoredEb011.Timeline.Events.Count == 1 && restoredEb011.Timeline.Events[0].Time == "23:45",
    "EB-011: Save/Load restores Stage 4 attachments without reviving deleted copy, plus Player Note and untouched Timeline");
restoredEb011.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb011.attachedSources, out _));
Check(restoredEb011.AttachedSources.Sources.Count == 5, "EB-011: repeated Load does not duplicate Stage 4 attachments");

// EB-012: Missing Clue 3 (Torn Paper) & Clue 4 (Security System Log), attach/dedupe/remove, Player Note, manual Timeline 23:45/23:52/00:10, Save/Load.
const string clue3SourceId = "doc-orion-torn-paper-record";
const string clue4SourceId = "doc-orion-security-system-log";
var eb012Board = new EvidenceBoardSession(3);
Check(eb012Board.AttachedSources.TryAttach(clue3SourceId, AttachedSourceType.Document, .28f, .70f, out AttachedSource? clue3Att)
    && clue3Att != null,
    "EB-012: attach Clue 3 Torn Paper record");
Check(eb012Board.AttachedSources.TryAttach(clue4SourceId, AttachedSourceType.Document, .68f, .70f, out AttachedSource? clue4Att)
    && clue4Att != null && clue4Att.AttachmentId != clue3Att!.AttachmentId,
    "EB-012: attach Clue 4 Security System Log with unique attachment ID");
Check(!eb012Board.AttachedSources.TryAttach(clue3SourceId, AttachedSourceType.Document, .50f, .50f, out _)
    && !eb012Board.AttachedSources.TryAttach(clue4SourceId, AttachedSourceType.Document, .50f, .50f, out _)
    && eb012Board.AttachedSources.Sources.Count == 2,
    "EB-012: re-attaching Clue 3 or Clue 4 does not create duplicates");
var eb012Note = eb012Board.PlayerNotes.Create(.48f, .42f);
eb012Note.SetText("Електронната брава отчита 23:45 и 23:52 със служебна карта. Дали часовникът ѝ е отделен от CCTV?");
Check(eb012Board.Timeline.TryCreate("2000-01-02", "00:10", "Проверка на охраната", "Журнал на електронната брава.", out TimelineEvent eb12Ev3, out _)
    && eb012Board.Timeline.TryCreate("2000-01-01", "23:52", "Заключване на вратата", "Журнал на електронната брава.", out TimelineEvent eb12Ev2, out _)
    && eb012Board.Timeline.TryCreate("2000-01-01", "23:45", "Отключване на лабораторната врата", "Използвана служебна карта.", out TimelineEvent eb12Ev1, out _)
    && eb012Board.Timeline.Events.Count == 3
    && eb012Board.Timeline.Events[0].Id == eb12Ev1.Id
    && eb012Board.Timeline.Events[1].Id == eb12Ev2.Id
    && eb012Board.Timeline.Events[2].Id == eb12Ev3.Id,
    "EB-012: manual Clue 4 timeline events (23:45, 23:52, 00:10) sort chronologically across midnight");
string eb012SaveBothJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(eb012Board.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(eb012Board.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(eb012Board.Timeline)
}, jsonOptions);
var loadedEb012Both = JsonSerializer.Deserialize<TestSaveEnvelope>(eb012SaveBothJson, jsonOptions)!;
var restoredEb012 = new EvidenceBoardSession(3);
restoredEb012.PlayerNotes.ReplaceAll(PlayerNotesSaveMapper.PrepareRestore(loadedEb012Both.playerNotes, out int eb12SkipN));
restoredEb012.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb012Both.attachedSources, out int eb12SkipS));
restoredEb012.Timeline.ReplaceAll(TimelineSaveMapper.PrepareRestore(loadedEb012Both.timelineEvents, out int eb12SkipT));
Check(eb12SkipN == 0 && eb12SkipS == 0 && eb12SkipT == 0
    && restoredEb012.AttachedSources.Sources.Count == 2
    && restoredEb012.AttachedSources.ContainsSource(clue3SourceId, AttachedSourceType.Document)
    && restoredEb012.AttachedSources.ContainsSource(clue4SourceId, AttachedSourceType.Document)
    && restoredEb012.PlayerNotes.Notes.Count == 1 && restoredEb012.PlayerNotes.Notes[0].Text == eb012Note.Text
    && restoredEb012.Timeline.Events.Count == 3,
    "EB-012: Save/Load restores Clue 3, Clue 4, Player Note and Timeline");
Check(restoredEb012.AttachedSources.Remove(clue3Att!.AttachmentId) && restoredEb012.AttachedSources.Sources.Count == 1,
    "EB-012: remove Clue 3 copy");
string eb012SaveAfterRemoveJson = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 4,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(restoredEb012.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(restoredEb012.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(restoredEb012.Timeline)
}, jsonOptions);
var loadedEb012AfterRemove = JsonSerializer.Deserialize<TestSaveEnvelope>(eb012SaveAfterRemoveJson, jsonOptions)!;
restoredEb012.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb012AfterRemove.attachedSources, out _));
Check(restoredEb012.AttachedSources.Sources.Count == 1
    && !restoredEb012.AttachedSources.ContainsSource(clue3SourceId, AttachedSourceType.Document)
    && restoredEb012.AttachedSources.ContainsSource(clue4SourceId, AttachedSourceType.Document),
    "EB-012: Save/Load after removal does not revive deleted Clue 3 copy");
restoredEb012.AttachedSources.ReplaceAll(AttachedSourcesSaveMapper.PrepareRestore(loadedEb012AfterRemove.attachedSources, out _));
Check(restoredEb012.AttachedSources.Sources.Count == 1, "EB-012: repeated Load does not duplicate Clue 4");

// EB-013: Stage 5 Phase 1 — Manual Case Theory Draft (6 fields, Cyrillic/multiline, partial/full/clear, case isolation, v5 Save/Load, legacy v4 compat).
var theoryBoard = new EvidenceBoardSession(3);
Check(theoryBoard.CaseTheories.Drafts.Count == 0
    && !theoryBoard.CaseTheories.TryGetDraft(CaseTheorySession.DefaultCaseId, out _),
    "EB-013: new session starts with empty theory collection");
CaseTheoryDraft orionDraft = theoryBoard.CaseTheories.GetOrCreateDraft("CASE-ORION-01");
Check(orionDraft.IsEmpty && ReferenceEquals(orionDraft, theoryBoard.CaseTheories.GetOrCreateDraft("CASE-ORION-01")),
    "EB-013: GetOrCreateDraft returns single editable draft per Case ID");
Check(CaseTheorySaveMapper.ToSaveDtos(theoryBoard.CaseTheories).Length == 0,
    "EB-013: empty theory draft serializes cleanly as empty array");

// Частична и пълна теория, кирилица и много редове, редакция и изчистване на поле, разделяне по Case ID.
orionDraft.SetMainSuspect("Emily Carter");
Check(!orionDraft.IsEmpty && orionDraft.MainSuspect == "Emily Carter" && orionDraft.Motive == "",
    "EB-013: partial theory draft is accepted without validation errors");
orionDraft.SetMainSuspect("Michael Reed");
orionDraft.SetParticipantsAndRoles("Daniel може да е участвал в достъпа.\nТрябва да проверя ролята на Sarah.");
orionDraft.SetMotive("Финансов интерес — възможна продажба на прототипа.");
orionDraft.SetReconstruction("Някой е влязъл с временната карта.\nCCTV часовникът изостава с 12 минути.");
orionDraft.SetSupportingEvidence("Финансовият превод, календарът и журналът за достъп.");
orionDraft.SetUnresolvedQuestions("Кой физически е използвал временната карта?");

CaseTheoryDraft otherCaseDraft = theoryBoard.CaseTheories.GetOrCreateDraft("CASE-0017");
otherCaseDraft.SetMainSuspect("Томас Рийд");
Check(theoryBoard.CaseTheories.Drafts.Count == 2
    && theoryBoard.CaseTheories.GetOrCreateDraft("CASE-ORION-01").MainSuspect == "Michael Reed"
    && theoryBoard.CaseTheories.GetOrCreateDraft("CASE-0017").MainSuspect == "Томас Рийд",
    "EB-013: theories stay strictly isolated by Case ID");

// Редакция и изчистване на отделно поле.
orionDraft.SetMotive("Променен мотив: корпоративен шпионаж.");
orionDraft.SetUnresolvedQuestions("");
Check(orionDraft.Motive == "Променен мотив: корпоративен шпионаж." && orionDraft.UnresolvedQuestions == "",
    "EB-013: editing motive and clearing individual field works freely");

// v5 Save/Load JSON roundtrip + повторен Load без дубликати + стар v4 Save без caseTheories + санитизация на невалидни DTO.
string jsonV5 = JsonSerializer.Serialize(new TestSaveEnvelope
{
    version = 5,
    playerNotes = PlayerNotesSaveMapper.ToSaveDtos(theoryBoard.PlayerNotes),
    attachedSources = AttachedSourcesSaveMapper.ToSaveDtos(theoryBoard.AttachedSources),
    timelineEvents = TimelineSaveMapper.ToSaveDtos(theoryBoard.Timeline),
    caseTheories = CaseTheorySaveMapper.ToSaveDtos(theoryBoard.CaseTheories)
}, jsonOptions);
var loadedV5 = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonV5, jsonOptions)!;
List<CaseTheoryDraft> restoredV5Theories = CaseTheorySaveMapper.PrepareRestore(loadedV5.caseTheories, out int skippedV5Theories);
Check(skippedV5Theories == 0 && restoredV5Theories.Count == 2, "EB-013: valid v5 theory snapshot restores 2 case drafts");
var restoredTheoryBoard = new EvidenceBoardSession(3);
restoredTheoryBoard.CaseTheories.ReplaceAll(restoredV5Theories);
Check(restoredTheoryBoard.CaseTheories.TryGetDraft("CASE-ORION-01", out CaseTheoryDraft? rOrion) && rOrion != null
    && rOrion.MainSuspect == "Michael Reed"
    && rOrion.ParticipantsAndRoles == orionDraft.ParticipantsAndRoles
    && rOrion.Motive == "Променен мотив: корпоративен шпионаж."
    && rOrion.Reconstruction == orionDraft.Reconstruction
    && rOrion.SupportingEvidence == orionDraft.SupportingEvidence
    && rOrion.UnresolvedQuestions == ""
    && restoredTheoryBoard.CaseTheories.TryGetDraft("CASE-0017", out CaseTheoryDraft? r0017) && r0017 != null
    && r0017.MainSuspect == "Томас Рийд",
    "EB-013: Save/Load restores Cyrillic multiline text, cleared fields, and Case ID isolation");

restoredTheoryBoard.CaseTheories.ReplaceAll(CaseTheorySaveMapper.PrepareRestore(loadedV5.caseTheories, out _));
Check(restoredTheoryBoard.CaseTheories.Drafts.Count == 2, "EB-013: repeated Load does not duplicate theory drafts");

var legacyV4NoTheory = JsonSerializer.Deserialize<TestSaveEnvelope>(jsonV4, jsonOptions)!;
List<CaseTheoryDraft> restoredLegacyTheory = CaseTheorySaveMapper.PrepareRestore(legacyV4NoTheory.caseTheories, out int skippedLegacyTheory);
Check(skippedLegacyTheory == 0 && restoredLegacyTheory.Count == 0, "EB-013: legacy v4 save without caseTheories yields empty list");
restoredTheoryBoard.CaseTheories.ReplaceAll(restoredLegacyTheory);
Check(restoredTheoryBoard.CaseTheories.Drafts.Count == 0, "EB-013: loading legacy v4 clears previous theory drafts cleanly");

var mixedTheoryDtos = new[]
{
    new CaseTheorySaveDto { caseId = "", mainSuspect = "Празен ID" },
    new CaseTheorySaveDto { caseId = "   ", mainSuspect = "Whitespace ID" },
    new CaseTheorySaveDto { caseId = "CASE-ORION-01", mainSuspect = "Първи запис" },
    new CaseTheorySaveDto { caseId = "CASE-ORION-01", mainSuspect = "Дублиран ID" }
};
List<CaseTheoryDraft> sanitizedTheories = CaseTheorySaveMapper.PrepareRestore(mixedTheoryDtos, out int skippedMixedTheories);
Check(skippedMixedTheories == 3 && sanitizedTheories.Count == 1 && sanitizedTheories[0].MainSuspect == "Първи запис",
    "EB-013: invalid and duplicate case theory DTOs are skipped safely");

Console.WriteLine("EvidenceBoardSession checks: PASS");

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
}

sealed class TestSaveEnvelope
{
    public int version;
    public PlayerNoteSaveDto[] playerNotes = null!;
    public AttachedSourceSaveDto[] attachedSources = null!;
    public TimelineEventSaveDto[] timelineEvents = null!;
    public CaseTheorySaveDto[] caseTheories = null!;
}
