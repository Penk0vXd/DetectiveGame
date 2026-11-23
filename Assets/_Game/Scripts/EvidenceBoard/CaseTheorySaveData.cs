using System;
using System.Collections.Generic;

// Сериализируем DTO за работна чернова на теория по конкретно дело (EB-013, EB-014, EB-016).
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
    public string[] referencedSourceIds = Array.Empty<string>();
    public string[] referencedPhotoSourceIds = Array.Empty<string>();
}

// Преобразуване и валидация между CaseTheorySession и Save DTO.
// Невалидни или дублирани записи се пропускат безопасно, а старите Save файлове без caseTheories или referencedSourceIds се зареждат чисто.
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

            string[] referencedIds = draft.ReferencedSourceIds.Count == 0
                ? Array.Empty<string>()
                : CopyIds(draft.ReferencedSourceIds);
            string[] referencedPhotoIds = draft.ReferencedPhotoSourceIds.Count == 0
                ? Array.Empty<string>()
                : CopyIds(draft.ReferencedPhotoSourceIds);

            result.Add(new CaseTheorySaveDto
            {
                caseId = draft.CaseId,
                mainSuspect = draft.MainSuspect,
                participantsAndRoles = draft.ParticipantsAndRoles,
                motive = draft.Motive,
                reconstruction = draft.Reconstruction,
                supportingEvidence = draft.SupportingEvidence,
                unresolvedQuestions = draft.UnresolvedQuestions,
                referencedSourceIds = referencedIds,
                referencedPhotoSourceIds = referencedPhotoIds
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
                dto.unresolvedQuestions,
                dto.referencedSourceIds,
                dto.referencedPhotoSourceIds));
        }

        return restored;
    }

    private static string[] CopyIds(IReadOnlyList<string> sourceIds)
    {
        var copy = new string[sourceIds.Count];
        for (int i = 0; i < sourceIds.Count; i++)
        {
            copy[i] = sourceIds[i];
        }

        return copy;
    }
}
