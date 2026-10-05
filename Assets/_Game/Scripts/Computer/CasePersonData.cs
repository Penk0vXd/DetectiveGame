using System;
using UnityEngine;

[Serializable]
public sealed class CasePersonData
{
    [SerializeField] private string name = string.Empty;
    [SerializeField] private string role = string.Empty;
    [SerializeField, TextArea(2, 6)] private string shortDescription = string.Empty;

    public string Name => name ?? string.Empty;
    public string Role => role ?? string.Empty;
    public string ShortDescription => shortDescription ?? string.Empty;
}
