using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectInteraction : MonoBehaviour
{
    private Vector3 originalScale;
    public bool isHovering;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color outlineColor = Color.yellow;

    [SerializeField] private GameObject heldPrefab;
    [SerializeField] private ObjectDragManager dragManager;

    [SerializeField] private ObjectInfoTooltip infoTooltip;

    private GameObject outlineObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalScale = transform.localScale;

        CreateOutline();
    }

    // Update is called once per frame
    void Update()
    {



        if (Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x,
            mousePosition.y, Camera.main.transform.position.z));

        Collider2D hit = Physics2D.OverlapPoint(worldPosition);

        bool isOver = hit != null && hit.gameObject == gameObject;

        if(isOver && !isHovering) //Enter
        {

            isHovering = true;
            outlineObject.SetActive(true);

            infoTooltip.Show(gameObject);
        }

        if(!isOver && isHovering) //Exit
        {
            isHovering = false;
            outlineObject.SetActive(false);

            infoTooltip.Hide();
        }

        if(isOver && Mouse.current.leftButton.wasPressedThisFrame) // OnMouseDown
        {
            if (dragManager == null || heldPrefab == null) return;

            dragManager.StartDrag(heldPrefab);
        }
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
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder -1;

        outlineObject.SetActive(false);
    }
}
