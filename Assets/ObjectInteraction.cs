using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    private Vector3 originalScale;
    public bool isHovering;

    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color outlineColor = Color.yellow;

    [SerializeField] private GameObject heldPrefab;
    [SerializeField] private ObjectDragManager dragManager;

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
        Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
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

    private void OnMouseEnter()
    {
        isHovering = true;
        outlineObject.SetActive(true);
    }

    private void OnMouseExit()
    {
        isHovering = false;
        outlineObject.SetActive(false);
    }

    private void OnMouseDown()
    {
        if(dragManager == null || heldPrefab == null) return;

        dragManager.StartDrag(heldPrefab);
    }
}
