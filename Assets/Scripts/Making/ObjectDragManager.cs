using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectDragManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D initialplacementArea;
    [SerializeField] private Collider2D currentPlacementArea;

    private GameObject draggingObject;
    private bool isDragging;

    private bool mainObjectPlaced;
    private bool draggingMainObject;

    bool isMainObject;

    Vector2 mousePosition;
    private void Awake()
    {
        mainCamera = Camera.main;

        currentPlacementArea = initialplacementArea;
    }

    private void Update()
    {
        if (!isDragging)
            return;

        MoveObject();

        if(Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    public void StartDrag(GameObject prefab)
    {
        if (isDragging || prefab == null)
            return;


        //if(!mainObjectPlaced && !isMainObject)
        //{
        //    Debug.LogError("Main is not exist.");
        //    return;
        //}

        Vector3 mousePosition = GetMouseWorldPosition();

        draggingObject = Instantiate(prefab, mousePosition, Quaternion.identity);

        isDragging = true;
    }

    void MoveObject()
    {
        if (draggingObject == null)
            return;

        draggingObject.transform.position = GetMouseWorldPosition();
    }

    void EndDrag()
    {
        Check_Board check = FindAnyObjectByType<Check_Board>();
        mousePosition = GetMouseWorldPosition();

        bool canPlace;

        if (!mainObjectPlaced)
            canPlace = initialplacementArea != null && initialplacementArea.OverlapPoint(mousePosition);
        else
            canPlace = currentPlacementArea != null && currentPlacementArea.OverlapPoint(mousePosition);


        if (canPlace)
        {
            if (check.GetCount(LayerMask.NameToLayer("Main_Kiring"), "Heart") > 1 ||
                check.GetCount(LayerMask.NameToLayer("Main_Kiring"), "Normal") > 1)
            {
                Destroy(draggingObject);
                Debug.LogError("Main_Kiring is not exist over 1");
                draggingObject = null;
            }
            else
            {
                if (!mainObjectPlaced)
                {
                    Transform innerArea = draggingObject.transform.Find("InnerArea");

                    if (innerArea == null || !innerArea.TryGetComponent<Collider2D>(out Collider2D innerCollider))
                    {
                        Debug.LogError("MainObject doesn't have InnerArea Collider.");
                        Destroy(draggingObject);
                        return;
                    }

                    mainObjectPlaced = true;
                    currentPlacementArea = innerCollider;
                }
            }
            draggingObject = null;
        }
        else
        {
            Destroy(draggingObject);
            draggingObject = null;
        }

       


        isDragging = false;
    }
    Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float cameraDistance = mainCamera.transform.position.z;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraDistance));

        worldPosition.z = 0f;

        return worldPosition;
    }
}
