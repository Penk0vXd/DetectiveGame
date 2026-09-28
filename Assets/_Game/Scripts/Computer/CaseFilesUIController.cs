using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CaseFilesUIController : MonoBehaviour
{
    [Header("Case Files Window")]
    [SerializeField] private GameObject caseFilesWindow = null;
    [SerializeField] private Transform caseListContent = null;
    [SerializeField] private CaseEntryUI caseEntryPrefab = null;

    [Header("Case Header")]
    [SerializeField] private TMP_Text caseNumberText = null;
    [SerializeField] private TMP_Text caseNameText = null;
    [SerializeField] private TMP_Text statusText = null;

    [Header("Case Overview")]
    [SerializeField] private TMP_Text dateText = null;
    [SerializeField] private TMP_Text locationText = null;
    [SerializeField] private TMP_Text victimText = null;
    [SerializeField] private TMP_Text leadDetectiveText = null;
    [SerializeField] private TMP_Text summaryText = null;

    [Header("Case Tabs")]
    [SerializeField] private GameObject overviewPanel = null;
    [SerializeField] private GameObject peoplePanel = null;
    [SerializeField] private GameObject recordsPanel = null;

    [Header("People Tab")]
    [SerializeField] private Transform peopleListContent = null;
    [SerializeField] private CasePersonEntryUI personEntryPrefab = null;
    [SerializeField] private TMP_Text personNameText = null;
    [SerializeField] private TMP_Text personRoleText = null;
    [SerializeField] private TMP_Text personDescriptionText = null;

    [Header("Records Tab")]
    [SerializeField] private Transform recordsListContent = null;
    [SerializeField] private CaseRecordEntryUI recordEntryPrefab = null;
    [SerializeField] private TMP_Text recordTitleText = null;
    [SerializeField] private TMP_Text recordTypeText = null;
    [SerializeField] private TMP_Text recordDateText = null;
    [SerializeField] private TMP_Text recordBodyText = null;

    [Header("Case Data")]
    [SerializeField] private List<CaseData> cases = new List<CaseData>();

    private bool isCaseListBuilt;
    private bool hasLoggedMissingWindow;
    private bool hasLoggedMissingListReferences;
    private bool hasLoggedMissingPeopleReferences;
    private bool hasLoggedMissingRecordsReferences;
    private CaseData selectedCase;
    private readonly List<CasePersonEntryUI> generatedPersonEntries = new List<CasePersonEntryUI>();
    private readonly List<CaseRecordEntryUI> generatedRecordEntries = new List<CaseRecordEntryUI>();

    public void OpenCaseFiles()
    {
        if (caseFilesWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // отваря case files
        caseFilesWindow.SetActive(true);
        BuildCaseList();
        ShowOverview();

        if (selectedCase == null)
        {
            ClearCaseDetails();
        }
        else
        {
            SelectCase(selectedCase);
        }
    }

    public void CloseCaseFiles()
    {
        if (caseFilesWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // затваря case files
        caseFilesWindow.SetActive(false);
    }

    public void BuildCaseList()
    {
        if (isCaseListBuilt)
        {
            return;
        }

        if (caseListContent == null || caseEntryPrefab == null)
        {
            LogMissingListReferencesOnce();
            return;
        }

        if (cases == null)
        {
            isCaseListBuilt = true;
            return;
        }

        // създава списъка
        foreach (CaseData caseData in cases)
        {
            if (caseData == null)
            {
                continue;
            }

            CaseEntryUI entry = Instantiate(caseEntryPrefab, caseListContent);
            entry.Initialize(caseData, this);
        }

        isCaseListBuilt = true;
    }

    public void SelectCase(CaseData caseData)
    {
        selectedCase = caseData;

        if (caseData == null)
        {
            ClearCaseDetails();
            return;
        }

        // показва случая
        SetText(caseNumberText, caseData.CaseNumber);
        SetText(caseNameText, caseData.CaseName);
        SetText(statusText, caseData.Status);
        SetText(dateText, caseData.DateOpened);
        SetText(locationText, caseData.Location);
        SetText(victimText, caseData.Victim);
        SetText(leadDetectiveText, caseData.LeadDetective);
        SetText(summaryText, caseData.Summary);
        RebuildPeopleList();
        RebuildRecordsList();
    }

    public void ShowOverview()
    {
        // показва overview
        SetActive(overviewPanel, true);
        SetActive(peoplePanel, false);
        SetActive(recordsPanel, false);
    }

    public void ShowPeople()
    {
        // показва хората
        SetActive(overviewPanel, false);
        SetActive(peoplePanel, true);
        SetActive(recordsPanel, false);
    }

    public void ShowRecords()
    {
        // показва записите
        SetActive(overviewPanel, false);
        SetActive(peoplePanel, false);
        SetActive(recordsPanel, true);
    }

    public void SelectPerson(CasePersonData person)
    {
        if (person == null)
        {
            ClearPersonDetails();
            return;
        }

        // показва човека
        SetText(personNameText, person.Name);
        SetText(personRoleText, person.Role);
        SetText(personDescriptionText, person.ShortDescription);
    }

    public void SelectRecord(CaseRecordData record)
    {
        if (record == null)
        {
            ClearRecordDetails();
            return;
        }

        // показва записа
        SetText(recordTitleText, record.Title);
        SetText(recordTypeText, record.RecordType);
        SetText(recordDateText, record.Date);
        SetText(recordBodyText, record.Body);
    }

    private void ClearCaseDetails()
    {
        SetText(caseNumberText, string.Empty);
        SetText(caseNameText, string.Empty);
        SetText(statusText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(locationText, string.Empty);
        SetText(victimText, string.Empty);
        SetText(leadDetectiveText, string.Empty);
        SetText(summaryText, string.Empty);
        ClearGeneratedEntries(generatedPersonEntries);
        ClearGeneratedEntries(generatedRecordEntries);
        ClearPersonDetails();
        ClearRecordDetails();
    }

    private void RebuildPeopleList()
    {
        ClearGeneratedEntries(generatedPersonEntries);
        ClearPersonDetails();

        if (selectedCase == null || selectedCase.People.Count == 0)
        {
            return;
        }

        if (peopleListContent == null || personEntryPrefab == null)
        {
            LogMissingPeopleReferencesOnce();
            return;
        }

        foreach (CasePersonData person in selectedCase.People)
        {
            if (person == null)
            {
                continue;
            }

            CasePersonEntryUI entry = Instantiate(personEntryPrefab, peopleListContent);
            entry.Initialize(person, SelectPerson);
            generatedPersonEntries.Add(entry);
        }
    }

    private void RebuildRecordsList()
    {
        ClearGeneratedEntries(generatedRecordEntries);
        ClearRecordDetails();

        if (selectedCase == null || selectedCase.Records.Count == 0)
        {
            return;
        }

        if (recordsListContent == null || recordEntryPrefab == null)
        {
            LogMissingRecordsReferencesOnce();
            return;
        }

        foreach (CaseRecordData record in selectedCase.Records)
        {
            if (record == null)
            {
                continue;
            }

            CaseRecordEntryUI entry = Instantiate(recordEntryPrefab, recordsListContent);
            entry.Initialize(record, SelectRecord);
            generatedRecordEntries.Add(entry);
        }
    }

    private void ClearPersonDetails()
    {
        SetText(personNameText, string.Empty);
        SetText(personRoleText, string.Empty);
        SetText(personDescriptionText, string.Empty);
    }

    private void ClearRecordDetails()
    {
        SetText(recordTitleText, string.Empty);
        SetText(recordTypeText, string.Empty);
        SetText(recordDateText, string.Empty);
        SetText(recordBodyText, string.Empty);
    }

    private static void ClearGeneratedEntries<T>(List<T> entries) where T : Component
    {
        foreach (T entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            entry.gameObject.SetActive(false);
            Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    private void LogMissingWindowOnce()
    {
        if (hasLoggedMissingWindow)
        {
            return;
        }

        hasLoggedMissingWindow = true;
        Debug.LogWarning("CaseFilesUIController няма зададен CaseFilesWindow", this);
    }

    private void LogMissingListReferencesOnce()
    {
        if (hasLoggedMissingListReferences)
        {
            return;
        }

        hasLoggedMissingListReferences = true;
        Debug.LogWarning("CaseFilesUIController няма зададени CaseListContent или CaseEntry prefab", this);
    }

    private void LogMissingPeopleReferencesOnce()
    {
        if (hasLoggedMissingPeopleReferences)
        {
            return;
        }

        hasLoggedMissingPeopleReferences = true;
        Debug.LogWarning("CaseFilesUIController няма зададени People list references", this);
    }

    private void LogMissingRecordsReferencesOnce()
    {
        if (hasLoggedMissingRecordsReferences)
        {
            return;
        }

        hasLoggedMissingRecordsReferences = true;
        Debug.LogWarning("CaseFilesUIController няма зададени Records list references", this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
