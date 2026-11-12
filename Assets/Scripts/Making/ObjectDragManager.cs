using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectDragManager : MonoBehaviour
{
    public float CharmGravity => Mathf.Max(0f, charmGravity);
    public float CharmLinearDamping => Mathf.Max(0f, charmLinearDamping);
    public float CharmAngularDamping => Mathf.Max(0f, charmAngularDamping);
    public float CharmCollisionRadiusFactor => Mathf.Clamp(charmCollisionRadiusFactor, 0.1f, 1f);

    [Header("References")]
    [SerializeField] private Collider2D initialplacementArea;

    [Header("Charm Attachment (World Units)")]
    [SerializeField, Min(0.01f)] private float charmAttachmentSpacing = 0.5f;
    [SerializeField, Min(0f)] private float charmMinimumDistance = 5f;

    [Header("Clasp Placement (World Units)")]
    [SerializeField, Min(0f), Tooltip("잠금장치 또는 커서가 체인 상단에 얼마나 가까우면 배치를 허용할지 정합니다.")]
    private float claspPlacementTolerance = 0.35f;

    [Header("Attachment Chain Visual")]
    [SerializeField] private Sprite chainLinkSprite;
    [SerializeField] private Material chainLinkMaterial;
    [SerializeField, Min(1)] private int chainLinkCount = 5;
    [SerializeField, Min(0.001f)] private float chainLinkWidth = 0.15f;
    [SerializeField] private bool preserveChainLinkAspect = true;
    [SerializeField, Tooltip("인접한 체인 조각 중심 사이에 추가할 간격 (월드 단위)")]
    private float chainLinkPadding;
    private bool warnedMissingChainSprite;

    [Header("Charm Physics")]
    [SerializeField, Min(0f)] private float charmGravity = 9.81f;
    [SerializeField, Min(0f)] private float charmLinearDamping = 0.5f;
    [SerializeField, Min(0f)] private float charmAngularDamping = 1f;
    [SerializeField, Range(0.1f, 1f), Tooltip("부적 크기에 대한 충돌 반경 비율")]
    private float charmCollisionRadiusFactor = 0.8f;

    [Header("Charm Collision SFX")]
    [SerializeField] private string charmCollisionSfx = "sfx_place_charm";
    [SerializeField, Min(0f)] private float charmCollisionMinSpeed = 0.5f;
    [SerializeField, Min(0f)] private float charmCollisionSfxCooldown = 0.15f;
    [SerializeField, Min(1)] private int charmCollisionMaxVoices = 2;
    private float nextCharmCollisionSfxTime;

    [SerializeField] private bool showCharmAttachmentDebug = true;
    public bool ShowCharmAttachmentDebug => showCharmAttachmentDebug;

    /// <summary>
    /// 현재 배치된 체인을 추적하는 변수
    /// </summary>
    KeyringMaterialObject currentBatchedChain;
    public KeyringMaterialObject CurrentChain => currentBatchedChain;
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

    public void PlayCharmCollisionSfx(float speed)
    {
        if (speed < Mathf.Max(0f, charmCollisionMinSpeed) || Time.unscaledTime < nextCharmCollisionSfxTime ||
            string.IsNullOrEmpty(charmCollisionSfx))
            return;

        if (AudioService.Instance.TryPlaySfx(charmCollisionSfx, Mathf.Max(1, charmCollisionMaxVoices), "charm_collision"))
            nextCharmCollisionSfxTime = Time.unscaledTime + Mathf.Max(0f, charmCollisionSfxCooldown);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (chainLinkSprite == null)
            chainLinkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/desk/Item_b_chain_01.png");
        if (chainLinkMaterial == null)
            chainLinkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chain_Element.mat");
    }
#endif

    void Awake()
    {
#if UNITY_EDITOR
        if (chainLinkSprite == null)
            chainLinkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/desk/Item_b_chain_01.png");
        if (chainLinkMaterial == null)
            chainLinkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chain_Element.mat");
#endif
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

        if (data.category == KeyringMaterialObject.KeyringMaterial.Category.Chain)
            InitializeMainChainVisual(draggingObject);

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
                if (prefab.CompareTag("Normal"))
                {
                    data.main = Receipt_Data.Main_Kiring.Normal;
                }
                else if (prefab.CompareTag("Heart"))
                {
                    data.main = Receipt_Data.Main_Kiring.Heart;
                }
                break;
            case KeyringMaterialObject.KeyringMaterial.Category.Clasp:
                if (prefab.CompareTag("A"))
                {
                    data.clasp = Receipt_Data.Clasp.A;
                }
                else if (prefab.CompareTag("B"))
                {
                    data.clasp = Receipt_Data.Clasp.B;
                }
                break;
            case KeyringMaterialObject.KeyringMaterial.Category.Charm:
                if (prefab.CompareTag("Clover"))
                    data.charmType = KeyringMaterialObject.KeyringMaterial.CharmType.Clover;
                else if (prefab.CompareTag("Note"))
                    data.charmType = KeyringMaterialObject.KeyringMaterial.CharmType.Note;
                else if (prefab.CompareTag("Rainbow"))
                    data.charmType = KeyringMaterialObject.KeyringMaterial.CharmType.Rainbow;
                else if (prefab.CompareTag("Number"))
                    data.charmType = KeyringMaterialObject.KeyringMaterial.CharmType.Number;

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
            canPlace = CanPlaceClasp(draggingObject.transform, _mousePosition, _headPlacementArea);
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
                    bool isCharm = materialObj.keyringMaterialData.category == KeyringMaterialObject.KeyringMaterial.Category.Charm;
                    Vector3 attachment = isCharm
                        ? FindCharmAttachment(draggingObject.transform, _bodyPlacementArea)
                        : Vector3.zero;
                    if (materialObj.keyringMaterialData.category == KeyringMaterialObject.KeyringMaterial.Category.Clasp)
                        PlaceClaspAtChainTop(draggingObject.transform, _headPlacementArea);

                    // 나머지는 현재 배치된 체인의 자식으로 추가
                    currentBatchedChain.AddChild(materialObj.keyringMaterialData);
                    if (isCharm)
                    {
                        AudioService.Instance.PlaySfx("sfx_place_charm");
                        draggingObject.GetComponent<KeyringCharmSway>().SetDebugAttachment(this, attachment);
                        AddAttachmentChain(draggingObject, attachment);
                    }
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

    private void InitializeMainChainVisual(GameObject chain)
    {
        var renderer = chain.GetComponent<ChainRenderer>();
        Transform body = chain.transform.Find("BodyArea");
        Transform head = chain.transform.Find("HeadArea");
        if (renderer == null || body == null || head == null ||
            !body.TryGetComponent<BoxCollider2D>(out var bodyArea) ||
            !head.TryGetComponent<Collider2D>(out var headArea))
            return;

        if (renderer.InitializeMainChain(bodyArea, headArea) &&
            chain.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer.enabled = false;
    }

    private bool CanPlaceClasp(Transform clasp, Vector2 cursorPosition, Collider2D headArea)
    {
        if (headArea == null || !headArea.enabled || !headArea.gameObject.activeInHierarchy)
            return false;

        Physics2D.SyncTransforms();
        float tolerance = Mathf.Max(0f, claspPlacementTolerance);
        Vector2 bottom = GetClaspBottom(clasp);
        return (headArea.ClosestPoint(cursorPosition) - cursorPosition).sqrMagnitude <= tolerance * tolerance ||
               (headArea.ClosestPoint(bottom) - bottom).sqrMagnitude <= tolerance * tolerance;
    }

    private void PlaceClaspAtChainTop(Transform clasp, Collider2D headArea)
    {
        Vector3 attachment = headArea.transform.TransformPoint(headArea.offset);
        Vector2 offset = (Vector2)(attachment - GetClaspBottom(clasp));
        clasp.position += new Vector3(offset.x, offset.y, 0f);
    }

    private static Vector3 GetClaspBottom(Transform clasp)
    {
        Vector3 claspBottom = clasp.position;
        Transform bottom = clasp.Find("Bottom");
        if (bottom != null)
        {
            claspBottom = bottom.position;
        }
        else if (clasp.TryGetComponent<SpriteRenderer>(out var spriteRenderer) && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.sprite.bounds;
            float bottomY = spriteRenderer.flipY ? -bounds.max.y : bounds.min.y;
            float centerX = spriteRenderer.flipX ? -bounds.center.x : bounds.center.x;
            claspBottom = spriteRenderer.transform.TransformPoint(new Vector3(centerX, bottomY, bounds.center.z));
        }

        return claspBottom;
    }

    private void AddAttachmentChain(GameObject target, Vector3 worldAttachment)
    {
        if (chainLinkSprite == null)
        {
            if (!warnedMissingChainSprite)
            {
                Debug.LogWarning("체인 연결을 표시하려면 ObjectDragManager의 Chain Link Sprite를 지정하세요.", this);
                warnedMissingChainSprite = true;
            }
            return;
        }

        var renderer = target.GetComponent<ChainRenderer>();
        if (renderer == null)
            renderer = target.AddComponent<ChainRenderer>();
        Transform chain = currentBatchedChain.transform;
        renderer.Initialize(chain, chain.InverseTransformPoint(worldAttachment), target.transform,
            chainLinkSprite, chainLinkMaterial, Mathf.Max(1, chainLinkCount), Mathf.Max(0.001f, chainLinkWidth),
            preserveChainLinkAspect, chainLinkPadding);
    }

    private Vector3 FindCharmAttachment(Transform charm, Collider2D bodyArea)
    {
        Vector3 start;
        Vector3 end;
        if (bodyArea is BoxCollider2D box)
        {
            Vector2 center = box.offset;
            start = box.transform.TransformPoint(center + Vector2.down * box.size.y * 0.5f);
            end = box.transform.TransformPoint(center + Vector2.up * box.size.y * 0.5f);
        }
        else
        {
            Bounds bounds = bodyArea.bounds;
            start = new Vector3(bounds.center.x, bounds.min.y, charm.position.z);
            end = new Vector3(bounds.center.x, bounds.max.y, charm.position.z);
        }

        Vector2 axis = (Vector2)(end - start);
        float length = axis.magnitude;
        Vector2 direction = length > 0f ? axis / length : Vector2.up;
        float projected = Vector2.Dot((Vector2)(charm.position - start), direction);
        int steps = Mathf.Max(1, Mathf.RoundToInt(length / Mathf.Max(0.01f, charmAttachmentSpacing)));
        float stepLength = length / steps;
        int index = stepLength > 0f ? Mathf.Clamp(Mathf.RoundToInt(projected / stepLength), 0, steps) : 0;
        Vector2 attachment = (Vector2)start + direction * (stepLength * index);
        Vector2 offset = (Vector2)charm.position - attachment;
        float distance = offset.magnitude;
        if (distance < charmMinimumDistance)
        {
            Vector2 outward = distance > 0.000001f ? offset / distance : new Vector2(direction.y, -direction.x);
            Vector2 position = attachment + outward * charmMinimumDistance;
            charm.position = new Vector3(position.x, position.y, charm.position.z);
        }

        return new Vector3(attachment.x, attachment.y, charm.position.z);
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
