using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ObjectDeleteManager : MonoBehaviour
{
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
                selectedObject = hit.gameObject;
                holdTimer = 0f;
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
            deleteSlider.gameObject.SetActive(false);
            Destroy(selectedObject);
            selectedObject = null;
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
        deleteSlider.gameObject.SetActive(false);
        selectedObject = null;
        holdTimer = 0;
    }
}
