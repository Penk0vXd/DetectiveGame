# Computer Setup Guide

**Език:** Български  
**Статус:** `IMPLEMENTED` C# foundation, Unity UI hierarchy, entry prefabs, Inspector references и button bindings; Play Mode validation остава ръчна  
**Последна проверка на кода:** 2026-09-29  
**Target:** Unity `6000.6.3f1`, URP, Windows PC, reference resolution `1920 × 1080`

Това е точната инструкция за изграждане на Police Department Workstation UI върху наличния Computer shell. Данните са serialized Inspector data; няма SQL, networking, automatic deductions или generic OS framework. Desktop остава background и `ComputerUIController` гарантира, че само един основен application window е отворен.

По `D-019` всички видими за играча Computer labels, buttons, emails, case records, database records, photo metadata и forensic reports са на български. GameObject names, C# identifiers, Inspector field names и служебни record IDs остават на английски. `PoliceTerminal SDF` използва dynamic Cyrillic fallback; не заменяй font setup-а с asset без кирилица.

Не редактирай `Assets/_Game/Scenes/DetectiveOffice.unity` като текст. Hierarchy-то и wiring-ът са генерирани чрез Unity Editor API от `Assets/_Game/Editor/ComputerUISetupTool.cs`. Инструментът може да се изпълни повторно от `Tools > DetectiveGame > Build Computer UI`; той rebuild-ва Computer UI subtree и запазва сцената. Всички имена по-долу са exact names, използвани от tool-а.

## 1. File map

Всички Computer C# файлове са в `Assets/_Game/Scripts/Computer/`.

| Filename | Recommended path | Purpose |
|---|---|---|
| `ComputerInteractable.cs` | `Assets/_Game/Scripts/Computer/ComputerInteractable.cs` | Physical interaction, enter/exit computer mode, player controls и cursor |
| `ComputerPowerController.cs` | `Assets/_Game/Scripts/Computer/ComputerPowerController.cs` | Power state transitions, boot, sleep, wake, restart и shutdown |
| `ComputerPowerState.cs` | `Assets/_Game/Scripts/Computer/ComputerPowerState.cs` | Enum за `Off`, `Booting`, `On`, `Sleeping` |
| `ComputerUIController.cs` | `Assets/_Game/Scripts/Computer/ComputerUIController.cs` | Computer shell, desktop panels, power menu и exclusive app switching |
| `EmailData.cs` | `Assets/_Game/Scripts/Computer/EmailData.cs` | Serialized data за едно email съобщение |
| `EmailEntryUI.cs` | `Assets/_Game/Scripts/Computer/EmailEntryUI.cs` | Един generated inbox row и selection callback |
| `EmailUIController.cs` | `Assets/_Game/Scripts/Computer/EmailUIController.cs` | Mail window, inbox generation и email details |
| `CaseData.cs` | `Assets/_Game/Scripts/Computer/CaseData.cs` | Serialized official case data, people и records |
| `CaseEntryUI.cs` | `Assets/_Game/Scripts/Computer/CaseEntryUI.cs` | Един generated case row и case selection |
| `CasePersonData.cs` | `Assets/_Game/Scripts/Computer/CasePersonData.cs` | Serialized officially linked person |
| `CasePersonEntryUI.cs` | `Assets/_Game/Scripts/Computer/CasePersonEntryUI.cs` | Един generated person row и selection callback |
| `CaseRecordData.cs` | `Assets/_Game/Scripts/Computer/CaseRecordData.cs` | Serialized official case record |
| `CaseRecordEntryUI.cs` | `Assets/_Game/Scripts/Computer/CaseRecordEntryUI.cs` | Един generated record row и selection callback |
| `CaseFilesUIController.cs` | `Assets/_Game/Scripts/Computer/CaseFilesUIController.cs` | Case list, case details, tabs, people и records |
| `DatabasePersonData.cs` | `Assets/_Game/Scripts/Computer/DatabasePersonData.cs` | Serialized Police Database person record |
| `DatabaseResultEntryUI.cs` | `Assets/_Game/Scripts/Computer/DatabaseResultEntryUI.cs` | Един generated database result и selection callback |
| `PoliceDatabaseUIController.cs` | `Assets/_Game/Scripts/Computer/PoliceDatabaseUIController.cs` | Case-insensitive FullName search, results, clear и details |
| `PhotoData.cs` | `Assets/_Game/Scripts/Computer/PhotoData.cs` | Serialized photo metadata и optional Sprite |
| `PhotoEntryUI.cs` | `Assets/_Game/Scripts/Computer/PhotoEntryUI.cs` | Един generated photo row и selection callback |
| `PhotosUIController.cs` | `Assets/_Game/Scripts/Computer/PhotosUIController.cs` | Gallery generation, photo preview и metadata |
| `ForensicsReportData.cs` | `Assets/_Game/Scripts/Computer/ForensicsReportData.cs` | Serialized forensic report |
| `ForensicsReportEntryUI.cs` | `Assets/_Game/Scripts/Computer/ForensicsReportEntryUI.cs` | Един generated report row и selection callback |
| `ForensicsUIController.cs` | `Assets/_Game/Scripts/Computer/ForensicsUIController.cs` | Reports list, selection и report details |
| `ComputerUISetupTool.cs` | `Assets/_Game/Editor/ComputerUISetupTool.cs` | Editor-only rebuild на hierarchy, prefabs, references, callbacks и development data |

Свързани, но не Computer-owned файлове:

| Filename | Path | Relation |
|---|---|---|
| `PlayerInputReader.cs` | `Assets/_Game/Scripts/Player/PlayerInputReader.cs` | Единствен owner на `DetectiveGameInput` |
| `FirstPersonController.cs` | `Assets/_Game/Scripts/Player/FirstPersonController.cs` | Movement/look се спират в computer mode |
| `PlayerInteraction.cs` | `Assets/_Game/Scripts/Player/PlayerInteraction.cs` | Raycast interaction се спира в computer mode |
| `IInteractable.cs` | `Assets/_Game/Scripts/Interaction/IInteractable.cs` | Contract, реализиран от `ComputerInteractable` |

## 2. Complete Canvas hierarchy

Създай следната hierarchy. `LabelText` под различни buttons е отделен child във всеки button.

```text
Canvas
└── ComputerRoot
    ├── ScreenOffPanel
    │   └── PowerStateText
    ├── BootPanel
    │   ├── BootLogoImage
    │   └── BootStatusText
    ├── DesktopPanel
    │   ├── Wallpaper
    │   ├── DesktopIcons
    │   │   ├── MailIcon
    │   │   │   ├── IconImage
    │   │   │   └── LabelText
    │   │   ├── CaseFilesIcon
    │   │   │   ├── IconImage
    │   │   │   └── LabelText
    │   │   ├── DatabaseIcon
    │   │   │   ├── IconImage
    │   │   │   └── LabelText
    │   │   ├── PhotosIcon
    │   │   │   ├── IconImage
    │   │   │   └── LabelText
    │   │   └── ForensicsIcon
    │   │       ├── IconImage
    │   │       └── LabelText
    │   ├── MailWindow
    │   │   ├── MailTitleBar
    │   │   │   ├── MailTitleText
    │   │   │   └── CloseMailButton
    │   │   │       └── LabelText
    │   │   └── MailContent
    │   │       ├── InboxPane
    │   │       │   ├── InboxHeaderText
    │   │       │   └── InboxScrollView
    │   │       │       ├── Viewport
    │   │       │       │   └── EmailListContent
    │   │       │       └── VerticalScrollbar
    │   │       └── EmailDetailsPane
    │   │           ├── SenderLabelText
    │   │           ├── SenderText
    │   │           ├── SubjectLabelText
    │   │           ├── SubjectText
    │   │           ├── DateLabelText
    │   │           ├── DateText
    │   │           ├── BodyLabelText
    │   │           └── EmailBodyScrollView
    │   │               ├── Viewport
    │   │               │   └── EmailBodyContent
    │   │               │       └── BodyText
    │   │               └── VerticalScrollbar
    │   ├── CaseFilesWindow
    │   │   ├── CaseFilesTitleBar
    │   │   │   ├── CaseFilesTitleText
    │   │   │   └── CloseCaseFilesButton
    │   │   │       └── LabelText
    │   │   └── CaseFilesContent
    │   │       ├── CaseListPane
    │   │       │   ├── CaseListHeaderText
    │   │       │   └── CaseListScrollView
    │   │       │       ├── Viewport
    │   │       │       │   └── CaseListContent
    │   │       │       └── VerticalScrollbar
    │   │       └── CaseDetailsPane
    │   │           ├── CaseHeader
    │   │           │   ├── CaseNumberText
    │   │           │   ├── CaseNameText
    │   │           │   └── CaseStatusText
    │   │           ├── CaseTabBar
    │   │           │   ├── OverviewButton
    │   │           │   │   └── LabelText
    │   │           │   ├── PeopleButton
    │   │           │   │   └── LabelText
    │   │           │   └── RecordsButton
    │   │           │       └── LabelText
    │   │           ├── OverviewPanel
    │   │           │   └── OverviewScrollView
    │   │           │       ├── Viewport
    │   │           │       │   └── OverviewContent
    │   │           │       │       ├── DateRow
    │   │           │       │       │   ├── DateLabelText
    │   │           │       │       │   └── DateOpenedText
    │   │           │       │       ├── LocationRow
    │   │           │       │       │   ├── LocationLabelText
    │   │           │       │       │   └── LocationText
    │   │           │       │       ├── VictimRow
    │   │           │       │       │   ├── VictimLabelText
    │   │           │       │       │   └── VictimText
    │   │           │       │       ├── LeadDetectiveRow
    │   │           │       │       │   ├── LeadDetectiveLabelText
    │   │           │       │       │   └── LeadDetectiveText
    │   │           │       │       ├── SummaryLabelText
    │   │           │       │       └── SummaryText
    │   │           │       └── VerticalScrollbar
    │   │           ├── PeoplePanel
    │   │           │   ├── PeopleListPane
    │   │           │   │   └── PeopleScrollView
    │   │           │   │       ├── Viewport
    │   │           │   │       │   └── PeopleListContent
    │   │           │   │       └── VerticalScrollbar
    │   │           │   └── PersonDetailsPane
    │   │           │       ├── PersonNameLabelText
    │   │           │       ├── PersonNameText
    │   │           │       ├── PersonRoleLabelText
    │   │           │       ├── PersonRoleText
    │   │           │       ├── PersonDescriptionLabelText
    │   │           │       └── PersonDescriptionText
    │   │           └── RecordsPanel
    │   │               ├── RecordsListPane
    │   │               │   └── RecordsScrollView
    │   │               │       ├── Viewport
    │   │               │       │   └── RecordsListContent
    │   │               │       └── VerticalScrollbar
    │   │               └── RecordDetailsPane
    │   │                   ├── RecordTitleLabelText
    │   │                   ├── RecordTitleText
    │   │                   ├── RecordTypeLabelText
    │   │                   ├── RecordTypeText
    │   │                   ├── RecordDateLabelText
    │   │                   ├── RecordDateText
    │   │                   ├── RecordBodyLabelText
    │   │                   └── RecordBodyText
    │   ├── DatabaseWindow
    │   │   ├── DatabaseTitleBar
    │   │   │   ├── DatabaseTitleText
    │   │   │   └── CloseDatabaseButton
    │   │   │       └── LabelText
    │   │   ├── DatabaseSearchBar
    │   │   │   ├── DatabaseSearchInput
    │   │   │   │   └── Text Area
    │   │   │   │       ├── Placeholder
    │   │   │   │       └── Text
    │   │   │   ├── SearchButton
    │   │   │   │   └── LabelText
    │   │   │   └── ClearButton
    │   │   │       └── LabelText
    │   │   └── DatabaseContent
    │   │       ├── DatabaseResultsPane
    │   │       │   ├── ResultsStatusText
    │   │       │   └── DatabaseResultsScrollView
    │   │       │       ├── Viewport
    │   │       │       │   └── DatabaseResultsContent
    │   │       │       └── VerticalScrollbar
    │   │       └── DatabaseDetailsPane
    │   │           ├── FullNameLabelText
    │   │           ├── FullNameText
    │   │           ├── DateOfBirthLabelText
    │   │           ├── DateOfBirthText
    │   │           ├── AddressLabelText
    │   │           ├── AddressText
    │   │           ├── OccupationLabelText
    │   │           ├── OccupationText
    │   │           ├── RecordSummaryLabelText
    │   │           └── RecordSummaryText
    │   ├── PhotosWindow
    │   │   ├── PhotosTitleBar
    │   │   │   ├── PhotosTitleText
    │   │   │   └── ClosePhotosButton
    │   │   │       └── LabelText
    │   │   └── PhotosContent
    │   │       ├── PhotoGalleryPane
    │   │       │   └── PhotosScrollView
    │   │       │       ├── Viewport
    │   │       │       │   └── PhotoListContent
    │   │       │       └── VerticalScrollbar
    │   │       └── PhotoPreviewPane
    │   │           ├── PhotoPreviewFrame
    │   │           │   └── PhotoPreviewImage
    │   │           ├── PhotoTitleLabelText
    │   │           ├── PhotoTitleText
    │   │           ├── PhotoDateLabelText
    │   │           ├── PhotoDateText
    │   │           ├── PhotoLocationLabelText
    │   │           ├── PhotoLocationText
    │   │           ├── PhotoDescriptionLabelText
    │   │           └── PhotoDescriptionText
    │   ├── ForensicsWindow
    │   │   ├── ForensicsTitleBar
    │   │   │   ├── ForensicsTitleText
    │   │   │   └── CloseForensicsButton
    │   │   │       └── LabelText
    │   │   └── ForensicsContent
    │   │       ├── ReportsListPane
    │   │       │   └── ReportsScrollView
    │   │       │       ├── Viewport
    │   │       │       │   └── ReportsContent
    │   │       │       └── VerticalScrollbar
    │   │       └── ReportDetailsPane
    │   │           ├── ReportNumberLabelText
    │   │           ├── ReportNumberText
    │   │           ├── ReportTitleLabelText
    │   │           ├── ReportTitleText
    │   │           ├── ReportStatusLabelText
    │   │           ├── ReportStatusText
    │   │           ├── ReportDateLabelText
    │   │           ├── ReportDateText
    │   │           ├── RelatedCaseLabelText
    │   │           ├── RelatedCaseText
    │   │           ├── ReportSummaryLabelText
    │   │           ├── ReportSummaryText
    │   │           ├── FullReportLabelText
    │   │           └── FullReportScrollView
    │   │               ├── Viewport
    │   │               │   └── FullReportContent
    │   │               │       └── FullReportText
    │   │               └── VerticalScrollbar
    │   └── Taskbar
    │       ├── WorkstationStatusText
    │       └── PowerButton
    │           └── LabelText
    ├── PowerMenuPanel
    │   ├── PowerMenuTitleText
    │   ├── SleepButton
    │   │   └── LabelText
    │   ├── RestartButton
    │   │   └── LabelText
    │   ├── ShutdownButton
    │   │   └── LabelText
    │   └── CancelPowerMenuButton
    │       └── LabelText
    └── ExitComputerButton
        └── LabelText

EventSystem
```

Всеки `VerticalScrollbar` от hierarchy-то запазва generated subtree:

```text
VerticalScrollbar
└── Sliding Area
    └── Handle
```

`EventSystem` е отделен scene root, не child на Canvas. Използвай точно един active `EventSystem` в сцената.

`EmailEntry`, `CaseEntry`, `CasePersonEntry`, `CaseRecordEntry`, `DatabaseResultEntry`, `PhotoEntry` и `ForensicsReportEntry` са prefab assets, не постоянни children на `Content` objects. Остави всички `Content` objects празни в scene-а.

## 3. Element type

Използвай следната точна type карта. Имената се отнасят до hierarchy-то от секция 2.

| Unity create type | Elements |
|---|---|
| `Canvas` | `Canvas` |
| `UI > Panel` | `ComputerRoot`, `ScreenOffPanel`, `BootPanel`, `DesktopPanel`, всички `*Window`, всички `*Pane`, `CaseHeader`, `CaseTabBar`, `DatabaseSearchBar`, `Taskbar`, `PowerMenuPanel`, `PhotoPreviewFrame` |
| `UI > Image` | `Wallpaper`, `BootLogoImage`, всички `IconImage`, `PhotoPreviewImage` |
| `UI > Button - TextMeshPro` | всички `*Icon`, `Close*Button`, `OverviewButton`, `PeopleButton`, `RecordsButton`, `SearchButton`, `ClearButton`, `PowerButton`, `SleepButton`, `RestartButton`, `ShutdownButton`, `CancelPowerMenuButton`, `ExitComputerButton` |
| `UI > Text - TextMeshPro` | всички names, завършващи на `Text`, включително label, value, status и body text objects |
| `UI > Scroll View` | `InboxScrollView`, `EmailBodyScrollView`, `CaseListScrollView`, `OverviewScrollView`, `PeopleScrollView`, `RecordsScrollView`, `DatabaseResultsScrollView`, `PhotosScrollView`, `ReportsScrollView`, `FullReportScrollView` |
| `UI > Input Field - TextMeshPro` | `DatabaseSearchInput`; запази generated `Text Area`, `Placeholder`, `Text` children |
| `Create Empty` и добави `RectTransform` | `DesktopIcons`, всички `*Content` layout roots, `MailContent`, `CaseFilesContent`, `CaseDetailsPane` ако предпочиташ без background, `PeoplePanel`, `RecordsPanel`, `DateRow`, `LocationRow`, `VictimRow`, `LeadDetectiveRow`, `DatabaseContent`, `PhotosContent`, `ForensicsContent` |
| Generated Scroll View child | всеки `Viewport`, `VerticalScrollbar` |
| Generated Scrollbar child | всеки `Sliding Area` и `Handle` |
| `UI > Event System` | `EventSystem`, отделен scene root |

Ако element фигурира едновременно като logical pane и не трябва да има видим background, използвай `Empty UI Object` вместо `Panel`, но запази същия `RectTransform`. Не премахвай `Image` от `Viewport`, защото `Mask` го използва.

## 4. Components

Всеки element използва component recipe според type-а си:

| Type | Required components |
|---|---|
| Canvas | `RectTransform`, `Canvas`, `CanvasScaler`, `GraphicRaycaster` |
| Panel | `RectTransform`, `CanvasRenderer`, `Image` |
| Empty UI Object | `RectTransform` |
| Image | `RectTransform`, `CanvasRenderer`, `Image` |
| Text - TextMeshPro | `RectTransform`, `CanvasRenderer`, `TextMeshProUGUI` |
| Button - TextMeshPro root | `RectTransform`, `CanvasRenderer`, `Image`, `Button` |
| Button `LabelText` child | `RectTransform`, `CanvasRenderer`, `TextMeshProUGUI` |
| TMP Input Field root | `RectTransform`, `CanvasRenderer`, `Image`, `TMP_InputField` |
| TMP Input `Text Area` | `RectTransform`, `RectMask2D` |
| TMP Input `Placeholder` и `Text` | `RectTransform`, `CanvasRenderer`, `TextMeshProUGUI` |
| Scroll View root | `RectTransform`, `CanvasRenderer`, `Image`, `ScrollRect` |
| Scroll View `Viewport` | `RectTransform`, `CanvasRenderer`, `Image`, `Mask` |
| `VerticalScrollbar` | `RectTransform`, `CanvasRenderer`, `Image`, `Scrollbar`; остави generated `Sliding Area/Handle` hierarchy |
| Scrollbar `Sliding Area` | `RectTransform` |
| Scrollbar `Handle` | `RectTransform`, `CanvasRenderer`, `Image` |
| EventSystem | `Transform`, `EventSystem`, `InputSystemUIInputModule` |
| Всеки list `Content` | `RectTransform`, `VerticalLayoutGroup`, `ContentSizeFitter` |
| `OverviewContent` | `RectTransform`, `VerticalLayoutGroup`, `ContentSizeFitter` |
| `EmailBodyContent`, `FullReportContent` | `RectTransform`, `VerticalLayoutGroup`, `ContentSizeFitter` |
| `DateRow`, `LocationRow`, `VictimRow`, `LeadDetectiveRow` | `RectTransform`, `HorizontalLayoutGroup`, `LayoutElement` |
| `DesktopIcons` | `RectTransform`, `VerticalLayoutGroup` |
| `CaseTabBar`, `DatabaseSearchBar` | `RectTransform`, `HorizontalLayoutGroup` |
| `EmailDetailsPane`, `PersonDetailsPane`, `RecordDetailsPane`, `DatabaseDetailsPane`, `PhotoPreviewPane`, `ReportDetailsPane` | `RectTransform`, `CanvasRenderer`, `Image`, `VerticalLayoutGroup` |

MonoBehaviour placement:

| GameObject | Add component |
|---|---|
| Existing physical `Computer` world object | `ComputerInteractable`, `ComputerPowerController` |
| `ComputerRoot` or existing persistent UI controller object | `ComputerUIController` |
| `DesktopPanel` | `EmailUIController`, `CaseFilesUIController`, `PoliceDatabaseUIController`, `PhotosUIController`, `ForensicsUIController` |
| `EmailEntry` prefab root | `EmailEntryUI` |
| `CaseEntry` prefab root | `CaseEntryUI` |
| `CasePersonEntry` prefab root | `CasePersonEntryUI` |
| `CaseRecordEntry` prefab root | `CaseRecordEntryUI` |
| `DatabaseResultEntry` prefab root | `DatabaseResultEntryUI` |
| `PhotoEntry` prefab root | `PhotoEntryUI` |
| `ForensicsReportEntry` prefab root | `ForensicsReportEntryUI` |

Не добавяй data classes като components. Те се появяват като nested serialized data в controller Inspector lists.

## 5. RectTransform settings

### 5.1 Canvas и full-screen roots

`CanvasScaler`:

| Setting | Value |
|---|---|
| UI Scale Mode | `Scale With Screen Size` |
| Reference Resolution | `1920 × 1080` |
| Screen Match Mode | `Match Width Or Height` |
| Match | `0.5` |
| Reference Pixels Per Unit | `100` |

| Element | Anchor Min / Max | Pivot | Size or offsets | Pos X / Y |
|---|---|---|---|---|
| `ComputerRoot` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Top/Bottom `0` | `0 / 0` |
| `ScreenOffPanel` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `0` | `0 / 0` |
| `BootPanel` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `0` | `0 / 0` |
| `DesktopPanel` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `0` | `0 / 0` |
| `Wallpaper` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `0` | `0 / 0` |
| `PowerStateText` | `(0.5,0.5) / (0.5,0.5)` | `(0.5,0.5)` | `520 × 50` | `0 / 0` |
| `BootLogoImage` | `(0.5,0.5) / (0.5,0.5)` | `(0.5,0.5)` | `160 × 160` | `0 / 70` |
| `BootStatusText` | `(0.5,0.5) / (0.5,0.5)` | `(0.5,0.5)` | `600 × 50` | `0 / -55` |
| `DesktopIcons` | `(0,1) / (0,1)` | `(0,1)` | `170 × 600` | `24 / -24` |
| всяко desktop `*Icon` | layout-driven | `(0.5,0.5)` | `150 × 92` чрез `LayoutElement` | layout-driven |
| `Taskbar` | `(0,0) / (1,0)` | `(0.5,0)` | Height `58`; Left/Right/Bottom `0` | `0 / 0` |
| `WorkstationStatusText` | `(0,0) / (1,1)` | `(0,0.5)` | Left `20`, Right `220`, Top/Bottom `0` | `0 / 0` |
| `PowerButton` | `(1,0.5) / (1,0.5)` | `(1,0.5)` | `170 × 42` | `-12 / 0` |
| `ExitComputerButton` | `(1,1) / (1,1)` | `(1,1)` | `170 × 44` | `-24 / -20` |
| `PowerMenuPanel` | `(0,0) / (0,0)` | `(0,0)` | `260 × 248` | `16 / 64` |

За `PowerMenuPanel` използвай `VerticalLayoutGroup`; children се управляват от layout. `PowerMenuTitleText` preferred height `36`; четирите buttons preferred height `42`.

### 5.2 Common application window

Приложи тези настройки на `MailWindow`, `CaseFilesWindow`, `DatabaseWindow`, `PhotosWindow`, `ForensicsWindow`:

| Element | Anchor Min / Max | Pivot | Size or offsets | Pos X / Y |
|---|---|---|---|---|
| app window | `(0.5,0.5) / (0.5,0.5)` | `(0.5,0.5)` | `1560 × 860` | `0 / 18` |
| matching `*TitleBar` | `(0,1) / (1,1)` | `(0.5,1)` | Height `52`; Left/Right/Top `0` | `0 / 0` |
| title text | `(0,0) / (1,1)` | `(0,0.5)` | Left `18`, Right `68`, Top/Bottom `0` | `0 / 0` |
| close button | `(1,1) / (1,1)` | `(1,1)` | `44 × 36` | `-8 / -8` |
| matching main content | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Bottom `0`, Top `52` | `0 / 0` |

### 5.3 Mail

| Element | Anchor Min / Max | Pivot | Size or offsets |
|---|---|---|---|
| `InboxPane` | `(0,0) / (0,1)` | `(0,0.5)` | Width `470`; Left/Top/Bottom `0` |
| `InboxHeaderText` | `(0,1) / (1,1)` | `(0.5,1)` | Height `44`; Left/Right/Top `12` |
| `InboxScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Bottom `12`, Top `56` |
| `EmailDetailsPane` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left `482`, Right `12`, Top/Bottom `12` |
| `EmailBodyScrollView` | layout-driven | `(0.5,0.5)` | flexible height `1`; minimum height `360` |

`EmailDetailsPane` children се управляват от `VerticalLayoutGroup`. Label preferred height `24`; sender/subject/date values `32`; `EmailBodyScrollView` получава остатъчната височина.

### 5.4 Case Files

| Element | Anchor Min / Max | Pivot | Size or offsets |
|---|---|---|---|
| `CaseListPane` | `(0,0) / (0,1)` | `(0,0.5)` | Width `420`; Left/Top/Bottom `0` |
| `CaseListHeaderText` | `(0,1) / (1,1)` | `(0.5,1)` | Height `44`; Left/Right/Top `12` |
| `CaseListScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Bottom `12`, Top `56` |
| `CaseDetailsPane` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left `432`, Right/Top/Bottom `0` |
| `CaseHeader` | `(0,1) / (1,1)` | `(0.5,1)` | Height `92`; Left/Right `12`, Top `12` |
| `CaseTabBar` | `(0,1) / (1,1)` | `(0.5,1)` | Height `46`; Left/Right `12`, Top `112` |
| `OverviewPanel`, `PeoplePanel`, `RecordsPanel` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Bottom `12`, Top `170` |
| `OverviewScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `0` |
| `PeopleListPane`, `RecordsListPane` | `(0,0) / (0,1)` | `(0,0.5)` | Width `410`; all outer offsets `0` |
| `PeopleScrollView`, `RecordsScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `0` |
| `PersonDetailsPane`, `RecordDetailsPane` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left `422`, Right/Top/Bottom `0` |

`CaseHeader`: `CaseNumberText` anchor top-left, `240 × 30`, Pos `16 / -14`; `CaseNameText` stretch horizontally, height `38`, Left `16`, Right `180`, Top `44`; `CaseStatusText` top-right, `150 × 32`, Pos `-16 / -14`, Pivot `(1,1)`.

`CaseTabBar` uses three equal-width buttons. Give each `LayoutElement` flexible width `1`, preferred height `40`.

`OverviewContent` and all generated list `Content` objects: Anchor Min `(0,1)`, Max `(1,1)`, Pivot `(0.5,1)`, anchored position `0/0`, Width offset `0`, initial Height `0`; `ContentSizeFitter` controls height.

### 5.5 Police Database

| Element | Anchor Min / Max | Pivot | Size or offsets |
|---|---|---|---|
| `DatabaseSearchBar` | `(0,1) / (1,1)` | `(0.5,1)` | Height `62`; Left/Right `12`, Top `64` |
| `DatabaseSearchInput` | layout-driven | `(0.5,0.5)` | flexible width `1`, preferred height `42` |
| `SearchButton` | layout-driven | `(0.5,0.5)` | `130 × 42` |
| `ClearButton` | layout-driven | `(0.5,0.5)` | `110 × 42` |
| `DatabaseContent` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Bottom `12`, Top `138` |
| `DatabaseResultsPane` | `(0,0) / (0,1)` | `(0,0.5)` | Width `520`; all outer offsets `0` |
| `ResultsStatusText` | `(0,1) / (1,1)` | `(0.5,1)` | Height `38`; Left/Right `10`, Top `8` |
| `DatabaseResultsScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Bottom `8`, Top `54` |
| `DatabaseDetailsPane` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left `532`, Right/Top/Bottom `0` |

### 5.6 Photos

| Element | Anchor Min / Max | Pivot | Size or offsets |
|---|---|---|---|
| `PhotoGalleryPane` | `(0,0) / (0,1)` | `(0,0.5)` | Width `520`; Left/Top/Bottom `0` |
| `PhotosScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `12` |
| `PhotoPreviewPane` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left `532`, Right/Top/Bottom `12` |
| `PhotoPreviewFrame` | layout-driven | `(0.5,0.5)` | preferred height `470`; flexible width `1` |
| `PhotoPreviewImage` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `12` |

На `PhotoPreviewImage` включи `Preserve Aspect`. Не използвай `AspectRatioFitter`; frame-ът определя максималната област.

### 5.7 Forensics

| Element | Anchor Min / Max | Pivot | Size or offsets |
|---|---|---|---|
| `ReportsListPane` | `(0,0) / (0,1)` | `(0,0.5)` | Width `520`; Left/Top/Bottom `0` |
| `ReportsScrollView` | `(0,0) / (1,1)` | `(0.5,0.5)` | all offsets `12` |
| `ReportDetailsPane` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left `532`, Right/Top/Bottom `12` |
| `FullReportScrollView` | layout-driven | `(0.5,0.5)` | flexible height `1`; minimum height `300` |

### 5.8 Common Scroll View children

За всеки list scroll:

| Element | Anchor Min / Max | Pivot | Offsets |
|---|---|---|---|
| `Viewport` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Top/Bottom `0`, Right `18` |
| list `Content` | `(0,1) / (1,1)` | `(0.5,1)` | Left/Right/Top `0`, Height `0` |
| `VerticalScrollbar` | `(1,0) / (1,1)` | `(1,0.5)` | Width `16`, Right/Top/Bottom `0` |
| `Sliding Area` | `(0,0) / (1,1)` | `(0.5,0.5)` | Left/Right/Top/Bottom `8` |
| `Handle` | stretch inside `Sliding Area` | `(0.5,0.5)` | generated default; minimum visual height `24` |

За `EmailBodyScrollView` и `FullReportScrollView` използвай същото, а `EmailBodyContent`/`FullReportContent` са stretch-width, top-anchored и height `0`.

## 6. Visual settings

`Police Desk 98` prototype style: оригинален police workstation, вдъхновен от късните 90-те, без директно копиране на Windows UI.

| Role | Color |
|---|---|
| Desktop background | `#006B6B` |
| Window body | `#C0C0C0` |
| Pane background | `#FFFFFF` |
| Title bar | `#000080` |
| Selected/hover accent | `#000080` |
| Taskbar | `#C0C0C0` |
| Primary dark text | `#000000` |
| Secondary text | `#303030` |
| Light title text | `#FFFFFF` |

- Window/pane `Image` alpha: `1.0`; overlay `PowerMenuPanel` alpha: `0.97`.
- UI font: `Assets/_Game/UI/Fonts/PoliceTerminal SDF.asset`, копиран от bundled TMP `Electronic Highway Sign`; chrome labels и buttons са uppercase.
- Title text: `28 px`, Bold, uppercase, left aligned, no wrapping.
- Main value text: `18 px`; label text: `15 px`, Bold, secondary color.
- List row title: `18 px`; metadata: `14–15 px`.
- Long body fields: `17 px`, top-left, wrapping ON, overflow `Overflow` inside a Scroll View.
- Button text: `17 px`, Bold, uppercase, center aligned. Close button: `20 px`, text `X`.
- Desktop icon label: `16 px`, uppercase, center aligned, wrapping ON.
- Panels използват твърд `1 px` тъмен outline. Buttons използват светъл горен/ляв и тъмен долен/десен shadow за квадратен beveled control.
- Няма rounded corners, transparency, gradients, glow, scanlines или hacker effects.
- `PhotoPreviewFrame` background: near-black `#11161A`; preview image white tint.
- Turn `Raycast Target` OFF on decorative Images and non-interactive TMP text. Keep it ON only for Button graphics, TMP Input Field and Scroll View controls.

## 7. Initial active state

| GameObject | INITIAL ACTIVE | Runtime rule |
|---|---|---|
| `Canvas` | ON | винаги наличен |
| `ComputerRoot` | OFF | ON само в focused computer mode |
| `ScreenOffPanel` | ON | показва се при `Off` и `Sleeping` |
| `BootPanel` | OFF | ON само при `Booting` |
| `DesktopPanel` | OFF | ON при `On`, включително когато app window е отворен |
| `Wallpaper`, `DesktopIcons`, `Taskbar` | ON | остават ON под active `DesktopPanel` |
| `MailWindow` | OFF | ON от `OpenMail` |
| `CaseFilesWindow` | OFF | ON от `OpenCaseFiles` |
| `DatabaseWindow` | OFF | ON от `OpenDatabase` |
| `PhotosWindow` | OFF | ON от `OpenPhotos` |
| `ForensicsWindow` | OFF | ON от `OpenForensics` |
| `OverviewPanel` | ON | default Case Files tab |
| `PeoplePanel` | OFF | ON само след `ShowPeople` |
| `RecordsPanel` | OFF | ON само след `ShowRecords` |
| `PowerMenuPanel` | OFF | toggle само при active desktop |
| `ExitComputerButton` | ON | видим във всеки computer screen state |
| всички Scroll View/Viewport/Content objects | ON | parent window контролира видимостта |
| `PhotoPreviewImage` GameObject | ON | `Image.enabled` е OFF без selected Sprite |
| prefab root objects | ON в prefab asset | instances се създават runtime |

Не поставяй app windows извън `DesktopPanel`. Desktop трябва да остане active background при отворено приложение.

## 8. Inspector references

### `ComputerInteractable` на physical Computer

| Field | Assign |
|---|---|
| Player Controller | Player → `FirstPersonController` |
| Player Interaction | Player → `PlayerInteraction` |
| Computer UI | UI controller object → `ComputerUIController` |
| Power Controller | physical Computer → `ComputerPowerController` |

### Existing Player dependencies

Не добавяй втори Player/input setup. Запази текущия Player wiring:

| Component | Serialized field | Current value |
|---|---|---|
| `FirstPersonController` | Move Speed | `5` |
| `FirstPersonController` | Mouse Sensitivity | `0.1` |
| `FirstPersonController` | Gravity | `-9.81` |
| `PlayerInteraction` | Interaction Distance | `3` |
| `PlayerInputReader` | няма serialized fields | един component на Player |

`FirstPersonController` и `PlayerInteraction` намират Player child Camera и `PlayerInputReader` чрез components на същия Player; не им добавяй Computer UI references.

### `ComputerPowerController`

| Field | Assign |
|---|---|
| Computer UI | `ComputerUIController` |
| Boot Duration | `3` |
| Shutdown Duration | `1.5` |

### `ComputerUIController`

| Field | Assign |
|---|---|
| Computer Root | `ComputerRoot` |
| Screen Off Panel | `ScreenOffPanel` |
| Boot Panel | `BootPanel` |
| Desktop Panel | `DesktopPanel` |
| Power Menu Panel | `PowerMenuPanel` |
| Email UI Controller | `DesktopPanel` → `EmailUIController` |
| Case Files UI Controller | `DesktopPanel` → `CaseFilesUIController` |
| Police Database UI Controller | `DesktopPanel` → `PoliceDatabaseUIController` |
| Photos UI Controller | `DesktopPanel` → `PhotosUIController` |
| Forensics UI Controller | `DesktopPanel` → `ForensicsUIController` |
| Database Window | `DatabaseWindow` |
| Photos Window | `PhotosWindow` |
| Forensics Window | `ForensicsWindow` |

Последните три legacy placeholder fields остават зададени към същите windows за backward-compatible fallback. При зададен controller се използва controller-ът.

### `EmailUIController`

| Field | Assign |
|---|---|
| Mail Window | `MailWindow` |
| Email List Content | `InboxScrollView/Viewport/EmailListContent` |
| Email Entry Prefab | `Assets/_Game/Prefabs/Computer/EmailEntry.prefab` |
| Sender Text | `EmailDetailsPane/SenderText` |
| Subject Text | `EmailDetailsPane/SubjectText` |
| Date Text | `EmailDetailsPane/DateText` |
| Body Text | `EmailBodyContent/BodyText` |
| Emails | test/content list |

### `EmailEntryUI` prefab

| Field | Assign |
|---|---|
| Button | prefab root `Button` |
| Sender Text | `SenderText` |
| Subject Text | `SubjectText` |
| Date Text | `DateText` |

### `CaseFilesUIController`

| Field | Assign |
|---|---|
| Case Files Window | `CaseFilesWindow` |
| Case List Content | `CaseListScrollView/Viewport/CaseListContent` |
| Case Entry Prefab | `Assets/_Game/Prefabs/Computer/CaseEntry.prefab` |
| Case Number Text | `CaseHeader/CaseNumberText` |
| Case Name Text | `CaseHeader/CaseNameText` |
| Status Text | `CaseHeader/CaseStatusText` |
| Date Text | `OverviewContent/DateRow/DateOpenedText` |
| Location Text | `OverviewContent/LocationRow/LocationText` |
| Victim Text | `OverviewContent/VictimRow/VictimText` |
| Lead Detective Text | `OverviewContent/LeadDetectiveRow/LeadDetectiveText` |
| Summary Text | `OverviewContent/SummaryText` |
| Overview Panel | `OverviewPanel` |
| People Panel | `PeoplePanel` |
| Records Panel | `RecordsPanel` |
| People List Content | `PeopleScrollView/Viewport/PeopleListContent` |
| Person Entry Prefab | `CasePersonEntry.prefab` |
| Person Name Text | `PersonDetailsPane/PersonNameText` |
| Person Role Text | `PersonDetailsPane/PersonRoleText` |
| Person Description Text | `PersonDetailsPane/PersonDescriptionText` |
| Records List Content | `RecordsScrollView/Viewport/RecordsListContent` |
| Record Entry Prefab | `CaseRecordEntry.prefab` |
| Record Title Text | `RecordDetailsPane/RecordTitleText` |
| Record Type Text | `RecordDetailsPane/RecordTypeText` |
| Record Date Text | `RecordDetailsPane/RecordDateText` |
| Record Body Text | `RecordDetailsPane/RecordBodyText` |
| Cases | Inspector case list |

### Case entry prefabs

| Component | Field | Assign |
|---|---|---|
| `CaseEntryUI` | Button | root Button |
| `CaseEntryUI` | Case Number Text | `CaseNumberText` |
| `CaseEntryUI` | Case Name Text | `CaseNameText` |
| `CaseEntryUI` | Status Text | `StatusText` |
| `CasePersonEntryUI` | Button | root Button |
| `CasePersonEntryUI` | Name Text | `NameText` |
| `CasePersonEntryUI` | Role Text | `RoleText` |
| `CaseRecordEntryUI` | Button | root Button |
| `CaseRecordEntryUI` | Title Text | `TitleText` |
| `CaseRecordEntryUI` | Record Type Text | `RecordTypeText` |
| `CaseRecordEntryUI` | Date Text | `DateText` |

### `PoliceDatabaseUIController`

| Field | Assign |
|---|---|
| Database Window | `DatabaseWindow` |
| Search Input | `DatabaseSearchInput` |
| Results Content | `DatabaseResultsScrollView/Viewport/DatabaseResultsContent` |
| Result Entry Prefab | `DatabaseResultEntry.prefab` |
| Results Status Text | `ResultsStatusText` |
| Full Name Text | `DatabaseDetailsPane/FullNameText` |
| Date Of Birth Text | `DatabaseDetailsPane/DateOfBirthText` |
| Address Text | `DatabaseDetailsPane/AddressText` |
| Occupation Text | `DatabaseDetailsPane/OccupationText` |
| Record Summary Text | `DatabaseDetailsPane/RecordSummaryText` |
| Records | Inspector database records list |

### `DatabaseResultEntryUI` prefab

| Field | Assign |
|---|---|
| Button | root Button |
| Full Name Text | `FullNameText` |
| Date Of Birth Text | `DateOfBirthText` |
| Occupation Text | `OccupationText` |

### `PhotosUIController`

| Field | Assign |
|---|---|
| Photos Window | `PhotosWindow` |
| Photo List Content | `PhotosScrollView/Viewport/PhotoListContent` |
| Photo Entry Prefab | `PhotoEntry.prefab` |
| Preview Image | `PhotoPreviewFrame/PhotoPreviewImage` |
| Title Text | `PhotoPreviewPane/PhotoTitleText` |
| Date Text | `PhotoPreviewPane/PhotoDateText` |
| Location Text | `PhotoPreviewPane/PhotoLocationText` |
| Description Text | `PhotoPreviewPane/PhotoDescriptionText` |
| Photos | Inspector photos list |

### `PhotoEntryUI` prefab

| Field | Assign |
|---|---|
| Button | root Button |
| Thumbnail Image | `ThumbnailImage` |
| Title Text | `TitleText` |
| Date Text | `DateText` |
| Location Text | `LocationText` |

### `ForensicsUIController`

| Field | Assign |
|---|---|
| Forensics Window | `ForensicsWindow` |
| Reports Content | `ReportsScrollView/Viewport/ReportsContent` |
| Report Entry Prefab | `ForensicsReportEntry.prefab` |
| Report Number Text | `ReportDetailsPane/ReportNumberText` |
| Title Text | `ReportDetailsPane/ReportTitleText` |
| Status Text | `ReportDetailsPane/ReportStatusText` |
| Date Text | `ReportDetailsPane/ReportDateText` |
| Related Case Text | `ReportDetailsPane/RelatedCaseText` |
| Summary Text | `ReportDetailsPane/ReportSummaryText` |
| Full Report Text | `FullReportContent/FullReportText` |
| Reports | Inspector reports list |

### `ForensicsReportEntryUI` prefab

| Field | Assign |
|---|---|
| Button | root Button |
| Report Number Text | `ReportNumberText` |
| Title Text | `TitleText` |
| Status Text | `StatusText` |
| Date Text | `DateText` |

## 9. Button OnClick bindings

| Button GameObject | Target GameObject | Component | Method |
|---|---|---|---|
| `MailIcon` | UI controller object | `ComputerUIController` | `OpenMail()` |
| `CaseFilesIcon` | UI controller object | `ComputerUIController` | `OpenCaseFiles()` |
| `DatabaseIcon` | UI controller object | `ComputerUIController` | `OpenDatabase()` |
| `PhotosIcon` | UI controller object | `ComputerUIController` | `OpenPhotos()` |
| `ForensicsIcon` | UI controller object | `ComputerUIController` | `OpenForensics()` |
| `CloseMailButton` | `DesktopPanel` | `EmailUIController` | `CloseMail()` |
| `CloseCaseFilesButton` | `DesktopPanel` | `CaseFilesUIController` | `CloseCaseFiles()` |
| `OverviewButton` | `DesktopPanel` | `CaseFilesUIController` | `ShowOverview()` |
| `PeopleButton` | `DesktopPanel` | `CaseFilesUIController` | `ShowPeople()` |
| `RecordsButton` | `DesktopPanel` | `CaseFilesUIController` | `ShowRecords()` |
| `CloseDatabaseButton` | `DesktopPanel` | `PoliceDatabaseUIController` | `CloseDatabase()` |
| `SearchButton` | `DesktopPanel` | `PoliceDatabaseUIController` | `Search()` |
| `ClearButton` | `DesktopPanel` | `PoliceDatabaseUIController` | `ClearSearch()` |
| `ClosePhotosButton` | `DesktopPanel` | `PhotosUIController` | `ClosePhotos()` |
| `CloseForensicsButton` | `DesktopPanel` | `ForensicsUIController` | `CloseForensics()` |
| `PowerButton` | UI controller object | `ComputerUIController` | `TogglePowerMenu()` |
| `CancelPowerMenuButton` | UI controller object | `ComputerUIController` | `HidePowerMenu()` |
| `SleepButton` | physical Computer | `ComputerPowerController` | `Sleep()` |
| `RestartButton` | physical Computer | `ComputerPowerController` | `Restart()` |
| `ShutdownButton` | physical Computer | `ComputerPowerController` | `ShutDown()` |
| `ExitComputerButton` | UI controller object | `ComputerUIController` | `ExitComputer()` |

Не добавяй manual OnClick за generated entry prefabs. Техните `Awake()` methods добавят един runtime listener и изпращат typed selection callback към app controller-а.

## 10. Prefabs

Папка за всички prefabs: `Assets/_Game/Prefabs/Computer/`. `EmailEntry.prefab` и `CaseEntry.prefab` вече съществуват. Създай останалите пет в Editor; не оставяй prefab instance като child на scene `Content` object.

### `EmailEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/EmailEntry.prefab`
- Status: съществува
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `90`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `VerticalLayoutGroup`, `EmailEntryUI`
- Hierarchy:

```text
EmailEntry
├── SenderText
├── SubjectText
└── DateText
```

- `VerticalLayoutGroup`: Padding L/R `10`, T/B `6`; Spacing `2`; Control Width/Height ON; Force Expand Width ON; Force Expand Height OFF.
- `LayoutElement`: Preferred Height `90`, Flexible Height `0`.
- Inspector: Button → root; Sender/Subject/Date Text → matching child.

### `CaseEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/CaseEntry.prefab`
- Status: съществува
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `90`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `VerticalLayoutGroup`, `CaseEntryUI`
- Hierarchy:

```text
CaseEntry
├── CaseNumberText
├── CaseNameText
└── StatusText
```

- `VerticalLayoutGroup`: Padding L/R `10`, T/B `6`; Spacing `2`; Control Width/Height ON; Force Expand Width ON; Force Expand Height OFF.
- `LayoutElement`: Preferred Height `90`, Flexible Height `0`.
- Inspector: Button → root; three TMP references → matching children.

### `CasePersonEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/CasePersonEntry.prefab`
- Status: трябва да се създаде
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `72`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `VerticalLayoutGroup`, `CasePersonEntryUI`
- Hierarchy:

```text
CasePersonEntry
├── NameText
└── RoleText
```

- `LayoutElement`: Preferred Height `72`, Flexible Height `0`.
- Text: Name `17 px` Bold; Role `14 px` secondary color.
- Inspector: Button → root; Name Text → `NameText`; Role Text → `RoleText`.

### `CaseRecordEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/CaseRecordEntry.prefab`
- Status: трябва да се създаде
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `90`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `VerticalLayoutGroup`, `CaseRecordEntryUI`
- Hierarchy:

```text
CaseRecordEntry
├── TitleText
├── RecordTypeText
└── DateText
```

- `LayoutElement`: Preferred Height `90`, Flexible Height `0`.
- Inspector: Button → root; Title/Record Type/Date Text → matching children.

### `DatabaseResultEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/DatabaseResultEntry.prefab`
- Status: трябва да се създаде
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `92`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `VerticalLayoutGroup`, `DatabaseResultEntryUI`
- Hierarchy:

```text
DatabaseResultEntry
├── FullNameText
├── DateOfBirthText
└── OccupationText
```

- `LayoutElement`: Preferred Height `92`, Flexible Height `0`.
- Inspector: Button → root; Full Name/Date Of Birth/Occupation Text → matching children.

### `PhotoEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/PhotoEntry.prefab`
- Status: трябва да се създаде
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `120`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `HorizontalLayoutGroup`, `PhotoEntryUI`
- Hierarchy:

```text
PhotoEntry
├── ThumbnailImage
└── Metadata
    ├── TitleText
    ├── DateText
    └── LocationText
```

- `ThumbnailImage`: `Image`, `LayoutElement`; preferred width `112`, preferred height `92`, Preserve Aspect ON.
- `Metadata`: `RectTransform`, `VerticalLayoutGroup`, `LayoutElement`; flexible width `1`.
- Root `HorizontalLayoutGroup`: Padding `10`; Spacing `10`; Control Width/Height ON; Force Expand Width ON; Force Expand Height OFF.
- Inspector: Button → root; Thumbnail Image → child; Title/Date/Location Text → `Metadata` children.

### `ForensicsReportEntry.prefab`

- Path: `Assets/_Game/Prefabs/Computer/ForensicsReportEntry.prefab`
- Status: трябва да се създаде
- Root type: `Button - TextMeshPro` без default label
- Recommended size: stretch width, preferred height `108`
- Root components: `RectTransform`, `CanvasRenderer`, `Image`, `Button`, `LayoutElement`, `VerticalLayoutGroup`, `ForensicsReportEntryUI`
- Hierarchy:

```text
ForensicsReportEntry
├── ReportNumberText
├── TitleText
├── StatusText
└── DateText
```

- `LayoutElement`: Preferred Height `108`, Flexible Height `0`.
- Inspector: Button → root; four TMP references → matching children.

За всички entry prefabs:

- Root Anchor Min/Max `(0.5,0.5)` в prefab mode, Pivot `(0.5,0.5)`. Parent `VerticalLayoutGroup` задава runtime width.
- Button Navigation: `Automatic`; Transition: `Color Tint`; Fade Duration `0.08`.
- Не задавай OnClick в prefab Inspector.
- Decorative text/image `Raycast Target`: OFF; root button Image: ON.
- Prefab root local Scale: `(1,1,1)`.

## 11. Layout Group settings

### List `Content` objects

Приложи на `EmailListContent`, `CaseListContent`, `PeopleListContent`, `RecordsListContent`, `DatabaseResultsContent`, `PhotoListContent`, `ReportsContent`:

| VerticalLayoutGroup setting | Value |
|---|---|
| Padding Left / Right | `6 / 6` |
| Padding Top / Bottom | `6 / 6` |
| Spacing | `6` |
| Child Alignment | `Upper Center` |
| Control Child Size Width | ON |
| Control Child Size Height | ON |
| Use Child Scale Width / Height | OFF / OFF |
| Child Force Expand Width | ON |
| Child Force Expand Height | OFF |
| Reverse Arrangement | OFF |

`ContentSizeFitter`: Horizontal Fit `Unconstrained`; Vertical Fit `Preferred Size`.

### `OverviewContent`, `EmailBodyContent`, `FullReportContent`

| Setting | Value |
|---|---|
| Padding | L/R `18`, T/B `16` |
| Spacing | `10` |
| Child Alignment | `Upper Left` |
| Control Child Size Width | ON |
| Control Child Size Height | ON |
| Force Expand Width | ON |
| Force Expand Height | OFF |
| ContentSizeFitter Horizontal | `Unconstrained` |
| ContentSizeFitter Vertical | `Preferred Size` |

`SummaryText`, `BodyText`, `FullReportText`, `PersonDescriptionText`, `RecordBodyText`, `RecordSummaryText` трябва да имат `LayoutElement` с Min Height `80`, Flexible Height `0`. За body/report fields ползвай Min Height `180`.

### Metadata/detail panes

За `EmailDetailsPane`, `PersonDetailsPane`, `RecordDetailsPane`, `DatabaseDetailsPane`, `PhotoPreviewPane`, `ReportDetailsPane`:

| VerticalLayoutGroup setting | Value |
|---|---|
| Padding | L/R `20`, T/B `18` |
| Spacing | `8` |
| Child Alignment | `Upper Left` |
| Control Child Size Width | ON |
| Control Child Size Height | ON |
| Force Expand Width | ON |
| Force Expand Height | OFF |

На scroll view child-а в `EmailDetailsPane` и `ReportDetailsPane` добави `LayoutElement`: Flexible Height `1`, Min Height `300`.

### `CaseTabBar`

| HorizontalLayoutGroup setting | Value |
|---|---|
| Padding | `0` |
| Spacing | `6` |
| Child Alignment | `Middle Left` |
| Control Child Size Width / Height | ON / ON |
| Force Expand Width | ON |
| Force Expand Height | ON |

Всеки tab button: `LayoutElement` Flexible Width `1`, Preferred Height `40`.

### `DatabaseSearchBar`

| HorizontalLayoutGroup setting | Value |
|---|---|
| Padding | L/R `10`, T/B `10` |
| Spacing | `8` |
| Child Alignment | `Middle Left` |
| Control Child Size Width / Height | ON / ON |
| Force Expand Width | OFF |
| Force Expand Height | OFF |

`DatabaseSearchInput`: Flexible Width `1`, Preferred Height `42`. Search/Clear buttons: Flexible Width `0`.

### `DesktopIcons`

| VerticalLayoutGroup setting | Value |
|---|---|
| Padding | `0` |
| Spacing | `12` |
| Child Alignment | `Upper Left` |
| Control Child Size Width / Height | ON / ON |
| Force Expand Width / Height | OFF / OFF |

### `PowerMenuPanel`

| VerticalLayoutGroup setting | Value |
|---|---|
| Padding | `12` on all sides |
| Spacing | `8` |
| Child Alignment | `Upper Center` |
| Control Child Size Width / Height | ON / ON |
| Force Expand Width | ON |
| Force Expand Height | OFF |

## 12. Scrolling

Всички седем scalable lists използват Scroll View:

| List | ScrollRect object | Viewport | Content reference | Vertical scrollbar |
|---|---|---|---|---|
| Mail Inbox | `InboxScrollView` | child `Viewport` | `EmailListContent` | child `VerticalScrollbar` |
| Case List | `CaseListScrollView` | child `Viewport` | `CaseListContent` | child `VerticalScrollbar` |
| People List | `PeopleScrollView` | child `Viewport` | `PeopleListContent` | child `VerticalScrollbar` |
| Records List | `RecordsScrollView` | child `Viewport` | `RecordsListContent` | child `VerticalScrollbar` |
| Database Results | `DatabaseResultsScrollView` | child `Viewport` | `DatabaseResultsContent` | child `VerticalScrollbar` |
| Photos | `PhotosScrollView` | child `Viewport` | `PhotoListContent` | child `VerticalScrollbar` |
| Forensics Reports | `ReportsScrollView` | child `Viewport` | `ReportsContent` | child `VerticalScrollbar` |

За всеки от тях `ScrollRect`:

| Setting | Value |
|---|---|
| Content | matching content от таблицата |
| Viewport | matching `Viewport` |
| Horizontal | OFF |
| Vertical | ON |
| Movement Type | `Clamped` |
| Inertia | ON |
| Deceleration Rate | `0.135` |
| Scroll Sensitivity | `25` |
| Vertical Scrollbar | matching `VerticalScrollbar` |
| Visibility | `Auto Hide And Expand Viewport` |
| Spacing | `-3` |

`Viewport/Mask`: Show Mask Graphic OFF. `Viewport/Image`: alpha `0.01`, Raycast Target ON.

Long email body и full forensic report използват отделните `EmailBodyScrollView` и `FullReportScrollView` със същите settings. Overview също е scrollable, защото summary може да е дълъг. Не поставяй nested scroll view вътре в list entry prefab.

## 13. Inspector test data

Тези данни са само development placeholders и не са narrative canon. Въвеждат се в controller Inspector lists, не в C# control logic.

### Mail — 3 emails

1. Подател: `Отдел Криминалистика`; Тема: `Предварителен доклад от огледа`; Дата: `14/10/2011 09:32`; Текст: `Предварителният оглед е завършен. Допълнителните резултати ще бъдат изпратени по-късно.`
2. Подател: `Полицай Милър`; Тема: `Претърсване на апартамента`; Дата: `14/10/2011 10:05`; Текст: `В кухнята намерихме бележка от паркинг. Отпечатаният час е 22:41.`
3. Sender: `Records Office`; Subject: `Case File Update`; Date: `14/10/2011 10:18`; Body: `The witness contact sheet has been added to CASE-0017.`

### Case Files — 2 cases

Case 1:

- Case Number: `CASE-0017`
- Case Name: `Убийство в апартамент на бул. „Ривърсайд“`
- Status: `ОТВОРЕНО`
- Date Opened: `14 октомври 2011`
- Location: `бул. „Ривърсайд“ 17`
- Victim: `Даниел Харис`
- Lead Detective: `Майкъл Картър`
- Summary: `Жертвата е открита мъртва в апартамента си около 23:20. Разследването продължава.`
- People:
  - Name `Даниел Харис`; Role `Жертва`; Short Description `Обитател на апартамента, в който е подаден сигналът.`
  - Name `Елена Мур`; Role `Свидетел`; Short Description `Съседка, която съобщава, че е чула движение в коридора.`
  - Name `Томас Рийд`; Role `Лице от интерес`; Short Description `Познат на жертвата, посочен в първоначалния доклад.`
  - Name `Полицай Милър`; Role `Полицай`; Short Description `Служителят, който е описал претърсването на апартамента.`
- Records:
  - Title `Първоначален доклад за инцидента`; Record Type `Доклад за инцидент`; Date `14/10/2011`; Body `Служителите са се отзовали на сигнал в 23:20 и са обезопасили апартамента.`
  - Title `Свидетелски показания — Елена Мур`; Record Type `Свидетелски показания`; Date `14/10/2011`; Body `Свидетелката съобщава за шум в коридора, но не посочва източника му.`
  - Title `Искане за аутопсия`; Record Type `Искане за аутопсия`; Date `14/10/2011`; Body `Поискан е официален медицински преглед. Резултатите се очакват.`

Case 2:

- Case Number: `CASE-0012`
- Case Name: `Разследване на пожар в склад`
- Status: `АРХИВИРАНО`
- Date Opened: `3 октомври 2011`
- Location: `Северна промишлена зона`
- Victim: `Няма`
- Lead Detective: `Сара Бенет`
- Summary: `Първоначалното разследване не открива потвърдени данни за умишлено деяние. Делото е архивирано.`
- People: Name `Sarah Bennett`; Role `Officer`; Short Description `Lead detective named in the archived file.`
- Records:
  - Title `Fire Marshal Summary`; Record Type `Official Report`; Date `05/10/2011`; Body `The available observations did not establish deliberate ignition.`
  - Title `Closure Note`; Record Type `Case Note`; Date `09/10/2011`; Body `The file was archived pending new official information.`

### Police Database — 3 records

1. Full Name `Томас Рийд`; Date Of Birth `22/03/1979`; Address `ул. „Уестбридж“ 42`; Occupation `Шофьор-доставчик`; Record Summary `Самоличността и адресът са потвърдени. Има едно предишно нарушение на пътя. Записът не съдържа заключение за активното дело.`
2. Full Name `Елена Мур`; Date Of Birth `08/11/1984`; Address `бул. „Ривърсайд“ 19`; Occupation `Счетоводител`; Record Summary `Настоящият адрес е потвърден. Няма посочени криминални регистрации.`
3. Full Name `Даниел Харис`; Date Of Birth `17/06/1975`; Address `бул. „Ривърсайд“ 17`; Occupation `Архитект`; Record Summary `Записът за самоличност е свързан с CASE-0017.`

Test searches: `томас`, `ТОМАС`, `Рийд` трябва да намерят Томас Рийд; `Несъществуващ` трябва да покаже точно `Няма намерени записи`; празна заявка трябва да покаже `Въведете пълно име`.

### Photos — 3 photos

1. Title `Входът на апартамента`; Date `14/10/2011 23:38`; Location `бул. „Ривърсайд“ 17`; Description `Външен изглед на входа, след като мястото е обезопасено.`; Image: assign a development Sprite.
2. Title `Кухненската маса`; Date `14/10/2011 23:52`; Location `Кухнята на апартамента`; Description `Обща снимка на масата и близките повърхности.`; Image: assign a development Sprite.
3. Title `Бележка от паркинг`; Date `15/10/2011 00:06`; Location `Кухнята на апартамента`; Description `Близък кадър на намерената бележка. Снимката документира само външния ѝ вид.`; Image: assign a development Sprite.

### Forensics — 2 reports

1. Report Number `FR-2011-184`; Title `Предварителен оглед`; Status `ЗАВЪРШЕН`; Date `14/10/2011`; Related Case `CASE-0017`; Summary `Налични са предварителни наблюдения.`; Full Report `Докладът съдържа квалифицирани предварителни наблюдения. Допълнителната лабораторна работа може да промени или уточни резултатите.`
2. Report Number `FR-2011-191`; Title `Анализ на следови материали`; Status `ИЗЧАКВА`; Date `15/10/2011`; Related Case `CASE-0017`; Summary `Пробите са получени в лабораторията.`; Full Report `Анализът предстои. Към този момент няма наличен резултат или интерпретация.`

## 14. Full test checklist

Започни с Console → Clear. Тествай в Play Mode в `Assets/_Game/Scenes/DetectiveOffice.unity`.

### Computer power и focused mode

- [ ] От `Off`, натисни `E` върху Computer: `ComputerRoot` се отваря, controls спират, cursor е visible/unlocked.
- [ ] Вижда се Boot panel за configured duration, после clean Desktop.
- [ ] `Exit Computer` затваря UI, без shutdown; movement/look/interaction се възстановяват.
- [ ] Re-enter: computer е още `On` и показва Desktop.
- [ ] `Sleep`: computer минава в `Sleeping`, computer mode се затваря; re-enter прави wake.
- [ ] `Restart`: active app се затваря, Boot panel се показва, после clean Desktop.
- [ ] `Shutdown`: active app се затваря, след delay computer mode се затваря и state е `Off`.
- [ ] След shutdown re-enter започва нов boot.
- [ ] Rapid repeated `E` и repeated power clicks не създават duplicate mode или stuck cursor.

### Mail

- [ ] Mail icon отваря само `MailWindow`.
- [ ] Точно 3 inbox entries се появяват; repeated close/open не ги дублира.
- [ ] Всеки entry показва правилните Sender, Subject, Date и Body.
- [ ] Close връща видим clean Desktop.

### Case Files

- [ ] Точно 2 case entries се появяват без duplication.
- [ ] Selection сменя header и Overview details правилно.
- [ ] `Overview`, `People`, `Records` показват само съответния panel.
- [ ] People rows съвпадат с избрания case; person selection показва Name, Role, Short Description.
- [ ] Records rows съвпадат с избрания case; record selection показва Title, Type, Date, Body.
- [ ] Смяна Case 1 → Case 2 не оставя entries от Case 1.
- [ ] Close връща Desktop.

### Police Database

- [ ] `thomas`, `THOMAS` и `Reed` дават Thomas Reed.
- [ ] Result selection показва всички пет fields.
- [ ] `Несъществуващ` показва `Няма намерени записи` и празни details.
- [ ] Празна заявка показва `Enter a full name`.
- [ ] `Clear` изчиства input, generated results, status и details.
- [ ] Repeated Search не дублира results.
- [ ] Close връща Desktop.

### Photos

- [ ] Точно 3 entries се появяват без duplication.
- [ ] Всеки entry избира правилния preview Sprite.
- [ ] Title, Date, Location и Description съвпадат със selection.
- [ ] Missing optional Sprite не хвърля exception; metadata остава видимо.
- [ ] Close връща Desktop.

### Forensics

- [ ] Точно 2 reports се появяват без duplication.
- [ ] Report selection показва Number, Title, Status, Date, Related Case, Summary и Full Report.
- [ ] `ИЗЧАКВА` е само official status и не стартира timer.
- [ ] Close връща Desktop.

### Exclusive app switching

- [ ] Open Mail → Case Files: Mail се затваря.
- [ ] Case Files → Database: Case Files се затваря.
- [ ] Database → Photos: Database се затваря.
- [ ] Photos → Forensics: Photos се затваря.
- [ ] Във всеки момент е active най-много един app window.
- [ ] Desktop background и Taskbar остават active.

### Regression и Console

- [ ] WASD, mouse look и `E` работят след всеки exit path.
- [ ] Document inspection, Phone и Evidence Board продължават да се отварят/затварят.
- [ ] Няма `MissingReferenceException`, `NullReferenceException` или missing-script components.
- [ ] Няма duplicate generated UI entries след full flow.
- [ ] Console завършва с `0` red errors.
- [ ] Провери UI при `1920 × 1080`, после при поне една различна 16:9 резолюция.

Compilation не доказва тези Play Mode резултати. Запиши runtime evidence в `docs/prototype/ACCEPTANCE_TESTS.md` completion record само след реално наблюдение.

## 15. Troubleshooting

### Window не се вижда

- Провери parent: app window трябва да е child на active `DesktopPanel`.
- Провери INITIAL ACTIVE и дали `ComputerPowerController.CurrentState` е `On`.
- Провери anchors, size `1560 × 860`, local Scale `(1,1,1)` и sibling order над `Wallpaper`.
- Провери matching controller window reference и `ComputerUIController` controller reference.

### Button не работи

- Провери, че scene има точно един active `EventSystem` и `InputSystemUIInputModule`.
- Провери Button `Interactable`, target Image `Raycast Target` и OnClick table от секция 9.
- Изключи `Raycast Target` на декоративен overlay, който покрива button-а.
- Entry prefab buttons нямат manual OnClick; callback се регистрира от entry script.

### List entries не се появяват

- Провери controller `Content` reference, prefab reference и дали Inspector data list има elements.
- Content трябва да е празен scene object под правилния Viewport.
- Prefab root трябва да има matching entry component и assigned TMP fields.
- Провери, че prefab asset root е Active и Scale `(1,1,1)`.

### Entries се дублират

- Не поставяй prefab instance ръчно под Content.
- Не bind-вай `BuildInbox`, `BuildCaseList`, `BuildGallery` или `BuildReportList` към buttons.
- Database Search сам изчиства старите generated results; не добавяй втори search listener.

### Text не се вижда или се реже

- Провери TMP reference, font asset, font size, color alpha и parent active state.
- За long text включи wrapping и използвай `ContentSizeFitter` само на content root, не върху Scroll View root.
- Провери `Viewport/Mask`, Content pivot `(0.5,1)` и top anchor.

### Scroll не работи

- Провери `ScrollRect.Content`, `ScrollRect.Viewport` и Vertical Scrollbar references.
- Horizontal OFF, Vertical ON; Content height трябва да стане по-голяма от Viewport чрез `ContentSizeFitter`.
- Провери, че Viewport Image има Raycast Target ON и alpha поне `0.01`.

### Police Database винаги връща no result

- Провери, че data е в `PoliceDatabaseUIController.Records`, а не в Case Files People.
- Search използва case-insensitive substring само върху `FullName`.
- Провери, че `DatabaseSearchInput` е assigned към `Search Input`.

### Photo metadata се вижда, но image липсва

- Провери `PhotoData.Image` Sprite reference и Texture Type `Sprite (2D and UI)`.
- Провери `PhotoPreviewImage` reference и Image color alpha `1`.
- Missing Sprite е валидно: controller-ът изключва Image component без exception.

### След Exit cursor или movement е stuck

- Провери `ComputerInteractable` references към същия Player и същия `ComputerUIController`.
- Не добавяй втори `PlayerInputReader`; той остава единствен owner на generated input wrapper-а.
- Провери Console за error преди exit; runtime exception може да прекъсне restoration flow.

### Missing-reference warnings

- Warning-ите са guard clauses, не автоматично wiring. Свържи всички fields от секция 8.
- Не използвай `Find`, `GameObject.Find` или string hierarchy lookup като workaround.

## Recommended Unity rebuild and verification order

1. Запази backup/commit на scene-а.
2. Изпълни `Tools > DetectiveGame > Build Computer UI` само когато искаш deliberate rebuild на Computer UI subtree.
3. При нужда assign-ни development Sprites към трите `PhotoData.Image` fields; липсващ Sprite е валидно fallback състояние.
4. Провери initial active states от секция 7 и serialized references от секция 8.
5. Clear Console и изпълни целия checklist от секция 14 в Play Mode.
