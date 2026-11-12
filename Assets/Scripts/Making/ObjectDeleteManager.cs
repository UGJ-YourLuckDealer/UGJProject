using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ObjectDeleteManager : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineScale = 1.1f;

    private GameObject outlineObject;
    private SpriteRenderer spriteRenderer;

    [Header("Delete Settings")]
    public LayerMask deleteLayer;
    public float deleteDuration = 3f;

    [Header("Delete UI")]
    public Slider deleteSlider;
    public Vector3 uiOffset = new Vector3(0, 0.7f, 0);

    private Camera mainCamera;

    private GameObject selectedObject;
    private float holdTimer = 0f;

    private void Awake()
    {
        mainCamera = Camera.main;

        deleteLayer = LayerMask.GetMask("Sub_Acrill");

        deleteSlider.gameObject.SetActive(false);
    }


    // Update is called once per frame
    void Update()
    {
        DetectObject();
        UpdateDelete();
    }

    void DetectObject()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Vector2 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

            Collider2D hit = Physics2D.OverlapPoint(worldPosition, deleteLayer);

            if (hit != null)
            {
                // 기존 선택 초기화
                if (outlineObject != null)
                    Destroy(outlineObject);

                selectedObject = hit.gameObject;
                holdTimer = 0f;

                // 외곽선 생성
                CreateOutline(selectedObject);
            }
        }
    }

    void UpdateDelete()
    {
        if (selectedObject == null) return;

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            CancelDelete();
            return;
        }

        if (Mouse.current.rightButton.isPressed)
        {
            holdTimer += Time.deltaTime;
            UpdateDeleteUI();
            Debug.Log((holdTimer / deleteDuration * 100).ToString("F1") + "%");
        }

        if (holdTimer >= deleteDuration)
        {
            Debug.Log("Delete Object");
            Customer_Data data = new Customer_Data();

            data.InCrease_Account(selectedObject);
            if (selectedObject.TryGetComponent<KeyringMaterialObject>(out var material) &&
                selectedObject.transform.parent != null &&
                selectedObject.transform.parent.TryGetComponent<KeyringMaterialObject>(out var chain))
            {
                chain.RemoveChild(material.keyringMaterialData);
            }
            deleteSlider.gameObject.SetActive(false);

            if (outlineObject != null)
                Destroy(outlineObject);

            Destroy(selectedObject);
            selectedObject = null;
            outlineObject = null;
            holdTimer = 0f;
        }
    }

    void UpdateDeleteUI()
    {
        if (selectedObject == null) return;

        deleteSlider.gameObject.SetActive(true);

        Vector3 worldPosition = selectedObject.transform.position + uiOffset;

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        deleteSlider.transform.position = screenPosition;

        deleteSlider.value = holdTimer / deleteDuration;
    }

    void CancelDelete()
    {
        if (outlineObject != null)
            Destroy(outlineObject);

        deleteSlider.gameObject.SetActive(false);
        selectedObject = null;
        outlineObject = null;
        holdTimer = 0;
    }

    void CreateOutline(GameObject target)
    {
        spriteRenderer = target.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        outlineObject = new GameObject("Outline");

        outlineObject.transform.SetParent(target.transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineScale;

        SpriteRenderer outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();

        outlineRenderer.sprite = spriteRenderer.sprite;
        outlineRenderer.color = outlineColor;

        outlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
    }
}
