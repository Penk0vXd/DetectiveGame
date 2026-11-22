using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Управлява страничния панел за преглед на избран прикачен документ или снимка (EB-005).
// Остава редом до корковата дъска с Player Notes, без да променя оригиналния запис.
public sealed class BoardSourcePreviewView : MonoBehaviour
{
    private TMP_FontAsset font;
    private Action closeRequested;
    private Action removeRequested;

    private TMP_Text badgeText;
    private TMP_Text titleText;
    private TMP_Text metaText;

    private GameObject documentContainer;
    private ScrollRect documentScroll;
    private RectTransform documentContent;
    private TMP_Text documentBodyText;

    private GameObject photoContainer;
    private Image photoImage;
    private GameObject photoMissingLabel;
    private ScrollRect photoCaptionScroll;
    private RectTransform photoCaptionContent;
    private TMP_Text photoCaptionText;

    public bool IsOpen => gameObject.activeSelf;

    public static BoardSourcePreviewView Create(
        Transform boardRoot,
        TMP_FontAsset font,
        Action onCloseRequested,
        Action onRemoveRequested)
    {
        // Позициониран в дясната колона на Evidence Board (същата зона като BoardPanel),
        // така че лявата коркова дъска с бележки остава напълно видима и активна.
        RectTransform root = PlayerNoteView.Rect(boardRoot, "AttachedSourceDetailsPanel",
            new Vector2(.52f, .13f), new Vector2(.96f, .90f));
        Image bg = root.gameObject.AddComponent<Image>();
        bg.sprite = EvidenceBoardVisuals.WoodFrameSprite;
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        bg.raycastTarget = true;

        Outline frameOutline = root.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        frameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow shadow = root.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.50f);
        shadow.effectDistance = new Vector2(0f, -6f);

        BoardSourcePreviewView view = root.gameObject.AddComponent<BoardSourcePreviewView>();
        view.font = font;
        view.closeRequested = onCloseRequested;
        view.removeRequested = onRemoveRequested;
        view.Build(root);
        root.gameObject.SetActive(false);
        return view;
    }

    public void Show(AttachedSourceType sourceType, BoardSourceRecord data)
    {
        if (data == null)
        {
            data = BoardSourceRecord.CreateMissing(string.Empty, sourceType);
        }

        bool isPhoto = sourceType == AttachedSourceType.Photo;
        badgeText.text = isPhoto ? "ПРЕГЛЕД • КОПИЕ НА СНИМКА" : "ПРЕГЛЕД • КОПИЕ НА ДОКУМЕНТ";
        titleText.text = data.Title;

        var metaParts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(data.SourceLabel)) metaParts.Add("Източник: " + data.SourceLabel);
        if (!string.IsNullOrWhiteSpace(data.SecondaryMeta)) metaParts.Add(data.SecondaryMeta);
        if (!string.IsNullOrWhiteSpace(data.Date)) metaParts.Add(data.Date);
        metaText.text = string.Join("  •  ", metaParts);

        documentContainer.SetActive(!isPhoto);
        photoContainer.SetActive(isPhoto);

        if (isPhoto)
        {
            photoImage.sprite = data.Image;
            photoImage.enabled = data.Image != null;
            photoMissingLabel.SetActive(data.Image == null);
            photoCaptionText.text = data.Body;
        }
        else
        {
            documentBodyText.text = data.Body;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (isPhoto)
        {
            ResetScrollTop(photoCaptionScroll, photoCaptionContent);
        }
        else
        {
            ResetScrollTop(documentScroll, documentContent);
        }
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private static void ResetScrollTop(ScrollRect scroll, RectTransform content)
    {
        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        if (scroll != null)
        {
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void Build(RectTransform root)
    {
        // Месингов кант отляво за визуално свързване с пантите на корковата дъска.
        RectTransform leftTrim = PlayerNoteView.Rect(root, "PreviewLeftTrim", new Vector2(0f, .01f), new Vector2(.008f, .99f));
        Image leftTrimImg = leftTrim.gameObject.AddComponent<Image>();
        leftTrimImg.color = EvidenceBoardVisuals.BrassAccent;
        leftTrimImg.raycastTarget = false;

        RectTransform inner = PlayerNoteView.Rect(root, "InnerSurface", new Vector2(.016f, .016f), new Vector2(.984f, .984f));
        Image innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(0.115f, 0.095f, 0.082f, 0.98f);
        innerImg.raycastTarget = true;

        // Горна лайсна с етикет и бърз бутон за затваряне.
        RectTransform headerRail = PlayerNoteView.Rect(inner, "PreviewHeaderRail", new Vector2(0f, .918f), new Vector2(1f, 1f));
        Image headerRailImg = headerRail.gameObject.AddComponent<Image>();
        headerRailImg.color = EvidenceBoardVisuals.HeaderRailColor;
        headerRailImg.raycastTarget = false;

        RectTransform headerAccent = PlayerNoteView.Rect(inner, "PreviewHeaderAccent", new Vector2(0f, .915f), new Vector2(1f, .919f));
        Image headerAccentImg = headerAccent.gameObject.AddComponent<Image>();
        headerAccentImg.color = EvidenceBoardVisuals.BrassHairline;
        headerAccentImg.raycastTarget = false;

        RectTransform badgeRect = PlayerNoteView.Rect(inner, "DetailBadge", new Vector2(.04f, .922f), new Vector2(.84f, .992f));
        badgeText = PlayerNoteView.Label(badgeRect, "Text", "", font, 14f, TextAlignmentOptions.MidlineLeft);
        badgeText.color = EvidenceBoardVisuals.StatusTextColor;
        badgeText.textWrappingMode = TextWrappingModes.NoWrap;
        badgeText.overflowMode = TextOverflowModes.Ellipsis;

        CreateButton(inner, "AttachedDetailCloseIconButton", "X",
            new Vector2(.895f, .928f), new Vector2(.968f, .988f),
            HandleCloseClicked, EvidenceBoardVisuals.ButtonStyle.CorkControl, 15f);

        RectTransform titleRect = PlayerNoteView.Rect(inner, "DetailTitle", new Vector2(.04f, .832f), new Vector2(.96f, .908f));
        titleText = PlayerNoteView.Label(titleRect, "Text", "", font, 21f, TextAlignmentOptions.MidlineLeft);
        titleText.color = EvidenceBoardVisuals.ParchmentText;
        titleText.fontStyle = FontStyles.Bold;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.lineSpacing = 2f;

        RectTransform metaRect = PlayerNoteView.Rect(inner, "DetailMeta", new Vector2(.04f, .775f), new Vector2(.96f, .830f));
        metaText = PlayerNoteView.Label(metaRect, "Text", "", font, 14.5f, TextAlignmentOptions.MidlineLeft);
        metaText.color = EvidenceBoardVisuals.MutedLabelText;
        metaText.textWrappingMode = TextWrappingModes.Normal;

        BuildDocumentContainer(inner);
        BuildPhotoContainer(inner);

        // Долна лайсна с действия: затваряне на прегледа и премахване на копието от дъската.
        RectTransform footerAccent = PlayerNoteView.Rect(inner, "PreviewFooterAccent", new Vector2(0f, .112f), new Vector2(1f, .116f));
        Image footerAccentImg = footerAccent.gameObject.AddComponent<Image>();
        footerAccentImg.color = EvidenceBoardVisuals.BrassHairline;
        footerAccentImg.raycastTarget = false;

        CreateButton(inner, "AttachedDetailBackButton", "ЗАТВОРИ",
            new Vector2(.04f, .024f), new Vector2(.35f, .098f),
            HandleCloseClicked, EvidenceBoardVisuals.ButtonStyle.CorkControl, 17f);
        CreateButton(inner, "AttachedDetailRemoveButton", "ПРЕМАХНИ ОТ ДЪСКАТА",
            new Vector2(.42f, .024f), new Vector2(.96f, .098f),
            HandleRemoveClicked, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger, 16.5f);
    }

    private void BuildDocumentContainer(RectTransform inner)
    {
        RectTransform docRoot = PlayerNoteView.Rect(inner, "DocumentDetailView", new Vector2(.04f, .130f), new Vector2(.96f, .762f));
        Image docPaper = docRoot.gameObject.AddComponent<Image>();
        docPaper.sprite = EvidenceBoardVisuals.PrintoutPaperSprite;
        docPaper.color = Color.white;
        docPaper.raycastTarget = true;

        RectTransform docViewport = PlayerNoteView.Rect(docRoot, "Viewport", new Vector2(.045f, .045f), new Vector2(.955f, .955f));
        docViewport.gameObject.AddComponent<RectMask2D>();
        documentContent = PlayerNoteView.Rect(docViewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
        documentContent.pivot = new Vector2(0.5f, 1f);
        ContentSizeFitter fitter = documentContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        documentBodyText = documentContent.gameObject.AddComponent<TextMeshProUGUI>();
        documentBodyText.font = font;
        documentBodyText.fontSize = 19f;
        documentBodyText.alignment = TextAlignmentOptions.TopLeft;
        documentBodyText.color = EvidenceBoardVisuals.PaperInk;
        documentBodyText.richText = false;
        documentBodyText.lineSpacing = 6f;
        documentBodyText.textWrappingMode = TextWrappingModes.Normal;
        documentBodyText.raycastTarget = false;

        documentScroll = docRoot.gameObject.AddComponent<ScrollRect>();
        documentScroll.horizontal = false;
        documentScroll.vertical = true;
        documentScroll.viewport = docViewport;
        documentScroll.content = documentContent;
        documentScroll.movementType = ScrollRect.MovementType.Clamped;
        documentScroll.scrollSensitivity = 28f;

        documentContainer = docRoot.gameObject;
    }

    private void BuildPhotoContainer(RectTransform inner)
    {
        RectTransform photoRoot = PlayerNoteView.Rect(inner, "PhotoDetailView", new Vector2(.04f, .130f), new Vector2(.96f, .762f));

        // Отпечатана снимка в пожълтяла фото рамка (2003).
        RectTransform photoFrame = PlayerNoteView.Rect(photoRoot, "PhotoPrintFrame", new Vector2(.04f, .27f), new Vector2(.96f, .99f));
        Image frameBg = photoFrame.gameObject.AddComponent<Image>();
        frameBg.sprite = EvidenceBoardVisuals.PhotoFrameSprite;
        frameBg.type = Image.Type.Sliced;
        frameBg.color = Color.white;
        frameBg.raycastTarget = false;

        RectTransform matte = PlayerNoteView.Rect(photoFrame, "PhotoMatte", new Vector2(.04f, .06f), new Vector2(.96f, .94f));
        Image matteBg = matte.gameObject.AddComponent<Image>();
        matteBg.color = new Color(0.09f, 0.10f, 0.11f, 1f);
        matteBg.raycastTarget = false;

        RectTransform imgRect = PlayerNoteView.Rect(matte, "DetailPhotoImage", new Vector2(.02f, .02f), new Vector2(.98f, .98f));
        photoImage = imgRect.gameObject.AddComponent<Image>();
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;

        RectTransform missingRect = PlayerNoteView.Rect(matte, "DetailPhotoMissingLabel", new Vector2(.08f, .32f), new Vector2(.92f, .68f));
        TMP_Text missingText = PlayerNoteView.Label(missingRect, "Text", "Няма налично изображение в записа", font, 17f, TextAlignmentOptions.Center);
        missingText.color = new Color(0.76f, 0.74f, 0.70f, 0.85f);
        missingText.textWrappingMode = TextWrappingModes.Normal;
        photoMissingLabel = missingRect.gameObject;

        // Скролируемо описание под снимката, за да не се изрязва при по-дълъг текст или 720p.
        RectTransform captionBox = PlayerNoteView.Rect(photoRoot, "PhotoDetailDescription", new Vector2(.02f, .01f), new Vector2(.98f, .245f));
        Image captionBg = captionBox.gameObject.AddComponent<Image>();
        captionBg.color = new Color(0.085f, 0.072f, 0.062f, 0.80f);
        captionBg.raycastTarget = true;

        RectTransform captionViewport = PlayerNoteView.Rect(captionBox, "Viewport", new Vector2(.03f, .08f), new Vector2(.97f, .92f));
        captionViewport.gameObject.AddComponent<RectMask2D>();
        photoCaptionContent = PlayerNoteView.Rect(captionViewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
        photoCaptionContent.pivot = new Vector2(0.5f, 1f);
        ContentSizeFitter captionFitter = photoCaptionContent.gameObject.AddComponent<ContentSizeFitter>();
        captionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        captionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        photoCaptionText = photoCaptionContent.gameObject.AddComponent<TextMeshProUGUI>();
        photoCaptionText.font = font;
        photoCaptionText.fontSize = 17f;
        photoCaptionText.alignment = TextAlignmentOptions.TopLeft;
        photoCaptionText.color = EvidenceBoardVisuals.ParchmentText;
        photoCaptionText.richText = false;
        photoCaptionText.lineSpacing = 4f;
        photoCaptionText.textWrappingMode = TextWrappingModes.Normal;
        photoCaptionText.raycastTarget = false;

        photoCaptionScroll = captionBox.gameObject.AddComponent<ScrollRect>();
        photoCaptionScroll.horizontal = false;
        photoCaptionScroll.vertical = true;
        photoCaptionScroll.viewport = captionViewport;
        photoCaptionScroll.content = photoCaptionContent;
        photoCaptionScroll.movementType = ScrollRect.MovementType.Clamped;
        photoCaptionScroll.scrollSensitivity = 24f;

        photoContainer = photoRoot.gameObject;
    }

    private void HandleCloseClicked() => closeRequested?.Invoke();

    private void HandleRemoveClicked() => removeRequested?.Invoke();

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
