using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Управлява визуализацията на ръчно прикачените копия (Документи и Снимки) върху корковата дъска,
// detail view прозореца им и потвърждението за премахване (без да засяга оригиналите в Computer).
public sealed class BoardAttachedSourcesView : MonoBehaviour
{
    private static readonly Vector2[] DefaultSpawnPositions =
    {
        new Vector2(.75f, .25f),
        new Vector2(.75f, .75f),
        new Vector2(.25f, .25f),
        new Vector2(.50f, .50f),
        new Vector2(.25f, .75f)
    };

    private readonly List<AttachedSourceView> views = new List<AttachedSourceView>();
    private AttachedSourcesSession session;
    private Func<string, AttachedSourceType, BoardSourceRecord> sourceResolver;
    private RectTransform surface;
    private RectTransform dialogParent;
    private TMP_FontAsset font;
    private Action<bool> countChanged;
    private Action detailOpened;
    private Action detailClosed;

    private GameObject removeConfirmDialog;
    private AttachedSourceView pendingRemove;
    private bool returnToBoardAfterRemove;

    private BoardSourcePreviewView previewView;
    private AttachedSourceView inspectedView;

    public bool IsDetailOpen => previewView != null && previewView.IsOpen;

    public static BoardAttachedSourcesView Create(
        Transform boardRoot,
        RectTransform corkFrame,
        RectTransform corkSurface,
        AttachedSourcesSession session,
        TMP_FontAsset font,
        Func<string, AttachedSourceType, BoardSourceRecord> sourceResolver,
        Action<bool> onCountChanged,
        Action onDetailOpened,
        Action onDetailClosed)
    {
        var holder = new GameObject("BoardAttachedSourcesController", typeof(RectTransform));
        holder.transform.SetParent(boardRoot, false);
        BoardAttachedSourcesView controller = holder.AddComponent<BoardAttachedSourcesView>();
        controller.session = session;
        controller.sourceResolver = sourceResolver;
        controller.surface = corkSurface;
        controller.dialogParent = corkFrame;
        controller.font = font;
        controller.countChanged = onCountChanged;
        controller.detailOpened = onDetailOpened;
        controller.detailClosed = onDetailClosed;

        controller.BuildRemoveConfirmDialog(boardRoot);
        controller.previewView = BoardSourcePreviewView.Create(
            boardRoot, font, controller.HandleBackFromDetails, controller.HandleRemoveFromDetails);
        controller.RebuildFromSession();
        return controller;
    }

    public Vector2 NextSpawnPosition()
    {
        int index = session != null ? session.Sources.Count : views.Count;
        Vector2 basePos = DefaultSpawnPositions[index % DefaultSpawnPositions.Length];
        int cycle = index / DefaultSpawnPositions.Length;
        float offset = (cycle % 3) * 0.03f;
        return new Vector2(Mathf.Clamp(basePos.x - offset, .22f, .78f), Mathf.Clamp(basePos.y + offset, .22f, .78f));
    }

    public void AddViewFor(AttachedSource attached)
    {
        if (attached == null || surface == null) return;
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i] != null && views[i].AttachmentId == attached.AttachmentId)
                return;
        }

        BoardSourceRecord record = Resolve(attached);
        AttachedSourceView view = AttachedSourceView.Create(
            surface, attached, record, font, OpenDetails, target => RequestRemove(target, fromDetails: false));
        views.Add(view);
        NotifyCountChanged();
    }

    public void RebuildFromSession()
    {
        CloseDetailsSilent();
        CancelRemove();
        for (int i = 0; i < views.Count; i++)
        {
            AttachedSourceView view = views[i];
            if (view == null) continue;
            view.gameObject.SetActive(false);
            view.transform.SetParent(null, false);
            Destroy(view.gameObject);
        }
        views.Clear();

        if (session != null && surface != null)
        {
            for (int i = 0; i < session.Sources.Count; i++)
            {
                AttachedSource item = session.Sources[i];
                BoardSourceRecord record = Resolve(item);
                views.Add(AttachedSourceView.Create(
                    surface, item, record, font, OpenDetails, target => RequestRemove(target, fromDetails: false)));
            }
        }

        NotifyCountChanged();
    }

    public void CloseDetailsSilent()
    {
        if (inspectedView != null)
        {
            inspectedView.SetSelected(false);
            inspectedView = null;
        }

        if (previewView != null)
            previewView.Hide();
    }

    public void CancelRemove()
    {
        pendingRemove = null;
        returnToBoardAfterRemove = false;
        if (removeConfirmDialog != null)
            removeConfirmDialog.SetActive(false);
    }

    private BoardSourceRecord Resolve(AttachedSource attached)
    {
        BoardSourceRecord resolved = sourceResolver?.Invoke(attached.SourceId, attached.SourceType);
        return resolved ?? BoardSourceRecord.CreateMissing(attached.SourceId, attached.SourceType);
    }

    private void OpenDetails(AttachedSourceView target)
    {
        if (target == null || previewView == null) return;
        CancelRemove();
        if (inspectedView != null && inspectedView != target)
            inspectedView.SetSelected(false);
        inspectedView = target;
        inspectedView.SetSelected(true);

        BoardSourceRecord data = target.Record ?? Resolve(target.Model);
        previewView.Show(target.SourceType, data);
        detailOpened?.Invoke();
    }

    private void HandleBackFromDetails()
    {
        CloseDetailsSilent();
        detailClosed?.Invoke();
    }

    private void HandleRemoveFromDetails()
    {
        if (inspectedView == null) return;
        RequestRemove(inspectedView, fromDetails: true);
    }

    private void RequestRemove(AttachedSourceView target, bool fromDetails)
    {
        if (target == null || !views.Contains(target)) return;
        pendingRemove = target;
        returnToBoardAfterRemove = fromDetails;
        if (removeConfirmDialog != null)
        {
            removeConfirmDialog.transform.SetAsLastSibling();
            removeConfirmDialog.SetActive(true);
        }
    }

    private void ConfirmRemove()
    {
        AttachedSourceView target = pendingRemove;
        bool wasFromDetails = returnToBoardAfterRemove;
        CancelRemove();
        if (target == null) return;

        session?.Remove(target.AttachmentId);
        views.Remove(target);
        target.gameObject.SetActive(false);
        target.transform.SetParent(null, false);
        Destroy(target.gameObject);
        NotifyCountChanged();

        if (wasFromDetails || inspectedView == target)
        {
            CloseDetailsSilent();
            detailClosed?.Invoke();
        }
    }

    private void NotifyCountChanged()
    {
        countChanged?.Invoke(views.Count > 0);
    }

    private void OnDisable()
    {
        CancelRemove();
    }

    private void BuildRemoveConfirmDialog(Transform boardRoot)
    {
        RectTransform overlay = PlayerNoteView.Rect(boardRoot, "RemoveAttachedSourceConfirmDialog",
            new Vector2(.03f, .13f), new Vector2(.97f, .90f));
        Image backdrop = overlay.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.06f, 0.045f, 0.035f, 0.72f);
        backdrop.raycastTarget = true;

        RectTransform card = PlayerNoteView.Rect(overlay, "DialogCard",
            new Vector2(.28f, .36f), new Vector2(.72f, .64f));
        Image cardBg = card.gameObject.AddComponent<Image>();
        cardBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        cardBg.type = Image.Type.Sliced;
        cardBg.color = new Color(0.15f, 0.12f, 0.10f, 0.98f);
        cardBg.raycastTarget = true;

        Outline cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = EvidenceBoardVisuals.BrassAccent;
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        cardShadow.effectDistance = new Vector2(0f, -6f);

        RectTransform promptBox = PlayerNoteView.Rect(card, "RemoveConfirmPrompt",
            new Vector2(.08f, .48f), new Vector2(.92f, .86f));
        TMP_Text prompt = PlayerNoteView.Label(promptBox, "Text",
            "Да премахна ли това копие от дъската?\nОригиналът в компютъра ще остане запазен.",
            font, 18f, TextAlignmentOptions.Center);
        prompt.color = EvidenceBoardVisuals.ParchmentText;
        prompt.textWrappingMode = TextWrappingModes.Normal;
        prompt.overflowMode = TextOverflowModes.Ellipsis;

        CreateButton(card, "ConfirmRemoveAttachedSourceButton", "Премахни",
            new Vector2(.10f, .14f), new Vector2(.46f, .40f),
            ConfirmRemove, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger, 18f);
        CreateButton(card, "CancelRemoveAttachedSourceButton", "Отказ",
            new Vector2(.54f, .14f), new Vector2(.90f, .40f),
            CancelRemove, EvidenceBoardVisuals.ButtonStyle.CorkControl, 18f);

        removeConfirmDialog = overlay.gameObject;
        removeConfirmDialog.SetActive(false);
    }

    private Button CreateButton(Transform parent, string name, string title, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction click, EvidenceBoardVisuals.ButtonStyle style, float fontSize)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);
        TMP_Text label = PlayerNoteView.Label(rect, "Label", title, font, fontSize, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        EvidenceBoardVisuals.ApplyButtonStyle(button, image, label, style);
        return button;
    }
}
