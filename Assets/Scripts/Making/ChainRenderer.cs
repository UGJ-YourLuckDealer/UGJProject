using UnityEngine;
using UnityEngine.Rendering;

public class ChainRenderer : MonoBehaviour
{
    [SerializeField] Sprite _sprite;
    [SerializeField] Material _material;
    [SerializeField] int _chainCount;
    [SerializeField] float _chainWidth;

    Transform _start;
    Transform _end;
    Vector3 _startLocalPoint;
    Vector3 _endLocalPoint;
    Mesh _mesh;
    MaterialPropertyBlock _properties;
    bool _initialized;

    public void Initialize(Transform start, Vector3 startLocalPoint, Transform end, Sprite sprite, Material material, int chainCount, float chainWidth)
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
        Initialize(transform, localBodyEnd, transform, _sprite, _material, _chainCount, _chainWidth);
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
        Vector3 offset = _end.TransformPoint(_endLocalPoint) - startPoint;
        float length = offset.magnitude;
        if (length <= Mathf.Epsilon)
            return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, offset / length);
        Vector3 scale = new Vector3(_chainWidth / spriteSize.x, length / _chainCount / spriteSize.y, 1f);
        Vector3 centerOffset = Vector3.Scale(_sprite.bounds.center, scale);
        var renderParams = new RenderParams(_material)
        {
            layer = gameObject.layer,
            matProps = _properties,
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };

        for (int i = 0; i < _chainCount; i++)
        {
            Vector3 center = startPoint + offset * ((i + 0.5f) / _chainCount);
            Matrix4x4 matrix = Matrix4x4.TRS(center - rotation * centerOffset, rotation, scale);
            Graphics.RenderMesh(renderParams, _mesh, 0, matrix);
        }
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
