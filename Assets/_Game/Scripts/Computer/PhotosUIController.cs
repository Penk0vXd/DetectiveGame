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
        if (previewImage != null)
        {
            previewImage.sprite = photo.Image;
            previewImage.enabled = photo.Image != null;
        }

        SetText(titleText, photo.Title);
        SetText(dateText, photo.Date);
        SetText(locationText, photo.Location);
        SetText(descriptionText, photo.Description);
    }

    private void ClearPhotoDetails()
    {
        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
        }

        SetText(titleText, string.Empty);
        SetText(dateText, string.Empty);
        SetText(locationText, string.Empty);
        SetText(descriptionText, string.Empty);
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
