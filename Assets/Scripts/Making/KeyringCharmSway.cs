using System.Collections.Generic;
using UnityEngine;

/// <summary>체인의 연결점에 물리 부적을 매답니다.</summary>
public class KeyringCharmSway : MonoBehaviour
{
    private static readonly HashSet<Collider2D> charmColliders = new();
    private readonly List<(Collider2D, Collider2D)> ignoredPairs = new();
    private Transform chain;
    private KeyringMaterialObject owner;
    private ObjectDragManager dragManager;
    private Rigidbody2D body;
    private bool createdBody;
    private RigidbodyType2D previousBodyType;
    private RigidbodyConstraints2D previousConstraints;
    private bool previousSimulated;
    private float previousGravityScale;
    private float previousLinearDamping;
    private float previousAngularDamping;
    private HingeJoint2D joint;
    private CircleCollider2D physicalCollider;
    private PhysicsMaterial2D collisionMaterial;
    private Collider2D[] originalColliders;
    private Vector3 localAttachment;
    private Vector2 localCharmAnchor;
    private bool hasAttachment;
    private bool attached;

    public void Initialize(Transform chainTransform, int id)
    {
        if (attached)
            DeactivatePhysics();
        hasAttachment = false;
        chain = chainTransform;
        owner = chain != null ? chain.GetComponent<KeyringMaterialObject>() : null;
        transform.SetParent(null, true);
        enabled = true;
    }

    public void SetDebugAttachment(ObjectDragManager manager, Vector3 worldAttachment)
    {
        if (chain == null || attached)
            return;

        dragManager = manager;
        localAttachment = chain.InverseTransformPoint(worldAttachment);
        localCharmAnchor = transform.InverseTransformPoint(worldAttachment);
        hasAttachment = true;
        if (isActiveAndEnabled)
            ActivatePhysics();
    }

    internal void Detach()
    {
        hasAttachment = false;
        chain = null;
        owner = null;
        enabled = false;
    }

    private void OnEnable()
    {
        if (!hasAttachment)
            return;
        if (chain == null)
        {
            Destroy(gameObject);
            return;
        }
        ActivatePhysics();
    }

    private void ActivatePhysics()
    {
        Rigidbody2D chainBody = chain.GetComponent<Rigidbody2D>();
        if (chainBody == null)
        {
            Debug.LogError("부적을 연결하려면 체인에 Rigidbody2D가 필요합니다.", this);
            return;
        }

        if (body == null)
        {
            originalColliders = GetComponentsInChildren<Collider2D>(true);
            body = GetComponent<Rigidbody2D>();
            createdBody = body == null;
            if (createdBody)
                body = gameObject.AddComponent<Rigidbody2D>();
        }
        previousBodyType = body.bodyType;
        previousConstraints = body.constraints;
        previousSimulated = body.simulated;
        previousGravityScale = body.gravityScale;
        previousLinearDamping = body.linearDamping;
        previousAngularDamping = body.angularDamping;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.constraints = RigidbodyConstraints2D.None;
        body.simulated = true;
        body.gravityScale = 0f;
        body.linearDamping = dragManager != null ? dragManager.CharmLinearDamping : 0.5f;
        body.angularDamping = dragManager != null ? dragManager.CharmAngularDamping : 1f;

        if (physicalCollider == null)
        {
            physicalCollider = gameObject.AddComponent<CircleCollider2D>();
            Bounds bounds = new Bounds(transform.position, Vector3.one * 0.2f);
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
            if (sprites.Length > 0)
            {
                bounds = sprites[0].bounds;
                for (int i = 1; i < sprites.Length; i++)
                    bounds.Encapsulate(sprites[i].bounds);
            }
            physicalCollider.offset = transform.InverseTransformPoint(bounds.center);
            float scale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
            physicalCollider.radius = Mathf.Max(0.01f, Mathf.Min(bounds.extents.x, bounds.extents.y) * 0.5f / scale);
            collisionMaterial = new PhysicsMaterial2D("Charm collision") { friction = 0f, bounciness = 0.05f };
            physicalCollider.sharedMaterial = collisionMaterial;
        }
        physicalCollider.enabled = true;
        charmColliders.Add(physicalCollider);

        if (joint == null)
            joint = gameObject.AddComponent<HingeJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = chainBody;
        joint.anchor = localCharmAnchor;
        joint.connectedAnchor = localAttachment;
        joint.enableCollision = false;
        joint.enabled = true;
        attached = true;
        UpdateCollisionIsolation();
    }

    private void FixedUpdate()
    {
        if (!attached)
            return;
        if (chain == null)
        {
            Destroy(gameObject);
            return;
        }

        // 프로젝트 전역 중력 설정과 무관하게 화면 아래로 당깁니다.
        float gravity = dragManager != null ? dragManager.CharmGravity : 9.81f;
        if (dragManager != null)
        {
            body.linearDamping = dragManager.CharmLinearDamping;
            body.angularDamping = dragManager.CharmAngularDamping;
        }
        body.AddForce(Vector2.down * (gravity * body.mass));
        UpdateCollisionIsolation();
    }

    private void UpdateCollisionIsolation()
    {
        foreach (Collider2D other in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
                continue;

            if (other != physicalCollider && !charmColliders.Contains(other))
                IgnorePair(physicalCollider, other);

            if (other.isTrigger || charmColliders.Contains(other))
                continue;
            foreach (Collider2D original in originalColliders)
            {
                if (original != null && original.enabled && !original.isTrigger && original != other)
                    IgnorePair(original, other);
            }
        }
    }

    private void IgnorePair(Collider2D first, Collider2D second)
    {
        if (first == null || second == null || !first.enabled || !first.gameObject.activeInHierarchy ||
            Physics2D.GetIgnoreCollision(first, second))
            return;
        Physics2D.IgnoreCollision(first, second);
        ignoredPairs.Add((first, second));
    }

    private void LateUpdate()
    {
        if (attached && chain != null && dragManager != null && dragManager.ShowCharmAttachmentDebug)
            Debug.DrawLine(chain.TransformPoint(localAttachment), transform.position, Color.cyan, 0f, false);
    }

    private void OnDisable()
    {
        DeactivatePhysics();
    }

    private void DeactivatePhysics()
    {
        if (!attached)
            return;

        attached = false;
        charmColliders.Remove(physicalCollider);
        foreach (var pair in ignoredPairs)
        {
            if (pair.Item1 != null && pair.Item2 != null && pair.Item1.enabled && pair.Item2.enabled &&
                pair.Item1.gameObject.activeInHierarchy && pair.Item2.gameObject.activeInHierarchy)
                Physics2D.IgnoreCollision(pair.Item1, pair.Item2, false);
        }
        ignoredPairs.Clear();
        if (joint != null)
            joint.enabled = false;
        if (physicalCollider != null)
            physicalCollider.enabled = false;
        if (body != null)
        {
            if (createdBody)
                body.simulated = false;
            else
            {
                body.bodyType = previousBodyType;
                body.constraints = previousConstraints;
                body.simulated = previousSimulated;
                body.gravityScale = previousGravityScale;
                body.linearDamping = previousLinearDamping;
                body.angularDamping = previousAngularDamping;
            }
        }
    }

    private void OnDestroy()
    {
        if (joint != null) Destroy(joint);
        if (physicalCollider != null) Destroy(physicalCollider);
        if (collisionMaterial != null) Destroy(collisionMaterial);
        if (createdBody && body != null) Destroy(body);
        if (owner != null && TryGetComponent<KeyringMaterialObject>(out var material))
            owner.ForgetChild(material.keyringMaterialData.id);
    }
}
