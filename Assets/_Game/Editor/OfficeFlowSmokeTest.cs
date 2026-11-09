using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Контролиран scene/runtime тест. Не замества физически input и човешки playtest.
public static class OfficeFlowSmokeTest
{
    private const string Pending = "DetectiveGame.OfficeFlowSmoke";
    private const string WitnessName = "Елена Мур";
    private const string WitnessObservation = "съобщава за шум в коридора, но не посочва източника му.";
    private const string WitnessClaim = WitnessName + " " + WitnessObservation;
    private const string WitnessPersonSummary = "Съседка, която " + WitnessObservation;
    private const string WitnessRecordTitle = "Свидетелски показания — Елена Мур";
    private const string WitnessRecordType = "Свидетелски показания";
    private const string WitnessRecordBody = "Свидетелката " + WitnessObservation;
    private const string InterestPersonName = "Томас Рийд";
    private const string InterestPersonRole = "Лице от интерес";
    private const string InterestPersonSummary = "Познат на жертвата. В CASE-0017 е включен като лице от интерес.";
    private const string OfficerName = "Полицай Милър";
    private const string OfficerRole = "Полицай";
    private const string OfficerSummary = "Служителят, който е описал претърсването на апартамента.";
    private const string PreliminaryEmailSender = "Отдел Криминалистика";
    private const string PreliminaryEmailSubject = "Предварителен доклад от огледа";
    private const string PreliminaryEmailDate = "15/10/2011 09:32";
    private const string PreliminaryEmailBody = "Предварителният оглед е завършен.\n\nДопълнителните резултати ще бъдат изпратени по-късно.";
    private const string ParkingContact = "Полицай Милър";
    private const string ParkingEmailSubject = "Претърсване на апартамента";
    private const string ParkingEmailDate = "15/10/2011 10:05";
    private const string ParkingEmailBody = "В кухнята намерихме бележка от паркинг.\n\nОтпечатаният час е 22:41.";
    private const string ParkingPhoneDialogue = "Бележката от паркинг е при материалите на бюрото ти. Намерихме я в кухнята на апартамента.";
    private const string ParkingDocumentTranscript = "ПАРКИНГ\n\nОтпечатан час: 22:41";
    private const string ParkingBoardTitle = "Бележка от паркинг";
    private const string ParkingBoardDescription = "Отпечатаният час върху бележката е 22:41.";
    private const string ActiveCaseNumber = "CASE-0017";
    private const string ArchivedCaseNumber = "CASE-0012";
    private const string ArchivedFireReportTitle = "Обобщение от пожарния инспектор";
    private const string ArchivedFireReportType = "Официален доклад";
    private const string ArchivedFireReportDate = "05/10/2011";
    private const string ArchivedFireReportBody = "Наличните наблюдения не установяват умишлено запалване.";
    private const string ArchivedClosureNoteTitle = "Бележка за приключване";
    private const string ArchivedClosureNoteType = "Бележка по делото";
    private const string ArchivedClosureNoteDate = "09/10/2011";
    private const string ArchivedClosureNoteBody = "Делото е архивирано до получаване на нова официална информация.";
    private const string ActiveCaseSummary = "Жертвата е открита мъртва в апартамента си. Първоначалният доклад отбелязва отзоваване на сигнал в 23:20. Разследването продължава.";
    private const string IncidentRecordTitle = "Първоначален доклад за инцидента";
    private const string IncidentRecordType = "Доклад за инцидент";
    private const string IncidentRecordDate = "14/10/2011";
    private const string IncidentRecordBody = "Служителите са се отзовали на сигнал в 23:20 и са обезопасили апартамента.";
    private const string AutopsyRequestTitle = "Искане за аутопсия";
    private const string AutopsyRequestType = "Искане за аутопсия";
    private const string AutopsyRequestDate = "14/10/2011";
    private const string AutopsyRequestBody = "Поискан е официален медицински преглед. Резултатите се очакват.";
    private const string TrafficDatabaseRecordName = "Томас Рийд";
    private const string TrafficDatabaseRecordSummary = "Самоличността и адресът са потвърдени. В наличния запис е посочено едно предишно нарушение на пътя. Записът не съдържа заключение за активното дело.";
    private const string LimitedDatabaseRecordName = "Елена Мур";
    private const string LimitedDatabaseRecordSummary = "Настоящият адрес е потвърден. В наличния запис не са посочени криминални регистрации.";
    private const string VictimDatabaseRecordName = "Даниел Харис";
    private const string VictimDatabaseRecordDateOfBirth = "17/06/1975";
    private const string VictimDatabaseRecordAddress = "бул. „Ривърсайд“ 17";
    private const string VictimDatabaseRecordOccupation = "Архитект";
    private const string VictimDatabaseRecordSummary = "Записът за самоличност е свързан с CASE-0017.";
    private const string PhotoCaptureLabel = "ЗАСНЕТО";
    private const string EntrancePhotoTitle = "Входът на апартамента";
    private const string EntrancePhotoCapturedAt = "14/10/2011 23:38";
    private const string EntrancePhotoLocation = "бул. „Ривърсайд“ 17";
    private const string EntrancePhotoDescription = "Външен изглед на входа, след като мястото е обезопасено.";
    private const string KitchenPhotoTitle = "Кухненската маса";
    private const string KitchenPhotoCapturedAt = "14/10/2011 23:52";
    private const string KitchenPhotoLocation = "Кухнята на апартамента";
    private const string KitchenPhotoDescription = "Обща снимка на масата и близките повърхности.";
    private const string ParkingPhotoTitle = "Бележка от паркинг";
    private const string ParkingPhotoCapturedAt = "15/10/2011 00:06";
    private const string ParkingPhotoLocation = "Кухнята на апартамента";
    private const string ParkingPhotoDescription = "Близък кадър на намерената бележка. Снимката документира само външния ѝ вид.";
    private const string ForensicsReportStatusLabel = "СТАТУС НА ДОКЛАДА";
    private const string ForensicsReportDateLabel = "ДАТА НА ДОКЛАДА";
    private const string PreliminaryReportNumber = "FR-2011-184";
    private const string PreliminaryReportTitle = "Предварителен оглед";
    private const string PreliminaryReportStatus = "ЗАВЪРШЕН";
    private const string PreliminaryReportDate = "14/10/2011";
    private const string PreliminaryReportCase = "CASE-0017";
    private const string PreliminaryReportSummary = "Налични са предварителни наблюдения.";
    private const string PreliminaryReportBody = "Докладът съдържа квалифицирани предварителни наблюдения. Допълнителната лабораторна работа може да промени или уточни резултатите.";
    private const string PendingReportNumber = "FR-2011-191";
    private const string PendingReportTitle = "Анализ на следови материали";
    private const string PendingReportStatus = "ИЗЧАКВА";
    private const string PendingReportDate = "15/10/2011";
    private const string PendingReportCase = "CASE-0017";
    private const string PendingReportSummary = "Пробите са получени в лабораторията.";
    private const string PendingReportBody = "Анализът предстои. Към този момент няма наличен резултат или интерпретация.";
    private static double started;
    private static PlayerInteraction player;
    private static PauseUIController pause;
    private static readonly List<string> errors = new List<string>();
    private static readonly List<string> overflows = new List<string>();
    private static int editorSearchErrors;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Използвай отделен batch Editor.");
        ValidateSetupPhoneContent();
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/DetectiveOffice.unity");
        SessionState.SetBool(Pending, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    private static void ValidateSetupPhoneContent()
    {
        GameObject probe = new GameObject("ComputerUISetupPhoneContentProbe");
        try
        {
            PhoneUIController phone = probe.AddComponent<PhoneUIController>();
            ComputerUISetupTool.ConfigurePrototypePhoneContent(phone);
            SerializedObject serialized = new SerializedObject(phone);
            serialized.Update();
            Require(serialized.FindProperty("contactName").stringValue == ParkingContact
                && serialized.FindProperty("dialogue").stringValue == ParkingPhoneDialogue,
                "Computer setup preserves the Phone source and desk-material link");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
        }
    }

    [InitializeOnLoadMethod]
    private static void Reload() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    private static void Subscribe()
    {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
    }
    private static void Log(string message, string stack, LogType type)
    {
        // Известният Editor search-index exception се отчита отделно, не като gameplay failure.
        if (type == LogType.Exception && stack.Contains("UnityEditor.Search.SearchDatabase"))
        {
            editorSearchErrors++;
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message);
    }
    private static async void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount < 3)
        {
            if (EditorApplication.timeSinceStartup - started > 90) Finish(1, "Play Mode timeout");
            return;
        }
        EditorApplication.update -= Tick;
        try
        {
            player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            pause = UnityEngine.Object.FindAnyObjectByType<PauseUIController>();
            Require(player != null && pause != null, "Player/Pause initialization");
            // Камерата остава в оригиналния office; движението е изключено само за стабилни captures.
            player.GetComponent<FirstPersonController>().enabled = false;
            await Computer();
            await Phone();
            await Document();
            await Board();
            // Свободен ред и повторно влизане без unlock state.
            await Document();
            await Phone();
            await Board();
            await Computer();
            Require(errors.Count == 0, "Unexpected runtime errors: " + string.Join("; ", errors));
            Require(overflows.Count == 0, "Text overflow: " + string.Join("; ", overflows));
            Finish(0, "PASS: Computer/Phone/Document/Board, exclusive apps, navigation/selection/entry/close/phone/board callbacks, sourced statement, reverse re-entry, content, pause x10, UI raycast blocking, transforms, power lifecycle; controlled callbacks, not human input");
        }
        catch (Exception error) { Debug.LogException(error); Finish(1, error.Message); }
    }

    private static async Task Computer()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<ComputerInteractable>();
        var ui = UnityEngine.Object.FindAnyObjectByType<ComputerUIController>();
        var power = UnityEngine.Object.FindAnyObjectByType<ComputerPowerController>();
        Aim(tool);
        tool.Interact();
        await Until(() => power.CurrentState == ComputerPowerState.On);
        var root = Field<GameObject>(ui, "computerRoot");
        var cases = Field<CaseFilesUIController>(ui, "caseFilesUIController");
        var database = Field<PoliceDatabaseUIController>(ui, "policeDatabaseUIController");
        var photos = Field<PhotosUIController>(ui, "photosUIController");
        var forensics = Field<ForensicsUIController>(ui, "forensicsUIController");
        var caseFilesWindow = Field<GameObject>(cases, "caseFilesWindow");
        var databaseWindow = Field<GameObject>(database, "databaseWindow");
        var photosWindow = Field<GameObject>(photos, "photosWindow");
        var forensicsWindow = Field<GameObject>(forensics, "forensicsWindow");
        var overviewPanel = Field<GameObject>(cases, "overviewPanel");
        var peoplePanel = Field<GameObject>(cases, "peoplePanel");
        var recordsPanel = Field<GameObject>(cases, "recordsPanel");
        var powerMenuPanel = Field<GameObject>(ui, "powerMenuPanel");
        ClickButton(root, "CaseFilesIcon");
        Require(caseFilesWindow.activeInHierarchy, "Case files desktop button opens window");
        Require(overviewPanel.activeInHierarchy && !peoplePanel.activeInHierarchy && !recordsPanel.activeInHierarchy,
            "Case files open on Overview tab");
        CaseData activeCase = Field<List<CaseData>>(cases, "cases").Single(item => item.CaseNumber == ActiveCaseNumber);
        CaseEntryUI[] caseEntries = Field<Transform>(cases, "caseListContent")
            .GetComponentsInChildren<CaseEntryUI>(false);
        Require(caseEntries.Length == Field<List<CaseData>>(cases, "cases").Count(item => item != null),
            "Case list has one active entry per case");
        CaseEntryUI activeCaseEntry = caseEntries.FirstOrDefault(entry =>
            ReferenceEquals(Field<CaseData>(entry, "caseData"), activeCase));
        Require(activeCaseEntry != null, "Active case entry exists");
        activeCaseEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "caseNumberText").text == activeCase.CaseNumber
            && Field<TMP_Text>(cases, "caseNameText").text == activeCase.CaseName
            && Field<TMP_Text>(cases, "statusText").text == activeCase.Status
            && Field<TMP_Text>(cases, "dateText").text == activeCase.DateOpened
            && Field<TMP_Text>(cases, "locationText").text == activeCase.Location
            && Field<TMP_Text>(cases, "victimText").text == activeCase.Victim
            && Field<TMP_Text>(cases, "leadDetectiveText").text == activeCase.LeadDetective
            && Field<TMP_Text>(cases, "summaryText").text == activeCase.Summary,
            "Case entry callback maps exact overview data");
        Require(Field<TMP_Text>(cases, "summaryText").text == ActiveCaseSummary,
            "Case overview attributes 23:20 to the incident report instead of discovery time");
        CasePersonData person = activeCase.People.First();
        ClickButton(root, "PeopleButton");
        Require(!overviewPanel.activeInHierarchy && peoplePanel.activeInHierarchy && !recordsPanel.activeInHierarchy,
            "People tab button opens only People panel");
        CasePersonEntryUI[] personEntries = Field<Transform>(cases, "peopleListContent")
            .GetComponentsInChildren<CasePersonEntryUI>(false);
        Require(personEntries.Length == activeCase.People.Count(item => item != null),
            "People list has one active entry per person");
        CasePersonEntryUI personEntry = personEntries.FirstOrDefault(entry =>
            ReferenceEquals(Field<CasePersonData>(entry, "personData"), person));
        Require(personEntry != null, "Case person entry exists");
        personEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "personNameText").text == person.Name
            && Field<TMP_Text>(cases, "personRoleText").text == person.Role
            && Field<TMP_Text>(cases, "personDescriptionText").text == person.ShortDescription,
            "Case person entry callback maps exact data");
        CasePersonData witness = activeCase.People.Single(item => item.Name == WitnessName);
        CasePersonEntryUI witnessEntry = personEntries.Single(entry =>
            ReferenceEquals(Field<CasePersonData>(entry, "personData"), witness));
        witnessEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "personDescriptionText").text == WitnessPersonSummary,
            "Witness summary preserves the source-limited claim without interpreting noise as movement");
        CasePersonData interestPerson = activeCase.People.Single(item => item.Name == InterestPersonName);
        CasePersonEntryUI interestPersonEntry = personEntries.Single(entry =>
            ReferenceEquals(Field<CasePersonData>(entry, "personData"), interestPerson));
        interestPersonEntry.GetComponent<Button>().onClick.Invoke();
        Require(interestPerson.Role == InterestPersonRole
            && interestPerson.ShortDescription == InterestPersonSummary
            && Field<TMP_Text>(cases, "personNameText").text == InterestPersonName
            && Field<TMP_Text>(cases, "personRoleText").text == InterestPersonRole
            && Field<TMP_Text>(cases, "personDescriptionText").text == InterestPersonSummary,
            "Interest-person summary does not cite an inaccessible incident-report claim");
        CasePersonData officer = activeCase.People.Single(item => item.Name == OfficerName);
        Require(officer.Role == OfficerRole && officer.ShortDescription == OfficerSummary,
            "Officer summary stays limited to the authored search description without unsupported leadership");
        CasePersonEntryUI officerEntry = personEntries.Single(entry =>
            ReferenceEquals(Field<CasePersonData>(entry, "personData"), officer));
        officerEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "personNameText").text == OfficerName
            && Field<TMP_Text>(cases, "personRoleText").text == OfficerRole
            && Field<TMP_Text>(cases, "personDescriptionText").text == OfficerSummary,
            "Officer Person callback renders exact source-limited role data");
        CaseRecordData caseRecord = activeCase.Records.Single(item => item.Title == IncidentRecordTitle);
        ClickButton(root, "RecordsButton");
        Require(!overviewPanel.activeInHierarchy && !peoplePanel.activeInHierarchy && recordsPanel.activeInHierarchy,
            "Records tab button opens only Records panel");
        CaseRecordEntryUI[] recordEntries = Field<Transform>(cases, "recordsListContent")
            .GetComponentsInChildren<CaseRecordEntryUI>(false);
        Require(recordEntries.Length == activeCase.Records.Count(item => item != null),
            "Records list has one active entry per record");
        CaseRecordEntryUI caseRecordEntry = recordEntries.FirstOrDefault(entry =>
            ReferenceEquals(Field<CaseRecordData>(entry, "recordData"), caseRecord));
        Require(caseRecordEntry != null, "Case record entry exists");
        caseRecordEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "recordTitleText").text == caseRecord.Title
            && Field<TMP_Text>(cases, "recordTypeText").text == caseRecord.RecordType
            && Field<TMP_Text>(cases, "recordDateText").text == caseRecord.Date
            && Field<TMP_Text>(cases, "recordBodyText").text == caseRecord.Body,
            "Case record entry callback maps exact data");
        Require(Field<TMP_Text>(cases, "recordTitleText").text == IncidentRecordTitle
            && Field<TMP_Text>(cases, "recordTypeText").text == IncidentRecordType
            && Field<TMP_Text>(cases, "recordDateText").text == IncidentRecordDate
            && Field<TMP_Text>(cases, "recordBodyText").text == IncidentRecordBody,
            "Incident record preserves the source and response-time boundary");
        CaseRecordData witnessRecord = activeCase.Records.Single(item => item.Title == WitnessRecordTitle);
        CaseRecordEntryUI witnessRecordEntry = recordEntries.Single(entry =>
            ReferenceEquals(Field<CaseRecordData>(entry, "recordData"), witnessRecord));
        witnessRecordEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "recordTitleText").text == WitnessRecordTitle
            && Field<TMP_Text>(cases, "recordTypeText").text == WitnessRecordType
            && Field<TMP_Text>(cases, "recordBodyText").text == WitnessRecordBody,
            "Witness record callback preserves source attribution and the Board claim boundary");
        CaseRecordData autopsyRequest = activeCase.Records.Single(item => item.Title == AutopsyRequestTitle);
        Require(autopsyRequest.RecordType == AutopsyRequestType
            && autopsyRequest.Date == AutopsyRequestDate
            && autopsyRequest.Body == AutopsyRequestBody,
            "Autopsy request remains a request without invented examination results");
        CaseRecordEntryUI autopsyRequestEntry = recordEntries.Single(entry =>
            ReferenceEquals(Field<CaseRecordData>(entry, "recordData"), autopsyRequest));
        autopsyRequestEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "recordTitleText").text == AutopsyRequestTitle
            && Field<TMP_Text>(cases, "recordTypeText").text == AutopsyRequestType
            && Field<TMP_Text>(cases, "recordDateText").text == AutopsyRequestDate
            && Field<TMP_Text>(cases, "recordBodyText").text == AutopsyRequestBody,
            "Autopsy request callback renders exact pending-result state");
        CaseData archivedCase = Field<List<CaseData>>(cases, "cases")
            .Single(item => item.CaseNumber == ArchivedCaseNumber);
        CaseEntryUI archivedCaseEntry = caseEntries.Single(entry =>
            ReferenceEquals(Field<CaseData>(entry, "caseData"), archivedCase));
        archivedCaseEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "caseNumberText").text == archivedCase.CaseNumber
            && Field<TMP_Text>(cases, "caseNameText").text == archivedCase.CaseName
            && Field<TMP_Text>(cases, "statusText").text == archivedCase.Status
            && Field<TMP_Text>(cases, "summaryText").text == archivedCase.Summary,
            "Case switch maps archived overview data");
        ClickButton(root, "PeopleButton");
        CasePersonEntryUI[] archivedPersonEntries = Field<Transform>(cases, "peopleListContent")
            .GetComponentsInChildren<CasePersonEntryUI>(false);
        Require(archivedPersonEntries.Length == archivedCase.People.Count(item => item != null)
            && archivedPersonEntries.All(entry => archivedCase.People.Contains(Field<CasePersonData>(entry, "personData"))),
            "Case switch replaces People entries without stale active-case rows");
        Require(Field<TMP_Text>(cases, "personNameText").text == string.Empty
            && Field<TMP_Text>(cases, "personRoleText").text == string.Empty
            && Field<TMP_Text>(cases, "personDescriptionText").text == string.Empty,
            "Case switch clears stale Person details");
        CasePersonData archivedPerson = archivedCase.People.Single(item => item != null);
        archivedPersonEntries.Single(entry => ReferenceEquals(Field<CasePersonData>(entry, "personData"), archivedPerson))
            .GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(cases, "personNameText").text == archivedPerson.Name
            && Field<TMP_Text>(cases, "personRoleText").text == archivedPerson.Role
            && Field<TMP_Text>(cases, "personDescriptionText").text == archivedPerson.ShortDescription,
            "Archived Person callback maps exact selected-case data");
        ClickButton(root, "RecordsButton");
        CaseRecordEntryUI[] archivedRecordEntries = Field<Transform>(cases, "recordsListContent")
            .GetComponentsInChildren<CaseRecordEntryUI>(false);
        Require(archivedRecordEntries.Length == archivedCase.Records.Count(item => item != null)
            && archivedRecordEntries.All(entry => archivedCase.Records.Contains(Field<CaseRecordData>(entry, "recordData"))),
            "Case switch replaces Record entries without stale active-case rows");
        Require(Field<TMP_Text>(cases, "recordTitleText").text == string.Empty
            && Field<TMP_Text>(cases, "recordTypeText").text == string.Empty
            && Field<TMP_Text>(cases, "recordDateText").text == string.Empty
            && Field<TMP_Text>(cases, "recordBodyText").text == string.Empty,
            "Case switch clears stale Record details");
        CaseRecordData archivedFireReport = archivedCase.Records.Single(item => item.Title == ArchivedFireReportTitle);
        Require(archivedFireReport.RecordType == ArchivedFireReportType
            && archivedFireReport.Date == ArchivedFireReportDate
            && archivedFireReport.Body == ArchivedFireReportBody,
            "Archived fire report keeps limited observations separate from objective cause");
        CaseRecordData archivedClosureNote = archivedCase.Records.Single(item => item.Title == ArchivedClosureNoteTitle);
        Require(archivedClosureNote.RecordType == ArchivedClosureNoteType
            && archivedClosureNote.Date == ArchivedClosureNoteDate
            && archivedClosureNote.Body == ArchivedClosureNoteBody,
            "Archived closure note remains conditional on new official information");
        CaseRecordData[] archivedRecordsInSelectionOrder = { archivedFireReport, archivedClosureNote };
        foreach (CaseRecordData selectedRecord in archivedRecordsInSelectionOrder)
        {
            archivedRecordEntries.Single(entry =>
                    ReferenceEquals(Field<CaseRecordData>(entry, "recordData"), selectedRecord))
                .GetComponent<Button>().onClick.Invoke();
            Require(Field<TMP_Text>(cases, "recordTitleText").text == selectedRecord.Title
                && Field<TMP_Text>(cases, "recordTypeText").text == selectedRecord.RecordType
                && Field<TMP_Text>(cases, "recordDateText").text == selectedRecord.Date
                && Field<TMP_Text>(cases, "recordBodyText").text == selectedRecord.Body,
                "Archived Record callback replaces details for the selected record");
        }
        activeCaseEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<Transform>(cases, "recordsListContent").GetComponentsInChildren<CaseRecordEntryUI>(false).Length
            == activeCase.Records.Count(item => item != null)
            && Field<TMP_Text>(cases, "recordTitleText").text == string.Empty,
            "Reverse case switch restores active Records without stale details");
        ClickButton(root, "PeopleButton");
        Require(Field<Transform>(cases, "peopleListContent").GetComponentsInChildren<CasePersonEntryUI>(false).Length
            == activeCase.People.Count(item => item != null)
            && Field<TMP_Text>(cases, "personNameText").text == string.Empty,
            "Reverse case switch restores active People without stale details");
        ClickButton(root, "OverviewButton");
        Require(overviewPanel.activeInHierarchy && !peoplePanel.activeInHierarchy && !recordsPanel.activeInHierarchy,
            "Overview tab button restores only Overview panel");
        await Capture("computer-case-record", Field<GameObject>(ui, "computerRoot"));
        CheckCloseCallback(caseFilesWindow, "CloseCaseFilesButton", ui.OpenCaseFiles, "Case files");
        ClickButton(root, "DatabaseIcon");
        Require(!caseFilesWindow.activeInHierarchy && databaseWindow.activeInHierarchy,
            "Database replaces case files");
        DatabasePersonData databaseRecord = Field<List<DatabasePersonData>>(database, "records")
            .Single(item => item.FullName == TrafficDatabaseRecordName);
        string[] databaseQueries = { "Рийд", "томас", "ТОМАС" };
        foreach (string query in databaseQueries)
        {
            Field<TMP_InputField>(database, "searchInput").SetTextWithoutNotify(query);
            ClickButton(root, "SearchButton");
            Require(Field<TMP_Text>(database, "resultsStatusText").text == "Намерени записи: 1",
                "Database Cyrillic case-insensitive substring search returns one Thomas record");
            DatabaseResultEntryUI[] databaseEntries = Field<Transform>(database, "resultsContent")
                .GetComponentsInChildren<DatabaseResultEntryUI>(false);
            Require(databaseEntries.Length == 1,
                "Database Cyrillic query builds one active result entry");
            DatabaseResultEntryUI databaseEntry = databaseEntries.Single(entry =>
                ReferenceEquals(Field<DatabasePersonData>(entry, "personData"), databaseRecord));
            databaseEntry.GetComponent<Button>().onClick.Invoke();
            Require(Field<TMP_Text>(database, "fullNameText").text == TrafficDatabaseRecordName,
                "Database Cyrillic query result callback selects Thomas record");
        }
        Require(Field<TMP_Text>(database, "fullNameText").text == databaseRecord.FullName
            && Field<TMP_Text>(database, "dateOfBirthText").text == databaseRecord.DateOfBirth
            && Field<TMP_Text>(database, "addressText").text == databaseRecord.Address
            && Field<TMP_Text>(database, "occupationText").text == databaseRecord.Occupation
            && Field<TMP_Text>(database, "recordSummaryText").text == databaseRecord.RecordSummary,
            "Database result entry callback maps exact record data");
        Require(databaseRecord.RecordSummary == TrafficDatabaseRecordSummary
            && Field<TMP_Text>(database, "fullNameText").text == TrafficDatabaseRecordName
            && Field<TMP_Text>(database, "recordSummaryText").text == TrafficDatabaseRecordSummary,
            "Database traffic history claim is limited to the available record");
        await Capture("computer-database", Field<GameObject>(ui, "computerRoot"));
        DatabasePersonData limitedRecord = Field<List<DatabasePersonData>>(database, "records")
            .Single(item => item.FullName == LimitedDatabaseRecordName);
        Field<TMP_InputField>(database, "searchInput").SetTextWithoutNotify("Елена");
        ClickButton(root, "SearchButton");
        Require(Field<TMP_Text>(database, "resultsStatusText").text == "Намерени записи: 1",
            "Database Search finds the source-limited record");
        DatabaseResultEntryUI[] limitedEntries = Field<Transform>(database, "resultsContent")
            .GetComponentsInChildren<DatabaseResultEntryUI>(false);
        DatabaseResultEntryUI limitedEntry = limitedEntries.Single(entry =>
            ReferenceEquals(Field<DatabasePersonData>(entry, "personData"), limitedRecord));
        limitedEntry.GetComponent<Button>().onClick.Invoke();
        Require(limitedRecord.RecordSummary == LimitedDatabaseRecordSummary
            && Field<TMP_Text>(database, "fullNameText").text == LimitedDatabaseRecordName
            && Field<TMP_Text>(database, "recordSummaryText").text == LimitedDatabaseRecordSummary,
            "Database record limits negative history claim to the available record");
        DatabasePersonData victimRecord = Field<List<DatabasePersonData>>(database, "records")
            .Single(item => item.FullName == VictimDatabaseRecordName);
        Require(victimRecord.DateOfBirth == VictimDatabaseRecordDateOfBirth
            && victimRecord.Address == VictimDatabaseRecordAddress
            && victimRecord.Occupation == VictimDatabaseRecordOccupation
            && victimRecord.RecordSummary == VictimDatabaseRecordSummary,
            "Database case linkage does not imply involvement in the crime");
        Field<TMP_InputField>(database, "searchInput").SetTextWithoutNotify("Даниел");
        ClickButton(root, "SearchButton");
        Require(Field<TMP_Text>(database, "resultsStatusText").text == "Намерени записи: 1",
            "Database Search finds the victim identity record");
        DatabaseResultEntryUI victimEntry = Field<Transform>(database, "resultsContent")
            .GetComponentsInChildren<DatabaseResultEntryUI>(false)
            .Single(entry => ReferenceEquals(Field<DatabasePersonData>(entry, "personData"), victimRecord));
        victimEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(database, "fullNameText").text == VictimDatabaseRecordName
            && Field<TMP_Text>(database, "dateOfBirthText").text == VictimDatabaseRecordDateOfBirth
            && Field<TMP_Text>(database, "addressText").text == VictimDatabaseRecordAddress
            && Field<TMP_Text>(database, "occupationText").text == VictimDatabaseRecordOccupation
            && Field<TMP_Text>(database, "recordSummaryText").text == VictimDatabaseRecordSummary,
            "Database victim result callback renders exact source-limited record");
        Field<TMP_InputField>(database, "searchInput").SetTextWithoutNotify("Несъществуващ");
        ClickButton(root, "SearchButton");
        Require(Field<TMP_Text>(database, "resultsStatusText").text == "Няма намерени записи"
            && Field<Transform>(database, "resultsContent").GetComponentsInChildren<DatabaseResultEntryUI>(false).Length == 0
            && Field<TMP_Text>(database, "fullNameText").text == string.Empty
            && Field<TMP_Text>(database, "recordSummaryText").text == string.Empty,
            "Database no-match search shows Bulgarian status and clears stale details");
        Field<TMP_InputField>(database, "searchInput").SetTextWithoutNotify("   ");
        ClickButton(root, "SearchButton");
        Require(Field<TMP_Text>(database, "resultsStatusText").text == "Въведете пълно име"
            && Field<Transform>(database, "resultsContent").GetComponentsInChildren<DatabaseResultEntryUI>(false).Length == 0
            && Field<TMP_Text>(database, "fullNameText").text == string.Empty
            && Field<TMP_Text>(database, "recordSummaryText").text == string.Empty,
            "Database whitespace query shows Bulgarian prompt and keeps results empty");
        ClickButton(root, "ClearButton");
        Require(Field<TMP_InputField>(database, "searchInput").text == string.Empty
            && Field<TMP_Text>(database, "resultsStatusText").text == string.Empty
            && Field<Transform>(database, "resultsContent").GetComponentsInChildren<DatabaseResultEntryUI>(false).Length == 0
            && Field<TMP_Text>(database, "fullNameText").text == string.Empty
            && Field<TMP_Text>(database, "recordSummaryText").text == string.Empty,
            "Database Clear button removes query, results and details");
        CheckCloseCallback(databaseWindow, "CloseDatabaseButton", ui.OpenDatabase, "Database");
        ClickButton(root, "PhotosIcon");
        Require(!databaseWindow.activeInHierarchy && photosWindow.activeInHierarchy,
            "Photos replace database");
        TMP_Text photoDateLabel = photosWindow.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.name == "PhotoDateLabelText");
        Require(photoDateLabel.text == PhotoCaptureLabel,
            "Photo timestamp is labeled as capture time instead of generic event date");
        List<PhotoData> photoRecords = Field<List<PhotoData>>(photos, "photos");
        PhotoData entrancePhoto = photoRecords.Single(item => item.Title == EntrancePhotoTitle);
        Require(entrancePhoto.Date == EntrancePhotoCapturedAt
            && entrancePhoto.Location == EntrancePhotoLocation
            && entrancePhoto.Description == EntrancePhotoDescription,
            "Entrance photo stays observational without identifying an inferred perpetrator");
        PhotoData kitchenPhoto = photoRecords.Single(item => item.Title == KitchenPhotoTitle);
        Require(kitchenPhoto.Date == KitchenPhotoCapturedAt
            && kitchenPhoto.Location == KitchenPhotoLocation
            && kitchenPhoto.Description == KitchenPhotoDescription,
            "Kitchen photo metadata stays limited to visible scene coverage");
        PhotoData parkingPhoto = photoRecords.Single(item => item.Title == ParkingPhotoTitle);
        Require(parkingPhoto.Date == ParkingPhotoCapturedAt
            && parkingPhoto.Location == ParkingPhotoLocation
            && parkingPhoto.Description == ParkingPhotoDescription,
            "Parking photo metadata preserves capture time, location and observation boundary");
        PhotoEntryUI[] photoEntries = Field<Transform>(photos, "photoListContent")
            .GetComponentsInChildren<PhotoEntryUI>(false);
        Require(photoEntries.Length == photoRecords.Count(item => item != null),
            "Gallery has one active entry per photo");
        Image previewImage = Field<Image>(photos, "previewImage");
        PhotoData[] photoSelectionOrder = { entrancePhoto, kitchenPhoto, parkingPhoto };
        foreach (PhotoData selectedPhoto in photoSelectionOrder)
        {
            PhotoEntryUI photoEntry = photoEntries.Single(entry =>
                ReferenceEquals(Field<PhotoData>(entry, "photoData"), selectedPhoto));
            photoEntry.GetComponent<Button>().onClick.Invoke();
            Require(Field<TMP_Text>(photos, "titleText").text == selectedPhoto.Title
                && Field<TMP_Text>(photos, "dateText").text == selectedPhoto.Date
                && Field<TMP_Text>(photos, "locationText").text == selectedPhoto.Location
                && Field<TMP_Text>(photos, "descriptionText").text == selectedPhoto.Description,
                "Photo entry callback replaces metadata for the selected record");
            Require(previewImage.sprite == selectedPhoto.Image
                && previewImage.enabled == (selectedPhoto.Image != null),
                "Photo entry callback replaces preview for the selected record");
        }
        await Capture("computer-photos", Field<GameObject>(ui, "computerRoot"));
        CheckCloseCallback(photosWindow, "ClosePhotosButton", ui.OpenPhotos, "Photos");
        ClickButton(root, "ForensicsIcon");
        Require(!photosWindow.activeInHierarchy && forensicsWindow.activeInHierarchy,
            "Forensics replace photos");
        TMP_Text reportStatusLabel = forensicsWindow.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.name == "ReportStatusLabelText");
        Require(reportStatusLabel.text == ForensicsReportStatusLabel,
            "Forensics status is labeled as report lifecycle instead of finding finality");
        TMP_Text reportDateLabel = forensicsWindow.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.name == "ReportDateLabelText");
        Require(reportDateLabel.text == ForensicsReportDateLabel,
            "Forensics date is labeled as report metadata instead of event time");
        ForensicsReportData report = Field<List<ForensicsReportData>>(forensics, "reports")
            .Single(item => item.ReportNumber == PreliminaryReportNumber);
        Require(report.Title == PreliminaryReportTitle
            && report.Status == PreliminaryReportStatus
            && report.Date == PreliminaryReportDate
            && report.RelatedCase == PreliminaryReportCase
            && report.Summary == PreliminaryReportSummary
            && report.FullReport == PreliminaryReportBody,
            "Preliminary forensic report preserves qualified findings and report lifecycle");
        ForensicsReportEntryUI[] reportEntries = Field<Transform>(forensics, "reportsContent")
            .GetComponentsInChildren<ForensicsReportEntryUI>(false);
        Require(reportEntries.Length == Field<List<ForensicsReportData>>(forensics, "reports").Count(item => item != null),
            "Forensics list has one active entry per report");
        ForensicsReportEntryUI reportEntry = reportEntries.FirstOrDefault(entry =>
            ReferenceEquals(Field<ForensicsReportData>(entry, "reportData"), report));
        Require(reportEntry != null, "Forensics report entry exists");
        reportEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(forensics, "reportNumberText").text == report.ReportNumber
            && Field<TMP_Text>(forensics, "titleText").text == report.Title
            && Field<TMP_Text>(forensics, "statusText").text == report.Status
            && Field<TMP_Text>(forensics, "dateText").text == report.Date
            && Field<TMP_Text>(forensics, "relatedCaseText").text == report.RelatedCase
            && Field<TMP_Text>(forensics, "summaryText").text == report.Summary
            && Field<TMP_Text>(forensics, "fullReportText").text == report.FullReport,
            "Forensics entry callback maps exact report data");
        await Capture("computer-forensics", Field<GameObject>(ui, "computerRoot"));
        ForensicsReportData pendingReport = Field<List<ForensicsReportData>>(forensics, "reports")
            .Single(item => item.ReportNumber == PendingReportNumber);
        Require(pendingReport.Title == PendingReportTitle
            && pendingReport.Status == PendingReportStatus
            && pendingReport.Date == PendingReportDate
            && pendingReport.RelatedCase == PendingReportCase
            && pendingReport.Summary == PendingReportSummary
            && pendingReport.FullReport == PendingReportBody,
            "Pending forensic report does not invent result or interpretation");
        ForensicsReportEntryUI pendingReportEntry = reportEntries.Single(entry =>
            ReferenceEquals(Field<ForensicsReportData>(entry, "reportData"), pendingReport));
        pendingReportEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(forensics, "reportNumberText").text == PendingReportNumber
            && Field<TMP_Text>(forensics, "titleText").text == PendingReportTitle
            && Field<TMP_Text>(forensics, "statusText").text == PendingReportStatus
            && Field<TMP_Text>(forensics, "dateText").text == PendingReportDate
            && Field<TMP_Text>(forensics, "relatedCaseText").text == PendingReportCase
            && Field<TMP_Text>(forensics, "summaryText").text == PendingReportSummary
            && Field<TMP_Text>(forensics, "fullReportText").text == PendingReportBody,
            "Pending forensic report callback renders exact qualified state");
        CheckCloseCallback(forensicsWindow, "CloseForensicsButton", ui.OpenForensics, "Forensics");
        ClickButton(root, "MailIcon");
        var email = Field<EmailUIController>(ui, "emailUIController");
        var mailWindow = Field<GameObject>(email, "mailWindow");
        Require(!forensicsWindow.activeInHierarchy && mailWindow.activeInHierarchy,
            "Mail replaces forensics");
        List<EmailData> emailRecords = Field<List<EmailData>>(email, "emails");
        EmailData preliminaryEmail = emailRecords.Single(item => item.Subject == PreliminaryEmailSubject);
        var record = emailRecords.Single(e => e.Subject == ParkingEmailSubject);
        Require(preliminaryEmail.Sender == PreliminaryEmailSender
            && preliminaryEmail.Date == PreliminaryEmailDate
            && preliminaryEmail.Body == PreliminaryEmailBody
            && record.Date == ParkingEmailDate,
            "Email timestamps follow incident and evidence-capture chronology");
        Require(record.Sender == ParkingContact
            && record.Body == ParkingEmailBody,
            "Parking email preserves source metadata and printed-time boundary");
        var emailEntries = Field<Transform>(email, "emailListContent")
            .GetComponentsInChildren<EmailEntryUI>(true);
        int expectedEmailCount = emailRecords.Count(item => item != null);
        Require(emailEntries.Length == expectedEmailCount, "Inbox has one entry per email");
        EmailEntryUI preliminaryEntry = emailEntries.Single(entry =>
            ReferenceEquals(Field<EmailData>(entry, "emailData"), preliminaryEmail));
        preliminaryEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(email, "senderText").text == PreliminaryEmailSender
            && Field<TMP_Text>(email, "subjectText").text == PreliminaryEmailSubject
            && Field<TMP_Text>(email, "dateText").text == PreliminaryEmailDate
            && Field<TMP_Text>(email, "bodyText").text == PreliminaryEmailBody,
            "Preliminary email callback renders post-incident source metadata");
        EmailEntryUI recordEntry = emailEntries.Single(entry => ReferenceEquals(Field<EmailData>(entry, "emailData"), record));
        recordEntry.GetComponent<Button>().onClick.Invoke();
        Require(Field<TMP_Text>(email, "senderText").text == record.Sender
            && Field<TMP_Text>(email, "subjectText").text == record.Subject
            && Field<TMP_Text>(email, "dateText").text == record.Date
            && Field<TMP_Text>(email, "bodyText").text == record.Body,
            "Email entry callback maps exact source metadata and body");
        await CheckPause(tool, mailWindow);
        await Capture("computer-mail", root);
        CheckCloseCallback(mailWindow, "CloseMailButton", ui.OpenMail, "Mail");
        Require(Field<Transform>(email, "emailListContent").GetComponentsInChildren<EmailEntryUI>(true).Length
            == expectedEmailCount, "Inbox re-entry does not duplicate entries");
        ClickButton(root, "ExitComputerButton");
        World();
        tool.Interact();
        ClickButton(root, "PowerButton");
        Require(powerMenuPanel.activeInHierarchy, "Power button opens power menu before Sleep");
        ClickButton(root, "SleepButton");
        Require(power.CurrentState == ComputerPowerState.Sleeping, "Sleep button enters Sleeping state");
        World();
        tool.Interact();
        Require(power.CurrentState == ComputerPowerState.On, "Wake state");
        ClickButton(root, "DatabaseIcon");
        ClickButton(root, "PowerButton");
        Require(powerMenuPanel.activeInHierarchy, "Power button opens power menu before Restart");
        ClickButton(root, "RestartButton");
        Require(power.CurrentState == ComputerPowerState.Booting
            && !databaseWindow.activeInHierarchy,
            "Restart boot closes active application");
        await Until(() => power.CurrentState == ComputerPowerState.On);
        ClickButton(root, "PhotosIcon");
        ClickButton(root, "PowerButton");
        Require(powerMenuPanel.activeInHierarchy, "Power button opens power menu before Shutdown");
        ClickButton(root, "ShutdownButton");
        Require(!photosWindow.activeInHierarchy,
            "Shutdown closes active application");
        await Until(() => power.CurrentState == ComputerPowerState.Off);
        World();
    }

    private static void CheckCloseCallback(GameObject window, string buttonName, Action reopen, string label)
    {
        Button closeButton = window.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.name == buttonName);
        Require(closeButton != null && closeButton.interactable, label + " close button wiring");
        closeButton.onClick.Invoke();
        Require(!window.activeInHierarchy, label + " close callback");
        reopen();
        Require(window.activeInHierarchy, label + " re-entry after close");
    }

    private static async Task Phone()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<PhoneInteractable>();
        var ui = UnityEngine.Object.FindAnyObjectByType<PhoneUIController>();
        Aim(tool);
        tool.Interact();
        var phoneRoot = Field<GameObject>(ui, "phoneRoot");
        var contactsPanel = Field<GameObject>(ui, "contactsPanel");
        var callPanel = Field<GameObject>(ui, "callPanel");
        Require(Field<TMP_Text>(ui, "contactNameText").text == Field<string>(ui, "contactName")
            && Field<TMP_Text>(ui, "dialogueText").text == Field<string>(ui, "dialogue"),
            "Phone shows exact contact source and dialogue");
        Require(Field<string>(ui, "contactName") == ParkingContact
            && Field<string>(ui, "dialogue") == ParkingPhoneDialogue,
            "Phone preserves source and desk-material link without reinterpreting 22:41");
        ClickButton(phoneRoot, "CallButton");
        Require(callPanel.activeInHierarchy && !contactsPanel.activeInHierarchy, "Call button opens call panel");
        await CheckPause(tool, callPanel);
        await Capture("phone-call", phoneRoot);
        ClickButton(phoneRoot, "HangUpButton");
        Require(contactsPanel.activeInHierarchy && !callPanel.activeInHierarchy, "HangUp button restores contacts");
        ClickButton(phoneRoot, "ExitPhoneButton");
        World();
    }

    private static void ClickButton(GameObject root, string buttonName)
    {
        Button button = root.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(candidate => candidate.name == buttonName);
        Require(button != null && button.interactable, buttonName + " wiring");
        button.onClick.Invoke();
    }
    private static async Task Document()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<DocumentInteractable>();
        Aim(tool);
        Transform parent = tool.transform.parent;
        Vector3 position = tool.transform.localPosition, scale = tool.transform.localScale;
        Quaternion rotation = tool.transform.localRotation;
        tool.Interact();
        var root = Field<GameObject>(tool, "readingOverlay");
        Require(root != null && root.transform.Find("ReadingPanel/DocumentText").GetComponent<TMP_Text>().text
            == ParkingDocumentTranscript, "Document preserves the printed-time wording");
        Require(root.transform.Find("ReadingPanel/CloseHint").GetComponent<TMP_Text>().text
            .Contains("[" + player.GetComponent<PlayerInputReader>().GetInteractBindingLabel() + "]"), "Document exit binding hint");
        await CheckPause(tool, root);
        await Capture("document", root);
        tool.Interact(); World();
        Require(tool.transform.parent == parent && Vector3.Distance(position, tool.transform.localPosition) < 0.0001f
            && Quaternion.Angle(rotation, tool.transform.localRotation) < 0.01f && tool.transform.localScale == scale,
            "Document original transform");
        await Task.Delay(50);
        Require(root == null, "Document overlay cleanup");
    }
    private static async Task Board()
    {
        var tool = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardInteractable>();
        var ui = UnityEngine.Object.FindAnyObjectByType<EvidenceBoardUIController>();
        Aim(tool);
        tool.Interact();
        var boardRoot = Field<GameObject>(ui, "boardRoot");
        var boardPanel = Field<GameObject>(ui, "boardPanel");
        var detailsPanel = Field<GameObject>(ui, "evidenceDetailsPanel");
        var boardSession = Field<EvidenceBoardSession>(ui, "session");
        var provenance = Field<TMP_Text[]>(ui, "cardProvenanceTexts");
        string[] expectedTitles =
        {
            "Снимка на жертвата",
            ParkingBoardTitle,
            "Показание — Елена Мур"
        };
        string[] expectedDescriptions =
        {
            "Снимка, направена на местопрестъплението.",
            ParkingBoardDescription,
            WitnessClaim
        };
        string[] expectedSources =
        {
            "СНИМКА · Източник: Наличната снимка от местопрестъплението",
            "ДОКУМЕНТ · Източник: Бележката от паркинг на бюрото",
            "ПОКАЗАНИЕ · Източник: Елена Мур (запис в досието)"
        };
        // Оставя един реален frame за Canvas/CanvasScaler след активиране на BoardRoot.
        await Task.Delay(50);
        Require(provenance[0].text == "СНИМКА · Наличната снимка от местопрестъплението"
            && provenance[1].text == "ДОКУМЕНТ · Бележката от паркинг на бюрото"
            && provenance[2].text == "ПОКАЗАНИЕ · Елена Мур (запис в досието)",
            "Board cards show type and original source without interpreting the claim");
        await Capture("board-overview", boardRoot);
        for (int i = 0; i < 3; i++)
        {
            ClickButton(boardRoot, "EvidenceCard" + (i + 1));
            var title = Field<TMP_Text>(ui, "evidenceTitleText");
            var description = Field<TMP_Text>(ui, "evidenceDescriptionText");
            Require(detailsPanel.activeInHierarchy && !boardPanel.activeInHierarchy,
                "Board card button opens details " + i);
            Require(title.text == expectedTitles[i] && description.text == expectedDescriptions[i],
                "Board card button maps exact evidence " + i);
            Require(Field<TMP_Text>(ui, "detailProvenanceText").text == expectedSources[i],
                "Board detail preserves type and source " + i);
            await CheckPause(tool, detailsPanel);
            await Capture("board-detail-" + i, boardRoot);
            ClickButton(boardRoot, "BackButton");
            Require(boardPanel.activeInHierarchy && !detailsPanel.activeInHierarchy,
                "Board Back button restores overview");
        }
        TMP_InputField notes = Field<TMP_InputField>(ui, "notesInput");
        notes.text = "Моя временна хипотеза";
        ClickButton(boardRoot, "LinkCardsButton");
        ClickButton(boardRoot, "EvidenceCard1");
        ClickButton(boardRoot, "EvidenceCard1");
        Require(boardSession.Links.Count == 0, "Board rejects a self link through card buttons");
        ClickButton(boardRoot, "LinkCardsButton");
        ClickButton(boardRoot, "EvidenceCard1");
        ClickButton(boardRoot, "CancelLinkButton");
        Require(boardSession.Links.Count == 0 && !Field<bool>(ui, "linking"),
            "Board Cancel clears an unfinished link selection");
        ClickButton(boardRoot, "LinkCardsButton");
        ClickButton(boardRoot, "EvidenceCard1");
        ClickButton(boardRoot, "EvidenceCard2");
        Require(boardSession.HasLink(0, 1) && boardSession.Links.Count == 1,
            "Board creates one player link through card buttons");
        ClickButton(boardRoot, "LinkCardsButton");
        ClickButton(boardRoot, "EvidenceCard2");
        ClickButton(boardRoot, "EvidenceCard1");
        Require(boardSession.Links.Count == 1, "Board does not duplicate a reversed link");
        await CheckPause(tool, boardPanel);
        Require(notes.text == "Моя временна хипотеза" && boardSession.Notes == notes.text,
            "Board notes survive Pause and Resume");
        await Capture("board-linked", boardRoot);
        ClickButton(boardRoot, "RemoveLink0");
        Require(boardSession.Links.Count == 0 && boardSession.Notes == "Моя временна хипотеза",
            "Removing a Board link keeps the player's notes");
        ClickButton(boardRoot, "LinkCardsButton");
        ClickButton(boardRoot, "EvidenceCard2");
        ClickButton(boardRoot, "EvidenceCard3");
        ClickButton(boardRoot, "ExitBoardButton");
        World();
        Aim(tool);
        tool.Interact();
        Require(Field<TMP_InputField>(ui, "notesInput").text == "Моя временна хипотеза"
            && boardSession.HasLink(1, 2),
            "Board notes and links survive Exit and re-entry in the same session");
        ClickButton(boardRoot, "RemoveLink2");
        ClickButton(boardRoot, "ExitBoardButton");
        World();
    }
    private static async Task CheckPause(MonoBehaviour owner, GameObject screen)
    {
        var camera = player.GetComponentInChildren<Camera>();
        Quaternion rotation = camera.transform.rotation;
        float fov = camera.fieldOfView;
        for (int i = 0; i < 10; i++)
        {
            Invoke(pause, "Pause");
            await Task.Delay(50);
            Require(player.ModeController.IsPaused && Time.timeScale == 0 && screen.activeInHierarchy
                && ReferenceEquals(player.ModeController.ActiveOwner, owner), "Pause preserves owner/screen");
            Require(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Pause cursor");
            if (i == 0) await Capture("pause-" + owner.GetType().Name, Field<GameObject>(pause, "pauseRoot"));
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * .8f, Screen.height * .5f) }, hits);
            Require(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(Field<GameObject>(pause, "pauseRoot").transform),
                $"Pause blocks underlying pointer raycast ({Screen.width}x{Screen.height}; hits={string.Join(",", hits.Select(h => h.gameObject.name))}; root={Field<GameObject>(pause, "pauseRoot").name})");
            Field<Button>(pause, "resumeButton").onClick.Invoke();
            Require(!player.ModeController.IsPaused && Time.timeScale == 1 && screen.activeInHierarchy
                && ReferenceEquals(player.ModeController.ActiveOwner, owner), "Resume preserves owner/screen");
            Require(Quaternion.Angle(rotation, camera.transform.rotation) < .01f && camera.fieldOfView == fov, "Pause camera");
            Require(!Field<bool>(player.GetComponent<FirstPersonController>(), "controlEnabled"), "No world movement in tool");
            Require(Cursor.visible == !(owner is DocumentInteractable), "Resume cursor visibility");
        }
    }
    private static void World()
    {
        Require(player.ModeController.CurrentMode == InteractionModeController.Mode.World
            && player.ModeController.ActiveOwner == null && player.IsWorldInteractionActive
            && Field<bool>(player.GetComponent<FirstPersonController>(), "controlEnabled"), "World controls restored");
        Require(!Cursor.visible, "World cursor hidden flag");
        Debug.Log("Office flow: physical cursor lock SKIPPED (batch Editor has no focused Game View)");
    }
    private static void Aim(MonoBehaviour tool)
    {
        var camera = player.GetComponentInChildren<Camera>();
        Vector3 center = tool.GetComponent<Collider>().bounds.center;
        foreach (float distance in new[] { 1.2f, 1.8f, 2.4f })
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI / 8;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            point.y = Mathf.Clamp(center.y + .35f, 1.2f, 1.8f);
            if (Physics.CheckSphere(point, .12f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (!Physics.Raycast(point, center - point, out RaycastHit hit, 3f) || hit.collider.gameObject != tool.gameObject) continue;
            camera.transform.position = point;
            camera.transform.rotation = Quaternion.LookRotation(center - point);
            return;
        }
        throw new Exception("No unobstructed viewpoint: " + tool.name);
    }
    private static async Task Capture(string name, GameObject root)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("Office flow: renders/overflow SKIPPED (null graphics device)");
            return;
        }
        Directory.CreateDirectory("Logs/OfficeFlowCaptures");
        var camera = player.GetComponentInChildren<Camera>();
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>()
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        foreach (int width in new[] { 1920, 1280 })
        {
            var texture = new RenderTexture(width, width * 9 / 16, 24);
            texture.Create();
            camera.targetTexture = texture;
            foreach (var canvas in canvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = .5f;
                canvas.scaleFactor = width / 1920f;
            }
            Canvas.ForceUpdateCanvases();
            // CanvasScaler и anchored controls се пренареждат в следващия Editor frame.
            // Без този frame вторият capture понякога пази transform от първата резолюция.
            await Task.Delay(50);
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = texture;
            var pixels = new Texture2D(width, width * 9 / 16, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes($"Logs/OfficeFlowCaptures/{name}-{width}.png", pixels.EncodeToPNG());
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                if (text.isTextOverflowing)
                {
                    overflows.Add($"{name}/{text.transform.parent.name}/{text.name}");
                    Debug.LogWarning($"Office text overflow: {name}/{text.transform.parent.name}/{text.name}: {text.text}; rect={text.rectTransform.rect}; size={text.fontSize}");
                }
            RenderTexture.active = null;
            camera.targetTexture = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(pixels);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = true;
            }
        }
    }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 15;
        while (!condition() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(30);
        Require(condition(), "Power transition timeout");
    }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        if (editorSearchErrors > 0) Debug.LogWarning($"Office flow: {editorSearchErrors} Editor SearchDatabase exception(s), reported separately from gameplay.");
        Debug.Log("Office flow smoke: " + message);
        EditorApplication.Exit(code);
    }
}
