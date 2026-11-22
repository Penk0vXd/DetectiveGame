using System;
using System.Collections.Generic;

// Сериализируем DTO за ръчно прикачен източник; пази само стабилни ID-та и позиция без дублиране на оригинала.
[Serializable]
public sealed class AttachedSourceSaveDto
{
    public string attachmentId = string.Empty;
    public string sourceId = string.Empty;
    public int sourceType;
    public float normalizedX;
    public float normalizedY;
    public int order;
}

// Преобразуване и валидация между AttachedSourcesSession и Save DTO.
// Невалидни или дублирани записи се пропускат безопасно без да нарушават валидните данни.
public static class AttachedSourcesSaveMapper
{
    public static AttachedSourceSaveDto[] ToSaveDtos(AttachedSourcesSession session)
    {
        if (session == null || session.Sources.Count == 0)
            return Array.Empty<AttachedSourceSaveDto>();

        var result = new AttachedSourceSaveDto[session.Sources.Count];
        for (int i = 0; i < session.Sources.Count; i++)
        {
            AttachedSource item = session.Sources[i];
            result[i] = new AttachedSourceSaveDto
            {
                attachmentId = item.AttachmentId,
                sourceId = item.SourceId,
                sourceType = (int)item.SourceType,
                normalizedX = item.NormalizedX,
                normalizedY = item.NormalizedY,
                order = i
            };
        }

        return result;
    }

    public static List<AttachedSource> PrepareRestore(AttachedSourceSaveDto[] dtos, out int skippedCount)
    {
        skippedCount = 0;
        var restored = new List<AttachedSource>();
        if (dtos == null || dtos.Length == 0)
            return restored;

        var indexed = new List<(AttachedSourceSaveDto Dto, int Index)>(dtos.Length);
        for (int i = 0; i < dtos.Length; i++)
            indexed.Add((dtos[i], i));

        indexed.Sort((a, b) =>
        {
            int orderA = a.Dto != null ? a.Dto.order : int.MaxValue;
            int orderB = b.Dto != null ? b.Dto.order : int.MaxValue;
            int cmp = orderA.CompareTo(orderB);
            return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
        });

        var seenAttachmentIds = new HashSet<string>(StringComparer.Ordinal);
        var seenSourceKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < indexed.Count; i++)
        {
            AttachedSourceSaveDto dto = indexed[i].Dto;
            if (!IsValidDto(dto))
            {
                skippedCount++;
                continue;
            }

            string sourceKey = dto.sourceType + ":" + dto.sourceId;
            if (!seenAttachmentIds.Add(dto.attachmentId) || !seenSourceKeys.Add(sourceKey))
            {
                skippedCount++;
                continue;
            }

            restored.Add(AttachedSource.Restore(
                dto.attachmentId,
                dto.sourceId,
                (AttachedSourceType)dto.sourceType,
                dto.normalizedX,
                dto.normalizedY));
        }

        return restored;
    }

    private static bool IsValidDto(AttachedSourceSaveDto dto)
    {
        return dto != null
            && !string.IsNullOrWhiteSpace(dto.attachmentId)
            && !string.IsNullOrWhiteSpace(dto.sourceId)
            && AttachedSource.IsValidSourceType((AttachedSourceType)dto.sourceType)
            && IsValidCoordinate(dto.normalizedX)
            && IsValidCoordinate(dto.normalizedY);
    }

    private static bool IsValidCoordinate(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }
}
