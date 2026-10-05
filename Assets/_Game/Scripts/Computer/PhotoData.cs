using System;
using UnityEngine;

[Serializable]
public sealed class PhotoData
{
    [SerializeField] private string title = string.Empty;
    [SerializeField] private string date = string.Empty;
    [SerializeField] private string location = string.Empty;
    [SerializeField, TextArea(3, 8)] private string description = string.Empty;
    [SerializeField] private Sprite image = null;

    public string Title => title ?? string.Empty;
    public string Date => date ?? string.Empty;
    public string Location => location ?? string.Empty;
    public string Description => description ?? string.Empty;
    public Sprite Image => image;
}
