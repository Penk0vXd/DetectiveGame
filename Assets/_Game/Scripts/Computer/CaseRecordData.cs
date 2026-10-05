using System;
using UnityEngine;

[Serializable]
public sealed class CaseRecordData
{
    [SerializeField] private string title = string.Empty;
    [SerializeField] private string recordType = string.Empty;
    [SerializeField] private string date = string.Empty;
    [SerializeField, TextArea(4, 12)] private string body = string.Empty;

    public string Title => title ?? string.Empty;
    public string RecordType => recordType ?? string.Empty;
    public string Date => date ?? string.Empty;
    public string Body => body ?? string.Empty;
}
