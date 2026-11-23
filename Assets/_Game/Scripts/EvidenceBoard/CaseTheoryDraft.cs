using System;
using System.Collections.Generic;

// Работна чернова на личната теория на играча за конкретно дело (EB-013, EB-014, EB-016).
// Съхранява свободно въведени текстове и ръчно избрани type-safe source ID препратки (документи и снимки) без оценка за правилност, статус на подаване или автоматични изводи.
public sealed class CaseTheoryDraft
{
    private readonly List<string> referencedSourceIds = new List<string>();
    private readonly List<string> referencedPhotoSourceIds = new List<string>();

    public string CaseId { get; }
    public string MainSuspect { get; private set; } = string.Empty;
    public string ParticipantsAndRoles { get; private set; } = string.Empty;
    public string Motive { get; private set; } = string.Empty;
    public string Reconstruction { get; private set; } = string.Empty;
    public string SupportingEvidence { get; private set; } = string.Empty;
    public string UnresolvedQuestions { get; private set; } = string.Empty;
    public IReadOnlyList<string> ReferencedSourceIds => referencedSourceIds;
    public IReadOnlyList<string> ReferencedPhotoSourceIds => referencedPhotoSourceIds;

    public bool IsEmpty =>
        string.IsNullOrEmpty(MainSuspect)
        && string.IsNullOrEmpty(ParticipantsAndRoles)
        && string.IsNullOrEmpty(Motive)
        && string.IsNullOrEmpty(Reconstruction)
        && string.IsNullOrEmpty(SupportingEvidence)
        && string.IsNullOrEmpty(UnresolvedQuestions)
        && referencedSourceIds.Count == 0
        && referencedPhotoSourceIds.Count == 0;

    public CaseTheoryDraft(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            throw new ArgumentException("Case ID cannot be empty.", nameof(caseId));
        CaseId = caseId.Trim();
    }

    public void SetMainSuspect(string value) => MainSuspect = value ?? string.Empty;
    public void SetParticipantsAndRoles(string value) => ParticipantsAndRoles = value ?? string.Empty;
    public void SetMotive(string value) => Motive = value ?? string.Empty;
    public void SetReconstruction(string value) => Reconstruction = value ?? string.Empty;
    public void SetSupportingEvidence(string value) => SupportingEvidence = value ?? string.Empty;
    public void SetUnresolvedQuestions(string value) => UnresolvedQuestions = value ?? string.Empty;

    public bool ContainsReferencedSource(string sourceId) =>
        ContainsId(referencedSourceIds, sourceId);

    public bool TryAddReferencedSource(string sourceId) =>
        TryAddId(referencedSourceIds, sourceId);

    public bool RemoveReferencedSource(string sourceId) =>
        RemoveId(referencedSourceIds, sourceId);

    public bool ContainsReferencedPhotoSource(string sourceId) =>
        ContainsId(referencedPhotoSourceIds, sourceId);

    public bool TryAddReferencedPhotoSource(string sourceId) =>
        TryAddId(referencedPhotoSourceIds, sourceId);

    public bool RemoveReferencedPhotoSource(string sourceId) =>
        RemoveId(referencedPhotoSourceIds, sourceId);

    public bool ContainsReferencedSource(AttachedSourceType sourceType, string sourceId) =>
        sourceType == AttachedSourceType.Photo
            ? ContainsReferencedPhotoSource(sourceId)
            : ContainsReferencedSource(sourceId);

    public bool TryAddReferencedSource(AttachedSourceType sourceType, string sourceId) =>
        sourceType == AttachedSourceType.Photo
            ? TryAddReferencedPhotoSource(sourceId)
            : TryAddReferencedSource(sourceId);

    public bool RemoveReferencedSource(AttachedSourceType sourceType, string sourceId) =>
        sourceType == AttachedSourceType.Photo
            ? RemoveReferencedPhotoSource(sourceId)
            : RemoveReferencedSource(sourceId);

    private static bool ContainsId(List<string> list, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        string trimmed = sourceId.Trim();
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], trimmed, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool TryAddId(List<string> list, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        string trimmed = sourceId.Trim();
        if (ContainsId(list, trimmed))
            return false;

        list.Add(trimmed);
        return true;
    }

    private static bool RemoveId(List<string> list, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        string trimmed = sourceId.Trim();
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], trimmed, StringComparison.Ordinal))
            {
                list.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public static CaseTheoryDraft Restore(
        string caseId,
        string mainSuspect,
        string participantsAndRoles,
        string motive,
        string reconstruction,
        string supportingEvidence,
        string unresolvedQuestions)
    {
        return Restore(
            caseId,
            mainSuspect,
            participantsAndRoles,
            motive,
            reconstruction,
            supportingEvidence,
            unresolvedQuestions,
            Array.Empty<string>(),
            Array.Empty<string>());
    }

    public static CaseTheoryDraft Restore(
        string caseId,
        string mainSuspect,
        string participantsAndRoles,
        string motive,
        string reconstruction,
        string supportingEvidence,
        string unresolvedQuestions,
        IEnumerable<string> referencedSourceIds)
    {
        return Restore(
            caseId,
            mainSuspect,
            participantsAndRoles,
            motive,
            reconstruction,
            supportingEvidence,
            unresolvedQuestions,
            referencedSourceIds,
            Array.Empty<string>());
    }

    public static CaseTheoryDraft Restore(
        string caseId,
        string mainSuspect,
        string participantsAndRoles,
        string motive,
        string reconstruction,
        string supportingEvidence,
        string unresolvedQuestions,
        IEnumerable<string> referencedSourceIds,
        IEnumerable<string> referencedPhotoSourceIds)
    {
        var draft = new CaseTheoryDraft(caseId);
        draft.SetMainSuspect(mainSuspect);
        draft.SetParticipantsAndRoles(participantsAndRoles);
        draft.SetMotive(motive);
        draft.SetReconstruction(reconstruction);
        draft.SetSupportingEvidence(supportingEvidence);
        draft.SetUnresolvedQuestions(unresolvedQuestions);
        if (referencedSourceIds != null)
        {
            foreach (string sourceId in referencedSourceIds)
            {
                draft.TryAddReferencedSource(sourceId);
            }
        }

        if (referencedPhotoSourceIds != null)
        {
            foreach (string photoId in referencedPhotoSourceIds)
            {
                draft.TryAddReferencedPhotoSource(photoId);
            }
        }

        return draft;
    }
}
