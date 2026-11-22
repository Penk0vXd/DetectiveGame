using System;
using System.Collections.Generic;

// Управлява работните теории на играча, разделени строго по Case ID (EB-013).
public sealed class CaseTheorySession
{
    public const string DefaultCaseId = "CASE-ORION-01";

    private readonly List<CaseTheoryDraft> drafts = new List<CaseTheoryDraft>();

    public IReadOnlyList<CaseTheoryDraft> Drafts => drafts;

    public CaseTheoryDraft GetOrCreateDraft(string caseId = DefaultCaseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
            throw new ArgumentException("Case ID cannot be empty.", nameof(caseId));

        string trimmed = caseId.Trim();
        if (TryGetDraft(trimmed, out CaseTheoryDraft existing))
            return existing;

        var created = new CaseTheoryDraft(trimmed);
        drafts.Add(created);
        return created;
    }

    public bool TryGetDraft(string caseId, out CaseTheoryDraft draft)
    {
        draft = default!;
        if (string.IsNullOrWhiteSpace(caseId))
            return false;

        string trimmed = caseId.Trim();
        for (int i = 0; i < drafts.Count; i++)
        {
            if (string.Equals(drafts[i].CaseId, trimmed, StringComparison.Ordinal))
            {
                draft = drafts[i];
                return true;
            }
        }

        return false;
    }

    public void ReplaceAll(IEnumerable<CaseTheoryDraft> restoredDrafts)
    {
        drafts.Clear();
        if (restoredDrafts == null)
            return;

        var seenCaseIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CaseTheoryDraft item in restoredDrafts)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(restoredDrafts), "Restored theory draft cannot be null.");
            if (!seenCaseIds.Add(item.CaseId))
                throw new ArgumentException($"Duplicate theory draft Case ID '{item.CaseId}'.", nameof(restoredDrafts));
            drafts.Add(item);
        }
    }
}
