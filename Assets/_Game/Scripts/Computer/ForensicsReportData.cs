using System;
using UnityEngine;

[Serializable]
public sealed class ForensicsReportData
{
    [SerializeField] private string reportNumber = string.Empty;
    [SerializeField] private string title = string.Empty;
    [SerializeField] private string status = string.Empty;
    [SerializeField] private string date = string.Empty;
    [SerializeField] private string relatedCase = string.Empty;
    [SerializeField, TextArea(3, 8)] private string summary = string.Empty;
    [SerializeField, TextArea(8, 20)] private string fullReport = string.Empty;

    public string ReportNumber => reportNumber ?? string.Empty;
    public string Title => title ?? string.Empty;
    public string Status => status ?? string.Empty;
    public string Date => date ?? string.Empty;
    public string RelatedCase => relatedCase ?? string.Empty;
    public string Summary => summary ?? string.Empty;
    public string FullReport => fullReport ?? string.Empty;
}
