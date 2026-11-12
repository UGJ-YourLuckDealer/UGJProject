using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;

public class ObjectInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private Vector3 originalScale;
    public bool isHovering;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color outlineColor = Color.yellow;

    [SerializeField] private GameObject heldPrefab;
    [SerializeField] private ObjectDragManager dragManager;

    [SerializeField] private string tooltipText = "None";
    [SerializeField] private Vector2 tooltipOffset = Vector2.zero;

    private GameObject outlineObject;
    private UnityEngine.UI.Image uiImage;
    private UnityEngine.UI.Outline uiOutline;

    void Awake()
    {
        originalScale = transform.localScale;
        uiImage = GetComponent<UnityEngine.UI.Image>();

        if (uiImage != null)
        {
            uiOutline = GetComponent<UnityEngine.UI.Outline>();
            if (uiOutline == null)
                uiOutline = gameObject.AddComponent<UnityEngine.UI.Outline>();
            uiOutline.effectColor = outlineColor;
            uiOutline.enabled = false;
        }
        else
        {
            CreateOutline();
        }
    }

    // Update is called once per frame
    void Update()
    {



        if (uiImage != null || Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x,
            mousePosition.y, Camera.main.transform.position.z));

        Collider2D hit = Physics2D.OverlapPoint(worldPosition);

        bool isOver = hit != null && hit.gameObject == gameObject;

        if (isOver && !isHovering) //Enter
        {
            isHovering = true;
            outlineObject.SetActive(true);
            ToolTip.Instance.SetTooltip(
                tooltipText,
                ToolTip.Instance.GuiCamera.WorldToScreenPoint(transform.position + (Vector3)tooltipOffset));
        }

        if (!isOver && isHovering) //Exit
        {
            isHovering = false;
            outlineObject.SetActive(false);
            ToolTip.Instance.HideTooltip();
        }

        if (isOver && Mouse.current.leftButton.wasPressedThisFrame) // OnMouseDown
        {
            if (dragManager == null || heldPrefab == null) return;

            dragManager.StartDrag(heldPrefab);
        }
    }

    public static bool IsPointerOverMaterialUI(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        var hits = new List<RaycastResult>();
        var pointer = new PointerEventData(eventSystem) { position = screenPosition };
        eventSystem.RaycastAll(pointer, hits);
        if (hits.Count == 0)
            return false;

        var interaction = hits[0].gameObject.GetComponentInParent<ObjectInteraction>();
        return interaction != null && interaction.GetComponent<UnityEngine.UI.Image>() != null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiImage == null || isHovering) return;

        isHovering = true;
        uiOutline.enabled = true;

        ToolTip tooltip = ToolTip.Instance;
        if (tooltip == null) return;


        Canvas canvas = uiImage.canvas;
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, transform.position);
        tooltip.SetTooltip(tooltipText, screenPosition + tooltipOffset);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiImage != null)
            ClearHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (uiImage == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (dragManager == null || heldPrefab == null) return;

        dragManager.StartDrag(heldPrefab);
    }

    void OnDisable()
    {
        ClearHover();
    }

    void ClearHover()
    {
        if (!isHovering) return;

        isHovering = false;
        if (uiOutline != null) uiOutline.enabled = false;
        if (outlineObject != null) outlineObject.SetActive(false);
        ToolTip tooltip = ToolTip.Instance;
        if (tooltip != null) tooltip.HideTooltip();
    }

    void CreateOutline()
    {
        outlineObject = new GameObject("Outline");

        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * 1.1f;

        SpriteRenderer outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();

        outlineRenderer.sprite = spriteRenderer.sprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        outlineObject.SetActive(false);
    }
}
