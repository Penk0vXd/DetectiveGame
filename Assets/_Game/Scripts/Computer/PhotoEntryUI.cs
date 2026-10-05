using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class PhotoEntryUI : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private Image thumbnailImage = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text dateText = null;
    [SerializeField] private TMP_Text locationText = null;

    private PhotoData photoData;
    private Action<PhotoData> selectedCallback;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Initialize(PhotoData data, Action<PhotoData> onSelected)
    {
        photoData = data;
        selectedCallback = onSelected;

        SetText(titleText, data == null ? string.Empty : data.Title);
        SetText(dateText, data == null ? string.Empty : data.Date);
        SetText(locationText, data == null ? string.Empty : data.Location);

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = data == null ? null : data.Image;
            thumbnailImage.enabled = data != null && data.Image != null;
        }
    }

    private void HandleClick()
    {
        if (photoData != null)
        {
            selectedCallback?.Invoke(photoData);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
