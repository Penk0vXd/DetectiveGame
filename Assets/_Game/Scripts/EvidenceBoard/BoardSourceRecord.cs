using UnityEngine;

// Read-only проекция на оригинален запис от Computer (Документ или Снимка) за визуализация на Evidence Board.
public sealed class BoardSourceRecord
{
    public string SourceId { get; }
    public AttachedSourceType SourceType { get; }
    public string Title { get; }
    public string SourceLabel { get; }
    public string SecondaryMeta { get; }
    public string Date { get; }
    public string Body { get; }
    public Sprite Image { get; }
    public bool IsMissing { get; }

    public BoardSourceRecord(string sourceId, AttachedSourceType sourceType, string title,
        string sourceLabel, string secondaryMeta, string date, string body, Sprite image, bool isMissing = false)
    {
        SourceId = sourceId ?? string.Empty;
        SourceType = sourceType;
        Title = title ?? string.Empty;
        SourceLabel = sourceLabel ?? string.Empty;
        SecondaryMeta = secondaryMeta ?? string.Empty;
        Date = date ?? string.Empty;
        Body = body ?? string.Empty;
        Image = image;
        IsMissing = isMissing;
    }

    public static BoardSourceRecord CreateMissing(string sourceId, AttachedSourceType sourceType)
    {
        string typeLabel = sourceType == AttachedSourceType.Photo ? "Снимка" : "Документ";
        return new BoardSourceRecord(
            sourceId,
            sourceType,
            "Източникът не е наличен",
            typeLabel,
            string.Empty,
            string.Empty,
            "Оригиналният запис не беше намерен в компютърната система.",
            null,
            isMissing: true);
    }
}
