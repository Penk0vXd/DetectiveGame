using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ComputerUISetupTool
{
    // еднократна настройка на computer UI
    private const string ScenePath = "Assets/_Game/Scenes/DetectiveOffice.unity";
    private const string PrefabFolder = "Assets/_Game/Prefabs/Computer";
    private const string RetroFontFolder = "Assets/_Game/UI/Fonts";
    private const string RetroFontPath = RetroFontFolder + "/PoliceTerminal SDF.asset";
    private const string RetroFontSourcePath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset";
    private const string RequestFileName = "DetectiveGameComputerUISetup.request";

    private static readonly Color DesktopColor = Hex("006B6B");
    private static readonly Color WindowColor = Hex("C0C0C0");
    private static readonly Color PaneColor = Hex("FFFFFF");
    private static readonly Color TitleColor = Hex("000080");
    private static readonly Color AccentColor = Hex("000080");
    private static readonly Color TaskbarColor = Hex("C0C0C0");
    private static readonly Color ButtonFaceColor = Hex("C0C0C0");
    private static readonly Color DarkTextColor = Hex("000000");
    private static readonly Color SecondaryTextColor = Hex("303030");
    private static readonly Color LightTextColor = Hex("FFFFFF");
    private static TMP_FontAsset retroFont;

    private sealed class ScrollParts
    {
        public GameObject Root;
        public RectTransform Viewport;
        public RectTransform Content;
        public ScrollRect ScrollRect;
        public Scrollbar Scrollbar;
    }

    private sealed class WindowParts
    {
        public GameObject Root;
        public RectTransform Content;
        public Button CloseButton;
    }

    [InitializeOnLoadMethod]
    private static void RunRequestedSetupAfterReload()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        EditorApplication.delayCall += RunRequestedSetup;
    }

    [MenuItem("Tools/DetectiveGame/Build Computer UI")]
    public static void BuildComputerUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Computer UI setup не се изпълнява в Play Mode");
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Computer UI");

        try
        {
            GameObject canvasObject = FindRoot(scene, "Canvas");
            if (canvasObject == null)
            {
                throw new InvalidOperationException("Scene няма root Canvas");
            }

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            Canvas canvasComponent = GetOrAdd<Canvas>(canvasObject);
            canvasComponent.pixelPerfect = true;
            GetOrAdd<GraphicRaycaster>(canvasObject);
            EnsureRetroFontAsset();

            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            RectTransform computerRoot = RequireChild(canvas, "ComputerRoot");
            RectTransform screenOffPanel = RequireChild(computerRoot, "ScreenOffPanel");
            RectTransform bootPanel = RequireChild(computerRoot, "BootPanel");
            RectTransform desktopPanel = RequireChild(computerRoot, "DesktopPanel");

            ConfigureFullStretch(computerRoot);
            ConfigureFullStretch(screenOffPanel);
            ConfigureFullStretch(bootPanel);
            ConfigureFullStretch(desktopPanel);

            ClearChildren(screenOffPanel);
            ClearChildren(bootPanel);
            ClearChildren(desktopPanel);

            BuildOffScreen(screenOffPanel);
            BuildBootScreen(bootPanel);

            ComputerUIController computerUI = FindSceneComponent<ComputerUIController>(scene);
            if (computerUI == null)
            {
                computerUI = Undo.AddComponent<ComputerUIController>(computerRoot.gameObject);
            }

            ComputerPowerController powerController = FindSceneComponent<ComputerPowerController>(scene);
            if (powerController == null)
            {
                throw new InvalidOperationException("Scene няма ComputerPowerController");
            }

            EmailUIController emailController = GetOrAdd<EmailUIController>(desktopPanel.gameObject);
            CaseFilesUIController caseController = GetOrAdd<CaseFilesUIController>(desktopPanel.gameObject);
            PoliceDatabaseUIController databaseController = GetOrAdd<PoliceDatabaseUIController>(desktopPanel.gameObject);
            PhotosUIController photosController = GetOrAdd<PhotosUIController>(desktopPanel.gameObject);
            ForensicsUIController forensicsController = GetOrAdd<ForensicsUIController>(desktopPanel.gameObject);

            CreateEntryPrefabs();

            Image desktopImage = GetOrAdd<Image>(desktopPanel.gameObject);
            desktopImage.color = DesktopColor;

            RectTransform wallpaper = CreateImage("Wallpaper", desktopPanel, DesktopColor);
            ConfigureFullStretch(wallpaper);
            wallpaper.SetAsFirstSibling();

            RectTransform desktopIcons = BuildDesktopIcons(desktopPanel, computerUI);
            WindowParts mail = BuildMailWindow(desktopPanel, emailController);
            WindowParts cases = BuildCaseFilesWindow(desktopPanel, caseController);
            WindowParts database = BuildDatabaseWindow(desktopPanel, databaseController);
            WindowParts photos = BuildPhotosWindow(desktopPanel, photosController);
            WindowParts forensics = BuildForensicsWindow(desktopPanel, forensicsController);
            BuildTaskbar(desktopPanel, computerUI);

            RectTransform oldPowerMenu = FindDirect(computerRoot, "PowerMenuPanel");
            if (oldPowerMenu != null)
            {
                Undo.DestroyObjectImmediate(oldPowerMenu.gameObject);
            }

            RectTransform oldExit = FindDirect(computerRoot, "ExitComputerButton");
            if (oldExit != null)
            {
                Undo.DestroyObjectImmediate(oldExit.gameObject);
            }

            RectTransform powerMenu = BuildPowerMenu(computerRoot, computerUI, powerController);
            Button exitButton = CreateButton("ExitComputerButton", computerRoot, "EXIT COMPUTER", 17f);
            SetRect(exitButton.GetComponent<RectTransform>(), Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -20f), new Vector2(170f, 44f));
            AddListener(exitButton, computerUI.ExitComputer);

            WireComputerController(computerUI, computerRoot, screenOffPanel, bootPanel, desktopPanel, powerMenu,
                emailController, caseController, databaseController, photosController, forensicsController,
                database.Root, photos.Root, forensics.Root);

            WirePowerController(powerController, computerUI);
            EnsureEventSystem(scene);
            PopulateDevelopmentData(caseController, databaseController, photosController, forensicsController);

            screenOffPanel.gameObject.SetActive(true);
            bootPanel.gameObject.SetActive(false);
            desktopPanel.gameObject.SetActive(false);
            mail.Root.SetActive(false);
            cases.Root.SetActive(false);
            database.Root.SetActive(false);
            photos.Root.SetActive(false);
            forensics.Root.SetActive(false);
            powerMenu.gameObject.SetActive(false);
            computerRoot.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvasObject);
            EditorUtility.SetDirty(computerUI);
            EditorUtility.SetDirty(powerController);
            EditorUtility.SetDirty(emailController);
            EditorUtility.SetDirty(caseController);
            EditorUtility.SetDirty(databaseController);
            EditorUtility.SetDirty(photosController);
            EditorUtility.SetDirty(forensicsController);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("DetectiveOffice scene не беше записана");
            }

            ValidateSetup(scene);
            Debug.Log("ComputerUISetupTool завърши hierarchy prefabs references data и button bindings");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static string RequestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", RequestFileName));

    private static void RunRequestedSetup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedSetup;
            return;
        }

        try
        {
            BuildComputerUI();
            File.Delete(RequestPath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void BuildOffScreen(RectTransform parent)
    {
        Image image = GetOrAdd<Image>(parent.gameObject);
        image.color = Hex("000000");
        TMP_Text text = CreateText("PowerStateText", parent, "POLICE WORKSTATION OFFLINE", 28f, LightTextColor, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Center, Center, Center, Vector2.zero, new Vector2(520f, 50f));
    }

    private static void BuildBootScreen(RectTransform parent)
    {
        Image image = GetOrAdd<Image>(parent.gameObject);
        image.color = Hex("000040");
        RectTransform logo = CreateImage("BootLogoImage", parent, TitleColor);
        SetRect(logo, Center, Center, Center, new Vector2(0f, 70f), new Vector2(160f, 160f));
        TMP_Text status = CreateText("BootStatusText", parent, "POLICE DEPARTMENT WORKSTATION\nSYSTEM STARTUP...", 26f, LightTextColor, TextAlignmentOptions.Center);
        SetRect(status.rectTransform, Center, Center, Center, new Vector2(0f, -55f), new Vector2(600f, 80f));
    }

    private static RectTransform BuildDesktopIcons(RectTransform parent, ComputerUIController computerUI)
    {
        RectTransform root = CreateEmpty("DesktopIcons", parent);
        SetRect(root, TopLeft, TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(170f, 600f));
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(root.gameObject);
        ConfigureVerticalLayout(layout, 0, 12f, TextAnchor.UpperLeft, true, true, false, false);

        CreateDesktopIcon(root, "MailIcon", "MAIL", computerUI.OpenMail);
        CreateDesktopIcon(root, "CaseFilesIcon", "CASE FILES", computerUI.OpenCaseFiles);
        CreateDesktopIcon(root, "DatabaseIcon", "POLICE DATABASE", computerUI.OpenDatabase);
        CreateDesktopIcon(root, "PhotosIcon", "PHOTOS", computerUI.OpenPhotos);
        CreateDesktopIcon(root, "ForensicsIcon", "FORENSICS", computerUI.OpenForensics);
        return root;
    }

    private static void CreateDesktopIcon(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(name, parent, label, 16f);
        LayoutElement element = GetOrAdd<LayoutElement>(button.gameObject);
        element.preferredWidth = 150f;
        element.preferredHeight = 92f;

        TMP_Text labelText = button.transform.Find("LabelText").GetComponent<TMP_Text>();
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(-8f, 34f));
        RectTransform icon = CreateImage("IconImage", button.transform, AccentColor);
        SetRect(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(46f, 46f));
        icon.SetAsFirstSibling();
        AddListener(button, action);
    }

    private static WindowParts BuildMailWindow(RectTransform parent, EmailUIController controller)
    {
        WindowParts window = CreateWindow(parent, "MailWindow", "MAIL", "CloseMailButton");
        AddListener(window.CloseButton, controller.CloseMail);

        RectTransform inbox = CreatePanel("InboxPane", window.Content, PaneColor);
        SetRect(inbox, BottomLeft, TopLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(470f, 0f));
        TMP_Text header = CreateText("InboxHeaderText", inbox, "INBOX", 20f, DarkTextColor, TextAlignmentOptions.Left);
        SetTopStretch(header.rectTransform, 12f, 12f, 12f, 44f);
        ScrollParts inboxScroll = CreateScrollView("InboxScrollView", inbox, "EmailListContent");
        SetStretch(inboxScroll.Root.GetComponent<RectTransform>(), 12f, 12f, 12f, 56f);

        RectTransform details = CreatePanel("EmailDetailsPane", window.Content, WindowColor);
        SetStretch(details, 482f, 12f, 12f, 12f);
        VerticalLayoutGroup detailsLayout = GetOrAdd<VerticalLayoutGroup>(details.gameObject);
        ConfigureVerticalLayout(detailsLayout, 18, 8f, TextAnchor.UpperLeft, true, true, true, false);

        CreateLayoutText("SenderLabelText", details, "SENDER", 15f, SecondaryTextColor, 24f, true);
        TMP_Text sender = CreateLayoutText("SenderText", details, string.Empty, 18f, DarkTextColor, 32f);
        CreateLayoutText("SubjectLabelText", details, "SUBJECT", 15f, SecondaryTextColor, 24f, true);
        TMP_Text subject = CreateLayoutText("SubjectText", details, string.Empty, 18f, DarkTextColor, 32f);
        CreateLayoutText("DateLabelText", details, "DATE", 15f, SecondaryTextColor, 24f, true);
        TMP_Text date = CreateLayoutText("DateText", details, string.Empty, 18f, DarkTextColor, 32f);
        CreateLayoutText("BodyLabelText", details, "MESSAGE", 15f, SecondaryTextColor, 24f, true);
        ScrollParts bodyScroll = CreateScrollView("EmailBodyScrollView", details, "EmailBodyContent", false);
        LayoutElement bodyScrollLayout = GetOrAdd<LayoutElement>(bodyScroll.Root);
        bodyScrollLayout.minHeight = 360f;
        bodyScrollLayout.flexibleHeight = 1f;
        ConfigureBodyContent(bodyScroll.Content);
        TMP_Text body = CreateLayoutText("BodyText", bodyScroll.Content, string.Empty, 17f, DarkTextColor, 180f);

        SetSerializedReferences(controller,
            ("mailWindow", window.Root), ("emailListContent", inboxScroll.Content),
            ("emailEntryPrefab", AssetDatabase.LoadAssetAtPath<EmailEntryUI>(PrefabFolder + "/EmailEntry.prefab")),
            ("senderText", sender), ("subjectText", subject), ("dateText", date), ("bodyText", body));
        return window;
    }

    private static WindowParts BuildCaseFilesWindow(RectTransform parent, CaseFilesUIController controller)
    {
        WindowParts window = CreateWindow(parent, "CaseFilesWindow", "CASE FILES", "CloseCaseFilesButton");
        AddListener(window.CloseButton, controller.CloseCaseFiles);

        RectTransform caseListPane = CreatePanel("CaseListPane", window.Content, PaneColor);
        SetRect(caseListPane, BottomLeft, TopLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(420f, 0f));
        TMP_Text listHeader = CreateText("CaseListHeaderText", caseListPane, "OFFICIAL CASES", 20f, DarkTextColor, TextAlignmentOptions.Left);
        SetTopStretch(listHeader.rectTransform, 12f, 12f, 12f, 44f);
        ScrollParts caseList = CreateScrollView("CaseListScrollView", caseListPane, "CaseListContent");
        SetStretch(caseList.Root.GetComponent<RectTransform>(), 12f, 12f, 12f, 56f);

        RectTransform details = CreateEmpty("CaseDetailsPane", window.Content);
        SetStretch(details, 432f, 0f, 0f, 0f);
        RectTransform header = CreatePanel("CaseHeader", details, WindowColor);
        SetTopStretch(header, 12f, 12f, 12f, 92f);
        TMP_Text caseNumber = CreateText("CaseNumberText", header, string.Empty, 18f, SecondaryTextColor, TextAlignmentOptions.Left);
        SetRect(caseNumber.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(16f, -14f), new Vector2(240f, 30f));
        TMP_Text caseName = CreateText("CaseNameText", header, string.Empty, 26f, DarkTextColor, TextAlignmentOptions.Left);
        SetTopStretch(caseName.rectTransform, 16f, 180f, 44f, 38f);
        TMP_Text caseStatus = CreateText("CaseStatusText", header, string.Empty, 17f, DarkTextColor, TextAlignmentOptions.Right);
        SetRect(caseStatus.rectTransform, TopRight, TopRight, TopRight, new Vector2(-16f, -14f), new Vector2(150f, 32f));

        RectTransform tabs = CreatePanel("CaseTabBar", details, WindowColor);
        SetTopStretch(tabs, 12f, 12f, 112f, 46f);
        HorizontalLayoutGroup tabLayout = GetOrAdd<HorizontalLayoutGroup>(tabs.gameObject);
        ConfigureHorizontalLayout(tabLayout, 0, 6f, TextAnchor.MiddleLeft, true, true, true, true);
        Button overviewButton = CreateButton("OverviewButton", tabs, "OVERVIEW", 17f);
        Button peopleButton = CreateButton("PeopleButton", tabs, "PEOPLE", 17f);
        Button recordsButton = CreateButton("RecordsButton", tabs, "RECORDS", 17f);
        foreach (Button button in new[] { overviewButton, peopleButton, recordsButton })
        {
            LayoutElement element = GetOrAdd<LayoutElement>(button.gameObject);
            element.flexibleWidth = 1f;
            element.preferredHeight = 40f;
        }

        RectTransform overview = CreateEmpty("OverviewPanel", details);
        SetStretch(overview, 12f, 12f, 12f, 170f);
        ScrollParts overviewScroll = CreateScrollView("OverviewScrollView", overview, "OverviewContent", false);
        ConfigureFullStretch(overviewScroll.Root.GetComponent<RectTransform>());
        ConfigureBodyContent(overviewScroll.Content);
        TMP_Text dateOpened = CreateOverviewRow(overviewScroll.Content, "DateRow", "DateLabelText", "DATE OPENED", "DateOpenedText");
        TMP_Text location = CreateOverviewRow(overviewScroll.Content, "LocationRow", "LocationLabelText", "LOCATION", "LocationText");
        TMP_Text victim = CreateOverviewRow(overviewScroll.Content, "VictimRow", "VictimLabelText", "VICTIM", "VictimText");
        TMP_Text lead = CreateOverviewRow(overviewScroll.Content, "LeadDetectiveRow", "LeadDetectiveLabelText", "LEAD DETECTIVE", "LeadDetectiveText");
        CreateLayoutText("SummaryLabelText", overviewScroll.Content, "SUMMARY", 15f, SecondaryTextColor, 24f, true);
        TMP_Text summary = CreateLayoutText("SummaryText", overviewScroll.Content, string.Empty, 17f, DarkTextColor, 120f);

        RectTransform people = CreateEmpty("PeoplePanel", details);
        SetStretch(people, 12f, 12f, 12f, 170f);
        RectTransform peopleListPane = CreatePanel("PeopleListPane", people, PaneColor);
        SetRect(peopleListPane, BottomLeft, TopLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(410f, 0f));
        ScrollParts peopleList = CreateScrollView("PeopleScrollView", peopleListPane, "PeopleListContent");
        ConfigureFullStretch(peopleList.Root.GetComponent<RectTransform>());
        RectTransform personDetails = CreatePanel("PersonDetailsPane", people, WindowColor);
        SetStretch(personDetails, 422f, 0f, 0f, 0f);
        ConfigureDetailsLayout(personDetails);
        CreateLayoutText("PersonNameLabelText", personDetails, "NAME", 15f, SecondaryTextColor, 24f, true);
        TMP_Text personName = CreateLayoutText("PersonNameText", personDetails, string.Empty, 20f, DarkTextColor, 34f);
        CreateLayoutText("PersonRoleLabelText", personDetails, "ROLE", 15f, SecondaryTextColor, 24f, true);
        TMP_Text personRole = CreateLayoutText("PersonRoleText", personDetails, string.Empty, 18f, DarkTextColor, 32f);
        CreateLayoutText("PersonDescriptionLabelText", personDetails, "DESCRIPTION", 15f, SecondaryTextColor, 24f, true);
        TMP_Text personDescription = CreateLayoutText("PersonDescriptionText", personDetails, string.Empty, 17f, DarkTextColor, 120f);

        RectTransform records = CreateEmpty("RecordsPanel", details);
        SetStretch(records, 12f, 12f, 12f, 170f);
        RectTransform recordsListPane = CreatePanel("RecordsListPane", records, PaneColor);
        SetRect(recordsListPane, BottomLeft, TopLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(410f, 0f));
        ScrollParts recordsList = CreateScrollView("RecordsScrollView", recordsListPane, "RecordsListContent");
        ConfigureFullStretch(recordsList.Root.GetComponent<RectTransform>());
        RectTransform recordDetails = CreatePanel("RecordDetailsPane", records, WindowColor);
        SetStretch(recordDetails, 422f, 0f, 0f, 0f);
        ConfigureDetailsLayout(recordDetails);
        CreateLayoutText("RecordTitleLabelText", recordDetails, "TITLE", 15f, SecondaryTextColor, 24f, true);
        TMP_Text recordTitle = CreateLayoutText("RecordTitleText", recordDetails, string.Empty, 20f, DarkTextColor, 34f);
        CreateLayoutText("RecordTypeLabelText", recordDetails, "RECORD TYPE", 15f, SecondaryTextColor, 24f, true);
        TMP_Text recordType = CreateLayoutText("RecordTypeText", recordDetails, string.Empty, 18f, DarkTextColor, 32f);
        CreateLayoutText("RecordDateLabelText", recordDetails, "DATE", 15f, SecondaryTextColor, 24f, true);
        TMP_Text recordDate = CreateLayoutText("RecordDateText", recordDetails, string.Empty, 18f, DarkTextColor, 32f);
        CreateLayoutText("RecordBodyLabelText", recordDetails, "OFFICIAL RECORD", 15f, SecondaryTextColor, 24f, true);
        TMP_Text recordBody = CreateLayoutText("RecordBodyText", recordDetails, string.Empty, 17f, DarkTextColor, 180f);

        AddListener(overviewButton, controller.ShowOverview);
        AddListener(peopleButton, controller.ShowPeople);
        AddListener(recordsButton, controller.ShowRecords);

        SetSerializedReferences(controller,
            ("caseFilesWindow", window.Root), ("caseListContent", caseList.Content),
            ("caseEntryPrefab", AssetDatabase.LoadAssetAtPath<CaseEntryUI>(PrefabFolder + "/CaseEntry.prefab")),
            ("caseNumberText", caseNumber), ("caseNameText", caseName), ("statusText", caseStatus),
            ("dateText", dateOpened), ("locationText", location), ("victimText", victim),
            ("leadDetectiveText", lead), ("summaryText", summary),
            ("overviewPanel", overview.gameObject), ("peoplePanel", people.gameObject), ("recordsPanel", records.gameObject),
            ("peopleListContent", peopleList.Content),
            ("personEntryPrefab", AssetDatabase.LoadAssetAtPath<CasePersonEntryUI>(PrefabFolder + "/CasePersonEntry.prefab")),
            ("personNameText", personName), ("personRoleText", personRole), ("personDescriptionText", personDescription),
            ("recordsListContent", recordsList.Content),
            ("recordEntryPrefab", AssetDatabase.LoadAssetAtPath<CaseRecordEntryUI>(PrefabFolder + "/CaseRecordEntry.prefab")),
            ("recordTitleText", recordTitle), ("recordTypeText", recordType), ("recordDateText", recordDate), ("recordBodyText", recordBody));

        overview.gameObject.SetActive(true);
        people.gameObject.SetActive(false);
        records.gameObject.SetActive(false);
        return window;
    }

    private static WindowParts BuildDatabaseWindow(RectTransform parent, PoliceDatabaseUIController controller)
    {
        WindowParts window = CreateWindow(parent, "DatabaseWindow", "POLICE DATABASE", "CloseDatabaseButton");
        AddListener(window.CloseButton, controller.CloseDatabase);

        RectTransform searchBar = CreatePanel("DatabaseSearchBar", window.Root.transform, WindowColor);
        SetTopStretch(searchBar, 12f, 12f, 64f, 62f);
        HorizontalLayoutGroup searchLayout = GetOrAdd<HorizontalLayoutGroup>(searchBar.gameObject);
        ConfigureHorizontalLayout(searchLayout, 10, 8f, TextAnchor.MiddleLeft, true, true, false, false);
        TMP_InputField input = CreateInputField("DatabaseSearchInput", searchBar, "Enter full name");
        LayoutElement inputLayout = GetOrAdd<LayoutElement>(input.gameObject);
        inputLayout.flexibleWidth = 1f;
        inputLayout.preferredHeight = 42f;
        Button searchButton = CreateButton("SearchButton", searchBar, "SEARCH", 17f);
        SetPreferredSize(searchButton.gameObject, 130f, 42f);
        Button clearButton = CreateButton("ClearButton", searchBar, "CLEAR", 17f);
        SetPreferredSize(clearButton.gameObject, 110f, 42f);
        AddListener(searchButton, controller.Search);
        AddListener(clearButton, controller.ClearSearch);

        RectTransform content = window.Content;
        SetStretch(content, 12f, 12f, 12f, 138f);
        RectTransform resultsPane = CreatePanel("DatabaseResultsPane", content, PaneColor);
        SetRect(resultsPane, BottomLeft, TopLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520f, 0f));
        TMP_Text status = CreateText("ResultsStatusText", resultsPane, string.Empty, 16f, SecondaryTextColor, TextAlignmentOptions.Left);
        SetTopStretch(status.rectTransform, 10f, 10f, 8f, 38f);
        ScrollParts results = CreateScrollView("DatabaseResultsScrollView", resultsPane, "DatabaseResultsContent");
        SetStretch(results.Root.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);

        RectTransform details = CreatePanel("DatabaseDetailsPane", content, WindowColor);
        SetStretch(details, 532f, 0f, 0f, 0f);
        ConfigureDetailsLayout(details);
        TMP_Text fullName = CreateDetailField(details, "FullName", "FULL NAME", 34f);
        TMP_Text dob = CreateDetailField(details, "DateOfBirth", "DATE OF BIRTH", 32f);
        TMP_Text address = CreateDetailField(details, "Address", "ADDRESS", 40f);
        TMP_Text occupation = CreateDetailField(details, "Occupation", "OCCUPATION", 32f);
        TMP_Text recordSummary = CreateDetailField(details, "RecordSummary", "RECORD SUMMARY", 180f);

        SetSerializedReferences(controller,
            ("databaseWindow", window.Root), ("searchInput", input), ("resultsContent", results.Content),
            ("resultEntryPrefab", AssetDatabase.LoadAssetAtPath<DatabaseResultEntryUI>(PrefabFolder + "/DatabaseResultEntry.prefab")),
            ("resultsStatusText", status), ("fullNameText", fullName), ("dateOfBirthText", dob),
            ("addressText", address), ("occupationText", occupation), ("recordSummaryText", recordSummary));
        return window;
    }

    private static WindowParts BuildPhotosWindow(RectTransform parent, PhotosUIController controller)
    {
        WindowParts window = CreateWindow(parent, "PhotosWindow", "PHOTOS", "ClosePhotosButton");
        AddListener(window.CloseButton, controller.ClosePhotos);
        RectTransform content = window.Content;
        RectTransform gallery = CreatePanel("PhotoGalleryPane", content, PaneColor);
        SetRect(gallery, BottomLeft, TopLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520f, 0f));
        ScrollParts photoList = CreateScrollView("PhotosScrollView", gallery, "PhotoListContent");
        SetStretch(photoList.Root.GetComponent<RectTransform>(), 12f, 12f, 12f, 12f);
        RectTransform preview = CreatePanel("PhotoPreviewPane", content, WindowColor);
        SetStretch(preview, 532f, 12f, 12f, 12f);
        ConfigureDetailsLayout(preview);
        RectTransform frame = CreatePanel("PhotoPreviewFrame", preview, Hex("11161A"));
        LayoutElement frameLayout = GetOrAdd<LayoutElement>(frame.gameObject);
        frameLayout.preferredHeight = 470f;
        frameLayout.flexibleWidth = 1f;
        RectTransform previewImageRect = CreateImage("PhotoPreviewImage", frame, Color.white);
        SetStretch(previewImageRect, 12f, 12f, 12f, 12f);
        Image previewImage = previewImageRect.GetComponent<Image>();
        previewImage.preserveAspect = true;
        TMP_Text title = CreateDetailField(preview, "PhotoTitle", "TITLE", 34f);
        TMP_Text date = CreateDetailField(preview, "PhotoDate", "DATE", 32f);
        TMP_Text location = CreateDetailField(preview, "PhotoLocation", "LOCATION", 32f);
        TMP_Text description = CreateDetailField(preview, "PhotoDescription", "DESCRIPTION", 120f);
        SetSerializedReferences(controller,
            ("photosWindow", window.Root), ("photoListContent", photoList.Content),
            ("photoEntryPrefab", AssetDatabase.LoadAssetAtPath<PhotoEntryUI>(PrefabFolder + "/PhotoEntry.prefab")),
            ("previewImage", previewImage), ("titleText", title), ("dateText", date),
            ("locationText", location), ("descriptionText", description));
        return window;
    }

    private static WindowParts BuildForensicsWindow(RectTransform parent, ForensicsUIController controller)
    {
        WindowParts window = CreateWindow(parent, "ForensicsWindow", "FORENSICS", "CloseForensicsButton");
        AddListener(window.CloseButton, controller.CloseForensics);
        RectTransform content = window.Content;
        RectTransform listPane = CreatePanel("ReportsListPane", content, PaneColor);
        SetRect(listPane, BottomLeft, TopLeft, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520f, 0f));
        ScrollParts reports = CreateScrollView("ReportsScrollView", listPane, "ReportsContent");
        SetStretch(reports.Root.GetComponent<RectTransform>(), 12f, 12f, 12f, 12f);
        RectTransform details = CreatePanel("ReportDetailsPane", content, WindowColor);
        SetStretch(details, 532f, 12f, 12f, 12f);
        ConfigureDetailsLayout(details);
        TMP_Text reportNumber = CreateDetailField(details, "ReportNumber", "REPORT NUMBER", 32f);
        TMP_Text title = CreateDetailField(details, "ReportTitle", "TITLE", 34f);
        TMP_Text status = CreateDetailField(details, "ReportStatus", "STATUS", 32f);
        TMP_Text date = CreateDetailField(details, "ReportDate", "DATE", 32f);
        TMP_Text relatedCase = CreateDetailField(details, "RelatedCase", "RELATED CASE", 32f);
        TMP_Text summary = CreateDetailField(details, "ReportSummary", "SUMMARY", 80f);
        CreateLayoutText("FullReportLabelText", details, "FULL REPORT", 15f, SecondaryTextColor, 24f, true);
        ScrollParts fullReportScroll = CreateScrollView("FullReportScrollView", details, "FullReportContent", false);
        LayoutElement scrollLayout = GetOrAdd<LayoutElement>(fullReportScroll.Root);
        scrollLayout.minHeight = 300f;
        scrollLayout.flexibleHeight = 1f;
        ConfigureBodyContent(fullReportScroll.Content);
        TMP_Text fullReport = CreateLayoutText("FullReportText", fullReportScroll.Content, string.Empty, 17f, DarkTextColor, 180f);
        SetSerializedReferences(controller,
            ("forensicsWindow", window.Root), ("reportsContent", reports.Content),
            ("reportEntryPrefab", AssetDatabase.LoadAssetAtPath<ForensicsReportEntryUI>(PrefabFolder + "/ForensicsReportEntry.prefab")),
            ("reportNumberText", reportNumber), ("titleText", title), ("statusText", status),
            ("dateText", date), ("relatedCaseText", relatedCase), ("summaryText", summary), ("fullReportText", fullReport));
        return window;
    }

    private static void BuildTaskbar(RectTransform parent, ComputerUIController controller)
    {
        RectTransform taskbar = CreatePanel("Taskbar", parent, TaskbarColor);
        SetRect(taskbar, BottomLeft, BottomRight, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 58f));
        TMP_Text status = CreateText("WorkstationStatusText", taskbar, "POLICE DEPARTMENT WORKSTATION // AUTHORIZED USE ONLY", 17f, DarkTextColor, TextAlignmentOptions.Left);
        SetStretch(status.rectTransform, 20f, 220f, 0f, 0f);
        Button power = CreateButton("PowerButton", taskbar, "POWER", 17f);
        SetRect(power.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(170f, 42f));
        AddListener(power, controller.TogglePowerMenu);
    }

    private static RectTransform BuildPowerMenu(RectTransform parent, ComputerUIController computerUI, ComputerPowerController powerController)
    {
        RectTransform menu = CreatePanel("PowerMenuPanel", parent, new Color(WindowColor.r, WindowColor.g, WindowColor.b, 0.97f));
        SetRect(menu, BottomLeft, BottomLeft, BottomLeft, new Vector2(16f, 64f), new Vector2(260f, 248f));
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(menu.gameObject);
        ConfigureVerticalLayout(layout, 12, 8f, TextAnchor.UpperCenter, true, true, true, false);
        CreateLayoutText("PowerMenuTitleText", menu, "POWER OPTIONS", 18f, DarkTextColor, 36f, true);
        Button sleep = CreateButton("SleepButton", menu, "SLEEP", 17f);
        Button restart = CreateButton("RestartButton", menu, "RESTART", 17f);
        Button shutdown = CreateButton("ShutdownButton", menu, "SHUT DOWN", 17f);
        Button cancel = CreateButton("CancelPowerMenuButton", menu, "CANCEL", 17f);
        foreach (Button button in new[] { sleep, restart, shutdown, cancel })
        {
            SetPreferredSize(button.gameObject, 0f, 42f, true);
        }
        AddListener(sleep, powerController.Sleep);
        AddListener(restart, powerController.Restart);
        AddListener(shutdown, powerController.ShutDown);
        AddListener(cancel, computerUI.HidePowerMenu);
        return menu;
    }

    private static WindowParts CreateWindow(RectTransform parent, string name, string title, string closeButtonName)
    {
        RectTransform root = CreatePanel(name, parent, WindowColor);
        SetRect(root, Center, Center, Center, new Vector2(0f, 18f), new Vector2(1560f, 860f));
        RectTransform titleBar = CreatePanel(name.Replace("Window", "TitleBar"), root, TitleColor);
        SetTopStretch(titleBar, 0f, 0f, 0f, 52f);
        TMP_Text titleText = CreateText(name.Replace("Window", "TitleText"), titleBar, title, 28f, LightTextColor, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        SetStretch(titleText.rectTransform, 18f, 68f, 0f, 0f);
        Button close = CreateButton(closeButtonName, titleBar, "X", 20f);
        SetRect(close.GetComponent<RectTransform>(), TopRight, TopRight, TopRight, new Vector2(-8f, -8f), new Vector2(44f, 36f));
        RectTransform content = CreateEmpty(name.Replace("Window", "Content"), root);
        SetStretch(content, 0f, 0f, 0f, 52f);
        return new WindowParts { Root = root.gameObject, Content = content, CloseButton = close };
    }

    private static ScrollParts CreateScrollView(string name, Transform parent, string contentName, bool listContent = true)
    {
        RectTransform root = CreateImage(name, parent, Hex("DFDFDF"));
        root.GetComponent<Image>().raycastTarget = true;
        AddRetroOutline(root.GetComponent<Image>());
        ScrollRect scrollRect = GetOrAdd<ScrollRect>(root.gameObject);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;

        RectTransform viewport = CreateImage("Viewport", root, new Color(1f, 1f, 1f, 0.01f));
        viewport.GetComponent<Image>().raycastTarget = true;
        SetStretch(viewport, 0f, 18f, 0f, 0f);
        Mask mask = GetOrAdd<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;

        RectTransform content = CreateEmpty(contentName, viewport);
        SetRect(content, TopLeft, TopRight, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 0f));
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        ConfigureVerticalLayout(layout, 6, 6f, TextAnchor.UpperCenter, true, true, true, false);
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform scrollbarRect = CreateImage("VerticalScrollbar", root, PaneColor);
        scrollbarRect.GetComponent<Image>().raycastTarget = true;
        SetRect(scrollbarRect, BottomRight, TopRight, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(16f, 0f));
        Scrollbar scrollbar = GetOrAdd<Scrollbar>(scrollbarRect.gameObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform slidingArea = CreateEmpty("Sliding Area", scrollbarRect);
        SetStretch(slidingArea, 8f, 8f, 8f, 8f);
        RectTransform handle = CreateImage("Handle", slidingArea, AccentColor);
        handle.GetComponent<Image>().raycastTarget = true;
        ConfigureFullStretch(handle);
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3f;
        return new ScrollParts { Root = root.gameObject, Viewport = viewport, Content = content, ScrollRect = scrollRect, Scrollbar = scrollbar };
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText)
    {
        RectTransform root = CreateImage(name, parent, Color.white);
        root.GetComponent<Image>().raycastTarget = true;
        AddRetroOutline(root.GetComponent<Image>());
        TMP_InputField input = GetOrAdd<TMP_InputField>(root.gameObject);
        RectTransform textArea = CreateEmpty("Text Area", root);
        SetStretch(textArea, 10f, 10f, 6f, 6f);
        GetOrAdd<RectMask2D>(textArea.gameObject);
        TMP_Text placeholder = CreateText("Placeholder", textArea, placeholderText, 17f, SecondaryTextColor, TextAlignmentOptions.MidlineLeft);
        ConfigureFullStretch(placeholder.rectTransform);
        TMP_Text text = CreateText("Text", textArea, string.Empty, 17f, DarkTextColor, TextAlignmentOptions.MidlineLeft);
        ConfigureFullStretch(text.rectTransform);
        input.textViewport = textArea;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static TMP_Text CreateOverviewRow(Transform parent, string rowName, string labelName, string label, string valueName)
    {
        RectTransform row = CreateEmpty(rowName, parent);
        LayoutElement rowLayout = GetOrAdd<LayoutElement>(row.gameObject);
        rowLayout.preferredHeight = 34f;
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        ConfigureHorizontalLayout(layout, 0, 10f, TextAnchor.MiddleLeft, true, true, false, false);
        TMP_Text labelText = CreateText(labelName, row, label, 15f, SecondaryTextColor, TextAlignmentOptions.Left);
        SetPreferredSize(labelText.gameObject, 190f, 30f);
        TMP_Text value = CreateText(valueName, row, string.Empty, 18f, DarkTextColor, TextAlignmentOptions.Left);
        SetPreferredSize(value.gameObject, 0f, 30f, true);
        return value;
    }

    private static TMP_Text CreateDetailField(Transform parent, string prefix, string label, float valueHeight)
    {
        CreateLayoutText(prefix + "LabelText", parent, label, 15f, SecondaryTextColor, 24f, true);
        return CreateLayoutText(prefix + "Text", parent, string.Empty, 18f, DarkTextColor, valueHeight);
    }

    private static void ConfigureDetailsLayout(RectTransform details)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(details.gameObject);
        ConfigureVerticalLayout(layout, 18, 8f, TextAnchor.UpperLeft, true, true, true, false);
    }

    private static void ConfigureBodyContent(RectTransform content)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        ConfigureVerticalLayout(layout, 16, 10f, TextAnchor.UpperLeft, true, true, true, false);
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void CreateEntryPrefabs()
    {
        CreateVerticalEntryPrefab<CasePersonEntryUI>("CasePersonEntry", 72f,
            new[] { "NameText", "RoleText" }, new[] { 17f, 14f },
            (component, root, fields) => SetSerializedReferences(component, ("button", root.GetComponent<Button>()), ("nameText", fields[0]), ("roleText", fields[1])));

        CreateVerticalEntryPrefab<CaseRecordEntryUI>("CaseRecordEntry", 90f,
            new[] { "TitleText", "RecordTypeText", "DateText" }, new[] { 17f, 14f, 14f },
            (component, root, fields) => SetSerializedReferences(component, ("button", root.GetComponent<Button>()), ("titleText", fields[0]), ("recordTypeText", fields[1]), ("dateText", fields[2])));

        CreateVerticalEntryPrefab<DatabaseResultEntryUI>("DatabaseResultEntry", 92f,
            new[] { "FullNameText", "DateOfBirthText", "OccupationText" }, new[] { 17f, 14f, 14f },
            (component, root, fields) => SetSerializedReferences(component, ("button", root.GetComponent<Button>()), ("fullNameText", fields[0]), ("dateOfBirthText", fields[1]), ("occupationText", fields[2])));

        CreatePhotoEntryPrefab();

        CreateVerticalEntryPrefab<ForensicsReportEntryUI>("ForensicsReportEntry", 108f,
            new[] { "ReportNumberText", "TitleText", "StatusText", "DateText" }, new[] { 14f, 17f, 14f, 14f },
            (component, root, fields) => SetSerializedReferences(component, ("button", root.GetComponent<Button>()), ("reportNumberText", fields[0]), ("titleText", fields[1]), ("statusText", fields[2]), ("dateText", fields[3])));

        AssetDatabase.SaveAssets();
    }

    private static void CreateVerticalEntryPrefab<T>(string name, float height, string[] fieldNames, float[] fontSizes,
        Action<T, GameObject, TMP_Text[]> wire) where T : Component
    {
        GameObject root = CreatePrefabButtonRoot(name, height);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(root);
        ConfigureVerticalLayout(layout, 6, 2f, TextAnchor.UpperLeft, true, true, true, false, 10);
        TMP_Text[] fields = new TMP_Text[fieldNames.Length];
        for (int index = 0; index < fieldNames.Length; index++)
        {
            fields[index] = CreateLayoutText(fieldNames[index], root.transform, string.Empty, fontSizes[index],
                index == 0 || fieldNames[index] == "TitleText" ? DarkTextColor : SecondaryTextColor, 20f, index == 0 || fieldNames[index] == "TitleText");
        }
        T component = root.AddComponent<T>();
        wire(component, root, fields);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreatePhotoEntryPrefab()
    {
        GameObject root = CreatePrefabButtonRoot("PhotoEntry", 120f);
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(root);
        ConfigureHorizontalLayout(layout, 10, 10f, TextAnchor.MiddleLeft, true, true, true, false);
        RectTransform thumbnailRect = CreateImage("ThumbnailImage", root.transform, Hex("AEB7BE"));
        Image thumbnail = thumbnailRect.GetComponent<Image>();
        thumbnail.preserveAspect = true;
        thumbnail.raycastTarget = false;
        SetPreferredSize(thumbnail.gameObject, 112f, 92f);
        RectTransform metadata = CreateEmpty("Metadata", root.transform);
        LayoutElement metadataLayout = GetOrAdd<LayoutElement>(metadata.gameObject);
        metadataLayout.flexibleWidth = 1f;
        VerticalLayoutGroup metadataGroup = GetOrAdd<VerticalLayoutGroup>(metadata.gameObject);
        ConfigureVerticalLayout(metadataGroup, 0, 2f, TextAnchor.UpperLeft, true, true, true, false);
        TMP_Text title = CreateLayoutText("TitleText", metadata, string.Empty, 17f, DarkTextColor, 24f, true);
        TMP_Text date = CreateLayoutText("DateText", metadata, string.Empty, 14f, SecondaryTextColor, 20f);
        TMP_Text location = CreateLayoutText("LocationText", metadata, string.Empty, 14f, SecondaryTextColor, 20f);
        PhotoEntryUI component = root.AddComponent<PhotoEntryUI>();
        SetSerializedReferences(component, ("button", root.GetComponent<Button>()), ("thumbnailImage", thumbnail),
            ("titleText", title), ("dateText", date), ("locationText", location));
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/PhotoEntry.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static GameObject CreatePrefabButtonRoot(string name, float height)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        root.GetComponent<Image>().color = Color.white;
        Button button = root.GetComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        button.colors = CreateButtonColors();
        LayoutElement element = root.GetComponent<LayoutElement>();
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        return root;
    }

    private static void WireComputerController(ComputerUIController controller, RectTransform computerRoot,
        RectTransform screenOff, RectTransform boot, RectTransform desktop, RectTransform powerMenu,
        EmailUIController email, CaseFilesUIController cases, PoliceDatabaseUIController database,
        PhotosUIController photos, ForensicsUIController forensics,
        GameObject databaseWindow, GameObject photosWindow, GameObject forensicsWindow)
    {
        SetSerializedReferences(controller,
            ("computerRoot", computerRoot.gameObject), ("screenOffPanel", screenOff.gameObject),
            ("bootPanel", boot.gameObject), ("desktopPanel", desktop.gameObject), ("powerMenuPanel", powerMenu.gameObject),
            ("emailUIController", email), ("caseFilesUIController", cases),
            ("policeDatabaseUIController", database), ("photosUIController", photos), ("forensicsUIController", forensics),
            ("databaseWindow", databaseWindow), ("photosWindow", photosWindow), ("forensicsWindow", forensicsWindow));
    }

    private static void WirePowerController(ComputerPowerController controller, ComputerUIController computerUI)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("computerUI").objectReferenceValue = computerUI;
        serialized.FindProperty("bootDuration").floatValue = 3f;
        serialized.FindProperty("shutdownDuration").floatValue = 1.5f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PopulateDevelopmentData(CaseFilesUIController cases, PoliceDatabaseUIController database,
        PhotosUIController photos, ForensicsUIController forensics)
    {
        PopulateCaseData(cases);
        PopulateDatabaseData(database);
        PopulatePhotoData(photos);
        PopulateForensicsData(forensics);
    }

    private static void PopulateCaseData(CaseFilesUIController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty cases = serialized.FindProperty("cases");
        if (cases.arraySize == 0)
        {
            cases.arraySize = 2;
            SetCase(cases.GetArrayElementAtIndex(0), "CASE-0017", "Riverside Apartment Homicide", "OPEN",
                "14 October 2011", "17 Riverside Avenue", "Daniel Harris", "Michael Carter",
                "The victim was discovered inside his apartment at approximately 23:20. The investigation remains active.");
            SetCase(cases.GetArrayElementAtIndex(1), "CASE-0012", "Warehouse Fire Investigation", "ARCHIVED",
                "03 October 2011", "North Industrial District", "None", "Sarah Bennett",
                "The initial investigation found no confirmed evidence of foul play. The case is archived.");
        }

        for (int index = 0; index < cases.arraySize; index++)
        {
            SerializedProperty current = cases.GetArrayElementAtIndex(index);
            string number = current.FindPropertyRelative("caseNumber").stringValue;
            SerializedProperty people = current.FindPropertyRelative("people");
            SerializedProperty records = current.FindPropertyRelative("records");
            if (number == "CASE-0017")
            {
                if (people.arraySize == 0)
                {
                    people.arraySize = 4;
                    SetPerson(people.GetArrayElementAtIndex(0), "Daniel Harris", "Victim", "Resident of the apartment where the incident was reported.");
                    SetPerson(people.GetArrayElementAtIndex(1), "Elena Moore", "Witness", "Neighbour who reported hearing activity in the corridor.");
                    SetPerson(people.GetArrayElementAtIndex(2), "Thomas Reed", "Person of Interest", "Known associate listed in the initial incident report.");
                    SetPerson(people.GetArrayElementAtIndex(3), "Officer Miller", "Officer", "Responding officer who documented the apartment search.");
                }
                if (records.arraySize == 0)
                {
                    records.arraySize = 3;
                    SetRecord(records.GetArrayElementAtIndex(0), "Initial Incident Report", "Incident Report", "14/10/2011", "Officers responded to a call at 23:20 and secured the apartment.");
                    SetRecord(records.GetArrayElementAtIndex(1), "Witness Statement — Elena Moore", "Witness Statement", "14/10/2011", "The witness reports corridor noise but does not identify its source.");
                    SetRecord(records.GetArrayElementAtIndex(2), "Autopsy Request", "Autopsy Request", "14/10/2011", "A formal examination was requested. Findings are pending.");
                }
            }
            else if (number == "CASE-0012")
            {
                if (people.arraySize == 0)
                {
                    people.arraySize = 1;
                    SetPerson(people.GetArrayElementAtIndex(0), "Sarah Bennett", "Officer", "Lead detective named in the archived file.");
                }
                if (records.arraySize == 0)
                {
                    records.arraySize = 2;
                    SetRecord(records.GetArrayElementAtIndex(0), "Fire Marshal Summary", "Official Report", "05/10/2011", "The available observations did not establish deliberate ignition.");
                    SetRecord(records.GetArrayElementAtIndex(1), "Closure Note", "Case Note", "09/10/2011", "The file was archived pending new official information.");
                }
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PopulateDatabaseData(PoliceDatabaseUIController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty records = serialized.FindProperty("records");
        if (records.arraySize == 0)
        {
            records.arraySize = 3;
            SetDatabaseRecord(records.GetArrayElementAtIndex(0), "Thomas Reed", "22/03/1979", "42 Westbridge Road", "Delivery Driver", "Identity and address verified. One prior traffic citation. No conclusion about the active case.");
            SetDatabaseRecord(records.GetArrayElementAtIndex(1), "Elena Moore", "08/11/1984", "19 Riverside Avenue", "Accountant", "Current address verified. No criminal record listed.");
            SetDatabaseRecord(records.GetArrayElementAtIndex(2), "Daniel Harris", "17/06/1975", "17 Riverside Avenue", "Architect", "Identity record associated with CASE-0017.");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PopulatePhotoData(PhotosUIController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty photos = serialized.FindProperty("photos");
        if (photos.arraySize == 0)
        {
            photos.arraySize = 3;
            SetPhoto(photos.GetArrayElementAtIndex(0), "Apartment Entrance", "14/10/2011 23:38", "17 Riverside Avenue", "Exterior view of the apartment entrance after the scene was secured.");
            SetPhoto(photos.GetArrayElementAtIndex(1), "Kitchen Table", "14/10/2011 23:52", "Apartment Kitchen", "Overview photograph of the table and nearby surfaces.");
            SetPhoto(photos.GetArrayElementAtIndex(2), "Parking Receipt", "15/10/2011 00:06", "Apartment Kitchen", "Close photograph of the recovered receipt. The photograph records appearance only.");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PopulateForensicsData(ForensicsUIController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty reports = serialized.FindProperty("reports");
        if (reports.arraySize == 0)
        {
            reports.arraySize = 2;
            SetReport(reports.GetArrayElementAtIndex(0), "FR-2011-184", "Preliminary Examination", "Completed", "14/10/2011", "CASE-0017", "Preliminary observations are available.", "The report records qualified preliminary observations. Additional laboratory work may change or refine the findings.");
            SetReport(reports.GetArrayElementAtIndex(1), "FR-2011-191", "Trace Material Analysis", "Pending", "15/10/2011", "CASE-0017", "Samples received by the laboratory.", "Analysis is pending. No result or interpretation is available at this time.");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetCase(SerializedProperty item, string number, string name, string status, string date,
        string location, string victim, string lead, string summary)
    {
        SetString(item, "caseNumber", number); SetString(item, "caseName", name); SetString(item, "status", status);
        SetString(item, "dateOpened", date); SetString(item, "location", location); SetString(item, "victim", victim);
        SetString(item, "leadDetective", lead); SetString(item, "summary", summary);
    }

    private static void SetPerson(SerializedProperty item, string name, string role, string description)
    {
        SetString(item, "name", name); SetString(item, "role", role); SetString(item, "shortDescription", description);
    }

    private static void SetRecord(SerializedProperty item, string title, string type, string date, string body)
    {
        SetString(item, "title", title); SetString(item, "recordType", type); SetString(item, "date", date); SetString(item, "body", body);
    }

    private static void SetDatabaseRecord(SerializedProperty item, string name, string dob, string address, string occupation, string summary)
    {
        SetString(item, "fullName", name); SetString(item, "dateOfBirth", dob); SetString(item, "address", address);
        SetString(item, "occupation", occupation); SetString(item, "recordSummary", summary);
    }

    private static void SetPhoto(SerializedProperty item, string title, string date, string location, string description)
    {
        SetString(item, "title", title); SetString(item, "date", date); SetString(item, "location", location); SetString(item, "description", description);
    }

    private static void SetReport(SerializedProperty item, string number, string title, string status, string date,
        string relatedCase, string summary, string fullReport)
    {
        SetString(item, "reportNumber", number); SetString(item, "title", title); SetString(item, "status", status);
        SetString(item, "date", date); SetString(item, "relatedCase", relatedCase); SetString(item, "summary", summary);
        SetString(item, "fullReport", fullReport);
    }

    private static void SetString(SerializedProperty parent, string name, string value)
    {
        parent.FindPropertyRelative(name).stringValue = value;
    }

    private static void ValidateSetup(Scene scene)
    {
        string[] requiredObjects =
        {
            "MailWindow", "CaseFilesWindow", "PeopleListContent", "RecordsListContent",
            "DatabaseWindow", "DatabaseSearchInput", "DatabaseResultsContent",
            "PhotosWindow", "PhotoListContent", "PhotoPreviewImage",
            "ForensicsWindow", "ReportsContent", "FullReportText", "PowerMenuPanel"
        };

        foreach (string objectName in requiredObjects)
        {
            if (FindInScene(scene, objectName) == null)
            {
                throw new InvalidOperationException("Липсва UI object " + objectName);
            }
        }

        string[] prefabs =
        {
            "CasePersonEntry.prefab", "CaseRecordEntry.prefab", "DatabaseResultEntry.prefab",
            "PhotoEntry.prefab", "ForensicsReportEntry.prefab"
        };
        foreach (string prefab in prefabs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefab) == null)
            {
                throw new InvalidOperationException("Липсва prefab " + prefab);
            }
        }
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem existing = FindSceneComponent<EventSystem>(scene);
        if (existing != null)
        {
            GetOrAdd<InputSystemUIInputModule>(existing.gameObject);
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
    }

    private static void SetSerializedReferences(Component component, params (string name, UnityEngine.Object value)[] values)
    {
        SerializedObject serialized = new SerializedObject(component);
        foreach ((string name, UnityEngine.Object value) in values)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(component.GetType().Name + " няма serialized field " + name);
            }
            property.objectReferenceValue = value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform CreateEmpty(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateImage(name, parent, color);
        AddRetroOutline(rect.GetComponent<Image>());
        return rect;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.font = RetroFont;
        text.characterSpacing = 1.5f;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static TMP_Text CreateLayoutText(string name, Transform parent, string value, float fontSize, Color color,
        float preferredHeight, bool bold = false)
    {
        TMP_Text text = CreateText(name, parent, value, fontSize, color, TextAlignmentOptions.TopLeft);
        if (bold)
        {
            text.fontStyle = FontStyles.Bold;
        }
        LayoutElement layout = GetOrAdd<LayoutElement>(text.gameObject);
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, float fontSize)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Image image = gameObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;
        AddRetroBevel(image);
        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = CreateButtonColors();
        TMP_Text labelText = CreateText("LabelText", rect, label, fontSize, DarkTextColor, TextAlignmentOptions.Center);
        labelText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        ConfigureFullStretch(labelText.rectTransform);
        return button;
    }

    private static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = ButtonFaceColor;
        colors.highlightedColor = Hex("E8E8E8");
        colors.pressedColor = Hex("9E9E9E");
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.48f, 0.5f, 0.55f);
        colors.fadeDuration = 0.08f;
        return colors;
    }

    private static TMP_FontAsset RetroFont
    {
        get
        {
            if (retroFont == null)
            {
                retroFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RetroFontPath);
            }
            return retroFont;
        }
    }

    private static void EnsureRetroFontAsset()
    {
        if (!AssetDatabase.IsValidFolder(RetroFontFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/UI"))
            {
                AssetDatabase.CreateFolder("Assets/_Game", "UI");
            }
            AssetDatabase.CreateFolder("Assets/_Game/UI", "Fonts");
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RetroFontPath) == null)
        {
            if (!AssetDatabase.CopyAsset(RetroFontSourcePath, RetroFontPath))
            {
                throw new InvalidOperationException("Retro TMP font asset не беше копиран");
            }
            AssetDatabase.ImportAsset(RetroFontPath, ImportAssetOptions.ForceSynchronousImport);
        }
        retroFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RetroFontPath);
    }

    private static void AddRetroOutline(Graphic graphic)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = Hex("202020");
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private static void AddRetroBevel(Graphic graphic)
    {
        Shadow highlight = graphic.gameObject.AddComponent<Shadow>();
        highlight.effectColor = Color.white;
        highlight.effectDistance = new Vector2(-2f, 2f);
        highlight.useGraphicAlpha = true;

        Shadow lowlight = graphic.gameObject.AddComponent<Shadow>();
        lowlight.effectColor = Hex("404040");
        lowlight.effectDistance = new Vector2(2f, -2f);
        lowlight.useGraphicAlpha = true;

        AddRetroOutline(graphic);
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void SetPreferredSize(GameObject gameObject, float width, float height, bool flexibleWidth = false)
    {
        LayoutElement element = GetOrAdd<LayoutElement>(gameObject);
        if (width > 0f)
        {
            element.preferredWidth = width;
        }
        element.preferredHeight = height;
        element.flexibleWidth = flexibleWidth ? 1f : 0f;
    }

    private static void ConfigureVerticalLayout(VerticalLayoutGroup layout, int padding, float spacing,
        TextAnchor alignment, bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight, int horizontalPadding = -1)
    {
        int xPadding = horizontalPadding >= 0 ? horizontalPadding : padding;
        layout.padding = new RectOffset(xPadding, xPadding, padding, padding);
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = controlWidth;
        layout.childControlHeight = controlHeight;
        layout.childForceExpandWidth = expandWidth;
        layout.childForceExpandHeight = expandHeight;
        layout.reverseArrangement = false;
    }

    private static void ConfigureHorizontalLayout(HorizontalLayoutGroup layout, int padding, float spacing,
        TextAnchor alignment, bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight)
    {
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = controlWidth;
        layout.childControlHeight = controlHeight;
        layout.childForceExpandWidth = expandWidth;
        layout.childForceExpandHeight = expandHeight;
        layout.reverseArrangement = false;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
    }

    private static void ConfigureFullStretch(RectTransform rect)
    {
        SetStretch(rect, 0f, 0f, 0f, 0f);
    }

    private static void SetStretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
    {
        rect.anchorMin = TopLeft;
        rect.anchorMax = TopRight;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
        rect.sizeDelta = new Vector2(-(left + right), height);
        rect.localScale = Vector3.one;
    }

    private static void ClearChildren(RectTransform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Undo.DestroyObjectImmediate(parent.GetChild(index).gameObject);
        }
    }

    private static RectTransform RequireChild(RectTransform parent, string name)
    {
        RectTransform child = FindDirect(parent, name);
        if (child == null)
        {
            throw new InvalidOperationException(parent.name + " няма child " + name);
        }
        return child;
    }

    private static RectTransform FindDirect(Transform parent, string name)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == name)
            {
                return child as RectTransform;
            }
        }
        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }
        return null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (transform.name == name)
                {
                    return transform.gameObject;
                }
            }
        }
        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }
        return null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Color Hex(string value)
    {
        if (!ColorUtility.TryParseHtmlString("#" + value, out Color color))
        {
            return Color.magenta;
        }
        return color;
    }

    private static Vector2 BottomLeft => Vector2.zero;
    private static Vector2 BottomRight => new Vector2(1f, 0f);
    private static Vector2 TopLeft => new Vector2(0f, 1f);
    private static Vector2 TopRight => Vector2.one;
    private static Vector2 Center => new Vector2(0.5f, 0.5f);
}
