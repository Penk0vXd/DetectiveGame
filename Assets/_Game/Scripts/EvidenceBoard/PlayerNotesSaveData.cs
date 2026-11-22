using System;
using System.Collections.Generic;

// Сериализируем DTO за една хартиена бележка, напълно отделен от Unity UI обектите.
[Serializable]
public sealed class PlayerNoteSaveDto
{
    public string id = string.Empty;
    public string text = string.Empty;
    public float normalizedX;
    public float normalizedY;
    public int order;
}

// Преобразуване и валидация между PlayerNotesSession и Save DTO.
// Политика при зареждане: невалидни отделни записи (празен/дублиран ID или невалидни координати)
// се пропускат безопасно без да провалят валидните бележки и без да мутират текущия session state предварително.
public static class PlayerNotesSaveMapper
{
    public static PlayerNoteSaveDto[] ToSaveDtos(PlayerNotesSession session)
    {
        if (session == null || session.Notes.Count == 0)
            return Array.Empty<PlayerNoteSaveDto>();

        var result = new PlayerNoteSaveDto[session.Notes.Count];
        for (int i = 0; i < session.Notes.Count; i++)
        {
            PlayerNote note = session.Notes[i];
            result[i] = new PlayerNoteSaveDto
            {
                id = note.Id,
                text = note.Text,
                normalizedX = note.NormalizedX,
                normalizedY = note.NormalizedY,
                order = i
            };
        }

        return result;
    }

    public static List<PlayerNote> PrepareRestore(PlayerNoteSaveDto[] dtos, out int skippedCount)
    {
        skippedCount = 0;
        var restored = new List<PlayerNote>();
        if (dtos == null || dtos.Length == 0)
            return restored;

        var indexed = new List<(PlayerNoteSaveDto Dto, int Index)>(dtos.Length);
        for (int i = 0; i < dtos.Length; i++)
            indexed.Add((dtos[i], i));

        indexed.Sort((a, b) =>
        {
            int orderA = a.Dto != null ? a.Dto.order : int.MaxValue;
            int orderB = b.Dto != null ? b.Dto.order : int.MaxValue;
            int cmp = orderA.CompareTo(orderB);
            return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
        });

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < indexed.Count; i++)
        {
            PlayerNoteSaveDto dto = indexed[i].Dto;
            if (!IsValidDto(dto) || !seenIds.Add(dto.id))
            {
                skippedCount++;
                continue;
            }

            restored.Add(PlayerNote.Restore(dto.id, dto.text, dto.normalizedX, dto.normalizedY));
        }

        return restored;
    }

    private static bool IsValidDto(PlayerNoteSaveDto dto)
    {
        return dto != null
            && !string.IsNullOrWhiteSpace(dto.id)
            && IsValidCoordinate(dto.normalizedX)
            && IsValidCoordinate(dto.normalizedY);
    }

    private static bool IsValidCoordinate(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }
}
