using System;

using SaintsField;
using UnityEngine;
using UnityEngine.InputSystem;


public class KeyringMaterialObject : MonoBehaviour
{
    [Serializable]
    public struct KeyringMaterial
    {
        public enum Category
        {
            [InspectorName("체인")]
            Chain,
            [InspectorName("잠금장치")]
            Clasp,
            [InspectorName("부적")]
            Charm
        }

        public enum CharmType
        {
            Unknown,
            Clover,
            Note,
            Rainbow,
            Number
        }

        public readonly int id;
        public Category category;
        public CharmType charmType;

        // TODO: 호환성 유지를 위해 이후 string으로 대체 바람
        public Receipt_Data.Main_Kiring? main;
        public Receipt_Data.Clasp? clasp;
        public Receipt_Data.Special? special;
        public GameObject? bindedObject;

        public KeyringMaterial(
            int id,
            Category category,
            GameObject? bindedObject = null,
            Receipt_Data.Main_Kiring? main = null,
            Receipt_Data.Clasp? clasp = null,
            Receipt_Data.Special? special = null)
        {
            this.id = id;
            this.category = category;
            this.charmType = CharmType.Unknown;
            this.bindedObject = bindedObject;
            this.main = main;
            this.clasp = clasp;
            this.special = special;
        }

        public override string ToString()
        {
            return $"KeyringMaterial(id={id}, category={category}, main={main}, clasp={clasp}, special={special})";
        }
    }

    public KeyringMaterial keyringMaterialData;
    public SaintsDictionary<int, KeyringMaterial> childs = new();

    public void SetKeyringMaterialData(KeyringMaterial data)
    {
        keyringMaterialData = data;
    }

    public void AddChild(KeyringMaterial child)
    {
        childs.Add(child.id, child);
        if (child.category != KeyringMaterial.Category.Charm)
        {
            if (child.category == KeyringMaterial.Category.Clasp &&
                child.bindedObject.TryGetComponent<Rigidbody2D>(out var claspBody))
            {
                claspBody.simulated = false;
                Destroy(claspBody);
            }
            child.bindedObject.transform.SetParent(transform);
        }

        if (child.category == KeyringMaterial.Category.Charm)
        {
            var sway = child.bindedObject.GetComponent<KeyringCharmSway>();
            if (sway == null)
                sway = child.bindedObject.AddComponent<KeyringCharmSway>();
            sway.Initialize(transform, child.id);
        }
    }

    public void RemoveChild(KeyringMaterial child)
    {
        childs.Remove(child.id);
        if (child.bindedObject == null)
            return;
        if (child.bindedObject.TryGetComponent<KeyringCharmSway>(out var sway))
            sway.Detach();
        if (child.bindedObject.TryGetComponent<ChainRenderer>(out var renderer))
            Destroy(renderer);
        child.bindedObject.transform.SetParent(null);
    }

    internal void ForgetChild(int id)
    {
        childs.Remove(id);
    }

    private Rigidbody2D chainBody;
    private Vector2 dragTarget;
    private bool hasDragTarget;
    private bool canDrag;
    private bool isDragging;
    private Vector3 dragOffset;
    private ObjectDragManager dragManager;
    private Collider2D boardArea;

    public void EnableDragging(ObjectDragManager manager, Check_Board board)
    {
        dragManager = manager;
        chainBody = GetComponent<Rigidbody2D>();
        if (chainBody == null)
            chainBody = gameObject.AddComponent<Rigidbody2D>();
        chainBody.bodyType = RigidbodyType2D.Kinematic;
        chainBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        boardArea = board != null ? board.BoardArea : null;
        canDrag = boardArea != null;
        if (!canDrag)
            Debug.LogError("체인을 이동하려면 Check_Board의 작업대 Collider2D가 필요합니다.", this);
    }

    private void Update()
    {
        if (!canDrag || keyringMaterialData.category != KeyringMaterial.Category.Chain)
            return;

        var mouse = Mouse.current;
        var camera = Camera.main;
        if (mouse == null || camera == null)
        {
            isDragging = false;
            return;
        }

        if (dragManager != null && dragManager.IsDragging)
        {
            isDragging = false;
            return;
        }

        Vector3 screenPosition = mouse.position.ReadValue();
        screenPosition.z = camera.WorldToScreenPoint(transform.position).z;
        Vector3 worldPosition = camera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = transform.position.z;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPosition))
            {
                if (hit.gameObject != gameObject)
                    continue;

                isDragging = true;
                dragOffset = transform.position - worldPosition;
                break;
            }
        }

        if (isDragging && mouse.leftButton.isPressed && boardArea != null && boardArea.enabled && boardArea.gameObject.activeInHierarchy)
        {
            Vector3 targetPosition = worldPosition + dragOffset;
            Vector2 constrainedPosition = boardArea.ClosestPoint(targetPosition);
            dragTarget = constrainedPosition;
            hasDragTarget = true;
        }
        else if (isDragging)
            isDragging = false;
    }

    private void FixedUpdate()
    {
        if (hasDragTarget && chainBody != null)
        {
            chainBody.MovePosition(dragTarget);
            hasDragTarget = false;
        }
    }

    private void OnDisable()
    {
        isDragging = false;
        hasDragTarget = false;
    }

    private void OnDestroy()
    {
        if (keyringMaterialData.category != KeyringMaterial.Category.Chain)
            return;

        foreach (var entry in childs)
        {
            var child = entry.Value;
            if (child.category == KeyringMaterial.Category.Charm && child.bindedObject != null)
                Destroy(child.bindedObject);
        }
        childs.Clear();
    }
}