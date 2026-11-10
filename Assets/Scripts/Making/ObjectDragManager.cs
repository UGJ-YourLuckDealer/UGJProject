using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectDragManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D initialplacementArea;
    [SerializeField] private Collider2D bodyPlacementArea;
    [SerializeField] private Collider2D headPlacementArea;

    private GameObject draggingObject;
    private bool isDragging;

    private bool mainObjectPlaced;
    private bool draggingMainObject;

    bool isMainObject;

    Vector2 mousePosition;
    private void Awake()
    {
        mainCamera = Camera.main;

        bodyPlacementArea = initialplacementArea;
        headPlacementArea = initialplacementArea;
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
        Customer_Data data = new Customer_Data();

        Check_Board check = FindAnyObjectByType<Check_Board>();
        mousePosition = GetMouseWorldPosition();

        bool canPlace;

        if (!mainObjectPlaced)
            canPlace = initialplacementArea != null && initialplacementArea.OverlapPoint(mousePosition);
        else if(draggingObject.layer == LayerMask.NameToLayer("Sub_Acrill"))
            canPlace = bodyPlacementArea != null && bodyPlacementArea.OverlapPoint(mousePosition);
        else
            canPlace = headPlacementArea != null && headPlacementArea.OverlapPoint(mousePosition);

        if (canPlace)
        {
            if (!mainObjectPlaced && draggingObject.layer != LayerMask.NameToLayer("Main_Kiring"))
            {
                Debug.LogError("Main_Kiring is not exist.");
                Destroy(draggingObject);
                isDragging = false;
                draggingObject = null;
                return;
            }

            if (check.GetCount(LayerMask.NameToLayer("Main_Kiring"), "Heart") > 1 ||
                check.GetCount(LayerMask.NameToLayer("Main_Kiring"), "Normal") > 1)
            {
                Destroy(draggingObject);
                Debug.LogError("Main_Kiring is not exist over 1");
                draggingObject = null;
            }
            else
            {
                if (!mainObjectPlaced && draggingObject.layer == LayerMask.NameToLayer("Main_Kiring"))
                {
                    Transform bodyArea = draggingObject.transform.Find("BodyArea");

                    if (bodyArea == null || !bodyArea.TryGetComponent<Collider2D>(out Collider2D bodyCollider))
                    {
                        Debug.LogError("MainObject doesn't have InnerArea Collider.");
                        Destroy(draggingObject);
                        isDragging = false;
                        draggingObject = null;
                        return;
                    }
                    

                    Transform headArea = draggingObject.transform.Find("HeadArea");

                    if (headArea == null || !headArea.TryGetComponent<Collider2D>(out Collider2D headCollider))
                    {
                        Debug.LogError("MainObject doesn't have InnerArea Collider.");
                        Destroy(draggingObject);
                        isDragging = false;
                        draggingObject = null;
                        return;
                    }

                    mainObjectPlaced = true;
                    bodyPlacementArea = bodyCollider;
                    headPlacementArea = headCollider;
                }

                if (check.GetCount(LayerMask.NameToLayer("Clasp"), "A") > 1 || check.GetCount(LayerMask.NameToLayer("Clasp"), "B") > 1)
                {
                    Debug.LogError("Clasp is not Exist over 1");
                    Destroy(draggingObject);
                    draggingObject = null;
                    isDragging = false;
                    return;

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
            data.Decrease_Account(draggingObject);
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
