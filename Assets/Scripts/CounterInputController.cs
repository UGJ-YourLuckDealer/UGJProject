using UnityEngine;
using UnityEngine.InputSystem;

public class CounterInputController : MonoBehaviour
{
    [SerializeField] PopupService popupService;

    [SerializeField] InputActionReference exitAction;

    void OnEnable()
    {
        exitAction.action.performed += OnExitAction;
    }

    void OnDisable()
    {
        exitAction.action.performed -= OnExitAction;
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
}