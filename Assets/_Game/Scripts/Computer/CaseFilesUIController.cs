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

    [Header("Case Data")]
    [SerializeField] private List<CaseData> cases = new List<CaseData>();

    private bool isCaseListBuilt;
    private bool hasLoggedMissingWindow;
    private bool hasLoggedMissingListReferences;
    private CaseData selectedCase;

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
