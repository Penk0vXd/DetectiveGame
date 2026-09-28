using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CaseData
{
    [SerializeField] private string caseNumber = string.Empty;
    [SerializeField] private string caseName = string.Empty;
    [SerializeField] private string status = string.Empty;
    [SerializeField] private string dateOpened = string.Empty;
    [SerializeField] private string location = string.Empty;
    [SerializeField] private string victim = string.Empty;
    [SerializeField] private string leadDetective = string.Empty;
    [SerializeField, TextArea(4, 10)] private string summary = string.Empty;
    [SerializeField] private List<CasePersonData> people = new List<CasePersonData>();
    [SerializeField] private List<CaseRecordData> records = new List<CaseRecordData>();

    public string CaseNumber => caseNumber ?? string.Empty;
    public string CaseName => caseName ?? string.Empty;
    public string Status => status ?? string.Empty;
    public string DateOpened => dateOpened ?? string.Empty;
    public string Location => location ?? string.Empty;
    public string Victim => victim ?? string.Empty;
    public string LeadDetective => leadDetective ?? string.Empty;
    public string Summary => summary ?? string.Empty;
    public IReadOnlyList<CasePersonData> People => people ?? (IReadOnlyList<CasePersonData>)Array.Empty<CasePersonData>();
    public IReadOnlyList<CaseRecordData> Records => records ?? (IReadOnlyList<CaseRecordData>)Array.Empty<CaseRecordData>();
}
