using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PoliceDatabaseUIController : MonoBehaviour
{
    [Header("Database Window")]
    [SerializeField] private GameObject databaseWindow = null;
    [SerializeField] private TMP_InputField searchInput = null;
    [SerializeField] private Transform resultsContent = null;
    [SerializeField] private DatabaseResultEntryUI resultEntryPrefab = null;
    [SerializeField] private TMP_Text resultsStatusText = null;

    [Header("Record Details")]
    [SerializeField] private TMP_Text fullNameText = null;
    [SerializeField] private TMP_Text dateOfBirthText = null;
    [SerializeField] private TMP_Text addressText = null;
    [SerializeField] private TMP_Text occupationText = null;
    [SerializeField] private TMP_Text recordSummaryText = null;

    [Header("Database Records")]
    [SerializeField] private List<DatabasePersonData> records = new List<DatabasePersonData>();

    private readonly List<DatabaseResultEntryUI> generatedResults = new List<DatabaseResultEntryUI>();
    private bool hasLoggedMissingWindow;
    private bool hasLoggedMissingSearchReferences;

    private void Awake()
    {
        ClearSearch();
    }

    public void OpenDatabase()
    {
        if (databaseWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // отваря приложението
        databaseWindow.SetActive(true);
    }

    public void CloseDatabase()
    {
        if (databaseWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // затваря приложението
        databaseWindow.SetActive(false);
    }

    public void Search()
    {
        ClearGeneratedResults();
        ClearDetails();

        if (searchInput == null || resultsContent == null || resultEntryPrefab == null)
        {
            LogMissingSearchReferencesOnce();
            return;
        }

        string query = searchInput.text == null ? string.Empty : searchInput.text.Trim();

        if (query.Length == 0)
        {
            SetText(resultsStatusText, "Enter a full name");
            return;
        }

        int resultCount = 0;

        if (records != null)
        {
            foreach (DatabasePersonData record in records)
            {
                if (record == null
                    || record.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                DatabaseResultEntryUI entry = Instantiate(resultEntryPrefab, resultsContent);
                entry.Initialize(record, SelectRecord);
                generatedResults.Add(entry);
                resultCount++;
            }
        }

        SetText(resultsStatusText, resultCount == 0 ? "No records found" : $"Records found: {resultCount}");
    }

    public void ClearSearch()
    {
        if (searchInput != null)
        {
            searchInput.SetTextWithoutNotify(string.Empty);
        }

        // изчиства резултатите
        ClearGeneratedResults();
        ClearDetails();
        SetText(resultsStatusText, string.Empty);
    }

    public void SelectRecord(DatabasePersonData record)
    {
        if (record == null)
        {
            ClearDetails();
            return;
        }

        // показва записа
        SetText(fullNameText, record.FullName);
        SetText(dateOfBirthText, record.DateOfBirth);
        SetText(addressText, record.Address);
        SetText(occupationText, record.Occupation);
        SetText(recordSummaryText, record.RecordSummary);
    }

    private void ClearGeneratedResults()
    {
        foreach (DatabaseResultEntryUI entry in generatedResults)
        {
            if (entry == null)
            {
                continue;
            }

            entry.gameObject.SetActive(false);
            Destroy(entry.gameObject);
        }

        generatedResults.Clear();
    }

    private void ClearDetails()
    {
        SetText(fullNameText, string.Empty);
        SetText(dateOfBirthText, string.Empty);
        SetText(addressText, string.Empty);
        SetText(occupationText, string.Empty);
        SetText(recordSummaryText, string.Empty);
    }

    private void LogMissingWindowOnce()
    {
        if (hasLoggedMissingWindow)
        {
            return;
        }

        hasLoggedMissingWindow = true;
        Debug.LogWarning("PoliceDatabaseUIController няма зададен DatabaseWindow", this);
    }

    private void LogMissingSearchReferencesOnce()
    {
        if (hasLoggedMissingSearchReferences)
        {
            return;
        }

        hasLoggedMissingSearchReferences = true;
        Debug.LogWarning("PoliceDatabaseUIController няма зададени search list references", this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
