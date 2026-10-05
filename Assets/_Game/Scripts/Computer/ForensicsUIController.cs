using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ForensicsUIController : MonoBehaviour
{
    [Header("Forensics Window")]
    [SerializeField] private GameObject forensicsWindow = null;
    [SerializeField] private Transform reportsContent = null;
    [SerializeField] private ForensicsReportEntryUI reportEntryPrefab = null;

    [Header("Report Details")]
    [SerializeField] private TMP_Text reportNumberText = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text statusText = null;
    [SerializeField] private TMP_Text dateText = null;
    [SerializeField] private TMP_Text relatedCaseText = null;
    [SerializeField] private TMP_Text summaryText = null;
    [SerializeField] private TMP_Text fullReportText = null;

    [Header("Forensics Reports")]
    [SerializeField] private List<ForensicsReportData> reports = new List<ForensicsReportData>();

    private bool isReportListBuilt;
    private bool hasLoggedMissingWindow;
    private bool hasLoggedMissingListReferences;

    private void Awake()
    {
        ClearReportDetails();
    }

    public void OpenForensics()
    {
        if (forensicsWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // отваря приложението
        forensicsWindow.SetActive(true);
        BuildReportList();
    }

    public void CloseForensics()
    {
        if (forensicsWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // затваря приложението
        forensicsWindow.SetActive(false);
    }

    public void BuildReportList()
    {
        if (isReportListBuilt)
        {
            return;
        }

        if (reportsContent == null || reportEntryPrefab == null)
        {
            LogMissingListReferencesOnce();
            return;
        }

        if (reports != null)
        {
            foreach (ForensicsReportData report in reports)
            {
                if (report == null)
                {
                    continue;
                }

                ForensicsReportEntryUI entry = Instantiate(reportEntryPrefab, reportsContent);
                entry.Initialize(report, SelectReport);
            }
        }

        isReportListBuilt = true;
    }

    public void SelectReport(ForensicsReportData report)
    {
        if (report == null)
        {
            ClearReportDetails();
            return;
        }

        // показва доклада
        SetText(reportNumberText, report.ReportNumber);
        SetText(titleText, report.Title);
        SetText(statusText, report.Status);
        SetText(dateText, report.Date);
        SetText(relatedCaseText, report.RelatedCase);
        SetText(summaryText, report.Summary);
        SetText(fullReportText, report.FullReport);
    }

    private void ClearReportDetails()
    {
        SetText(reportNumberText, string.Empty);
        SetText(titleText, string.Empty);
        SetText(statusText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(relatedCaseText, string.Empty);
        SetText(summaryText, string.Empty);
        SetText(fullReportText, string.Empty);
    }

    private void LogMissingWindowOnce()
    {
        if (hasLoggedMissingWindow)
        {
            return;
        }

        hasLoggedMissingWindow = true;
        Debug.LogWarning("ForensicsUIController няма зададен ForensicsWindow", this);
    }

    private void LogMissingListReferencesOnce()
    {
        if (hasLoggedMissingListReferences)
        {
            return;
        }

        hasLoggedMissingListReferences = true;
        Debug.LogWarning("ForensicsUIController няма зададени report list references", this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
