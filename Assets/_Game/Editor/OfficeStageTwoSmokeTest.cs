using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Контролиран Play Mode тест на state и persistence; не заменя физически UI test.
public static class OfficeStageTwoSmokeTest
{
    private const string Pending = "DetectiveGame.StageTwoSmoke";
    private const string SettingsKey = "office-settings-v1";
    private static double started;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Използвай batch Editor.");
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/DetectiveOffice.unity");
        SessionState.SetBool(Pending, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Reload()
    {
        if (SessionState.GetBool(Pending, false)) Subscribe();
    }

    private static void Subscribe()
    {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount < 3)
        {
            if (EditorApplication.timeSinceStartup - started > 90)
                Finish(1, "Play Mode timeout");
            return;
        }
        EditorApplication.update -= Tick;
        string savePath = Path.Combine(Application.persistentDataPath, "office-save-v1.json");
        bool hadSave = File.Exists(savePath);
        byte[] previousSave = hadSave ? File.ReadAllBytes(savePath) : null;
        bool hadSettings = PlayerPrefs.HasKey(SettingsKey);
        string previousSettings = hadSettings ? PlayerPrefs.GetString(SettingsKey) : null;
        int resultCode = 0;
        string resultMessage = "PASS";
        try
        {
            var interaction = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            var player = interaction.GetComponent<FirstPersonController>();
            var input = interaction.GetComponent<PlayerInputReader>();
            var pause = interaction.GetComponent<PauseUIController>();
            var save = interaction.GetComponent<OfficeSaveController>();
            var power = UnityEngine.Object.FindAnyObjectByType<ComputerPowerController>();
            Require(player != null && input != null && pause != null && save != null && power != null,
                "Office components initialize");
            Require(UnityEngine.Object.FindAnyObjectByType<OfficeSettingsUI>(FindObjectsInactive.Include) != null,
                "Settings runtime view exists");

            Vector3 original = player.transform.position;
            float yaw = player.transform.eulerAngles.y;
            float pitch = player.ViewPitch;
            ComputerPowerState computer = power.StableState;
            Require(save.TrySave(out string saveMessage), "Save succeeds: " + saveMessage);
            player.RestorePose(original + Vector3.right, yaw + 25f, pitch + 10f);
            power.RestoreStableState(ComputerPowerState.On);
            Require(save.TryLoad(out string loadMessage), "Load succeeds: " + loadMessage);
            Require(Vector3.Distance(player.transform.position, original) < .001f, "Player position restored");
            Require(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, yaw)) < .001f,
                "Player yaw restored");
            Require(Mathf.Abs(player.ViewPitch - pitch) < .001f, "View pitch restored");
            Require(power.StableState == computer, "Computer power restored");
            Require(interaction.ModeController.CurrentMode == InteractionModeController.Mode.World,
                "Load returns World mode");

            var phone = UnityEngine.Object.FindAnyObjectByType<PhoneInteractable>();
            phone.Interact();
            Require(interaction.ModeController.ActiveOwner == phone, "Phone owner entered");
            Require(save.TryLoad(out loadMessage), "Load from Phone succeeds: " + loadMessage);
            Require(interaction.ModeController.ActiveOwner == null
                && interaction.ModeController.CurrentMode == InteractionModeController.Mode.World,
                "Load releases Phone owner");

            var computerTool = UnityEngine.Object.FindAnyObjectByType<ComputerInteractable>();
            computerTool.Interact();
            Require(interaction.ModeController.ActiveOwner == computerTool, "Computer owner entered");
            Require(save.TryLoad(out loadMessage) && interaction.ModeController.ActiveOwner == null,
                "Load releases Computer owner");

            var boardTool = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardInteractable>();
            boardTool.Interact();
            Require(interaction.ModeController.ActiveOwner == boardTool, "Board owner entered");
            var boardUI = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardUIController>();
            var boardSession = (EvidenceBoardSession)typeof(EvidenceBoardUIController)
                .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
            var boardNotes = (TMP_InputField)typeof(EvidenceBoardUIController)
                .GetField("notesInput", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
            boardNotes.text = "Временна бележка";
            Require(save.TryLoad(out loadMessage) && interaction.ModeController.ActiveOwner == null,
                "Load releases Board owner");
            Require(boardNotes.text == "Временна бележка", "Load keeps session-only Board notes in memory");

            // EB-003: Save/Load на Player Notes, кирилица, позиции, изтриване, повторен Load, стар v1 Save и повреден Save.
            PlayerNote p1 = boardSession.PlayerNotes.Create(.31f, .72f);
            PlayerNote p2 = boardSession.PlayerNotes.Create(.68f, .64f);
            PlayerNote p3 = boardSession.PlayerNotes.Create(.44f, .31f);
            p1.SetText("Запазена бележка 1\nВтори ред");
            p2.SetText("Изтрита преди Save");
            p3.SetText("Запазена бележка 3");
            boardSession.PlayerNotes.Remove(p2.Id);
            Require(save.TrySave(out saveMessage), "Save with Player Notes succeeds: " + saveMessage);
            boardSession.PlayerNotes.Create(.50f, .50f).SetText("Незаписана временна бележка");
            Require(save.TryLoad(out loadMessage), "Load restores Player Notes: " + loadMessage);
            Require(boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.PlayerNotes.Notes[0].Id == p1.Id
                && boardSession.PlayerNotes.Notes[0].Text == "Запазена бележка 1\nВтори ред"
                && Mathf.Approximately(boardSession.PlayerNotes.Notes[0].NormalizedX, .31f)
                && Mathf.Approximately(boardSession.PlayerNotes.Notes[0].NormalizedY, .72f)
                && boardSession.PlayerNotes.Notes[1].Id == p3.Id
                && boardSession.PlayerNotes.Notes[1].Text == "Запазена бележка 3",
                "Player Notes IDs, Cyrillic multiline text, positions, and deletion persist across Save/Load");
            Require(save.TryLoad(out loadMessage) && boardSession.PlayerNotes.Notes.Count == 2,
                "Repeated Load does not duplicate Player Notes");
            string savedV2Json = File.ReadAllText(savePath);
            File.WriteAllText(savePath, "{\"version\":1,\"scene\":\"DetectiveOffice\",\"position\":{\"x\":0,\"y\":1,\"z\":0},\"yaw\":0,\"pitch\":0,\"computer\":0}");
            Require(save.TryLoad(out loadMessage) && boardSession.PlayerNotes.Notes.Count == 0,
                "Legacy v1 save without playerNotes loads cleanly and clears previous session notes");
            File.WriteAllText(savePath, savedV2Json);
            Require(save.TryLoad(out loadMessage) && boardSession.PlayerNotes.Notes.Count == 2,
                "Restoring v2 save brings back saved Player Notes");

            // EB-004: Manual Evidence Attachment System (Computer Document & Photo -> Board -> Detail -> Remove -> Save/Load).
            var computerUI = UnityEngine.Object.FindAnyObjectByType<ComputerUIController>(FindObjectsInactive.Include);
            var caseFiles = UnityEngine.Object.FindAnyObjectByType<CaseFilesUIController>(FindObjectsInactive.Include);
            var photosUI = UnityEngine.Object.FindAnyObjectByType<PhotosUIController>(FindObjectsInactive.Include);
            Require(computerUI != null && caseFiles != null && photosUI != null, "Computer controllers exist for EB-004");

            power.RestoreStableState(ComputerPowerState.On);
            computerTool.Interact();
            computerUI.OpenCaseFiles();
            Require(caseFiles.TryFindRecordBySourceId("doc-case0017-incident-report", out CaseData incidentCase, out CaseRecordData incidentRecord),
                "EB-004: stable sourceId resolves incident report");
            caseFiles.SelectCase(incidentCase);
            caseFiles.ShowRecords();
            caseFiles.SelectRecord(incidentRecord);
            Button attachDocButton = caseFiles.GetComponentsInChildren<Button>(true)
                .Single(b => b.name == "AttachCaseRecordToBoardButton");
            TMP_Text docAttachStatus = attachDocButton.transform.parent.Find("BoardAttachStatusText").GetComponent<TMP_Text>();
            attachDocButton.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == 1
                && docAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage
                && interaction.ModeController.ActiveOwner == computerTool,
                "EB-004: attaching document adds copy with neutral status and stays in Computer mode");
            attachDocButton.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == 1
                && docAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-004: re-attaching document shows neutral already-attached status without duplicate");

            // Подготвя временен Sprite за проверка на реално визуализиране на image asset при снимка.
            Require(photosUI.TryFindPhotoBySourceId("photo-case0017-parking-ticket", out PhotoData parkingPhoto),
                "EB-004: stable sourceId resolves parking photo");
            FieldInfo photoImageField = typeof(PhotoData).GetField("image", BindingFlags.Instance | BindingFlags.NonPublic);
            Sprite originalPhotoSprite = (Sprite)photoImageField.GetValue(parkingPhoto);
            Texture2D testTex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Sprite testSprite = Sprite.Create(testTex, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f));
            try
            {
                photoImageField.SetValue(parkingPhoto, testSprite);
                computerUI.OpenPhotos();
                photosUI.SelectPhoto(parkingPhoto);
                Button attachPhotoButton = photosUI.GetComponentsInChildren<Button>(true)
                    .Single(b => b.name == "AttachPhotoToBoardButton");
                TMP_Text photoAttachStatus = attachPhotoButton.transform.parent.Find("BoardAttachStatusText").GetComponent<TMP_Text>();
                attachPhotoButton.onClick.Invoke();
                Require(boardSession.AttachedSources.Sources.Count == 2
                    && photoAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                    "EB-004: attaching photo adds copy with neutral status");
                attachPhotoButton.onClick.Invoke();
                Require(boardSession.AttachedSources.Sources.Count == 2
                    && photoAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                    "EB-004: re-attaching photo avoids duplicates");
                computerUI.ExitComputer();

                boardTool.Interact();
                GameObject boardRootObj = (GameObject)typeof(EvidenceBoardUIController)
                    .GetField("boardRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
                AttachedSourceView[] attachedViews = boardRootObj.GetComponentsInChildren<AttachedSourceView>(true);
                Require(attachedViews.Length == 2, "EB-004: board renders both attached copies");
                AttachedSourceView docView = attachedViews.Single(v => v.SourceType == AttachedSourceType.Document);
                AttachedSourceView photoView = attachedViews.Single(v => v.SourceType == AttachedSourceType.Photo);
                Require(photoView.PhotoImage != null && photoView.PhotoImage.sprite == testSprite && photoView.PhotoImage.enabled,
                    "EB-004: photo copy renders real assigned image asset");

                docView.ApplyNormalizedPosition(.64f, .33f);
                docView.OpenDetails();
                GameObject detailPanel = boardRootObj.transform.Find("AttachedSourceDetailsPanel").gameObject;
                Require(detailPanel.activeSelf, "EB-004: document detail view opens");
                detailPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AttachedDetailBackButton").onClick.Invoke();
                Require(!detailPanel.activeSelf, "EB-004: back button closes document detail view");

                photoView.OpenDetails();
                Image detailPhotoImg = detailPanel.GetComponentsInChildren<Image>(true).Single(img => img.name == "DetailPhotoImage");
                Require(detailPanel.activeSelf && detailPhotoImg.sprite == testSprite && detailPhotoImg.enabled,
                    "EB-004: photo detail view renders real assigned image asset");
                detailPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AttachedDetailBackButton").onClick.Invoke();

                // EB-005: Investigation Workspace & Source Comparison (side-by-side preview + Player Notes).
                GameObject notesBoardObj = boardRootObj.transform.Find("PlayerNotesBoard").gameObject;
                GameObject boardPanelObj = (GameObject)typeof(EvidenceBoardUIController)
                    .GetField("boardPanel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
                RectTransform notesBoardRect = (RectTransform)notesBoardObj.transform;
                RectTransform previewPanelRect = (RectTransform)detailPanel.transform;
                RectTransform exitBoardRect = (RectTransform)boardRootObj.transform.Find("ExitBoardButton");
                Require(notesBoardRect.anchorMax.x < previewPanelRect.anchorMin.x
                    && exitBoardRect.anchorMax.y < previewPanelRect.anchorMin.y,
                    "EB-005: side-by-side preview does not overlap PlayerNotesBoard or ExitBoardButton");

                docView.OpenDetails();
                Require(detailPanel.activeSelf && notesBoardObj.activeInHierarchy && !boardPanelObj.activeSelf,
                    "EB-005: opening document keeps PlayerNotesBoard active side-by-side");
                TMP_Text previewTitle = detailPanel.transform.Find("InnerSurface/DetailTitle/Text").GetComponent<TMP_Text>();
                TMP_Text previewMeta = detailPanel.transform.Find("InnerSurface/DetailMeta/Text").GetComponent<TMP_Text>();
                TMP_Text previewDocBody = detailPanel.transform.Find("InnerSurface/DocumentDetailView/Viewport/Content").GetComponent<TMP_Text>();
                Require(previewTitle.text == incidentRecord.Title
                    && previewMeta.text.Contains("CASE-0017")
                    && previewDocBody.text == incidentRecord.Body,
                    "EB-005: document preview shows original unmodified title, source and body");

                // Създаване и редактиране на бележка с кирилица и много редове при отворен preview.
                int notesBeforeWorkspace = boardSession.PlayerNotes.Notes.Count;
                notesBoardObj.GetComponentsInChildren<Button>(true).Single(b => b.name == "NewPlayerNoteButton").onClick.Invoke();
                Require(boardSession.PlayerNotes.Notes.Count == notesBeforeWorkspace + 1
                    && boardSession.AttachedSources.Sources.Count == 2
                    && detailPanel.activeSelf && notesBoardObj.activeInHierarchy,
                    "EB-005: creating a note while preview is open keeps preview open without duplicating source");
                PlayerNote comparisonNote = boardSession.PlayerNotes.Notes[boardSession.PlayerNotes.Notes.Count - 1];
                PlayerNoteView comparisonView = notesBoardObj.GetComponentsInChildren<PlayerNoteView>(true)
                    .Single(v => v.NoteId == comparisonNote.Id);
                TMP_InputField comparisonInput = comparisonView.GetComponent<TMP_InputField>();
                const string comparisonText = "Този човек твърди, че е бил на среща,\nно часовете не съвпадат.";
                comparisonInput.text = comparisonText;
                Require(comparisonNote.Text == comparisonText,
                    "EB-005: editing note with Cyrillic multiline text updates model while preview is open");

                // Превключване между два прикачени източника без загуба на текста в бележката.
                photoView.OpenDetails();
                Require(detailPanel.activeSelf
                    && detailPhotoImg.sprite == testSprite && detailPhotoImg.enabled
                    && !detailPanel.transform.Find("InnerSurface/DocumentDetailView").gameObject.activeSelf
                    && detailPanel.transform.Find("InnerSurface/PhotoDetailView").gameObject.activeSelf
                    && comparisonNote.Text == comparisonText && comparisonInput.text == comparisonText,
                    "EB-005: switching from document to photo preview preserves note text");
                docView.OpenDetails();
                Require(detailPanel.activeSelf
                    && detailPanel.transform.Find("InnerSurface/DocumentDetailView").gameObject.activeSelf
                    && previewDocBody.text == incidentRecord.Body
                    && comparisonNote.Text == comparisonText && comparisonInput.text == comparisonText,
                    "EB-005: switching back to document preserves original body and note text");

                // Pause/Resume при отворен preview запазва preview и редактирания текст.
                typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, null);
                Require(interaction.ModeController.IsPaused && detailPanel.activeSelf && comparisonNote.Text == comparisonText,
                    "EB-005: Pause preserves open preview and edited note");
                pause.Resume();
                Require(!interaction.ModeController.IsPaused && detailPanel.activeSelf
                    && notesBoardObj.activeInHierarchy && comparisonNote.Text == comparisonText,
                    "EB-005: Resume preserves open preview, notes board, and edited note");

                // Drag и Delete на бележка при отворен preview.
                comparisonView.ApplyNormalizedPosition(.35f, .62f);
                Require(Mathf.Approximately(comparisonNote.NormalizedX, .35f)
                    && Mathf.Approximately(comparisonNote.NormalizedY, .62f)
                    && detailPanel.activeSelf,
                    "EB-005: dragging note while preview is open updates position and keeps preview open");

                // Затваряне на preview чрез X бутона не затваря дъската и връща трите оригинални evidence карти.
                detailPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AttachedDetailCloseIconButton").onClick.Invoke();
                Require(!detailPanel.activeSelf && boardPanelObj.activeSelf && boardUI.IsOpen
                    && comparisonNote.Text == comparisonText,
                    "EB-005: closing preview restores original evidence panel without closing Board or losing note text");

                // Exit/Re-entry пази бележката, а preview започва затворен.
                boardRootObj.transform.Find("ExitBoardButton").GetComponent<Button>().onClick.Invoke();
                Require(!boardUI.IsOpen && interaction.ModeController.ActiveOwner == null,
                    "EB-005: ExitBoardButton closes Board cleanly");
                boardTool.Interact();
                Require(boardUI.IsOpen && !detailPanel.activeSelf && boardPanelObj.activeSelf
                    && boardSession.PlayerNotes.Notes.Any(n => n.Id == comparisonNote.Id && n.Text == comparisonText),
                    "EB-005: Re-entry starts with preview closed and preserves comparison note");

                // Премахване на временната бележка при отворен preview за възстановяване на базовия брой бележки за EB-004 Save/Load проверката.
                docView = boardRootObj.GetComponentsInChildren<AttachedSourceView>(true).Single(v => v.SourceType == AttachedSourceType.Document);
                photoView = boardRootObj.GetComponentsInChildren<AttachedSourceView>(true).Single(v => v.SourceType == AttachedSourceType.Photo);
                docView.OpenDetails();
                comparisonView = notesBoardObj.GetComponentsInChildren<PlayerNoteView>(true).Single(v => v.NoteId == comparisonNote.Id);
                comparisonView.GetComponentsInChildren<Button>(true).Single(b => b.name == "DeleteNoteButton").onClick.Invoke();
                notesBoardObj.GetComponentsInChildren<Button>(true).Single(b => b.name == "ConfirmDeleteNoteButton").onClick.Invoke();
                Require(boardSession.PlayerNotes.Notes.Count == notesBeforeWorkspace && detailPanel.activeSelf,
                    "EB-005: deleting a note with confirmation works while preview stays open");
                detailPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AttachedDetailBackButton").onClick.Invoke();
                Require(!detailPanel.activeSelf && boardPanelObj.activeSelf,
                    "EB-005: closing preview with back button restores board panel");

                Require(save.TrySave(out saveMessage), "EB-004: Save with attached sources succeeds: " + saveMessage);
                string savedV3Json = File.ReadAllText(savePath);

                // Премахване с отказ и потвърждение без изтриване на оригинала в компютъра.
                photoView.GetComponentsInChildren<Button>(true).Single(b => b.name == "RemoveAttachedSourceButton").onClick.Invoke();
                boardRootObj.GetComponentsInChildren<Button>(true).Single(b => b.name == "CancelRemoveAttachedSourceButton").onClick.Invoke();
                Require(boardSession.AttachedSources.Sources.Count == 2, "EB-004: cancel remove keeps attached source");
                photoView.GetComponentsInChildren<Button>(true).Single(b => b.name == "RemoveAttachedSourceButton").onClick.Invoke();
                boardRootObj.GetComponentsInChildren<Button>(true).Single(b => b.name == "ConfirmRemoveAttachedSourceButton").onClick.Invoke();
                Require(boardSession.AttachedSources.Sources.Count == 1
                    && photosUI.TryFindPhotoBySourceId("photo-case0017-parking-ticket", out PhotoData stillOriginal)
                    && stillOriginal == parkingPhoto,
                    "EB-004: confirm remove removes board copy without deleting original photo");

                // Load възстановява и двете копия + позицията + Player Notes без дубликати.
                Require(save.TryLoad(out loadMessage), "EB-004: Load restores v3 save: " + loadMessage);
                Require(boardSession.AttachedSources.Sources.Count == 2
                    && Mathf.Approximately(boardSession.AttachedSources.Sources[0].NormalizedX, .64f)
                    && Mathf.Approximately(boardSession.AttachedSources.Sources[0].NormalizedY, .33f)
                    && boardSession.PlayerNotes.Notes.Count == 2,
                    "EB-004: Save/Load restores attached sources, positions, and Player Notes");
                Require(save.TryLoad(out loadMessage) && boardSession.AttachedSources.Sources.Count == 2,
                    "EB-004: repeated Load does not duplicate attached sources");

                // Липсващ оригинален sourceId в Save не крашва Load.
                File.WriteAllText(savePath, savedV3Json.Replace("doc-case0017-incident-report", "doc-missing-unknown-id"));
                Require(save.TryLoad(out loadMessage) && boardSession.AttachedSources.Sources.Count == 2,
                    "EB-004: missing original source ID during Load is handled safely without crash");

                // Стар v2 save без attachedSources поле се зарежда чисто и пази Player Notes.
                string legacyV2NoAttachmentsJson = "{\"version\":2,\"scene\":\"DetectiveOffice\",\"position\":{\"x\":0,\"y\":1,\"z\":0},\"yaw\":0,\"pitch\":0,\"computer\":0,\"playerNotes\":[{\"id\":\"legacy-v2-note\",\"text\":\"От v2\",\"normalizedX\":0.25,\"normalizedY\":0.75,\"order\":0}]}";
                File.WriteAllText(savePath, legacyV2NoAttachmentsJson);
                Require(save.TryLoad(out loadMessage)
                    && boardSession.AttachedSources.Sources.Count == 0
                    && boardSession.PlayerNotes.Notes.Count == 1
                    && boardSession.PlayerNotes.Notes[0].Id == "legacy-v2-note",
                    "EB-004: legacy v2 save without attachedSources clears attachments and keeps Player Notes");

                File.WriteAllText(savePath, savedV3Json);
                Require(save.TryLoad(out loadMessage) && boardSession.AttachedSources.Sources.Count == 2,
                    "EB-004: restoring v3 save brings back attached sources");
            }
            finally
            {
                photoImageField.SetValue(parkingPhoto, originalPhotoSprite);
                UnityEngine.Object.DestroyImmediate(testSprite);
                UnityEngine.Object.DestroyImmediate(testTex);
            }

            // EB-006: Manual Investigation Timeline (UI create/edit/delete, sort, midnight, ties, untimed, validation, Board<->Timeline, Pause, Exit, Save/Load).
            boardTool.Interact();
            GameObject tlBoardRoot = (GameObject)typeof(EvidenceBoardUIController)
                .GetField("boardRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
            GameObject tlPanel = tlBoardRoot.transform.Find("InvestigationTimelinePanel").gameObject;
            Button timelineTab = tlBoardRoot.transform.Find("TimelineModeTabButton").GetComponent<Button>();
            Button boardTab = tlBoardRoot.transform.Find("BoardModeTabButton").GetComponent<Button>();
            timelineTab.onClick.Invoke();
            Require(tlPanel.activeSelf && !tlBoardRoot.transform.Find("PlayerNotesBoard").gameObject.activeSelf,
                "EB-006: Timeline tab opens InvestigationTimelinePanel and hides corkboard");

            TMP_InputField tlDate = tlPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TimelineDateInput");
            TMP_InputField tlTime = tlPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TimelineTimeInput");
            TMP_InputField tlTitle = tlPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TimelineTitleInput");
            TMP_InputField tlDesc = tlPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TimelineDescriptionInput");
            Button tlSaveBtn = tlPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "SaveTimelineEventButton");
            TMP_Text tlValText = tlPanel.transform.Find("InnerSurface/TimelineEditorPanel/TimelineValidationText/Text").GetComponent<TMP_Text>();

            // Невалидна дата и час се отказват.
            tlDate.text = "2003-02-30"; tlTime.text = "25:99"; tlTitle.text = "Невалидно"; tlDesc.text = "";
            tlSaveBtn.onClick.Invoke();
            Require(boardSession.Timeline.Events.Count == 0 && !string.IsNullOrEmpty(tlValText.text),
                "EB-006: invalid date/time rejected in UI with validation text");

            // Създаване на събития извън хронологичен ред (преди/след полунощ, еднакъв час, без час, кирилица и много редове).
            tlDate.text = "2003-10-15"; tlTime.text = "00:05"; tlTitle.text = "След полунощ"; tlDesc.text = "Излизане от сградата.";
            tlSaveBtn.onClick.Invoke();
            tlDate.text = "2003-10-14"; tlTime.text = "23:45"; tlTitle.text = "CCTV вход"; tlDesc.text = "Човек с тъмно яке.\nВтори ред.";
            tlSaveBtn.onClick.Invoke();
            tlDate.text = "2003-10-14"; tlTime.text = "23:45"; tlTitle.text = "Шум в коридора"; tlDesc.text = "Еднакъв час.";
            tlSaveBtn.onClick.Invoke();
            tlDate.text = "2003-10-14"; tlTime.text = ""; tlTitle.text = "Неуточнен час"; tlDesc.text = "Без час.";
            tlSaveBtn.onClick.Invoke();
            Require(boardSession.Timeline.Events.Count == 4
                && boardSession.Timeline.Events[0].Title == "CCTV вход"
                && boardSession.Timeline.Events[1].Title == "Шум в коридора"
                && boardSession.Timeline.Events[2].Title == "Неуточнен час" && !boardSession.Timeline.Events[2].HasTime
                && boardSession.Timeline.Events[3].Title == "След полунощ",
                "EB-006: UI creates and sorts events chronologically with ties, midnight and untimed group");
            Require(tlPanel.transform.Find("InnerSurface/TimelineListPanel/Viewport/Content/TimelineUntimedGroupHeader-2003-10-14") != null,
                "EB-006: untimed events show dedicated date sub-group header");

            // Редактиране на час и описание преподрежда събитието.
            TimelineEventView cctvCard = tlPanel.GetComponentsInChildren<TimelineEventView>(true).Single(v => v.Model.Title == "CCTV вход");
            cctvCard.GetComponentsInChildren<Button>(true).Single(b => b.name == "EditTimelineEventButton").onClick.Invoke();
            tlTime.text = "23:57";
            tlDesc.text = "Коригирано +12 мин.\nВтори ред.";
            tlSaveBtn.onClick.Invoke();
            Require(boardSession.Timeline.Events[0].Title == "Шум в коридора"
                && boardSession.Timeline.Events[1].Title == "CCTV вход"
                && boardSession.Timeline.Events[1].Time == "23:57",
                "EB-006: editing event time re-sorts card chronologically");

            // Изтриване с отказ и потвърждение.
            TimelineEventView noiseCard = tlPanel.GetComponentsInChildren<TimelineEventView>(true).Single(v => v.Model.Title == "Шум в коридора");
            noiseCard.GetComponentsInChildren<Button>(true).Single(b => b.name == "DeleteTimelineEventButton").onClick.Invoke();
            tlPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "CancelDeleteTimelineEventButton").onClick.Invoke();
            Require(boardSession.Timeline.Events.Count == 4, "EB-006: canceling delete preserves timeline event");
            noiseCard.GetComponentsInChildren<Button>(true).Single(b => b.name == "DeleteTimelineEventButton").onClick.Invoke();
            tlPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "ConfirmDeleteTimelineEventButton").onClick.Invoke();
            Require(boardSession.Timeline.Events.Count == 3
                && tlPanel.GetComponentsInChildren<TimelineEventView>(true).Length == 3,
                "EB-006: confirming delete removes only selected timeline event");

            // Превключване Board <-> Timeline не дублира елементи и пази Player Notes & Attached Sources.
            boardTab.onClick.Invoke();
            Require(!tlPanel.activeSelf && tlBoardRoot.transform.Find("PlayerNotesBoard").gameObject.activeSelf
                && boardSession.PlayerNotes.Notes.Count == 2 && boardSession.AttachedSources.Sources.Count == 2,
                "EB-006: switching back to Board preserves Player Notes and Attached Sources");
            timelineTab.onClick.Invoke();
            Require(tlPanel.activeSelf && tlPanel.GetComponentsInChildren<TimelineEventView>(true).Length == 3,
                "EB-006: reopening Timeline does not duplicate UI elements");

            // Pause/Resume при отворена времева линия.
            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, null);
            Require(interaction.ModeController.IsPaused && tlPanel.activeSelf, "EB-006: Pause preserves open Timeline");
            pause.Resume();
            Require(!interaction.ModeController.IsPaused && tlPanel.activeSelf && boardSession.Timeline.Events.Count == 3,
                "EB-006: Resume restores Timeline with events intact");

            // Save от отворена Timeline + Exit/Re-entry + Load + стар v3 Save без timelineEvents + повторен Load.
            Require(save.TrySave(out saveMessage), "EB-006: Save from Timeline succeeds: " + saveMessage);
            string savedV4Json = File.ReadAllText(savePath);
            tlBoardRoot.transform.Find("ExitBoardButton").GetComponent<Button>().onClick.Invoke();
            Require(!boardUI.IsOpen && interaction.ModeController.ActiveOwner == null, "EB-006: Exit from Timeline restores World");
            boardTool.Interact();
            timelineTab.onClick.Invoke();
            Require(boardSession.Timeline.Events.Count == 3, "EB-006: Re-entry preserves timeline events");

            // Временно събитие преди Load не трябва да остава след Load.
            tlDate.text = "2003-10-16"; tlTime.text = "09:00"; tlTitle.text = "Незаписано"; tlDesc.text = "";
            tlSaveBtn.onClick.Invoke();
            Require(boardSession.Timeline.Events.Count == 4, "EB-006: temporary unsaved event created");
            Require(save.TryLoad(out loadMessage)
                && boardSession.Timeline.Events.Count == 3
                && boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.AttachedSources.Sources.Count == 2,
                "EB-006: Load restores v4 Timeline, Player Notes and Attached Sources without unsaved event");
            Require(save.TryLoad(out loadMessage) && boardSession.Timeline.Events.Count == 3,
                "EB-006: repeated Load does not duplicate timeline events");

            // Стар v3 Save без timelineEvents се зарежда чисто и изчиства времевата линия.
            string legacyV3NoTimelineJson = "{\"version\":3,\"scene\":\"DetectiveOffice\",\"position\":{\"x\":0,\"y\":1,\"z\":0},\"yaw\":0,\"pitch\":0,\"computer\":0,\"playerNotes\":[{\"id\":\"v3-note\",\"text\":\"От v3\",\"normalizedX\":0.25,\"normalizedY\":0.75,\"order\":0}],\"attachedSources\":[]}";
            File.WriteAllText(savePath, legacyV3NoTimelineJson);
            Require(save.TryLoad(out loadMessage)
                && boardSession.Timeline.Events.Count == 0
                && boardSession.PlayerNotes.Notes.Count == 1,
                "EB-006: legacy v3 save without timelineEvents clears timeline and keeps Player Notes");

            File.WriteAllText(savePath, savedV4Json);
            Require(save.TryLoad(out loadMessage)
                && boardSession.Timeline.Events.Count == 3
                && boardSession.PlayerNotes.Notes.Count == 2,
                "EB-006: restoring v4 save brings back Timeline, Player Notes and Attached Sources");

            // EB-007: Timeline visual layout non-overlap (long title + multiline description, clean card sprite, rail outside card/text).
            boardTool.Interact();
            timelineTab.onClick.Invoke();
            tlDate.text = "2011-10-14";
            tlTime.text = "23:20";
            tlTitle.text = "Много дълго заглавие на събитие за проверка на пренасянето и разстоянието до линията на времевата ос";
            tlDesc.text = "Първи ред описание за сравнение на източници.\nВтори ред с по-дълъг текст на кирилица.\nТрети ред за проверка на височината.";
            tlSaveBtn.onClick.Invoke();
            RectTransform tlFeedContent = tlPanel.transform.Find("InnerSurface/TimelineListPanel/Viewport/Content") as RectTransform;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tlFeedContent);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tlFeedContent);

            TimelineEventView longCard = tlPanel.GetComponentsInChildren<TimelineEventView>(true)
                .Single(v => v.Model.Date == "2011-10-14" && v.Model.Time == "23:20");
            Image longCardBg = longCard.transform.Find("EventCard").GetComponent<Image>();
            Type visualsType = typeof(TimelineEventView).Assembly.GetType("EvidenceBoardVisuals");
            Sprite expectedTimelineSprite = (Sprite)visualsType.GetProperty("TimelineCardPaperSprite", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            Require(longCardBg.sprite == expectedTimelineSprite,
                "EB-007: Timeline EventCard uses clean paper sprite without baked horizontal rule");

            Rect railBounds = GetWorldRect((RectTransform)longCard.transform.Find("TimelineRail"));
            Rect lineBounds = GetWorldRect((RectTransform)longCard.transform.Find("TimelineRail/VerticalLine"));
            Rect nodeBounds = GetWorldRect((RectTransform)longCard.transform.Find("TimelineRail/Node"));
            Rect cardBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard"));
            Rect headerBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard/HeaderRow"));
            Rect timeBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard/HeaderRow/EventTime"));
            Rect titleBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard/HeaderRow/EventTitle"));
            Rect editBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard/HeaderRow/EditTimelineEventButton"));
            Rect deleteBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard/HeaderRow/DeleteTimelineEventButton"));
            Rect descBounds = GetWorldRect((RectTransform)longCard.transform.Find("EventCard/EventDescription"));

            Require(railBounds.xMax < cardBounds.xMin && lineBounds.xMax < cardBounds.xMin && nodeBounds.xMax < cardBounds.xMin,
                "EB-007: Timeline rail, vertical line and node stay strictly outside EventCard with positive gap");
            Require(lineBounds.xMax < timeBounds.xMin && lineBounds.xMax < titleBounds.xMin && lineBounds.xMax < descBounds.xMin,
                "EB-007: Timeline line never overlaps time, title or description text");
            Require(timeBounds.xMax <= titleBounds.xMin + 0.5f && titleBounds.xMax <= editBounds.xMin + 0.5f && editBounds.xMax <= deleteBounds.xMin + 0.5f,
                "EB-007: Timeline header elements do not overlap horizontally with long title");
            Require(descBounds.yMax <= headerBounds.yMin + 0.5f,
                "EB-007: Timeline description sits below HeaderRow without vertical overlap");

            // Почистване на тестовото EB-007 събитие чрез възстановяване на v4 save.
            Require(save.TryLoad(out loadMessage) && boardSession.Timeline.Events.Count == 3,
                "EB-007: restoring v4 save after visual check preserves clean state");

            // EB-007B: Project Orion Stage 1 content (Initial Security Report & CCTV Observation) end-to-end verification.
            const string orionCaseNumber = "CASE-ORION-01";
            const string orionSecId = "doc-orion-initial-security-report";
            const string orionCctvId = "doc-orion-cctv-observation";
            var allCases = (List<CaseData>)typeof(CaseFilesUIController)
                .GetField("cases", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(caseFiles);
            var allPhotos = (List<PhotoData>)typeof(PhotosUIController)
                .GetField("photos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(photosUI);
            Require(allCases.Any(c => c.CaseNumber == orionCaseNumber)
                && allCases.Any(c => c.CaseNumber == "CASE-0017")
                && allCases.Any(c => c.CaseNumber == "CASE-0012"),
                "EB-007B: Project Orion case exists alongside untouched legacy CASE-0017 and CASE-0012");

            var seenSourceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CaseData c in allCases)
            {
                foreach (CaseRecordData r in c.Records)
                {
                    Require(!string.IsNullOrWhiteSpace(r.SourceId) && seenSourceIds.Add(r.SourceId),
                        "EB-007B: every CaseRecordData has a non-empty globally unique sourceId: " + r.SourceId);
                }
            }
            foreach (PhotoData p in allPhotos)
            {
                Require(!string.IsNullOrWhiteSpace(p.SourceId) && seenSourceIds.Add(p.SourceId),
                    "EB-007B: every PhotoData has a non-empty globally unique sourceId: " + p.SourceId);
            }

            Require(caseFiles.TryFindRecordBySourceId(orionSecId, out CaseData orionCase, out CaseRecordData orionSecRecord),
                "EB-007B: Project Orion Initial Security Report resolves by stable sourceId");
            Require(caseFiles.TryFindRecordBySourceId(orionCctvId, out CaseData orionCase2, out CaseRecordData orionCctvRecord),
                "EB-007B: Project Orion CCTV Observation resolves by stable sourceId");
            Require(ReferenceEquals(orionCase, orionCase2)
                && orionCase.CaseNumber == orionCaseNumber
                && orionCase.People.Count == 4
                && orionCase.Records.Count == 23,
                "EB-007B..EB-012: Project Orion exposes 4 personnel entries and 23 records (all 17 Clues + 4 personnel files + 2 Stage 1)");

            // Проверка на фактологическата точност и липсата на късни разкрития / неодобрени календарни дати.
            string secBody = orionSecRecord.Body;
            Require(secBody.Contains("Blackwood Technologies")
                && secBody.Contains("Project Orion")
                && secBody.Contains("23:30")
                && secBody.Contains("23:45")
                && secBody.Contains("00:10")
                && secBody.Contains("Алармата не се е задействала")
                && secBody.Contains("заключена")
                && string.IsNullOrEmpty(orionSecRecord.Date),
                "EB-007B: Initial Security Report preserves all Clue 1 facts without unapproved calendar date");

            string cctvBody = orionCctvRecord.Body;
            Require(cctvBody.Contains("Текстова справка")
                && cctvBody.Contains("тъмно яке")
                && cctvBody.Contains("качулка")
                && cctvBody.Contains("малка чанта")
                && cctvBody.Contains("23:45")
                && cctvBody.Contains("23:52")
                && cctvBody.Contains("не се разпознава")
                && cctvBody.Contains("Не се виждат други хора")
                && string.IsNullOrEmpty(orionCctvRecord.Date),
                "EB-007B: CCTV Observation preserves all Clue 2 facts as text summary without unapproved calendar date");

            // EB-008: проверка на четирите служебни досиета, показанията им и Clue 5 (Employee Access Records).
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-personnel-daniel-brooks", out CaseData danielCase, out CaseRecordData danielRecord),
                "EB-008: Daniel Brooks personnel record resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-personnel-emily-carter", out CaseData emilyCase, out CaseRecordData emilyRecord),
                "EB-008: Emily Carter personnel record resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-personnel-michael-reed", out CaseData michaelCase, out CaseRecordData michaelRecord),
                "EB-008: Michael Reed personnel record resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-personnel-sarah-mitchell", out CaseData sarahCase, out CaseRecordData sarahRecord),
                "EB-008: Sarah Mitchell personnel record resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-employee-access-records", out CaseData accessCase, out CaseRecordData accessRecord),
                "EB-008: Employee Access Records (Clue 5) resolves");
            Require(ReferenceEquals(orionCase, danielCase) && ReferenceEquals(orionCase, emilyCase)
                && ReferenceEquals(orionCase, michaelCase) && ReferenceEquals(orionCase, sarahCase)
                && ReferenceEquals(orionCase, accessCase),
                "EB-008: all new records belong to CASE-ORION-01");

            Require(orionCase.People.Any(p => p.Name == "Daniel Brooks" && p.Role.Contains("Security Technician"))
                && orionCase.People.Any(p => p.Name == "Emily Carter" && p.Role.Contains("Research Scientist"))
                && orionCase.People.Any(p => p.Name == "Michael Reed" && p.Role.Contains("Project Manager"))
                && orionCase.People.Any(p => p.Name == "Sarah Mitchell" && p.Role.Contains("Administrative Manager")),
                "EB-008: all 4 employees exist in People tab with accurate names and roles");

            Require(danielRecord.Body.Contains("Daniel Brooks") && danielRecord.Body.Contains("35") && danielRecord.Body.Contains("Security Technician") && danielRecord.Body.Contains("22:00")
                && emilyRecord.Body.Contains("Emily Carter") && emilyRecord.Body.Contains("29") && emilyRecord.Body.Contains("Research Scientist") && emilyRecord.Body.Contains("23:20")
                && michaelRecord.Body.Contains("Michael Reed") && michaelRecord.Body.Contains("42") && michaelRecord.Body.Contains("Project Manager") && michaelRecord.Body.Contains("полунощ")
                && sarahRecord.Body.Contains("Sarah Mitchell") && sarahRecord.Body.Contains("34") && sarahRecord.Body.Contains("Administrative Manager") && sarahRecord.Body.Contains("вкъщи"),
                "EB-008: personnel records contain accurate names, ages, roles and initial statements");

            Require(accessRecord.Body.Contains("22:15") && accessRecord.Body.Contains("Daniel Brooks")
                && accessRecord.Body.Contains("23:20") && accessRecord.Body.Contains("Emily Carter")
                && accessRecord.Body.Contains("21:40") && accessRecord.Body.Contains("Michael Reed")
                && accessRecord.Body.Contains("Sarah Mitchell") && accessRecord.Body.Contains("временна карта") && accessRecord.Body.Contains("върната")
                && string.IsNullOrEmpty(accessRecord.Date),
                "EB-008: Clue 5 Employee Access Records contains all confirmed times and facts without unapproved calendar date");

            // EB-009: проверка на Clue 6 (3 комуникации) и Clue 7 (изтрита история на Daniel Brooks).
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-comm-daniel-unknown", out CaseData commACase, out CaseRecordData commARecord),
                "EB-009: Communication A (Daniel / Unknown) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-comm-emily-michael", out CaseData commBCase, out CaseRecordData commBRecord),
                "EB-009: Communication B (Emily / Michael) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-comm-sarah-unknown", out CaseData commCCase, out CaseRecordData commCRecord),
                "EB-009: Communication C (Sarah / Unknown) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-daniel-deleted-history", out CaseData historyCase, out CaseRecordData historyRecord),
                "EB-009: Daniel deleted computer history (Clue 7) resolves");
            Require(ReferenceEquals(orionCase, commACase) && ReferenceEquals(orionCase, commBCase)
                && ReferenceEquals(orionCase, commCCase) && ReferenceEquals(orionCase, historyCase),
                "EB-009: all 4 communication/history records belong to CASE-ORION-01");

            Require(commARecord.Body.Contains("Daniel Brooks") && commARecord.Body.Contains("Неизвестен")
                && commARecord.Body.Contains("23:05") && commARecord.Body.Contains("Всичко е готово. Системата няма да засече нищо.")
                && commARecord.Body.Contains("23:07") && commARecord.Body.Contains("Погрижи се никой да не те види.")
                && string.IsNullOrEmpty(commARecord.Date),
                "EB-009: Communication A preserves participants, exact times and Bulgarian translation without calendar date");
            Require(commBRecord.Body.Contains("Emily Carter") && commBRecord.Body.Contains("Michael Reed")
                && commBRecord.Body.Contains("21:15") && commBRecord.Body.Contains("Знам какво си направил с бюджета за изследвания.")
                && commBRecord.Body.Contains("21:17") && commBRecord.Body.Contains("Трябва да си държиш устата затворена.")
                && string.IsNullOrEmpty(commBRecord.Date),
                "EB-009: Communication B preserves participants, exact times and Bulgarian translation without calendar date");
            Require(commCRecord.Body.Contains("Sarah Mitchell") && commCRecord.Body.Contains("Неизвестен")
                && commCRecord.Body.Contains("22:50") && commCRecord.Body.Contains("Дадох ти това, което поиска. Това е последният път.")
                && string.IsNullOrEmpty(commCRecord.Date),
                "EB-009: Communication C preserves participants, exact time and Bulgarian translation without calendar date");
            Require(historyRecord.Body.Contains("Daniel Brooks")
                && historyRecord.Body.Contains("Как временно да се деактивират електронните известия за достъп, без да се задейства аларма.")
                && string.IsNullOrEmpty(historyRecord.Date),
                "EB-009: Clue 7 deleted computer history preserves exact Bulgarian query without invented metadata");

            // EB-010: проверка на Stage 3 (Clue 8-12).
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-michael-deleted-email", out CaseData clue8Case, out CaseRecordData clue8Record),
                "EB-010: Clue 8 (Michael deleted email) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-financial-transfer-orion-solutions", out CaseData clue9Case, out CaseRecordData clue9Record),
                "EB-010: Clue 9 (Financial transfer to Orion Solutions) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-voice-recording-transcript", out CaseData clue10Case, out CaseRecordData clue10Record),
                "EB-010: Clue 10 (Voice recording transcript) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-emily-laboratory-notes", out CaseData clue11Case, out CaseRecordData clue11Record),
                "EB-010: Clue 11 (Emily laboratory notes) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-hotel-photograph-description", out CaseData clue12Case, out CaseRecordData clue12Record),
                "EB-010: Clue 12 (Hotel photograph description) resolves");
            Require(ReferenceEquals(orionCase, clue8Case) && ReferenceEquals(orionCase, clue9Case)
                && ReferenceEquals(orionCase, clue10Case) && ReferenceEquals(orionCase, clue11Case)
                && ReferenceEquals(orionCase, clue12Case),
                "EB-010: all 5 Stage 3 records belong to CASE-ORION-01");

            Require(clue8Record.Body.Contains("Michael Reed") && clue8Record.Body.Contains("2 милиона евро")
                && clue8Record.Body.Contains("два дни преди кражбата") && !clue8Record.Body.Contains("@")
                && string.IsNullOrEmpty(clue8Record.Date),
                "EB-010: Clue 8 preserves original deleted email facts without invented email address or calendar date");
            Require(clue9Record.Body.Contains("Michael Reed") && clue9Record.Body.Contains("Orion Solutions")
                && clue9Record.Body.Contains("15 000 евро") && clue9Record.Body.Contains("пет дни преди кражбата")
                && clue9Record.Body.Contains("две седмици по-рано") && string.IsNullOrEmpty(clue9Record.Date),
                "EB-010: Clue 9 preserves original financial transfer facts without invented account/date");
            Require(clue10Record.Body.Contains("Препис") && clue10Record.Body.Contains("два мъжки гласа")
                && clue10Record.Body.Contains("Купувачът иска потвърждение преди полунощ.")
                && clue10Record.Body.Contains("Не се притеснявай. Имам човек вътре.")
                && clue10Record.Body.Contains("След като прототипът бъде доставен.")
                && string.IsNullOrEmpty(clue10Record.Date),
                "EB-010: Clue 10 is clearly labeled as transcript and preserves exact original lines");
            Require(clue11Record.Body.Contains("Emily Carter") && clue11Record.Body.Contains("търговско производство")
                && clue11Record.Body.Contains("контролираната си среда") && clue11Record.Body.Contains("нестабилни")
                && string.IsNullOrEmpty(clue11Record.Date),
                "EB-010: Clue 11 preserves Emily's original laboratory notes");
            Require(clue12Record.Body.Contains("Текстово описание на фотография") && clue12Record.Body.Contains("Michael Reed")
                && clue12Record.Body.Contains("пред хотел") && clue12Record.Body.Contains("Orion Solutions")
                && clue12Record.Body.Contains("Окончателно споразумение. 11 октомври.")
                && string.IsNullOrEmpty(clue12Record.Date),
                "EB-010: Clue 12 is clearly labeled as text description and preserves exact original facts");

            // EB-011: проверка на Stage 4 (Clue 13-17).
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-cctv-clock-discrepancy", out CaseData clue13Case, out CaseRecordData clue13Record),
                "EB-011: Clue 13 (CCTV clock discrepancy) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-daniel-phone-location", out CaseData clue14Case, out CaseRecordData clue14Record),
                "EB-011: Clue 14 (Daniel phone location) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-emily-parking-message", out CaseData clue15Case, out CaseRecordData clue15Record),
                "EB-011: Clue 15 (Emily parking message) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-temp-card-registration", out CaseData clue16Case, out CaseRecordData clue16Record),
                "EB-011: Clue 16 (Temporary card registration) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-michael-modified-calendar", out CaseData clue17Case, out CaseRecordData clue17Record),
                "EB-011: Clue 17 (Michael modified calendar) resolves");
            Require(ReferenceEquals(orionCase, clue13Case) && ReferenceEquals(orionCase, clue14Case)
                && ReferenceEquals(orionCase, clue15Case) && ReferenceEquals(orionCase, clue16Case)
                && ReferenceEquals(orionCase, clue17Case),
                "EB-011: all 5 Stage 4 records belong to CASE-ORION-01");

            Require(clue13Record.Body.Contains("изостава с 12 минути")
                && !clue13Record.Body.Contains("23:57") && !clue13Record.Body.Contains("00:04")
                && orionCctvRecord.Body.Contains("23:45") && orionCctvRecord.Body.Contains("23:52")
                && string.IsNullOrEmpty(clue13Record.Date),
                "EB-011: Clue 13 reports 12-minute slow offset without auto-computing corrected times or modifying Clue 2");
            Require(clue14Record.Body.Contains("Daniel Brooks") && clue14Record.Body.Contains("22:00")
                && clue14Record.Body.Contains("23:30") && clue14Record.Body.Contains("23:55") && clue14Record.Body.Contains("00:20")
                && !clue14Record.Body.Contains("GPS") && string.IsNullOrEmpty(clue14Record.Date),
                "EB-011: Clue 14 preserves exact phone location times without invented GPS/addresses/interpretation");
            Require(clue15Record.Body.Contains("Emily Carter") && clue15Record.Body.Contains("23:48")
                && clue15Record.Body.Contains("Забравих си бележника. Ще се върна утре.")
                && string.IsNullOrEmpty(clue15Record.Date),
                "EB-011: Clue 15 preserves exact 23:48 parking message without inventing recipient or guilt");
            Require(clue16Record.Body.Contains("Sarah Mitchell") && clue16Record.Body.Contains("Daniel Brooks")
                && clue16Record.Body.Contains("23:56") && clue16Record.Body.Contains("00:05")
                && !accessRecord.Body.Contains("23:56") && string.IsNullOrEmpty(clue16Record.Date),
                "EB-011: Clue 16 preserves temporary card registration/times without modifying Clue 5");
            Require(clue17Record.Body.Contains("Michael Reed") && clue17Record.Body.Contains("22:30")
                && clue17Record.Body.Contains("отменена") && clue17Record.Body.Contains("ръчно редактиран")
                && string.IsNullOrEmpty(clue17Record.Date),
                "EB-011: Clue 17 preserves 22:30 cancellation and manual edit without automatic lie verdict");

            // EB-012: проверка на липсващите Clue 3 (Torn Paper) и Clue 4 (Security System Log).
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-torn-paper-record", out CaseData clue3Case, out CaseRecordData clue3Record),
                "EB-012: Clue 3 (Torn Paper archival record) resolves");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-security-system-log", out CaseData clue4Case, out CaseRecordData clue4Record),
                "EB-012: Clue 4 (Security System Log) resolves");
            Require(ReferenceEquals(orionCase, clue3Case) && ReferenceEquals(orionCase, clue4Case),
                "EB-012: Clue 3 and Clue 4 belong to CASE-ORION-01");
            Require(clue3Record.Title == "Откъснат лист — лабораторна врата"
                && clue3Record.RecordType == "Архивен запис за веществено доказателство"
                && clue3Record.Body.Contains("Доставката трябва да се извърши преди полунощ. Не се свързвай повече с мен.")
                && clue3Record.Body.Contains("Подпис: липсва.")
                && clue3Record.Body.Contains("Автор: неизвестен.")
                && string.IsNullOrEmpty(clue3Record.Date),
                "EB-012: Clue 3 preserves exact handwritten message, no signature, unknown author and archival framing");
            Require(clue4Record.Title == "Журнал на системата за сигурност — лаборатория"
                && clue4Record.RecordType == "Технически запис за електронна брава"
                && clue4Record.Body.Contains("23:45 — Лабораторната врата е отключена.")
                && clue4Record.Body.Contains("23:52 — Лабораторната врата е заключена отново.")
                && clue4Record.Body.Contains("00:10 — Охраната извършва проверка на помещението.")
                && clue4Record.Body.Contains("служебна карта за достъп на служител")
                && !clue4Record.Body.Contains("Daniel") && !clue4Record.Body.Contains("Michael")
                && !clue4Record.Body.Contains("Sarah") && !clue4Record.Body.Contains("Emily")
                && string.IsNullOrEmpty(clue4Record.Date),
                "EB-012: Clue 4 preserves exact electronic lock timestamps and employee card confirmation without naming a suspect");

            string combinedOrionText = orionCase.CaseName + "\n" + orionCase.Summary + "\n" + orionCase.DateOpened + "\n"
                + string.Join("\n", orionCase.People.Select(p => p.Name + " " + p.Role + " " + p.ShortDescription)) + "\n"
                + string.Join("\n", orionCase.Records.Select(r => r.Title + " " + r.RecordType + " " + r.Date + " " + r.Body));
            foreach (string forbidden in new[]
            {
                "2026", "2003", "IP ", "GPS",
                "Suspect identified", "Suspect confirmed", "Lie detected", "Correct deduction", "New motive discovered",
                "Michael is suspicious", "Evidence linked", "Accomplice identified", "виновен", "mastermind", "организатор",
                "Daniel е излъгал", "Michael е излъгал", "Доказателство за вина", "Открита връзка", "Правилно заключение"
            })
            {
                Require(!combinedOrionText.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    "EB-011: Project Orion content must not contain Solution spoilers, unapproved years, or automatic deductions: " + forbidden);
            }

            // Отваряне в Computer, четене, ръчно прикрепяне без дубликати, side-by-side сравнение, Player Note, Timeline и Save/Load.
            power.RestoreStableState(ComputerPowerState.On);
            computerTool.Interact();
            computerUI.OpenCaseFiles();
            caseFiles.SelectCase(orionCase);
            caseFiles.ShowRecords();
            caseFiles.SelectRecord(orionSecRecord);
            Button orionAttachBtn = caseFiles.GetComponentsInChildren<Button>(true)
                .Single(b => b.name == "AttachCaseRecordToBoardButton");
            TMP_Text orionAttachStatus = orionAttachBtn.transform.parent.Find("BoardAttachStatusText").GetComponent<TMP_Text>();
            int attachedBeforeOrion = boardSession.AttachedSources.Sources.Count;
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 1
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-007B: attaching Initial Security Report adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 1
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-007B: re-attaching Initial Security Report does not create duplicate");

            caseFiles.SelectRecord(orionCctvRecord);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 2
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-007B: attaching CCTV Observation adds second board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 2
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-007B: re-attaching CCTV Observation does not create duplicate");

            caseFiles.SelectRecord(danielRecord);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 3
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-008: attaching Daniel Brooks personnel record adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 3
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-008: re-attaching Daniel Brooks personnel record does not create duplicate");

            caseFiles.SelectRecord(accessRecord);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 4
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-008: attaching Clue 5 Employee Access Records adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 4
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-008: re-attaching Employee Access Records does not create duplicate");

            caseFiles.SelectRecord(commARecord);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 5
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-009: attaching Communication A adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 5
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-009: re-attaching Communication A does not create duplicate");

            caseFiles.SelectRecord(historyRecord);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 6
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-009: attaching Daniel deleted history adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 6
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-009: re-attaching Daniel deleted history does not create duplicate");

            caseFiles.SelectRecord(clue9Record);
            Require(caseFiles.GetComponentsInChildren<ScrollRect>(true).Any(s => s.name == "RecordBodyScrollView"),
                "EB-010: long Case Files document bodies are wrapped in RecordBodyScrollView");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 7
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-010: attaching Clue 9 financial record adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 7
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-010: re-attaching Clue 9 does not create duplicate");

            caseFiles.SelectRecord(michaelRecord);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 8,
                "EB-010: attaching Michael personnel record for side-by-side comparison with Clue 9");

            caseFiles.SelectRecord(clue13Record);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 9
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-011: attaching Clue 13 CCTV discrepancy adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 9
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-011: re-attaching Clue 13 does not create duplicate");

            caseFiles.SelectRecord(clue17Record);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 10
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-011: attaching Clue 17 Michael calendar record adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 10
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-011: re-attaching Clue 17 does not create duplicate");

            caseFiles.SelectRecord(clue3Record);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 11
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-012: attaching Clue 3 Torn Paper record adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 11
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-012: re-attaching Clue 3 does not create duplicate");

            caseFiles.SelectRecord(clue4Record);
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 12
                && orionAttachStatus.text == ComputerBoardAttachBar.AddedStatusMessage,
                "EB-012: attaching Clue 4 Security System Log adds board copy");
            orionAttachBtn.onClick.Invoke();
            Require(boardSession.AttachedSources.Sources.Count == attachedBeforeOrion + 12
                && orionAttachStatus.text == ComputerBoardAttachBar.AlreadyAttachedStatusMessage,
                "EB-012: re-attaching Clue 4 does not create duplicate");
            computerUI.ExitComputer();

            boardTool.Interact();
            GameObject orionBoardRoot = (GameObject)typeof(EvidenceBoardUIController)
                .GetField("boardRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
            AttachedSourceView[] orionAttachedViews = orionBoardRoot.GetComponentsInChildren<AttachedSourceView>(true);
            AttachedSourceView orionSecView = orionAttachedViews.Single(v => v.SourceId == orionSecId);
            AttachedSourceView orionCctvView = orionAttachedViews.Single(v => v.SourceId == orionCctvId);
            AttachedSourceView danielView = orionAttachedViews.Single(v => v.SourceId == "doc-orion-personnel-daniel-brooks");
            AttachedSourceView accessView = orionAttachedViews.Single(v => v.SourceId == "doc-orion-employee-access-records");
            AttachedSourceView commAView = orionAttachedViews.Single(v => v.SourceId == "doc-orion-comm-daniel-unknown");
            AttachedSourceView historyView = orionAttachedViews.Single(v => v.SourceId == "doc-orion-daniel-deleted-history");
            AttachedSourceView clue9View = orionAttachedViews.Single(v => v.SourceId == "doc-orion-financial-transfer-orion-solutions");
            AttachedSourceView michaelView = orionAttachedViews.Single(v => v.SourceId == "doc-orion-personnel-michael-reed");
            AttachedSourceView clue13View = orionAttachedViews.Single(v => v.SourceId == "doc-orion-cctv-clock-discrepancy");
            AttachedSourceView clue17View = orionAttachedViews.Single(v => v.SourceId == "doc-orion-michael-modified-calendar");
            AttachedSourceView clue3View = orionAttachedViews.Single(v => v.SourceId == "doc-orion-torn-paper-record");
            AttachedSourceView clue4View = orionAttachedViews.Single(v => v.SourceId == "doc-orion-security-system-log");
            GameObject orionDetailPanel = orionBoardRoot.transform.Find("AttachedSourceDetailsPanel").gameObject;
            TMP_Text orionPreviewTitle = orionDetailPanel.transform.Find("InnerSurface/DetailTitle/Text").GetComponent<TMP_Text>();
            TMP_Text orionPreviewBody = orionDetailPanel.transform.Find("InnerSurface/DocumentDetailView/Viewport/Content").GetComponent<TMP_Text>();

            orionSecView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionBoardRoot.transform.Find("PlayerNotesBoard").gameObject.activeInHierarchy
                && orionPreviewTitle.text == orionSecRecord.Title
                && orionPreviewBody.text == orionSecRecord.Body,
                "EB-007B: Initial Security Report opens in side-by-side preview alongside Player Notes");
            orionCctvView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == orionCctvRecord.Title
                && orionPreviewBody.text == orionCctvRecord.Body,
                "EB-007B: switching to CCTV Observation preview enables direct comparison");
            danielView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == danielRecord.Title
                && orionPreviewBody.text == danielRecord.Body,
                "EB-008: Daniel Brooks personnel record opens in side-by-side preview");
            accessView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == accessRecord.Title
                && orionPreviewBody.text == accessRecord.Body,
                "EB-008: switching to Employee Access Records preview enables comparison without automatic deduction");
            commAView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == commARecord.Title
                && orionPreviewBody.text == commARecord.Body,
                "EB-009: Communication A opens in side-by-side preview");
            historyView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == historyRecord.Title
                && orionPreviewBody.text == historyRecord.Body,
                "EB-009: switching to Daniel deleted computer history enables direct comparison");
            clue9View.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == clue9Record.Title
                && orionPreviewBody.text == clue9Record.Body,
                "EB-010: Clue 9 financial record opens in side-by-side preview");
            michaelView.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == michaelRecord.Title
                && orionPreviewBody.text == michaelRecord.Body,
                "EB-010: switching from Clue 9 to Michael personnel record enables motive comparison");
            clue13View.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == clue13Record.Title
                && orionPreviewBody.text == clue13Record.Body,
                "EB-011: Clue 13 CCTV discrepancy opens in side-by-side preview");
            clue17View.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == clue17Record.Title
                && orionPreviewBody.text == clue17Record.Body,
                "EB-011: switching to Clue 17 Michael calendar record enables alibi comparison without automatic deduction");
            clue3View.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == clue3Record.Title
                && orionPreviewBody.text == clue3Record.Body,
                "EB-012: Clue 3 Torn Paper archival record opens in side-by-side preview");
            clue4View.OpenDetails();
            Require(orionDetailPanel.activeSelf
                && orionPreviewTitle.text == clue4Record.Title
                && orionPreviewBody.text == clue4Record.Body,
                "EB-012: switching to Clue 4 Security System Log enables direct comparison with Clue 1 and Clue 2");
            orionDetailPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AttachedDetailBackButton").onClick.Invoke();

            // Ръчна бележка и ръчни Timeline събития за Project Orion + Save/Load.
            orionSecView.ApplyNormalizedPosition(.34f, .66f);
            PlayerNote orionPlayerNote = boardSession.PlayerNotes.Create(.48f, .44f);
            orionPlayerNote.SetText("23:45 достъп и влизане в коридора, но алармата не се е задействала.");
            timelineTab.onClick.Invoke();
            tlDate.text = "2000-01-01"; tlTime.text = "23:30"; tlTitle.text = "Проверка на лабораторията"; tlDesc.text = "Всичко изглежда нормално.";
            tlSaveBtn.onClick.Invoke();
            tlDate.text = "2000-01-01"; tlTime.text = "23:45"; tlTitle.text = "Събитие за достъп"; tlDesc.text = "Охранителната система отчита достъп.";
            tlSaveBtn.onClick.Invoke();
            tlDate.text = "2000-01-01"; tlTime.text = "23:52"; tlTitle.text = "Човек напуска коридора"; tlDesc.text = "CCTV справка.";
            tlSaveBtn.onClick.Invoke();
            tlDate.text = "2000-01-02"; tlTime.text = "00:10"; tlTitle.text = "Установена липса на прототипа"; tlDesc.text = "Project Orion липсва.";
            tlSaveBtn.onClick.Invoke();
            Require(save.TrySave(out saveMessage), "EB-007B: Save with Project Orion attachments, note and timeline succeeds: " + saveMessage);
            Require(save.TryLoad(out loadMessage)
                && boardSession.AttachedSources.ContainsSource(orionSecId, AttachedSourceType.Document)
                && boardSession.AttachedSources.ContainsSource(orionCctvId, AttachedSourceType.Document)
                && boardSession.PlayerNotes.Notes.Any(n => n.Id == orionPlayerNote.Id && n.Text == orionPlayerNote.Text)
                && boardSession.Timeline.Events.Any(e => e.Time == "23:30" && e.Title == "Проверка на лабораторията")
                && boardSession.Timeline.Events.Any(e => e.Time == "00:10" && e.Title == "Установена липса на прототипа"),
                "EB-007B: Save/Load restores Project Orion attachments, Player Note and manual Timeline events");

            // Премахване на копие от дъската не изтрива оригинала в Computer; после възстановяваме базовия v4 save.
            boardTool.Interact();
            AttachedSourceView loadedOrionCctv = orionBoardRoot.GetComponentsInChildren<AttachedSourceView>(true)
                .Single(v => v.SourceId == orionCctvId);
            loadedOrionCctv.GetComponentsInChildren<Button>(true).Single(b => b.name == "RemoveAttachedSourceButton").onClick.Invoke();
            orionBoardRoot.GetComponentsInChildren<Button>(true).Single(b => b.name == "ConfirmRemoveAttachedSourceButton").onClick.Invoke();
            Require(!boardSession.AttachedSources.ContainsSource(orionCctvId, AttachedSourceType.Document)
                && caseFiles.TryFindRecordBySourceId(orionCctvId, out _, out CaseRecordData stillOrionCctv)
                && stillOrionCctv.Body == cctvBody,
                "EB-007B: removing Project Orion board copy preserves original Computer record");
            File.WriteAllText(savePath, savedV4Json);
            Require(save.TryLoad(out loadMessage)
                && boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.AttachedSources.Sources.Count == 2
                && boardSession.Timeline.Events.Count == 3,
                "EB-007B: restoring baseline v4 save keeps existing test continuation intact");

            // EB-013: Manual Case Theory ("МОЯТА ТЕОРИЯ") — 6 fields, Cyrillic/multiline, view switching, Pause/Resume, Exit/Re-entry, Save/Load v5 + legacy v4 compat, no submission/evaluation.
            boardTool.Interact();
            GameObject thBoardRoot = (GameObject)typeof(EvidenceBoardUIController)
                .GetField("boardRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boardUI);
            GameObject thPanel = thBoardRoot.transform.Find("CaseTheoryPanel").gameObject;
            Button theoryTab = thBoardRoot.transform.Find("TheoryModeTabButton").GetComponent<Button>();
            theoryTab.onClick.Invoke();
            Require(thPanel.activeSelf
                && !thBoardRoot.transform.Find("PlayerNotesBoard").gameObject.activeSelf
                && !tlPanel.activeSelf,
                "EB-013: TheoryModeTabButton opens CaseTheoryPanel and hides corkboard and timeline");

            // Няма бутони за обвинение, проверка или приключване на случая.
            foreach (TMP_Text label in thPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                foreach (string forbiddenBtn in new[] { "ОБВИНИ", "ИЗПРАТИ ОБВИНЕНИЕ", "ПРИКЛЮЧИ СЛУЧАЯ", "ПРОВЕРИ ТЕОРИЯТА", "ПРОВЕРИ" })
                {
                    Require(!label.text.Contains(forbiddenBtn, StringComparison.OrdinalIgnoreCase),
                        "EB-013: CaseTheoryPanel must not contain final submission or evaluation control: " + forbiddenBtn);
                }
            }

            TMP_InputField thSuspect = thPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TheoryMainSuspectInput");
            TMP_InputField thParticipants = thPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TheoryParticipantsInput");
            TMP_InputField thMotive = thPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TheoryMotiveInput");
            TMP_InputField thReconstruction = thPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TheoryReconstructionInput");
            TMP_InputField thEvidence = thPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TheorySupportingEvidenceInput");
            TMP_InputField thQuestions = thPanel.GetComponentsInChildren<TMP_InputField>(true).Single(i => i.name == "TheoryUnresolvedQuestionsInput");

            thSuspect.text = "Michael Reed";
            thParticipants.text = "Daniel може да е участвал в достъпа.\nТрябва да проверя ролята на Sarah.";
            thMotive.text = "Възможно е да има финансов интерес.";
            thReconstruction.text = "Някой е влязъл с временната карта.\nВтори ред реконструкция.";
            thEvidence.text = "Финансовият превод, календарът и журналът за достъп.";
            thQuestions.text = "Кой физически е използвал временната карта?";

            // Превключване Theory -> Board -> Theory и Theory -> Timeline -> Theory запазва текста.
            boardTab.onClick.Invoke();
            Require(!thPanel.activeSelf && thBoardRoot.transform.Find("PlayerNotesBoard").gameObject.activeSelf,
                "EB-013: switching Theory -> Board works cleanly");
            theoryTab.onClick.Invoke();
            Require(thPanel.activeSelf && thSuspect.text == "Michael Reed" && thReconstruction.text.Contains("Втори ред"),
                "EB-013: switching Board -> Theory preserves entered text");
            timelineTab.onClick.Invoke();
            Require(tlPanel.activeSelf && !thPanel.activeSelf, "EB-013: switching Theory -> Timeline works cleanly");
            theoryTab.onClick.Invoke();
            Require(thPanel.activeSelf && thMotive.text == "Възможно е да има финансов интерес.",
                "EB-013: switching Timeline -> Theory preserves entered text");

            // Редакция и изчистване на поле + Pause/Resume + Exit/Re-entry.
            thSuspect.text = "Daniel Brooks";
            thQuestions.text = "";
            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, null);
            Require(interaction.ModeController.IsPaused && thPanel.activeSelf && thSuspect.text == "Daniel Brooks",
                "EB-013: Pause preserves open Theory view and edited text");
            pause.Resume();
            Require(!interaction.ModeController.IsPaused && thPanel.activeSelf && thSuspect.text == "Daniel Brooks",
                "EB-013: Resume preserves open Theory view and edited text");
            thBoardRoot.transform.Find("ExitBoardButton").GetComponent<Button>().onClick.Invoke();
            Require(!boardUI.IsOpen && interaction.ModeController.ActiveOwner == null, "EB-013: Exit from Theory restores World");
            boardTool.Interact();
            theoryTab.onClick.Invoke();
            Require(thSuspect.text == "Daniel Brooks" && thQuestions.text == "",
                "EB-013: Exit/Re-entry preserves edited and cleared theory fields");

            // Save v5 -> временна промяна -> Load -> повторен Load -> стар v4 Save без caseTheories.
            Require(save.TrySave(out saveMessage), "EB-013: Save v5 with Case Theory succeeds: " + saveMessage);
            thSuspect.text = "Незаписана промяна";
            Require(save.TryLoad(out loadMessage)
                && boardSession.CaseTheories.TryGetDraft("CASE-ORION-01", out CaseTheoryDraft loadedOrionTheory)
                && loadedOrionTheory.MainSuspect == "Daniel Brooks"
                && loadedOrionTheory.ParticipantsAndRoles.Contains("Sarah")
                && loadedOrionTheory.Motive == "Възможно е да има финансов интерес."
                && loadedOrionTheory.UnresolvedQuestions == ""
                && boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.AttachedSources.Sources.Count == 2
                && boardSession.Timeline.Events.Count == 3,
                "EB-013: Save/Load restores Case Theory without affecting Player Notes, Attached Sources or Timeline");
            Require(save.TryLoad(out loadMessage) && boardSession.CaseTheories.Drafts.Count == 1,
                "EB-013: repeated Load does not duplicate Case Theory drafts");
            File.WriteAllText(savePath, savedV4Json);
            Require(save.TryLoad(out loadMessage) && boardSession.CaseTheories.Drafts.Count == 0
                && boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.AttachedSources.Sources.Count == 2
                && boardSession.Timeline.Events.Count == 3,
                "EB-013: legacy v4 save without caseTheories loads cleanly with empty theory draft");

            // EB-014: Manual Evidence References in Case Theory (ИЗБРАНИ ИЗТОЧНИЦИ, 23 CASE-ORION-01 docs, selector, dedupe, remove, board independence, missing ID, Save/Load).
            boardTool.Interact();
            theoryTab.onClick.Invoke();
            Require(thPanel.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "ИЗБРАНИ ИЗТОЧНИЦИ"),
                "EB-014: ИЗБРАНИ ИЗТОЧНИЦИ section exists in CaseTheoryPanel");
            Button addSourceBtn = thPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AddTheorySourceButton");
            GameObject selectorOverlay = thPanel.transform.Find("TheorySourceSelectorOverlay").gameObject;
            Require(!selectorOverlay.activeSelf, "EB-014: source selector starts closed");

            // Отваряне на selector-а: всички 23 Project Orion документа са налични динамично от CaseFilesUIController.
            addSourceBtn.onClick.Invoke();
            Require(selectorOverlay.activeSelf && selectorOverlay.GetComponent<Image>().raycastTarget,
                "EB-014: AddTheorySourceButton opens modal selector overlay with raycast blocking");
            Transform selectorContent = selectorOverlay.transform.Find("TheorySourceSelectorDialog/InnerSurface/TheorySourceSelectorScrollView/Viewport/Content");
            Button[] selectorItemButtons = selectorContent.GetComponentsInChildren<Button>(false);
            Require(selectorItemButtons.Length == 23
                && selectorItemButtons.All(b => b.name.StartsWith("TheorySourceSelectorItem_doc-orion-", StringComparison.Ordinal)),
                "EB-014: selector exposes all 23 CASE-ORION-01 documents without mixing other cases");

            // Затваряне без избор.
            selectorOverlay.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseTheorySourceSelectorButton").onClick.Invoke();
            Require(!selectorOverlay.activeSelf && boardSession.CaseTheories.Drafts.Count == 0,
                "EB-014: closing selector without selection adds nothing");

            // Ръчно добавяне на два документа + проверка за ред, защита от дубликати и непроменени 6 полета / Notes / Timeline / Board.
            thSuspect.text = "Michael Reed";
            thEvidence.text = "Свободен текст за доказателствата.";
            addSourceBtn.onClick.Invoke();
            const string refDoc1 = "doc-orion-michael-modified-calendar";
            const string refDoc2 = "doc-orion-financial-transfer-orion-solutions";
            selectorContent.Find("TheorySourceSelectorItem_" + refDoc1).GetComponent<Button>().onClick.Invoke();
            selectorContent.Find("TheorySourceSelectorItem_" + refDoc2).GetComponent<Button>().onClick.Invoke();
            Require(!selectorContent.Find("TheorySourceSelectorItem_" + refDoc1).GetComponent<Button>().interactable
                && selectorContent.Find("TheorySourceSelectorItem_" + refDoc1 + "/StatusText").GetComponent<TMP_Text>().text == "ДОБАВЕН"
                && !selectorContent.Find("TheorySourceSelectorItem_" + refDoc2).GetComponent<Button>().interactable,
                "EB-014: added items are clearly marked ДОБАВЕН and protected against duplicate clicks");
            selectorOverlay.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseTheorySourceSelectorButton").onClick.Invoke();

            Require(boardSession.CaseTheories.TryGetDraft("CASE-ORION-01", out CaseTheoryDraft eb14Draft)
                && eb14Draft.ReferencedSourceIds.Count == 2
                && eb14Draft.ReferencedSourceIds[0] == refDoc1
                && eb14Draft.ReferencedSourceIds[1] == refDoc2
                && eb14Draft.MainSuspect == "Michael Reed"
                && eb14Draft.SupportingEvidence == "Свободен текст за доказателствата."
                && boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.AttachedSources.Sources.Count == 2
                && boardSession.Timeline.Events.Count == 3,
                "EB-014: adding references stores stable IDs in order without mutating text fields, notes, board or timeline");

            // Независимост от Evidence Board: премахване на board копие не трие препратката; премахване на препратка не трие оригинала или board копието.
            Require(boardUI.TryAttachSource(refDoc1, AttachedSourceType.Document), "EB-014: attach refDoc1 to board");
            AttachedSource attachedRefDoc1 = boardSession.AttachedSources.Sources.Single(s => s.SourceId == refDoc1);
            Require(boardSession.AttachedSources.Remove(attachedRefDoc1.AttachmentId)
                && eb14Draft.ContainsReferencedSource(refDoc1),
                "EB-014: removing attached board copy preserves theory reference");
            Require(boardUI.TryAttachSource(refDoc2, AttachedSourceType.Document), "EB-014: attach refDoc2 to board");
            Transform refList = thPanel.transform.Find("InnerSurface/TheoryFormScrollView/Viewport/Content/TheoryReferencedSourcesSection/TheoryReferencedSourcesList");
            refList.Find("TheoryReferencedSourceItem_" + refDoc2 + "/RemoveTheorySourceButton").GetComponent<Button>().onClick.Invoke();
            Require(eb14Draft.ReferencedSourceIds.Count == 1
                && eb14Draft.ReferencedSourceIds[0] == refDoc1
                && boardSession.AttachedSources.ContainsSource(refDoc2, AttachedSourceType.Document)
                && caseFiles.TryFindRecordBySourceId(refDoc2, out _, out CaseRecordData stillRefDoc2)
                && !string.IsNullOrWhiteSpace(stillRefDoc2.Body),
                "EB-014: removing theory reference preserves original Case File and board copy");
            AttachedSource attachedRefDoc2 = boardSession.AttachedSources.Sources.Single(s => s.SourceId == refDoc2);
            boardSession.AttachedSources.Remove(attachedRefDoc2.AttachmentId);

            // Превключване на табове, Pause/Resume, Exit/Re-entry + Save/Load + повторен Load + липсващ source ID.
            boardTab.onClick.Invoke();
            theoryTab.onClick.Invoke();
            timelineTab.onClick.Invoke();
            theoryTab.onClick.Invoke();
            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, null);
            pause.Resume();
            thBoardRoot.transform.Find("ExitBoardButton").GetComponent<Button>().onClick.Invoke();
            boardTool.Interact();
            theoryTab.onClick.Invoke();
            Require(eb14Draft.ReferencedSourceIds.Count == 1 && eb14Draft.ReferencedSourceIds[0] == refDoc1,
                "EB-014: tab switches, Pause/Resume, and Exit/Re-entry preserve referenced sources");

            Require(save.TrySave(out saveMessage), "EB-014: Save with referenced sources succeeds: " + saveMessage);
            eb14Draft.TryAddReferencedSource(refDoc2);
            Require(save.TryLoad(out loadMessage)
                && boardSession.CaseTheories.TryGetDraft("CASE-ORION-01", out CaseTheoryDraft loadedEb14Draft)
                && loadedEb14Draft.ReferencedSourceIds.Count == 1
                && loadedEb14Draft.ReferencedSourceIds[0] == refDoc1
                && loadedEb14Draft.MainSuspect == "Michael Reed",
                "EB-014: Save/Load restores referenced sources without reviving removed or unsaved ones");
            Require(save.TryLoad(out loadMessage)
                && boardSession.CaseTheories.GetOrCreateDraft("CASE-ORION-01").ReferencedSourceIds.Count == 1,
                "EB-014: repeated Load does not duplicate referenced sources");

            // Липсващ source ID се запазва при Load, визуализира „Недостъпен източник“ и може да бъде премахнат.
            string savedWithRefJson = File.ReadAllText(savePath);
            string missingIdJson = savedWithRefJson.Replace(refDoc1, "doc-orion-missing-test-id");
            File.WriteAllText(savePath, missingIdJson);
            CaseTheoryDraft missingIdDraft = null;
            Require(save.TryLoad(out loadMessage)
                && boardSession.CaseTheories.TryGetDraft("CASE-ORION-01", out missingIdDraft)
                && missingIdDraft.ReferencedSourceIds.Count == 1
                && missingIdDraft.ReferencedSourceIds[0] == "doc-orion-missing-test-id",
                "EB-014: missing source ID is preserved across Load without error");
            boardTool.Interact();
            theoryTab.onClick.Invoke();
            Transform missingRow = refList.Find("TheoryReferencedSourceItem_doc-orion-missing-test-id");
            Require(missingRow != null
                && missingRow.Find("InfoColumn/TitleText").GetComponent<TMP_Text>().text == "Недостъпен източник",
                "EB-014: missing source ID renders neutral Недостъпен източник label");
            missingRow.Find("RemoveTheorySourceButton").GetComponent<Button>().onClick.Invoke();
            Require(missingIdDraft.ReferencedSourceIds.Count == 0,
                "EB-014: player can remove missing source ID reference");
            thBoardRoot.transform.Find("ExitBoardButton").GetComponent<Button>().onClick.Invoke();

            // EB-015: Quick Read-Only Preview of Referenced Sources (ПРЕГЛЕД, reused BoardSourcePreviewView, modal blocking, state/scroll preservation, board independence, missing & cross-case safety).
            File.WriteAllText(savePath, savedV4Json);
            Require(save.TryLoad(out loadMessage), "EB-015: baseline v4 load before EB-015 checks");
            boardTool.Interact();
            theoryTab.onClick.Invoke();

            thSuspect.text = "Michael Reed — EB015";
            thParticipants.text = "Daniel Brooks и Sarah Mitchell";
            thMotive.text = "Финансов мотив и скрит трансфер.";
            thReconstruction.text = "Ред 1: достъп в 23:45.\nРед 2: излизане в 23:52.";
            thEvidence.text = "Свободен текст преди отваряне на прегледа.";
            thQuestions.text = "Оставащ въпрос за синхронизацията.";

            const string eb15Doc1 = "doc-orion-michael-modified-calendar";
            const string eb15Doc2 = "doc-orion-financial-transfer-orion-solutions";
            addSourceBtn.onClick.Invoke();
            selectorContent.Find("TheorySourceSelectorItem_" + eb15Doc1).GetComponent<Button>().onClick.Invoke();
            selectorContent.Find("TheorySourceSelectorItem_" + eb15Doc2).GetComponent<Button>().onClick.Invoke();
            selectorOverlay.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseTheorySourceSelectorButton").onClick.Invoke();

            // Потвърждава, че документите НЕ са прикачени върху Evidence Board.
            Require(!boardSession.AttachedSources.ContainsSource(eb15Doc1, AttachedSourceType.Document)
                && !boardSession.AttachedSources.ContainsSource(eb15Doc2, AttachedSourceType.Document),
                "EB-015: referenced sources are not attached to Evidence Board prior to preview");

            GameObject previewOverlay = thPanel.transform.Find("TheorySourcePreviewOverlay").gameObject;
            Require(!previewOverlay.activeSelf && previewOverlay.GetComponent<Image>().raycastTarget,
                "EB-015: preview overlay starts closed and has modal raycast blocking");
            BoardSourcePreviewView theoryPreviewView = previewOverlay.GetComponentInChildren<BoardSourcePreviewView>(true);
            Require(theoryPreviewView != null, "EB-015: TheorySourcePreviewOverlay reuses BoardSourcePreviewView");

            ScrollRect theoryScroll = thPanel.transform.Find("InnerSurface/TheoryFormScrollView").GetComponent<ScrollRect>();
            theoryScroll.verticalNormalizedPosition = 0.25f;

            // Отваряне на ПРЕГЛЕД за първия документ (без да е прикачен към дъската).
            Transform eb15Row1 = refList.Find("TheoryReferencedSourceItem_" + eb15Doc1);
            Button eb15PreviewBtn1 = eb15Row1.Find("PreviewTheorySourceButton").GetComponent<Button>();
            Require(eb15PreviewBtn1.interactable && eb15PreviewBtn1.GetComponentInChildren<TMP_Text>().text == "ПРЕГЛЕД",
                "EB-015: valid referenced source has interactable ПРЕГЛЕД button");

            CaseRecordData eb15Record1 = null;
            CaseRecordData eb15Record2 = null;
            Require(caseFiles.TryFindRecordBySourceId(eb15Doc1, out _, out eb15Record1)
                && caseFiles.TryFindRecordBySourceId(eb15Doc2, out _, out eb15Record2),
                "EB-015: resolve expected original CaseRecordData");

            int attachedBeforePreview = boardSession.AttachedSources.Sources.Count;
            eb15PreviewBtn1.onClick.Invoke();

            TMP_Text theoryPreviewTitle = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/DetailTitle/Text").GetComponent<TMP_Text>();
            TMP_Text theoryPreviewMeta = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/DetailMeta/Text").GetComponent<TMP_Text>();
            TMP_Text theoryPreviewBody = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/DocumentDetailView/Viewport/Content").GetComponent<TMP_Text>();
            ScrollRect theoryPreviewDocScroll = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/DocumentDetailView").GetComponent<ScrollRect>();
            Button closeTheoryPreviewBtn = previewOverlay.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseTheorySourcePreviewButton");

            Require(previewOverlay.activeSelf && theoryPreviewView.IsOpen && thPanel.activeSelf
                && theoryPreviewTitle.text == eb15Record1.Title
                && theoryPreviewMeta.text.Contains(eb15Record1.RecordType)
                && theoryPreviewBody.text == eb15Record1.Body
                && theoryPreviewDocScroll != null && theoryPreviewDocScroll.vertical
                && closeTheoryPreviewBtn.GetComponentInChildren<TMP_Text>().text == "ЗАТВОРИ ПРЕГЛЕДА"
                && previewOverlay.GetComponentsInChildren<Button>(true).All(b => b.name != "AttachedDetailRemoveButton")
                && boardSession.AttachedSources.Sources.Count == attachedBeforePreview,
                "EB-015: preview opens original document read-only with title, type, full body, scroll, ЗАТВОРИ ПРЕГЛЕДА and no board mutation");

            // Pause/Resume при отворен preview пази preview и теорията.
            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, null);
            Require(interaction.ModeController.IsPaused && previewOverlay.activeSelf && thSuspect.text == "Michael Reed — EB015",
                "EB-015: Pause preserves open preview and unsaved theory edits");
            pause.Resume();
            Require(!interaction.ModeController.IsPaused && previewOverlay.activeSelf && thPanel.activeSelf,
                "EB-015: Resume returns to open preview inside Theory");

            // Затваряне с ЗАТВОРИ ПРЕГЛЕДА връща в МОЯТА ТЕОРИЯ и пази 6-те полета, скрола и реда на източниците.
            closeTheoryPreviewBtn.onClick.Invoke();
            CaseTheoryDraft eb15Draft = boardSession.CaseTheories.GetOrCreateDraft("CASE-ORION-01");
            Require(!previewOverlay.activeSelf && !theoryPreviewView.IsOpen && thPanel.activeSelf
                && thSuspect.text == "Michael Reed — EB015"
                && thParticipants.text == "Daniel Brooks и Sarah Mitchell"
                && thMotive.text == "Финансов мотив и скрит трансфер."
                && thReconstruction.text == "Ред 1: достъп в 23:45.\nРед 2: излизане в 23:52."
                && thEvidence.text == "Свободен текст преди отваряне на прегледа."
                && thQuestions.text == "Оставащ въпрос за синхронизацията."
                && Mathf.Approximately(theoryScroll.verticalNormalizedPosition, 0.25f)
                && eb15Draft.ReferencedSourceIds.Count == 2
                && eb15Draft.ReferencedSourceIds[0] == eb15Doc1
                && eb15Draft.ReferencedSourceIds[1] == eb15Doc2
                && refList.childCount == 3, // EmptyLabel + 2 rows (без дублиране)
                "EB-015: closing preview returns to Theory and preserves all 6 fields, scroll position, and reference order without duplicates");

            // Последователен преглед на втори документ.
            refList.Find("TheoryReferencedSourceItem_" + eb15Doc2 + "/PreviewTheorySourceButton").GetComponent<Button>().onClick.Invoke();
            Require(previewOverlay.activeSelf
                && theoryPreviewTitle.text == eb15Record2.Title
                && theoryPreviewBody.text == eb15Record2.Body,
                "EB-015: opening second referenced document updates preview accurately");
            closeTheoryPreviewBtn.onClick.Invoke();
            Require(!previewOverlay.activeSelf, "EB-015: closing second preview succeeds");

            // Независимост от Evidence Board: прикачи -> премахни от дъската -> прегледай отново.
            Require(boardUI.TryAttachSource(eb15Doc1, AttachedSourceType.Document), "EB-015: attach eb15Doc1 to board");
            AttachedSource eb15BoardCopy = boardSession.AttachedSources.Sources.Single(s => s.SourceId == eb15Doc1);
            Require(boardSession.AttachedSources.Remove(eb15BoardCopy.AttachmentId), "EB-015: remove eb15Doc1 copy from board");
            refList.Find("TheoryReferencedSourceItem_" + eb15Doc1 + "/PreviewTheorySourceButton").GetComponent<Button>().onClick.Invoke();
            Require(previewOverlay.activeSelf && theoryPreviewTitle.text == eb15Record1.Title
                && !boardSession.AttachedSources.ContainsSource(eb15Doc1, AttachedSourceType.Document),
                "EB-015: preview still works after removing board copy without re-attaching");
            closeTheoryPreviewBtn.onClick.Invoke();

            // Липсващ source ID и чужд Case ID (CASE-0017 в CASE-ORION-01 теория): ПРЕГЛЕД е деактивиран и не отваря стар/празен/чужд документ.
            eb15Draft.TryAddReferencedSource("doc-orion-missing-eb15");
            eb15Draft.TryAddReferencedSource("doc-case0017-incident-report");
            BoardCaseTheoryView theoryViewComponent = thPanel.GetComponent<BoardCaseTheoryView>();
            theoryViewComponent.RefreshFromSession();
            Transform eb15MissingRow = refList.Find("TheoryReferencedSourceItem_doc-orion-missing-eb15");
            Transform eb15CrossCaseRow = refList.Find("TheoryReferencedSourceItem_doc-case0017-incident-report");
            Require(eb15MissingRow != null
                && eb15MissingRow.Find("InfoColumn/TitleText").GetComponent<TMP_Text>().text == "Недостъпен източник"
                && eb15MissingRow.Find("InfoColumn/TypeText").GetComponent<TMP_Text>().text == "Документът не е наличен за преглед."
                && !eb15MissingRow.Find("PreviewTheorySourceButton").GetComponent<Button>().interactable
                && !theoryViewComponent.OpenSourcePreview("doc-orion-missing-eb15")
                && !previewOverlay.activeSelf,
                "EB-015: missing source ID disables ПРЕГЛЕД, shows neutral text, and never opens stale/empty preview");
            Require(eb15CrossCaseRow != null
                && !eb15CrossCaseRow.Find("PreviewTheorySourceButton").GetComponent<Button>().interactable
                && !theoryViewComponent.OpenSourcePreview("doc-case0017-incident-report")
                && !previewOverlay.activeSelf,
                "EB-015: cross-case source ID is blocked by Case ID isolation");

            // Съществуващият Board Source Preview продължава да работи нормално.
            boardTab.onClick.Invoke();
            AttachedSourceView existingBoardDoc = thBoardRoot.GetComponentsInChildren<AttachedSourceView>(true)
                .First(v => v.SourceType == AttachedSourceType.Document);
            existingBoardDoc.OpenDetails();
            GameObject existingDetailPanel = thBoardRoot.transform.Find("AttachedSourceDetailsPanel").gameObject;
            Require(existingDetailPanel.activeSelf && thBoardRoot.transform.Find("PlayerNotesBoard").gameObject.activeInHierarchy,
                "EB-015: existing Board side-by-side preview still works");
            existingDetailPanel.GetComponentsInChildren<Button>(true).Single(b => b.name == "AttachedDetailBackButton").onClick.Invoke();
            Require(!existingDetailPanel.activeSelf, "EB-015: closing existing Board preview still works");

            thBoardRoot.transform.Find("ExitBoardButton").GetComponent<Button>().onClick.Invoke();

            // EB-016: Manual Photo References & Read-Only Preview (real empty-state audit for CASE-ORION-01 + isolated in-memory synthetic photo fixture checks).
            File.WriteAllText(savePath, savedV4Json);
            Require(save.TryLoad(out loadMessage), "EB-016: baseline v4 load before EB-016 checks");
            boardTool.Interact();
            theoryTab.onClick.Invoke();

            // 1. Реален одит на production сцената: CASE-ORION-01 няма реални снимки, няма cross-case leakage от CASE-0017, Clue 12 остава документ.
            Require(photosUI.GetPhotosForCase("CASE-ORION-01").Count == 0,
                "EB-016: production scene has 0 attributed photos for CASE-ORION-01 (no cross-case leakage)");
            Require(caseFiles.TryFindRecordBySourceId("doc-orion-hotel-photograph-description", out CaseData eb16Clue12Case, out CaseRecordData eb16Clue12Doc)
                && eb16Clue12Case != null && eb16Clue12Case.CaseNumber == "CASE-ORION-01" && eb16Clue12Doc != null,
                "EB-016: Clue 12 remains a Document in CASE-ORION-01");

            addSourceBtn.onClick.Invoke();
            Button docsCategoryBtn = selectorOverlay.transform.Find("TheorySourceSelectorDialog/InnerSurface/TheorySelectorDocumentsTabButton").GetComponent<Button>();
            Button photosCategoryBtn = selectorOverlay.transform.Find("TheorySourceSelectorDialog/InnerSurface/TheorySelectorPhotosTabButton").GetComponent<Button>();
            TMP_Text selectorEmptyTmp = selectorContent.Find("TheorySourceSelectorEmpty").GetComponent<TMP_Text>();
            Require(selectorContent.GetComponentsInChildren<Button>(false).Length == 23,
                "EB-016: Documents tab in selector still shows all 23 CASE-ORION-01 documents");

            photosCategoryBtn.onClick.Invoke();
            Require(selectorContent.GetComponentsInChildren<Button>(false).Length == 0
                && selectorEmptyTmp.gameObject.activeSelf
                && selectorEmptyTmp.text == "Няма налични снимки за това дело.",
                "EB-016: Photos tab shows neutral empty state when CASE-ORION-01 has no real photos");
            docsCategoryBtn.onClick.Invoke();
            Require(selectorContent.GetComponentsInChildren<Button>(false).Length == 23,
                "EB-016: switching back to Documents tab restores all 23 CASE-ORION-01 documents");
            selectorOverlay.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseTheorySourceSelectorButton").onClick.Invoke();

            // 2. Изолиран синтетичен in-memory Photo fixture (не се записва в сцената; възстановява се в finally).
            FieldInfo photosListField = typeof(PhotosUIController).GetField("photos", BindingFlags.Instance | BindingFlags.NonPublic);
            var runtimePhotosList = (List<PhotoData>)photosListField.GetValue(photosUI);
            int originalPhotosCount = runtimePhotosList.Count;
            Texture2D synthTex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Sprite synthSprite = Sprite.Create(synthTex, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f));
            PhotoData synthOrionPhoto = new PhotoData();
            PhotoData synthOrionMissingAssetPhoto = new PhotoData();
            PhotoData synthOtherCasePhoto = new PhotoData();
            typeof(PhotoData).GetField("sourceId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, "synthetic-eb16-orion-photo");
            typeof(PhotoData).GetField("caseId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, "CASE-ORION-01");
            typeof(PhotoData).GetField("title", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, "Синтетична снимка Orion");
            typeof(PhotoData).GetField("location", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, "Лаборатория");
            typeof(PhotoData).GetField("date", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, "2000-01-01");
            typeof(PhotoData).GetField("description", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, "Тестово описание на синтетична снимка.");
            typeof(PhotoData).GetField("image", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionPhoto, synthSprite);

            typeof(PhotoData).GetField("sourceId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionMissingAssetPhoto, "synthetic-eb16-missing-asset");
            typeof(PhotoData).GetField("caseId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionMissingAssetPhoto, "CASE-ORION-01");
            typeof(PhotoData).GetField("title", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOrionMissingAssetPhoto, "Снимка без asset");

            typeof(PhotoData).GetField("sourceId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOtherCasePhoto, "synthetic-eb16-case0017-photo");
            typeof(PhotoData).GetField("caseId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOtherCasePhoto, "CASE-0017");
            typeof(PhotoData).GetField("title", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOtherCasePhoto, "Чужда снимка CASE-0017");
            typeof(PhotoData).GetField("image", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(synthOtherCasePhoto, synthSprite);

            try
            {
                runtimePhotosList.Add(synthOrionPhoto);
                runtimePhotosList.Add(synthOrionMissingAssetPhoto);
                runtimePhotosList.Add(synthOtherCasePhoto);

                thSuspect.text = "Michael Reed — EB016";
                theoryScroll.verticalNormalizedPosition = 0.30f;

                addSourceBtn.onClick.Invoke();
                // Добавя 1 документ и 1 валидна синтетична снимка за CASE-ORION-01.
                selectorContent.Find("TheorySourceSelectorItem_" + eb15Doc1).GetComponent<Button>().onClick.Invoke();
                photosCategoryBtn.onClick.Invoke();
                Button[] synthPhotoButtons = selectorContent.GetComponentsInChildren<Button>(false);
                Require(synthPhotoButtons.Length == 1 && synthPhotoButtons[0].name == "TheoryPhotoSelectorItem_synthetic-eb16-orion-photo",
                    "EB-016: Photos tab shows only valid CASE-ORION-01 photo with asset (excludes missing asset and CASE-0017 photo)");
                synthPhotoButtons[0].onClick.Invoke();
                Require(!selectorContent.Find("TheoryPhotoSelectorItem_synthetic-eb16-orion-photo").GetComponent<Button>().interactable,
                    "EB-016: added photo is protected against duplicate clicks in selector");
                selectorOverlay.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseTheorySourceSelectorButton").onClick.Invoke();

                // Отваряне на Photo Preview (без прикачено копие на дъската).
                Transform synthPhotoRow = refList.Find("TheoryReferencedPhotoItem_synthetic-eb16-orion-photo");
                Require(synthPhotoRow != null
                    && synthPhotoRow.Find("InfoColumn/TypeText").GetComponent<TMP_Text>().text.StartsWith("СНИМКА", StringComparison.Ordinal)
                    && !boardSession.AttachedSources.ContainsSource("synthetic-eb16-orion-photo", AttachedSourceType.Photo),
                    "EB-016: selected photo row displays СНИМКА type and does not attach to Evidence Board");

                synthPhotoRow.Find("PreviewTheorySourceButton").GetComponent<Button>().onClick.Invoke();
                Image theoryPreviewPhotoImg = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/PhotoDetailView/PhotoPrintFrame/PhotoMatte/DetailPhotoImage").GetComponent<Image>();
                GameObject theoryPreviewDocContainer = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/DocumentDetailView").gameObject;
                GameObject theoryPreviewPhotoContainer = previewOverlay.transform.Find("TheorySourcePreviewPanel/InnerSurface/PhotoDetailView").gameObject;

                Require(previewOverlay.activeSelf
                    && theoryPreviewPhotoContainer.activeSelf
                    && !theoryPreviewDocContainer.activeSelf
                    && theoryPreviewTitle.text == "Синтетична снимка Orion"
                    && theoryPreviewPhotoImg.sprite == synthSprite
                    && theoryPreviewPhotoImg.enabled
                    && string.IsNullOrEmpty(theoryPreviewBody.text),
                    "EB-016: Photo Preview renders synthetic sprite, title, and clears document body via reused BoardSourcePreviewView");

                closeTheoryPreviewBtn.onClick.Invoke();
                Require(!previewOverlay.activeSelf
                    && thSuspect.text == "Michael Reed — EB016"
                    && Mathf.Approximately(theoryScroll.verticalNormalizedPosition, 0.30f),
                    "EB-016: closing Photo Preview returns to Theory preserving text and scroll position");

                // Превключване към Document Preview чисти предишния Sprite.
                refList.Find("TheoryReferencedSourceItem_" + eb15Doc1 + "/PreviewTheorySourceButton").GetComponent<Button>().onClick.Invoke();
                Require(previewOverlay.activeSelf
                    && theoryPreviewDocContainer.activeSelf
                    && !theoryPreviewPhotoContainer.activeSelf
                    && theoryPreviewPhotoImg.sprite == null,
                    "EB-016: switching from Photo to Document Preview clears previous sprite cleanly");
                closeTheoryPreviewBtn.onClick.Invoke();

                // Липсващ asset, неизвестен ID и чужд Case ID за снимки: Недостъпна снимка, ПРЕГЛЕД деактивиран, не отваря последната снимка.
                CaseTheoryDraft eb16Draft = boardSession.CaseTheories.GetOrCreateDraft("CASE-ORION-01");
                eb16Draft.TryAddReferencedPhotoSource("synthetic-eb16-missing-asset");
                eb16Draft.TryAddReferencedPhotoSource("synthetic-eb16-case0017-photo");
                eb16Draft.TryAddReferencedPhotoSource("synthetic-eb16-nonexistent");
                theoryViewComponent.RefreshFromSession();

                foreach (string badPhotoId in new[] { "synthetic-eb16-missing-asset", "synthetic-eb16-case0017-photo", "synthetic-eb16-nonexistent" })
                {
                    Transform badRow = refList.Find("TheoryReferencedPhotoItem_" + badPhotoId);
                    Require(badRow != null
                        && badRow.Find("InfoColumn/TitleText").GetComponent<TMP_Text>().text == "Недостъпна снимка"
                        && !badRow.Find("PreviewTheorySourceButton").GetComponent<Button>().interactable
                        && !theoryViewComponent.OpenSourcePreview(AttachedSourceType.Photo, badPhotoId)
                        && !previewOverlay.activeSelf,
                        "EB-016: unavailable/missing-asset/cross-case photo shows Недостъпна снимка and refuses preview: " + badPhotoId);
                    badRow.Find("RemoveTheorySourceButton").GetComponent<Button>().onClick.Invoke();
                }

                // Save/Load v5 с документ + снимка и повторен Load без дубликати.
                Require(save.TrySave(out saveMessage), "EB-016: Save v5 with photo + document references succeeds: " + saveMessage);
                eb16Draft.RemoveReferencedPhotoSource("synthetic-eb16-orion-photo");
                Require(save.TryLoad(out loadMessage)
                    && boardSession.CaseTheories.TryGetDraft("CASE-ORION-01", out CaseTheoryDraft loadedEb16Draft)
                    && loadedEb16Draft.ReferencedSourceIds.Count == 1
                    && loadedEb16Draft.ReferencedPhotoSourceIds.Count == 1
                    && loadedEb16Draft.ReferencedPhotoSourceIds[0] == "synthetic-eb16-orion-photo",
                    "EB-016: Save/Load restores both document and photo references");
                Require(save.TryLoad(out loadMessage)
                    && boardSession.CaseTheories.GetOrCreateDraft("CASE-ORION-01").ReferencedPhotoSourceIds.Count == 1,
                    "EB-016: repeated Load does not duplicate photo references");
            }
            finally
            {
                while (runtimePhotosList.Count > originalPhotosCount)
                    runtimePhotosList.RemoveAt(runtimePhotosList.Count - 1);
                UnityEngine.Object.DestroyImmediate(synthSprite);
                UnityEngine.Object.DestroyImmediate(synthTex);
            }

            // Възстановява базовия v4 save за останалата част от smoke теста.
            File.WriteAllText(savePath, savedV4Json);
            Require(save.TryLoad(out loadMessage)
                && boardSession.PlayerNotes.Notes.Count == 2
                && boardSession.AttachedSources.Sources.Count == 2
                && boardSession.Timeline.Events.Count == 3
                && boardSession.CaseTheories.Drafts.Count == 0,
                "EB-014: restoring baseline v4 save keeps remaining smoke checks intact");

            var documentTool = UnityEngine.Object.FindAnyObjectByType<DocumentInteractable>();
            Vector3 documentPosition = documentTool.transform.position;
            documentTool.Interact();
            Require(interaction.ModeController.ActiveOwner == documentTool, "Document owner entered");
            Require(save.TryLoad(out loadMessage) && interaction.ModeController.ActiveOwner == null,
                "Load releases Document owner");
            Require(Vector3.Distance(documentTool.transform.position, documentPosition) < .001f,
                "Load returns document transform");

            typeof(PauseUIController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pause, null);
            GameObject pauseRoot = GameObject.Find("RuntimePauseCanvas");
            Require(pauseRoot != null && interaction.ModeController.IsPaused, "Pause runtime view opens");
            pauseRoot.transform.Find("MainPanel/SaveButton").GetComponent<Button>().onClick.Invoke();
            Require(pauseRoot.transform.Find("SaveConfirmationPanel").gameObject.activeSelf,
                "Existing slot asks before overwrite");
            pauseRoot.transform.Find("SaveConfirmationPanel/ConfirmButton").GetComponent<Button>().onClick.Invoke();
            Require(pauseRoot.transform.Find("MainPanel").gameObject.activeSelf, "Save returns to menu");
            pauseRoot.transform.Find("MainPanel/SettingsButton").GetComponent<Button>().onClick.Invoke();
            Require(pauseRoot.transform.Find("SettingsPanel").gameObject.activeSelf,
                "Settings view opens from pause");
            pause.Resume();
            Require(!interaction.ModeController.IsPaused && interaction.ModeController.CurrentMode
                == InteractionModeController.Mode.World, "Esc/Resume path restores World");

            Vector3 beforeCorruptLoad = player.transform.position;
            File.WriteAllText(savePath, "{not valid json");
            Require(!save.TryLoad(out loadMessage), "Corrupt slot is rejected");
            Require(Vector3.Distance(player.transform.position, beforeCorruptLoad) < .001f
                && boardSession.PlayerNotes.Notes.Count == 2,
                "Corrupt load does not mutate World or Player Notes");

            OfficeSettingsData originalSettings = OfficeSettingsStore.Current;
            OfficeSettingsData conflict = originalSettings.Copy();
            conflict.bindings[4] = conflict.bindings[0];
            Require(!OfficeSettingsStore.Valid(conflict), "Duplicate binding rejected");
            conflict.bindings[4] = "<Keyboard>/escape";
            Require(!OfficeSettingsStore.Valid(conflict), "Esc binding rejected");
            OfficeSettingsData changed = originalSettings.Copy();
            changed.volume = .3f;
            changed.bindings[4] = "<Keyboard>/f";
            Require(OfficeSettingsStore.TryApply(changed, input, out string settingsMessage),
                "Settings apply succeeds: " + settingsMessage);
            Require(Mathf.Abs(AudioListener.volume - .3f) < .001f
                && input.GetInteractBindingLabel().Contains("F"), "Settings apply to audio and input");
            OfficeSettingsStore.Load();
            Require(Mathf.Abs(OfficeSettingsStore.Current.volume - .3f) < .001f,
                "Settings persist through reload");
            Require(OfficeSettingsStore.TryApply(originalSettings, input, out settingsMessage),
                "Settings restore succeeds: " + settingsMessage);
            Debug.Log("Office Stage 2 checks: save/load pose and power, four tool owners, pause Save/Settings views, corrupt file, settings persistence/conflicts");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            resultCode = 1;
            resultMessage = exception.Message;
        }
        finally
        {
            if (hadSave) File.WriteAllBytes(savePath, previousSave);
            else if (File.Exists(savePath)) File.Delete(savePath);
            if (hadSettings) PlayerPrefs.SetString(SettingsKey, previousSettings);
            else PlayerPrefs.DeleteKey(SettingsKey);
            PlayerPrefs.Save();
        }
        Finish(resultCode, resultMessage);
    }

    private static Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[2].x);
        float xMax = Mathf.Max(corners[0].x, corners[2].x);
        float yMin = Mathf.Min(corners[0].y, corners[2].y);
        float yMax = Mathf.Max(corners[0].y, corners[2].y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        Debug.Log("Office Stage 2 smoke: " + message);
        EditorApplication.Exit(code);
    }
}
