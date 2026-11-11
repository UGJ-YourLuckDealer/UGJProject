using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectDragManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D initialplacementArea;

    /// <summary>
    /// 현재 배치된 체인을 추적하는 변수
    /// </summary>
    KeyringMaterialObject currentBatchedChain;
    private GameObject draggingObject;
    private bool isDragging;
    public bool IsDragging => isDragging;

    private bool mainObjectPlaced;
    private bool draggingMainObject;

    /// <summary>
    /// Keyring Material ID 부여용
    /// </summary>
    int _id;
    bool _isMainObject;
    Vector2 _mousePosition;
    Camera _mainCamera;
    Collider2D _bodyPlacementArea;
    Collider2D _headPlacementArea;

    void Awake()
    {
        _id = 0;
        // 카메라 캐싱
        _mainCamera = Camera.main;

        // 제작 시작 초기화
        _bodyPlacementArea = initialplacementArea;
        _headPlacementArea = initialplacementArea;
    }

    private void Update()
    {
        if (!isDragging)
            return;

        MoveObject();

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    public void StartDrag(GameObject prefab)
    {
        if (isDragging || prefab == null)
            return;

        Vector3 mousePosition = GetMouseWorldPosition();

        KeyringMaterialObject.KeyringMaterial data = LayerToKeyringMaterialData(prefab);

        // 이미 Keyring Chain이 배치되어 있으면 막는 플래그
        if (mainObjectPlaced && data.category == KeyringMaterialObject.KeyringMaterial.Category.Chain)
        {
            isDragging = false;
            Destroy(data.bindedObject);
            return;
        }

        draggingObject = data.bindedObject;
        draggingObject.transform.position = mousePosition;
        draggingObject.AddComponent<KeyringMaterialObject>().SetKeyringMaterialData(data);

        isDragging = true;
    }

    /// <summary>
    /// 기존 데이터를 바탕으로 새로운 KeyringMaterialObject를 생성합니다.
    /// </summary>
    KeyringMaterialObject.KeyringMaterial LayerToKeyringMaterialData(GameObject prefab)
    {
        _id++;
        var go = Instantiate(prefab);
        var category = KeyringMaterialObject.KeyringMaterial.Category.Chain;

        // Layer에 따라 카테코리 파싱 처리
        string layerToStr = LayerMask.LayerToName(prefab.layer);
        if (layerToStr.ToLower() == "main_kiring")
        {
            category = KeyringMaterialObject.KeyringMaterial.Category.Chain;
        }
        else if (layerToStr.ToLower() == "clasp")
        {
            category = KeyringMaterialObject.KeyringMaterial.Category.Clasp;
        }
        else if (layerToStr.ToLower() == "sub_acrill")
        {
            category = KeyringMaterialObject.KeyringMaterial.Category.Charm;
        }

        var data = new KeyringMaterialObject.KeyringMaterial(id: _id, category: category, bindedObject: go);

        switch (data.category)
        {
            case KeyringMaterialObject.KeyringMaterial.Category.Chain:
                if (prefab.name.ToLower().Contains("normal"))
                {
                    data.main = Receipt_Data.Main_Kiring.Normal;
                }
                else
                {
                    data.main = Receipt_Data.Main_Kiring.Heart;
                }
                break;
            case KeyringMaterialObject.KeyringMaterial.Category.Clasp:
                if (prefab.name.ToLower().Contains("a"))
                {
                    data.clasp = Receipt_Data.Clasp.A;
                }
                else
                {
                    data.clasp = Receipt_Data.Clasp.B;
                }
                break;
            case KeyringMaterialObject.KeyringMaterial.Category.Charm:
                if (prefab.name.ToLower().Contains("four"))
                {
                    data.special = Receipt_Data.Special.Four;
                }
                else if (prefab.name.ToLower().Contains("seven"))
                {
                    data.special = Receipt_Data.Special.Seven;
                }
                break;
        }

        return data;
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
        _mousePosition = GetMouseWorldPosition();

        bool canPlace;
        KeyringMaterialObject materialObj = draggingObject.GetComponent<KeyringMaterialObject>();

        if (!mainObjectPlaced)
            canPlace = initialplacementArea != null && initialplacementArea.OverlapPoint(_mousePosition);
        else if (materialObj.keyringMaterialData.category == KeyringMaterialObject.KeyringMaterial.Category.Charm)
            canPlace = _bodyPlacementArea != null && _bodyPlacementArea.OverlapPoint(_mousePosition);
        else if (materialObj.keyringMaterialData.category == KeyringMaterialObject.KeyringMaterial.Category.Clasp)
            canPlace = _headPlacementArea != null && _headPlacementArea.OverlapPoint(_mousePosition);
        else
        {
            Debug.Log(materialObj.keyringMaterialData);
            throw new Exception("Dragging Object가 유효하지 않음, 이런 일이 발생하면 안됨");
        }

        if (canPlace)
        {
            if (!mainObjectPlaced
                && materialObj.keyringMaterialData.category != KeyringMaterialObject.KeyringMaterial.Category.Chain)
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
                if (!mainObjectPlaced
                    && materialObj.keyringMaterialData.category == KeyringMaterialObject.KeyringMaterial.Category.Chain)
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
                    _bodyPlacementArea = bodyCollider;
                    _headPlacementArea = headCollider;

                    // 현재 배치된 키링을 추적하기
                    if (currentBatchedChain == null
                        && materialObj.keyringMaterialData.category == KeyringMaterialObject.KeyringMaterial.Category.Chain)
                    {
                        currentBatchedChain = materialObj;
                    }
                }

                if (check.GetCount(LayerMask.NameToLayer("Clasp"), "A") > 1 || check.GetCount(LayerMask.NameToLayer("Clasp"), "B") > 1)
                {
                    Debug.LogError("Clasp is not Exist over 1");
                    Destroy(draggingObject);
                    draggingObject = null;
                    isDragging = false;
                    return;

                }

                // 현재 배치된 키링 체인이 있다면...
                if (currentBatchedChain != null && currentBatchedChain != materialObj)
                {
                    // 나머지는 현재 배치된 체인의 자식으로 추가
                    currentBatchedChain.AddChild(materialObj.keyringMaterialData);
                }

                if (currentBatchedChain == materialObj)
                    materialObj.EnableDragging(this, check);

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

        float cameraDistance = _mainCamera.transform.position.z;

        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraDistance));

        worldPosition.z = 0f;

        return worldPosition;
    }
}
