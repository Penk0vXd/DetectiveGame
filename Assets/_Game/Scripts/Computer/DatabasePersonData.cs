using System;
using UnityEngine;

[Serializable]
public sealed class DatabasePersonData
{
    [SerializeField] private string fullName = string.Empty;
    [SerializeField] private string dateOfBirth = string.Empty;
    [SerializeField] private string address = string.Empty;
    [SerializeField] private string occupation = string.Empty;
    [SerializeField, TextArea(4, 10)] private string recordSummary = string.Empty;

    public string FullName => fullName ?? string.Empty;
    public string DateOfBirth => dateOfBirth ?? string.Empty;
    public string Address => address ?? string.Empty;
    public string Occupation => occupation ?? string.Empty;
    public string RecordSummary => recordSummary ?? string.Empty;
}
