using System;

// Работна чернова на личната теория на играча за конкретно дело (EB-013).
// Съхранява свободно въведени текстове без оценка за правилност, статус на подаване или автоматични изводи.
public sealed class CaseTheoryDraft
{
    public string CaseId { get; }
    public string MainSuspect { get; private set; } = string.Empty;
    public string ParticipantsAndRoles { get; private set; } = string.Empty;
    public string Motive { get; private set; } = string.Empty;
    public string Reconstruction { get; private set; } = string.Empty;
    public string SupportingEvidence { get; private set; } = string.Empty;
    public string UnresolvedQuestions { get; private set; } = string.Empty;

    public bool IsEmpty =>
        string.IsNullOrEmpty(MainSuspect)
        && string.IsNullOrEmpty(ParticipantsAndRoles)
        && string.IsNullOrEmpty(Motive)
        && string.IsNullOrEmpty(Reconstruction)
        && string.IsNullOrEmpty(SupportingEvidence)
        && string.IsNullOrEmpty(UnresolvedQuestions);

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

    public static CaseTheoryDraft Restore(
        string caseId,
        string mainSuspect,
        string participantsAndRoles,
        string motive,
        string reconstruction,
        string supportingEvidence,
        string unresolvedQuestions)
    {
        var draft = new CaseTheoryDraft(caseId);
        draft.SetMainSuspect(mainSuspect);
        draft.SetParticipantsAndRoles(participantsAndRoles);
        draft.SetMotive(motive);
        draft.SetReconstruction(reconstruction);
        draft.SetSupportingEvidence(supportingEvidence);
        draft.SetUnresolvedQuestions(unresolvedQuestions);
        return draft;
    }
}
