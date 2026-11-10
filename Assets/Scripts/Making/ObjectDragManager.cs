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

                // 재료를 내려놓으면 재료비 차감 (기획: 모든 재료 $1) - 자본 시스템 연결
                CapitalSystem capital = FindAnyObjectByType<CapitalSystem>();
                if (capital != null)
                {
                    float before = capital.Current;
                    capital.Add(-1f);
                    Debug.Log($"[재료비 차감] {before:0.00} → {capital.Current:0.00} (-1.00)");
                }
                else
                {
                    Debug.LogWarning("[재료비 차감] 씬에 CapitalSystem이 없습니다. GameSystems 프리팹을 배치해 주세요.");
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
