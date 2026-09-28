using System;
using UnityEngine;

[Serializable]
public sealed class EmailData
{
    [SerializeField] private string sender = string.Empty;
    [SerializeField] private string subject = string.Empty;
    [SerializeField] private string date = string.Empty;
    [SerializeField, TextArea(4, 10)] private string body = string.Empty;

    public string Sender => sender ?? string.Empty;
    public string Subject => subject ?? string.Empty;
    public string Date => date ?? string.Empty;
    public string Body => body ?? string.Empty;
}
