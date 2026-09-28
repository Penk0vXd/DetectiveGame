using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class CaseEntryUI : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private TMP_Text caseNumberText = null;
    [SerializeField] private TMP_Text caseNameText = null;
    [SerializeField] private TMP_Text statusText = null;

    private CaseData caseData;
    private CaseFilesUIController caseFilesUIController;

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

    public void Initialize(CaseData data, CaseFilesUIController controller)
    {
        caseData = data;
        caseFilesUIController = controller;

        if (data == null)
        {
            ClearText();
            return;
        }

        SetText(caseNumberText, data.CaseNumber);
        SetText(caseNameText, data.CaseName);
        SetText(statusText, data.Status);
    }

    private void HandleClick()
    {
        if (caseData == null || caseFilesUIController == null)
        {
            return;
        }

        caseFilesUIController.SelectCase(caseData);
    }

    private void ClearText()
    {
        SetText(caseNumberText, string.Empty);
        SetText(caseNameText, string.Empty);
        SetText(statusText, string.Empty);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
