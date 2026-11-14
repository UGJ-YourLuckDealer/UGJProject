using UnityEngine;
using UnityEngine.InputSystem;

public class MakingInputController : MonoBehaviour
{
    [SerializeField] PopupService popupService;

    [SerializeField] InputActionReference exitAction;
    [SerializeField] InputActionReference shiftAction;
    [SerializeField] InputActionReference spaceAction;

    void OnEnable()
    {
        exitAction.action.performed += OnExitAction;
        shiftAction.action.performed += OnShiftAction;
        spaceAction.action.performed += OnSpaceAction;
    }

    void OnDisable()
    {
        exitAction.action.performed -= OnExitAction;
        shiftAction.action.performed -= OnShiftAction;
        spaceAction.action.performed -= OnSpaceAction;
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
        const string popupKey = "popup_making_recipe";

        if (popupService.IsPopupOpen(popupKey))
        {
            popupService.ClosePopup(popupKey);
        }
        else
        {
            if (popupService.IsPopupOpen("popup_making_dialogue_history"))
            {
                popupService.ClosePopup("popup_making_dialogue_history");
            }

            popupService.ShowPopup(popupKey);
            var popup = popupService.GetPopup(popupKey);
            popup.GetComponent<MakingNoteController>().OpenTab(MakingNoteController.Tab.Recipe);
        }
    }

    void OnSpaceAction(InputAction.CallbackContext ctx)
    {
        const string popupKey = "popup_making_dialogue_history";

        if (popupService.IsPopupOpen(popupKey))
        {
            popupService.ClosePopup(popupKey);
        }
        else
        {
            if (popupService.IsPopupOpen("popup_making_recipe"))
            {
                popupService.ClosePopup("popup_making_recipe");
            }

            popupService.ShowPopup(popupKey);
            var popup = popupService.GetPopup(popupKey);
            popup.GetComponent<MakingNoteController>().OpenTab(MakingNoteController.Tab.DialogueHistory);
        }
    }
}