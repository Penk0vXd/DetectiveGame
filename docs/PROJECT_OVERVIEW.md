# DetectiveGame — исторически преглед на проекта

> **STALE SNAPSHOT WARNING — 2026-09-28:** Този документ е ранна снимка и вече съдържа остарели твърдения, включително че Case Files и някои computer references не са свързани. Текущата сцена вече съдържа тези връзки. За актуален implementation status използвай [`prototype/CURRENT_IMPLEMENTATION.md`](prototype/CURRENT_IMPLEMENTATION.md), а за навигация — [`00_INDEX.md`](00_INDEX.md). Не използвай този файл като canonical source за нови AI задачи.

> Документ, генериран чрез анализ на целия код, сцената (`DetectiveOffice.unity`), префабите и настройките на проекта, към 2026-09-27. Целта му е да служи като „снимка" на текущото състояние: какво е написано, как е окачено в Hierarchy, кое работи, кое не и защо.

---

## 1. Какво представлява играта

„DetectiveGame" е **first-person detective/investigation игра** в един кабинет („DetectiveOffice"). Играчът се движи в стая, гледа през камера от първо лице и може да проверява няколко интерактивни обекта:

- документ на бюрото (взима се пред камерата и се разглежда),
- компютър (с включване/изключване, boot екран и desktop с приложения — засега само Mail работи изцяло),
- телефон (контакт + разговор с диалог текст),
- дъска за доказателства („Evidence Board") с 3 карти доказателства, всяка с детайлен изглед.

В момента играта е **гъст прототип/vertical slice** — една стая, поставена геометрия (кубове вместо истински модели), и работеща основна логика за взаимодействие, но с една незавършена система (Case Files) и два напълно празни stub-а (Database, Photos).

---

## 2. Технологии и версии

| Компонент | Версия |
|---|---|
| Unity Editor | `6000.6.3f1` (Unity 6) |
| Render Pipeline | Universal RP (URP) `17.6.0` |
| Input System | новият `com.unity.inputsystem` `1.20.0` (не стария `Input.GetAxis`) |
| UI | UGUI + **TextMeshPro** (вече е част от `com.unity.ugui 2.6.0`, не отделен пакет) |
| AI Navigation | пакетът е инсталиран (`com.unity.ai.navigation`), но **не се използва никъде** в момента (няма NavMesh агенти) |

Скриптовете нямат namespace и се компилират в default `Assembly-CSharp`.

---

## 3. Структура на файловете

```
Assets/_Game/
├── Art/            Materials, Models, Textures — ПРАЗНИ папки (все още няма финално арт съдържание;
│                    всичко видимо в сцената са primitive кубове от Unity)
├── Audio/           празна
├── Data/            празна
├── Input/
│   ├── DetectiveGameInput.inputactions   — картата с бутони (виж §6)
│   └── DetectiveGameInput.cs             — автогенериран C# clas от Unity (не се пипа на ръка)
├── Prefabs/Computer/
│   ├── CaseEntry.prefab      — ред в списъка "Case Files" (не се използва все още, виж §9.1)
│   └── EmailEntry.prefab     — ред в списъка "Inbox" (използва се, работи)
├── Scenes/
│   └── DetectiveOffice.unity — единствената сцена в играта
├── Scripts/
│   ├── Computer/    — 8 файла: логиката на компютъра и имейла
│   ├── EvidenceBoard/
│   ├── Interaction/ — интерфейс + документи + тестов обект
│   ├── Phone/
│   └── Player/      — движение, input, raycast interaction
└── UI/              празна (UI-то е директно в сцената, не в отделни UI prefab-и, с изключение на горните два)
```

Изтрити са стандартните demo файлове на Unity темплейта (`SampleScene`, `Readme`, `TutorialInfo`) — това е чистене, не грешка.

---

## 4. Hierarchy на `DetectiveOffice.unity`

Това е реалната структура на сцената в момента (8 root обекта, 91 GameObject-а общо), реконструирана директно от `.unity` файла:

- **Environment**
  - **Floor** — _BoxCollider, MeshRenderer, MeshFilter_
  - **Wall_Back / Wall_Left / Wall_Front_Left / Wall_Front_Right / Wall_Front_Top / Wall_Right** — същите компоненти (просто стени/под, placeholder геометрия)
- **Directional Light** — _Light + UniversalAdditionalLightData (URP)_
- **Canvas** — _CanvasScaler, GraphicRaycaster, Canvas_, и **тук седят трите основни UI контролера**: `ComputerUIController`, `PhoneUIController`, `EvidenceBoardUIController` (плюс един „забравен" празен `Button`, виж §9.2)
  - **ComputerRoot** _(компютърният екран, стартира изключен)_
    - **ScreenOffPanel** — черен екран, когато компютърът е изгасен
    - **BootPanel** → **BootLogoText**, **LoadingText** — екранът при стартиране
    - **DesktopPanel** _(тук седи и `EmailUIController`)_
      - **Wallpaper**
      - **DesktopIcons** → **MailIcon**, **CaseFilesIcon**, **DatabaseIcon**, **PhotosIcon** (всяка с `Text (TMP)` дете)
      - **Taskbar** → **StartButton**, **WorkstationText**, **ClockText**
      - **PowerMenuPanel** → **SleepButton**, **RestartButton**, **ShutDownButton**
      - **MailWindow** *(неактивен по подразбиране)* → **TitleBar** (заглавие + **CloseMailButton**), **InboxPanel** → **EmailListContent** (тук се пълни списъкът по код), **EmailContentPanel** → **SenderText/SubjectText/DateText/BodyText**
    - **ExitComputerButton**
  - **PhoneRoot** → **ContactNameText**, **ContactsPanel** → **CallButton**, **CallPanel** → **DialogueText**, **HangUpButton**, **ExitPhoneButton**
  - **BoardRoot** _(с `HorizontalLayoutGroup`)_
    - **BoardPanel** → **EvidenceCard1/2/3** (всяка `Button` + `LayoutElement`, дете **Title**/**Description**)
    - **EvidenceDetailsPanel** → **EvidenceTitleText**, **EvidenceDescriptionText**, **BackButton**
    - **ExitBoardButton**
- **Document_Test** — _`DocumentInteractable`_, документ за разглеждане
- **Furniture**
  - **Desk_Top** — само геометрия
  - **Computer** — _`ComputerInteractable` + `ComputerPowerController`_
  - **Phone** — _`PhoneInteractable`_
  - **EvidenceBoard** — _`EvidenceBoardInteractable`_
- **TestInteractable** — тестов куб, който само се завърта при interact (виж §10)
- **EventSystem** — стандартен Unity EventSystem с `InputSystemUIInputModule` (за новия Input System)
- **Player** — _`CharacterController`, `FirstPersonController`, `PlayerInteraction`, `PlayerInputReader`_
  - **Main Camera** — _`Camera`, `AudioListener`, `UniversalAdditionalCameraData`_

---

## 5. Обяснение на скриптовете (по системи)

### 5.1 Player & Interaction (`Assets/_Game/Scripts/Player`, `Interaction`)

**`FirstPersonController.cs`** — движение и завъртане на играча.
- Изисква `CharacterController` + `PlayerInputReader` на същия обект (`[RequireComponent]`).
- `Awake()` хваща камерата чрез `GetComponentInChildren<Camera>()` — ако няма камера дете, изключва компонента с грешка.
- `Update()`: чете `Look`/`Move` от `PlayerInputReader`, върти играча хоризонтално (`transform.Rotate`) и камерата вертикално (clamp -80°/+80°), после движи `CharacterController` с проста гравитация.
- `SetControlEnabled(bool)` — публичен метод, чрез който **всички** interactable-и (компютър, телефон, дъска, документ) спират движението на играча, докато е отворен UI прозорец.

**`PlayerInputReader.cs`** — тънка обвивка над генерирания `DetectiveGameInput` (Input Actions asset). Дава `ReadMove()`, `ReadLook()`, `ReadInteract()`. Има защита (`EnsureInputReady`) в случай, че `input` бъде `null` след domain reload.

**`PlayerInteraction.cs`** — сърцето на interact логиката:
- Всеки кадър, ако е натиснат `Interact` (клавиш **E**), пуска `Physics.Raycast` напред от камерата (обхват `interactionDistance = 3`).
- Ако удареният `Collider` има компонент, имплементиращ `IInteractable`, вика `Interact()`.
- Специален случай за документи: ако резултатът е `DocumentInteractable` и той влиза в режим на разглеждане (`IsInspecting`), пази референция `inspectedDocument`, за да може следващото натискане на E (дори без да гледаш точно в документа) да го затвори.

**`IInteractable.cs`** — единственият интерфейс в проекта: `void Interact()`. Всичко interactable в играта минава през него (полиморфизъм — `PlayerInteraction` не знае нищо за компютър/телефон/дъска, само вика `Interact()`).

**`DocumentInteractable.cs`** — взима документа от масата и го „залепва" пред камерата:
- При `Interact()` премества `transform` като дете на камерата (`SetParent(playerCamera.transform)`), на фиксирана позиция/ъгъл, и спира движението на играча.
- При повторно `Interact()` го връща на оригиналния родител/позиция/ротация (пазени в `originalParent/originalLocalPosition/originalLocalRotation`).

**`TestInteractable.cs`** — най-простият interactable: само лог + завъртане на 45°. Чисто тестов обект (виж §10).

### 5.2 Компютър (`Scripts/Computer`)

Тук има **state machine за захранването** + **desktop с приложения**, разделени в 4 класа:

- **`ComputerPowerState.cs`** — `enum { Off, Booting, On, Sleeping }`.
- **`ComputerPowerController.cs`** — управлява самото състояние:
  - `EnterComputerMode()` се вика при отваряне и решава какво да покаже според текущото състояние (ако е Off → `StartBoot()`; ако Sleeping → `Wake()`; и т.н.).
  - `StartBoot()` пуска корутина `BootRoutine()` (по подразбиране 3 сек.), после state → `On` → `ComputerUIController.ShowDesktop()`.
  - `Sleep()` / `Restart()` / `ShutDown()` — викани от бутоните в Power Menu; `ShutDown()` пуска `ShutdownRoutine()` (1.5 сек.) и после **емитва `ComputerModeExitRequested`**, събитие, което кара `ComputerInteractable` да върне управлението на играча и да свали курсора.
- **`ComputerUIController.cs`** — чист "view" слой: показва/скрива панели (`ShowDesktop/ShowBootScreen/ShowOffScreen/ShowShutdownScreen`), управлява Power Menu (`TogglePowerMenu`), и има методи за всяко desktop приложение (`OpenMail/CloseMail`, `OpenCaseFiles/CloseCaseFiles`, `OpenDatabase/OpenPhotos/OpenForensics` за stub прозорците). `CloseAllDesktopApplications()` затваря всичко, преди да отвори ново приложение (single-window поведение).
- **`ComputerInteractable.cs`** — самият обект в 3D света, който играчът натиска E върху. При `Interact()`:
  1. вика `computerUI.OpenComputer()` (връща `false`, ако вече е отворен),
  2. спира `FirstPersonController`/`PlayerInteraction`, показва курсора,
  3. вика `powerController.EnterComputerMode()`.
  - Слуша `computerUI.ExitRequested` и `powerController.ComputerModeExitRequested`, за да затвори компютъра и върне управлението (напр. когато играчът натисне Shut Down).

**Email приложение:**
- **`EmailData.cs`** — чист data клас (`sender`, `subject`, `date`, `body`), сериализиран директно в Inspector-а на `EmailUIController` (списък `emails`).
- **`EmailUIController.cs`** — `BuildInbox()` инстанцира `EmailEntryUI` префаб за всеки имейл в списъка (само веднъж, пази `isInboxBuilt`), `SelectEmail()` пълни детайлния панел.
- **`EmailEntryUI.cs`** — по един ред в списъка; при клик вика callback-a, подаден при `Initialize(data, onSelected)`.

**Case Files (незавършено — виж §9.1):**
- **`CaseData.cs`** — data клас (`caseNumber`, `caseName`, `status`, `dateOpened`, `location`, `victim`, `leadDetective`, `summary`).
- **`CaseEntryUI.cs`** / **`CaseFilesUIController.cs`** — огледални на Email-логиката (списък + детайли + табове Overview/People/Records), напълно написани и коректни, **но никъде не се използват в сцената**.

### 5.3 Телефон (`Scripts/Phone`)

- **`PhoneUIController.cs`** — по-просто от компютъра: няма power state machine. `contactName`/`dialogue` са прости текстови полета в Inspector-а (в момента с placeholder текст, виж §10). `StartCall()`/`HangUp()`/`ShowContacts()` просто local UI превключване.
- **`PhoneInteractable.cs`** — идентичен модел на `ComputerInteractable`, но без power state.

### 5.4 Evidence Board (`Scripts/EvidenceBoard`)

- **`EvidenceBoardUIController.cs`** — интересно: **картите с доказателства не са отделен `.cs` файл** — данните им (`title`, `description`) са малък вложен `[Serializable] class EvidenceCardData`, дефиниран директно в масив по подразбиране (`evidenceCards`) с 3 фиксирани записа за код-примера (могат да се презапишат в Inspector). `ShowEvidenceDetails(int evidenceIndex)` се вика директно от бутоните на всяка карта, с индекс 0/1/2, подаден като UnityEvent аргумент.
- **`EvidenceBoardInteractable.cs`** — същият модел като Computer/Phone.

---

## 6. Input система

`DetectiveGameInput.inputactions` дефинира **само една ActionMap `Player`**, с 3 действия:

| Action | Тип | Binding-и |
|---|---|---|
| `Move` | Vector2 (2D Vector composite) | W/A/S/D |
| `Look` | Vector2 | `<Pointer>/delta` (мишка) |
| `Interact` | Button | клавиш **E** |

Няма геймпад/контролер binding-и, няма отделен UI control scheme — само клавиатура + мишка.

---

## 7. Логика на бутоните — „кое къде води"

Проверих директно `OnClick()` списъците на **всичките 20** `Button` компонента в сцената. Ето реалната карта (не какво пише в кода, а какво наистина е свързано в Editor-а):

| Бутон | При клик вика | Забележка |
|---|---|---|
| `MailIcon` | `EmailUIController.OpenMail()` **директно** | ⚠️ заобикаля `ComputerUIController` — виж §9.2 |
| `CaseFilesIcon` | *(нищо)* | ⚠️ виж §9.1 |
| `DatabaseIcon` | *(нищо)* | очакван stub, виж §9.3 |
| `PhotosIcon` | *(нищо)* | очакван stub, виж §9.3 |
| `StartButton` | `ComputerUIController.TogglePowerMenu()` | ок |
| `SleepButton` | `ComputerPowerController.Sleep()` | ок |
| `RestartButton` | `ComputerPowerController.Restart()` | ок |
| `ShutDownButton` | `ComputerPowerController.ShutDown()` | ок |
| `CloseMailButton` | `EmailUIController.CloseMail()` | ок |
| `ExitComputerButton` | `ComputerUIController.ExitComputer()` | ок |
| `CallButton` | `PhoneUIController.StartCall()` | ок |
| `HangUpButton` | `PhoneUIController.HangUp()` | ок |
| `ExitPhoneButton` | `PhoneUIController.ExitPhone()` | ок |
| `EvidenceCard1/2/3` | `EvidenceBoardUIController.ShowEvidenceDetails(0/1/2)` | ок, индексите са коректни |
| `BackButton` (Board) | `EvidenceBoardUIController.BackToBoard()` | ок |
| `ExitBoardButton` | `EvidenceBoardUIController.ExitBoard()` | ок |

**Поток при отваряне на компютъра:** играч гледа в „Computer" → E → `ComputerInteractable.Interact()` → `ComputerUIController.OpenComputer()` (показва `ComputerRoot`) → `ComputerPowerController.EnterComputerMode()` → ако е Off: 3 сек. boot екран → `On` → `ShowDesktop()` → играчът вижда 4 икони + taskbar.

**Поток при "Shut Down":** `ShutDownButton` → `ComputerPowerController.ShutDown()` → затваря всички desktop приложения, показва shutdown екран за 1.5 сек. → state `Off` → събитие `ComputerModeExitRequested` → `ComputerInteractable` затваря целия компютърен UI и връща управлението на играча.

---

## 8. Докъде сме стигнали (progress matrix)

| Система | Статус | Коментар |
|---|---|---|
| Движение + камера (FPS controller) | ✅ Готово | |
| Raycast взаимодействие (`IInteractable`) | ✅ Готово | |
| Разглеждане на документ | ✅ Готово | само 1 тестов документ в сцената |
| Компютър: захранване (boot/sleep/restart/shutdown) | ✅ Готово | |
| Компютър: Mail приложение | ✅ Работи | но заобикаля `ComputerUIController` (§9.2) |
| Компютър: Case Files приложение | 🟡 Код готов, **не е сложено в сцената** | виж §9.1 |
| Компютър: Database / Photos / Forensics | ⛔ Само икони, без прозорци и логика | нарочен/очакван stub |
| Телефон | ✅ Работи | текстовете са placeholder (§10) |
| Evidence Board (3 карти) | ✅ Готово | |
| Art / модели / текстури | ⛔ Няма — всичко е primitive геометрия | папките `Art/*` са празни |
| Звук | ⛔ Няма | папката `Audio` е празна |
| Git история на тази работа | ⚠️ Некомитната | виж §11 |

---

## 9. Открити проблеми/грешки

### 9.1 🔴 „Case Files" не работи — компонентът липсва в сцената

`CaseFilesUIController` **не е закачен на нито един GameObject** в `DetectiveOffice.unity` (за разлика от `ComputerUIController`/`PhoneUIController`/`EvidenceBoardUIController`, които всичките седят на `Canvas`). Освен това `CaseFilesIcon.OnClick()` е **празен списък** — бутонът буквално не вика нищо.

Кодът (`CaseFilesUIController.cs`, `CaseEntryUI.cs`, `CaseData.cs`, `CaseEntry.prefab`) е напълно написан и коректен — просто никой не го е "монтирал" в сцената.

**Как да се оправи:**
1. Добави компонент `CaseFilesUIController` (напр. на `Canvas`, до другите три).
2. Направи UI прозорец "Case Files" (по модел на `MailWindow`): списък + панел с детайли (Overview/People/Records табове).
3. Свържи полетата в Inspector-а: `caseFilesWindow`, `caseListContent`, `caseEntryPrefab` (= `CaseEntry.prefab`), текстовите полета, и попълни списъка `cases` с реални `CaseData` записи.
4. Свържи `CaseFilesIcon.OnClick()` към новия контролер (виж и 9.2 по-долу за правилния начин).

### 9.2 🟠 Полетата "Desktop Applications" на `ComputerUIController` не са зададени в сцената

Сцената е записана **преди** тези полета (`emailUIController`, `caseFilesUIController`, `databaseWindow`, `photosWindow`, `forensicsWindow`) да бъдат добавени в `ComputerUIController.cs` — в самия `.unity` файл ги няма изобщо (само `computerRoot/screenOffPanel/bootPanel/desktopPanel/powerMenuPanel`). Практически:

- `ComputerUIController.OpenMail()` / `CloseMail()` в момента са мъртъв код — винаги излизат рано, защото `emailUIController == null`.
- Точно затова `MailIcon` е свързан **директно** към `EmailUIController.OpenMail()`, заобикаляйки `ComputerUIController` изцяло. Това е причината имейлът въобще работи в момента — но означава, че `CloseAllDesktopApplications()` никога не се вика, когато отваряш пощата, т.е. **прозорците не се затварят автоматично един друг** по предвидения начин.

**Как да се оправи:** Отвори сцената в Editor-а → избери `Canvas` → в Inspector-а на `ComputerUIController` довлечи `DesktopPanel`-овия `EmailUIController` в полето `Email Ui Controller` (и бъдещия `CaseFilesUIController` в неговото поле). После смени `MailIcon.OnClick()` да вика `ComputerUIController.OpenMail()` вместо директно `EmailUIController.OpenMail()`, за да минава през единната логика за затваряне на другите прозорци.

### 9.3 🟡 Database / Photos / Forensics са само икони

`DatabaseIcon` и `PhotosIcon` имат празни `OnClick()`, и под `DesktopPanel` няма никакви `DatabaseWindow`/`PhotosWindow`/`ForensicsWindow` GameObject-и — само иконите съществуват. Методите в кода (`OpenDatabase/OpenPhotos/OpenForensics`) чакат GameObject референции, които никога не са направени. Това вероятно е нарочно (следваща стъпка в разработката), но си струва да се знае, за да не се мисли, че е счупено нещо съществуващо.

### 9.4 🟢 Дребни/козметични неща

- На `Canvas` (root) и на `EvidenceDetailsPanel` има по един **неизползван `Button` компонент** с празен `OnClick()` — вероятно случайно добавени по време на прототипиране. Безобидни (никой не ги вика), но е добре да се изтрият за чистота.
- `ProjectSettings/EditorBuildSettings.asset` пази стар (изтрит) ред `Assets/Scenes/SampleScene.unity` (изключен, но Unity ще показва "missing scene" предупреждение в Build Profiles прозореца). Може спокойно да се премахне от списъка.
- `PhoneUIController` полетата `Contact Name` / `Dialogue` в сцената в момента са placeholder текст (`"bau bau"` / `"jdofajifajifahifkk"`) — очевидно тестови стойности, не финален текст.
- `TestInteractable` (тестовият завъртащ се куб) все още стои като реален обект в "истинската" сцена — полезен за тест на interaction системата, но трябва да се маха/скрие преди истински playtest или билд.

### 9.5 Проверка на реалните Unity Editor логове (`Logs/Editor.log`)

Освен статичния прочит на кода, прегледах и `Logs/Editor.log` (~103 000 реда, реални Play Mode сесии от 25-27 септември) — това показва какво се е случило наистина при пускане на играта в Editor-а, не само какво "би трябвало" да работи.

**Компилация:** 0 грешки (`error CS`), 0 предупреждения (`warning CS`) в целия лог — кодът се компилира чисто.

**🔴 Намерен реален runtime срив (вече самопоправен):** По едно време играчът е бил напълно "замръзнал" — `PlayerInputReader` е хвърлял `NullReferenceException` **на всеки кадър** (11 690 пъти в лога, ~5800+ поредни кадъра), защото `input` полето е останало `null` след live recompile по време на Play Mode (домейн reload изчиства несериализирани полета, а старата версия на `OnEnable()`/`ReadLook()`/`ReadMove()`/`ReadInteract()` не е проверявала за това). Практически: гледане, движение и interact не са работили изобщо през цялата тази сесия.
  - **Добра новина:** текущият `PlayerInputReader.cs` вече съдържа точно правилната защита — `CreateInput()` се вика в `OnEnable()`, и `EnsureInputReady()` се вика в началото на `ReadMove()`/`ReadLook()`/`ReadInteract()`. Номерата на редовете в старите грешки (16, 39, 44) не съвпадат с текущия файл — потвърждава, че кодът вече е бил коригиран след този срив. Не би трябвало да се повтори.

**⚠️ Продължаващ (но безобиден) шум в Editor конзолата:** При всяко влизане/излизане от Play Mode се появява `IndexOutOfRangeException` от `UnityEngine.UI.Selectable.OnEnable/OnDisable` (виден е и в самия текущ край на лога, т.е. все още се случва). Причината е в `ProjectSettings/EditorSettings.asset`: `Enter Play Mode Options` е включено с **и двете** отметки "Reload Domain" и "Reload Scene" изключени (за по-бързо влизане в Play Mode). Това е познат "quirk" на UGUI пакета точно с тази комбинация настройки — Unity буквално го споменава сам в лога ("If you experience any issues, please disable 'Enter Play Mode Options'"). Не засяга реален build, само Editor конзолата. Ако те дразни — Edit → Project Settings → Editor → изключи "Enter Play Mode Options" (ще плащаш с по-бавно влизане в Play Mode).

**Две еднократни "липсват references" грешки в лога** — и двете вече неактуални:
- `PhoneInteractable няма зададени нужните references` — засечено точно в момента на добавяне на компонента през Add Component менюто (напълно очаквано, полетата тепърва се попълват).
- `ComputerUIController няма зададени нужните references` — засечено при по-ранна Play сесия, преди петте Panel полета да бъдат довързани. И двете вече са коригирани — текущата сцена ги има изрядно попълнени (виж §7 в таблицата с проверени неща).

### ✅ Проверено, че НЕ е грешка (за да не си губиш времето по това)

- Всички референции на `FirstPersonController`/`PlayerInteraction`/`ComputerInteractable`/`PhoneInteractable`/`EvidenceBoardInteractable`/`DocumentInteractable` в сцената са коректно свързани (проверих всяко field по fileID).
- Индексите на трите Evidence карти (0/1/2) са различни и правилни.
- Двата "непознати" GUID-а, които на пръв поглед изглеждаха като "липсващ скрипт", всъщност са вградени Unity компоненти (`UnityEngine.UI.LayoutElement` и `UnityEngine.UI.HorizontalLayoutGroup`) — няма счупени/липсващи скриптове в проекта.
- `CaseEntryUI`/`EmailEntryUI` префабите имат коректно свързани текстови полета (полето `button` е празно в `CaseEntry.prefab`, но кодът има fallback `GetComponent<Button>()` в `Awake()`, така че това не чупи нищо).

---

## 10. Placeholder / недовършено съдържание

- **Геометрия:** стените, подът и мебелите са primitive кубове (`BoxCollider + MeshRenderer + MeshFilter`) — папките `Art/Models`, `Art/Materials`, `Art/Textures` са празни.
- **Звук:** папка `Audio` е празна, никъде няма `AudioSource` освен `AudioListener`-а на камерата.
- **Текст:** телефонният диалог и част от demo имейлите изглеждат като placeholder/тестови данни.
- `TestInteractable` е тестов обект, не част от финалната игра.

---

## 11. Git / версии — статус

- Проектът има само **един commit** ("Initial check-in") — стандартният Unity темплейт.
- **Цялата реална игра (`Assets/_Game/*` — всички скриптове, сцената, input actions, префаби) все още е untracked в git** (виж `git status` в началото на сесията). Изтритите demo файлове и промените в `ProjectSettings`/`Settings` също не са комитнати.
- Практически: ако нещо се случи с локалната папка в момента, ще загубиш цялата написана логика — струва си да направиш commit скоро.

---

## 12. Отворени въпроси към теб

1. **Case Files** (§9.1) — искаш ли аз да построя UI прозореца и да свържа контролера, или предпочиташ да го направиш сам в Editor-а (моят анализ ти дава точно кои полета трябва да се запълнят)?
2. **Mail routing** (§9.2) — да пренасоча ли `MailIcon` през `ComputerUIController.OpenMail()` (за консистентно затваряне на прозорци), или има причина да е директно към `EmailUIController`?
3. **Database / Photos / Forensics** — приоритет ли са в момента, или засега само плейсхолдър икони, докато не стигнеш до тях?
4. Искаш ли git commit на цялата текуща работа (`Assets/_Game`, промените в `ProjectSettings`, новите TextMesh Pro/Build Profiles папки), за да е защитена?

---

## 13. Препоръчани следващи стъпки (по приоритет)

1. Направи git commit на текущото състояние (виж §11) — просто за сигурност.
2. Довърши свързването на `ComputerUIController` → `EmailUIController` (§9.2), за да минава Mail през единния "затвори другите прозорци" механизъм.
3. Довърши Case Files (§9.1) — кодът е готов, липсва само UI + свързване в сцената.
4. Изчисти дребните неща от §9.4 (излишни Button компоненти, стария scene ред в Build Settings, placeholder текстове в телефона).
5. Реши какво да е следващо: Database/Photos/Forensics приложения, финално арт съдържание, звук, или повече от една стая/сцена.
