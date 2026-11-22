using System;
using System.Collections.Generic;

// Сериализируем DTO за работна чернова на теория по конкретно дело (EB-013).
[Serializable]
public sealed class CaseTheorySaveDto
{
    public string caseId = string.Empty;
    public string mainSuspect = string.Empty;
    public string participantsAndRoles = string.Empty;
    public string motive = string.Empty;
    public string reconstruction = string.Empty;
    public string supportingEvidence = string.Empty;
    public string unresolvedQuestions = string.Empty;
}

// Преобразуване и валидация между CaseTheorySession и Save DTO.
// Невалидни или дублирани записи се пропускат безопасно, а старите Save файлове без caseTheories се зареждат с празна чернова.
public static class CaseTheorySaveMapper
{
    public static CaseTheorySaveDto[] ToSaveDtos(CaseTheorySession session)
    {
        if (session == null || session.Drafts.Count == 0)
            return Array.Empty<CaseTheorySaveDto>();

        var result = new List<CaseTheorySaveDto>(session.Drafts.Count);
        for (int i = 0; i < session.Drafts.Count; i++)
        {
            CaseTheoryDraft draft = session.Drafts[i];
            if (draft == null || draft.IsEmpty)
                continue;

            result.Add(new CaseTheorySaveDto
            {
                caseId = draft.CaseId,
                mainSuspect = draft.MainSuspect,
                participantsAndRoles = draft.ParticipantsAndRoles,
                motive = draft.Motive,
                reconstruction = draft.Reconstruction,
                supportingEvidence = draft.SupportingEvidence,
                unresolvedQuestions = draft.UnresolvedQuestions
            });
        }

        return result.ToArray();
    }

    public static List<CaseTheoryDraft> PrepareRestore(CaseTheorySaveDto[] dtos, out int skippedCount)
    {
        skippedCount = 0;
        var restored = new List<CaseTheoryDraft>();
        if (dtos == null || dtos.Length == 0)
            return restored;

        var seenCaseIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < dtos.Length; i++)
        {
            CaseTheorySaveDto dto = dtos[i];
            if (dto == null || string.IsNullOrWhiteSpace(dto.caseId))
            {
                skippedCount++;
                continue;
            }

            string trimmedCaseId = dto.caseId.Trim();
            if (!seenCaseIds.Add(trimmedCaseId))
            {
                skippedCount++;
                continue;
            }

            restored.Add(CaseTheoryDraft.Restore(
                trimmedCaseId,
                dto.mainSuspect,
                dto.participantsAndRoles,
                dto.motive,
                dto.reconstruction,
                dto.supportingEvidence,
                dto.unresolvedQuestions));
        }

        return restored;
    }
}
