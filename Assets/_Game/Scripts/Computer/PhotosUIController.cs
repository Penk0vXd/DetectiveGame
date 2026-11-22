using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhotosUIController : MonoBehaviour
{
    [Header("Photos Window")]
    [SerializeField] private GameObject photosWindow = null;
    [SerializeField] private Transform photoListContent = null;
    [SerializeField] private PhotoEntryUI photoEntryPrefab = null;

    [Header("Photo Preview")]
    [SerializeField] private Image previewImage = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text dateText = null;
    [SerializeField] private TMP_Text locationText = null;
    [SerializeField] private TMP_Text descriptionText = null;

    [Header("Photo Data")]
    [SerializeField] private List<PhotoData> photos = new List<PhotoData>();

    private bool isGalleryBuilt;
    private bool hasLoggedMissingWindow;
    private bool hasLoggedMissingGalleryReferences;
    private PhotoData selectedPhoto;
    private ComputerBoardAttachBar photoAttachBar;
    private EvidenceBoardUIController evidenceBoard;

    public bool TryFindPhotoBySourceId(string sourceId, out PhotoData photo)
    {
        photo = null;
        if (string.IsNullOrWhiteSpace(sourceId) || photos == null)
        {
            return false;
        }

        for (int i = 0; i < photos.Count; i++)
        {
            PhotoData candidate = photos[i];
            if (candidate != null && string.Equals(candidate.SourceId, sourceId, System.StringComparison.Ordinal))
            {
                photo = candidate;
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        ClearPhotoDetails();
    }

    public void OpenPhotos()
    {
        if (photosWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // отваря приложението
        photosWindow.SetActive(true);
        BuildGallery();
        RefreshPhotoAttachBar();
    }

    public void ClosePhotos()
    {
        if (photosWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // затваря приложението
        photosWindow.SetActive(false);
    }

    public void BuildGallery()
    {
        if (isGalleryBuilt)
        {
            return;
        }

        if (photoListContent == null || photoEntryPrefab == null)
        {
            LogMissingGalleryReferencesOnce();
            return;
        }

        if (photos != null)
        {
            foreach (PhotoData photo in photos)
            {
                if (photo == null)
                {
                    continue;
                }

                PhotoEntryUI entry = Instantiate(photoEntryPrefab, photoListContent);
                entry.Initialize(photo, SelectPhoto);
            }
        }

        isGalleryBuilt = true;
    }

    public void SelectPhoto(PhotoData photo)
    {
        if (photo == null)
        {
            ClearPhotoDetails();
            return;
        }

        // показва снимката
        selectedPhoto = photo;
        if (previewImage != null)
        {
            previewImage.sprite = photo.Image;
            previewImage.enabled = photo.Image != null;
        }

        SetText(titleText, photo.Title);
        SetText(dateText, photo.Date);
        SetText(locationText, photo.Location);
        SetText(descriptionText, photo.Description);
        RefreshPhotoAttachBar();
    }

    private void ClearPhotoDetails()
    {
        selectedPhoto = null;
        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
        }

        SetText(titleText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(locationText, string.Empty);
        SetText(descriptionText, string.Empty);
        photoAttachBar?.SetHidden();
    }

    private void EnsurePhotoAttachBar()
    {
        if (photoAttachBar != null || descriptionText == null || descriptionText.transform.parent == null)
        {
            return;
        }

        // Освобождава ~50px вертикално от рамката за преглед, за да няма text overflow при 1280x720 и 1920x1080.
        if (previewImage != null && previewImage.transform.parent != null)
        {
            LayoutElement frameLayout = previewImage.transform.parent.GetComponent<LayoutElement>();
            if (frameLayout != null && frameLayout.preferredHeight > 420f)
            {
                frameLayout.preferredHeight = 418f;
            }
        }

        TMP_FontAsset font = titleText != null ? titleText.font : descriptionText.font;
        photoAttachBar = ComputerBoardAttachBar.Create(
            descriptionText.transform.parent,
            font,
            "AttachPhotoToBoardButton",
            AttachSelectedPhotoToBoard);
    }

    private void RefreshPhotoAttachBar()
    {
        EnsurePhotoAttachBar();
        if (photoAttachBar == null)
        {
            return;
        }

        if (selectedPhoto == null || string.IsNullOrWhiteSpace(selectedPhoto.SourceId))
        {
            photoAttachBar.SetHidden();
            return;
        }

        EvidenceBoardUIController board = ResolveEvidenceBoard();
        bool alreadyAttached = board != null
            && board.IsSourceAttached(selectedPhoto.SourceId, AttachedSourceType.Photo);
        photoAttachBar.ShowReady(alreadyAttached);
    }

    private void AttachSelectedPhotoToBoard()
    {
        if (selectedPhoto == null || string.IsNullOrWhiteSpace(selectedPhoto.SourceId))
        {
            return;
        }

        EnsurePhotoAttachBar();
        EvidenceBoardUIController board = ResolveEvidenceBoard();
        if (board == null)
        {
            return;
        }

        bool added = board.TryAttachSource(selectedPhoto.SourceId, AttachedSourceType.Photo);
        photoAttachBar?.ShowStatus(added
            ? ComputerBoardAttachBar.AddedStatusMessage
            : ComputerBoardAttachBar.AlreadyAttachedStatusMessage);
    }

    private EvidenceBoardUIController ResolveEvidenceBoard()
    {
        if (evidenceBoard == null)
        {
            evidenceBoard = FindAnyObjectByType<EvidenceBoardUIController>(FindObjectsInactive.Include);
        }

        return evidenceBoard;
    }

    private void LogMissingWindowOnce()
    {
        if (hasLoggedMissingWindow)
        {
            return;
        }

        hasLoggedMissingWindow = true;
        Debug.LogWarning("PhotosUIController няма зададен PhotosWindow", this);
    }

    private void LogMissingGalleryReferencesOnce()
    {
        if (hasLoggedMissingGalleryReferences)
        {
            return;
        }

        hasLoggedMissingGalleryReferences = true;
        Debug.LogWarning("PhotosUIController няма зададени gallery references", this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
