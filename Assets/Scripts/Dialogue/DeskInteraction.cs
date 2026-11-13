
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeskInteraction : MonoBehaviour
{
    public bool isHovering;

    [SerializeField] private Image image;

    ImageOutline imageoutline;

    DialogueManager manager;

    void Start()
    {
        imageoutline = FindAnyObjectByType<ImageOutline>();
        manager = FindAnyObjectByType<DialogueManager>();
        imageoutline.Hide();
    }

    void Update()
    {



        if (manager == null || !manager.CanEnterMaking)
            return;

        if (Mouse.current == null)
        {

            return;
        }






        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Camera uiCamera = null;

        if (image.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = image.canvas.worldCamera;

        bool isOver = RectTransformUtility.RectangleContainsScreenPoint(
            image.rectTransform,
            mousePosition,
            uiCamera
        );

        isHovering = isOver;

        if (isOver)
        {
            imageoutline.Show_Outline();
        }
        else
        {
            imageoutline.Hide();
        }

        if (isOver && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SceneFlow.GoToMaking(manager);
        }
    }


}
