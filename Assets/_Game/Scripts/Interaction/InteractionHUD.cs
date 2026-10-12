using TMPro;
using UnityEngine;

// PROTOTYPE CODE: представя interaction данни, без да управлява инструменти.
public class InteractionHUD : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private GameObject indicatorRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text promptShadow;

    private void Awake()
    {
        if (playerInteraction == null || inputReader == null || indicatorRoot == null || promptText == null || promptShadow == null)
        {
            Debug.LogError("InteractionHUD няма зададени нужните references", this);
            enabled = false;
        }
        Hide();
    }

    private void LateUpdate()
    {
        // Чете резултата след gameplay Update, включително влизане в инструмент.
        if (playerInteraction == null || inputReader == null || indicatorRoot == null || promptText == null || promptShadow == null
            || !playerInteraction.IsWorldInteractionActive)
        {
            Hide();
            return;
        }

        indicatorRoot.SetActive(true);
        IInteractable target = playerInteraction.CurrentTarget;
        string binding = target != null ? inputReader.GetInteractBindingLabel() : string.Empty;
        bool showPrompt = target != null && !string.IsNullOrWhiteSpace(binding);
        promptText.gameObject.SetActive(showPrompt);
        promptShadow.gameObject.SetActive(showPrompt);
        promptText.text = showPrompt
            ? $"{target.DisplayName}\n[{binding}] {target.ActionLabel}"
            : string.Empty;
        // Отделен TMP слой пази сянката и при glyphs от Cyrillic fallback.
        promptShadow.text = promptText.text;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void Hide()
    {
        if (indicatorRoot != null)
            indicatorRoot.SetActive(false);
        if (promptText != null)
            promptText.text = string.Empty;
        if (promptShadow != null)
            promptShadow.text = string.Empty;
    }
}
