using UnityEngine;
using UnityEngine.InputSystem;

public class MakingInputController : MonoBehaviour
{
    [SerializeField] PopupService popupService;

    [SerializeField] InputActionReference exitAction;
    [SerializeField] InputActionReference shiftAction;

    void OnEnable()
    {
        exitAction.action.performed += OnExitAction;
        shiftAction.action.performed += OnShiftAction;
    }

    void OnDisable()
    {
        exitAction.action.performed -= OnExitAction;
        shiftAction.action.performed -= OnShiftAction;
    }

    void OnExitAction(InputAction.CallbackContext ctx)
    {
        const string popupKey = "popup_making_settings";

        if (popupService.IsPopupOpen(popupKey))
        {
            popupService.ClosePopup(popupKey);
        }
        else
        {
            popupService.ShowPopup(popupKey);
        }
    }

    void OnShiftAction(InputAction.CallbackContext ctx)
    {
        const string popupKey = "popup_making_dialogue_history";

        if (popupService.IsPopupOpen(popupKey))
        {
            popupService.ClosePopup(popupKey);
        }
        else
        {
            popupService.ShowPopup(popupKey);
        }
    }
}