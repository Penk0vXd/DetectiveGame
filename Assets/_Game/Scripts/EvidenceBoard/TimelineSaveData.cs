using System;
using System.Collections.Generic;

// Сериализируем DTO за ръчно събитие от времевата линия (EB-006).
[Serializable]
public sealed class TimelineEventSaveDto
{
    public string id = string.Empty;
    public string date = string.Empty;
    public string time = string.Empty;
    public string title = string.Empty;
    public string description = string.Empty;
    public int insertionOrder;
}

// Преобразуване и валидация между TimelineSession и Save DTO.
// Невалидни или дублирани записи се пропускат безопасно без да нарушават валидните данни.
public static class TimelineSaveMapper
{
    public static TimelineEventSaveDto[] ToSaveDtos(TimelineSession session)
    {
        if (session == null || session.Events.Count == 0)
            return Array.Empty<TimelineEventSaveDto>();

        var result = new TimelineEventSaveDto[session.Events.Count];
        for (int i = 0; i < session.Events.Count; i++)
        {
            TimelineEvent item = session.Events[i];
            result[i] = new TimelineEventSaveDto
            {
                id = item.Id,
                date = item.Date,
                time = item.Time,
                title = item.Title,
                description = item.Description,
                insertionOrder = item.InsertionOrder
            };
        }

        return result;
    }

    public static List<TimelineEvent> PrepareRestore(TimelineEventSaveDto[] dtos, out int skippedCount)
    {
        skippedCount = 0;
        var restored = new List<TimelineEvent>();
        if (dtos == null || dtos.Length == 0)
            return restored;

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < dtos.Length; i++)
        {
            TimelineEventSaveDto dto = dtos[i];
            if (dto == null || string.IsNullOrWhiteSpace(dto.id))
            {
                skippedCount++;
                continue;
            }

            string trimmedId = dto.id.Trim();
            if (!TimelineEvent.TryValidateFields(dto.date, dto.time, dto.title,
                    out string normalizedDate, out _,
                    out string normalizedTime, out _,
                    out string normalizedTitle, out _))
            {
                skippedCount++;
                continue;
            }

            if (!seenIds.Add(trimmedId))
            {
                skippedCount++;
                continue;
            }

            int safeOrder = dto.insertionOrder >= 0 ? dto.insertionOrder : i;
            restored.Add(TimelineEvent.Restore(
                trimmedId,
                normalizedDate,
                normalizedTime,
                normalizedTitle,
                dto.description ?? string.Empty,
                safeOrder));
        }

        restored.Sort(TimelineEvent.CompareChronological);
        return restored;
    }
}
