using UnityEngine;
using UnityEngine.Rendering;

public class ChainRenderer : MonoBehaviour
{
    [SerializeField] Sprite _sprite;
    [SerializeField] Material _material;
    [SerializeField, Tooltip("렌더링할 체인 조각 수")] int _chainCount;
    [SerializeField, Tooltip("체인 조각의 목표 폭 (월드 단위)")] float _chainWidth;
    [SerializeField, Tooltip("체인 조각의 원본 가로세로 비율을 유지합니다.")] bool _preserveAspect = true;
    [SerializeField, Tooltip("인접한 조각 중심 사이에 추가할 간격 (월드 단위)")] float _chainPadding;
    [SerializeField, Tooltip("체인이 끝점을 초과할 때 조각과 간격을 함께 축소합니다.")] bool _fitWithinEndpoints = true;

    Transform _start;
    Transform _end;
    Vector3 _startLocalPoint;
    Vector3 _endLocalPoint;

    Mesh _mesh;
    MaterialPropertyBlock _properties;
    bool _initialized;

    public void Initialize(Transform start, Vector3 startLocalPoint, Transform end, Sprite sprite, Material material, int chainCount, float chainWidth, bool preserveAspect = true, float chainPadding = 0f)
    {
        ReleaseMesh();
        _start = start;
        _startLocalPoint = startLocalPoint;
        _end = end;
        _endLocalPoint = Vector3.zero;

        _sprite = sprite;
        _material = material;
        _chainCount = chainCount;
        _chainWidth = chainWidth;
        _preserveAspect = preserveAspect;
        _chainPadding = chainPadding;
        _initialized = true;

        if (_sprite == null || _material == null)
        {
            Debug.LogWarning("체인 렌더링에는 Sprite와 Material이 모두 필요합니다.", this);
            return;
        }

        Vector2[] vertices = _sprite.vertices;
        ushort[] triangles = _sprite.triangles;
        var meshVertices = new Vector3[vertices.Length];
        var colors = new Color[vertices.Length];
        var indices = new int[triangles.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            meshVertices[i] = vertices[i];
            colors[i] = Color.white;
        }
        for (int i = 0; i < triangles.Length; i++)
            indices[i] = triangles[i];

        _mesh = new Mesh { name = "Chain Sprite Mesh" };
        _mesh.vertices = meshVertices;
        _mesh.uv = _sprite.uv;
        _mesh.colors = colors;
        _mesh.triangles = indices;
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _properties = new MaterialPropertyBlock();
        _properties.SetTexture("_MainTex", _sprite.texture);
        _properties.SetTexture("_BaseMap", _sprite.texture);
    }

    public bool InitializeMainChain(BoxCollider2D bodyArea, Collider2D headArea)
    {
        if (bodyArea == null || headArea == null || _sprite == null || _material == null ||
            _chainCount <= 0 || _chainWidth <= 0f)
            return false;

        Vector3 bodyEnd = bodyArea.transform.TransformPoint(bodyArea.offset + Vector2.down * bodyArea.size.y * 0.5f);
        Vector3 headEnd = headArea.transform.TransformPoint(headArea.offset);
        Vector3 localBodyEnd = transform.InverseTransformPoint(bodyEnd);
        Vector3 localHeadEnd = transform.InverseTransformPoint(headEnd);
        Initialize(transform, localBodyEnd, transform, _sprite, _material, _chainCount, _chainWidth, _preserveAspect, _chainPadding);
        _endLocalPoint = localHeadEnd;
        return _mesh != null;
    }

    void LateUpdate()
    {
        if (!_initialized || _mesh == null || _material == null || _start == null || _end == null ||
            !_start.gameObject.activeInHierarchy || !_end.gameObject.activeInHierarchy ||
            _chainCount <= 0 || _chainWidth <= 0f)
            return;

        Vector3 spriteSize = _sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        Vector3 startPoint = _start.TransformPoint(_startLocalPoint);
        Vector3 endPoint = GetEndPoint();
        Vector3 offset = endPoint - startPoint;
        float length = offset.magnitude;
        if (length <= Mathf.Epsilon)
            return;

        int count = _chainCount;
        float widthScale = _chainWidth / spriteSize.x;
        Vector3 scale = _preserveAspect
            ? new Vector3(widthScale, widthScale, 1f)
            : new Vector3(widthScale, length / count / spriteSize.y, 1f);

        float spacing = Mathf.Max(0f, length / count + _chainPadding);
        float span = (count - 1) * spacing + spriteSize.y * scale.y;
        if (_fitWithinEndpoints && span > length)
        {
            float fit = length / span;
            scale.x *= fit;
            scale.y *= fit;
            spacing *= fit;
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, offset / length);
        Vector3 centerOffset = Vector3.Scale(_sprite.bounds.center, scale);
        var renderParams = new RenderParams(_material)
        {
            layer = gameObject.layer,
            matProps = _properties,
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };

        Vector3 midpoint = (startPoint + endPoint) * 0.5f;
        Vector3 direction = offset / length;
        for (int i = 0; i < count; i++)
        {
            Vector3 center = _start == _end
                ? midpoint + direction * ((i - (count - 1) * 0.5f) * spacing)
                : endPoint - direction * (spriteSize.y * scale.y * 0.5f + (count - 1 - i) * spacing);
            Matrix4x4 matrix = Matrix4x4.TRS(center - rotation * centerOffset, rotation, scale);
            Graphics.RenderMesh(renderParams, _mesh, 0, matrix);
        }
    }

    Vector3 GetEndPoint()
    {
        return _start == _end ? _end.TransformPoint(_endLocalPoint) : GetAttachmentPosition(_end);
    }

    public static Vector3 GetAttachmentPosition(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        Transform attachment = target.Find("ChainAttachment");
        if (attachment == null)
            attachment = target.Find("Top");
        if (attachment != null)
            return attachment.position;

        SpriteRenderer spriteRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return target.position;

        Bounds bounds = spriteRenderer.sprite.bounds;
        float centerX = spriteRenderer.flipX ? -bounds.center.x : bounds.center.x;
        float top = spriteRenderer.flipY ? -bounds.min.y : bounds.max.y;
        return spriteRenderer.transform.TransformPoint(new Vector3(centerX, top, bounds.center.z));
    }

    void OnDestroy()
    {
        ReleaseMesh();
    }

    void ReleaseMesh()
    {
        if (_mesh != null)
            Destroy(_mesh);
        _mesh = null;
        _properties = null;
    }
}
