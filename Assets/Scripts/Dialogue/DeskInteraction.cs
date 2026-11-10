
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeskInteraction : MonoBehaviour
{
    private Vector3 originalScale;
    public bool isHovering;

    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private Image image;
    [SerializeField] private Color outlineColor = Color.white;

    private GameObject outlineObject;
    private RectTransform outlineRect;

    DialogueManager manager;

    void Start()
    {
        manager = FindAnyObjectByType<DialogueManager>();
        originalScale = image.rectTransform.localScale;

        CreateOutline();
    }

    void Update()
    {
        

        
        if (!manager.line.isEnd)
            return;
        
        if (Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // UI 이미지 영역 감지
        Camera uiCamera = null;

        if (image.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = image.canvas.worldCamera;

        bool isOver = RectTransformUtility.RectangleContainsScreenPoint(
            image.rectTransform,
            mousePosition,
            uiCamera
        );

        isHovering = isOver;

        // 이미지 확대
        Vector3 targetScale = isHovering
            ? originalScale * hoverScale
            : originalScale;

        image.rectTransform.localScale = Vector3.Lerp(
            image.rectTransform.localScale,
            targetScale,
            Time.deltaTime * 10f
        );

        // Outline 표시
        //outlineObject.SetActive(isHovering);

        //outlineRect.localScale = targetScale * 1.1f;

        // 클릭 감지
        if (isOver && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SceneFlow.GoToMaking(manager);
        }
    }

    void CreateOutline()
    {
        outlineObject = new GameObject("Outline");

        outlineObject.transform.SetParent(image.transform.parent, false);

        outlineRect = outlineObject.AddComponent<RectTransform>();

        RectTransform imageRect = image.rectTransform;

        outlineRect.anchorMin = imageRect.anchorMin;
        outlineRect.anchorMax = imageRect.anchorMax;
        outlineRect.pivot = imageRect.pivot;
        outlineRect.anchoredPosition = imageRect.anchoredPosition;
        outlineRect.sizeDelta = imageRect.sizeDelta;
        outlineRect.localRotation = imageRect.localRotation;
        outlineRect.localScale = originalScale * 1.1f;

        // Image 뒤에 배치
        outlineRect.SetSiblingIndex(imageRect.GetSiblingIndex());

        Image outlineImage = outlineObject.AddComponent<Image>();

        outlineImage.sprite = image.sprite;
        outlineImage.color = outlineColor;
        outlineImage.raycastTarget = false;

        outlineObject.SetActive(false);
    }
}
