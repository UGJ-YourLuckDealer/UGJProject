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

        GetBodyEndpoints(bodyArea, out Vector3 bodyEnd, out _);
        Vector3 headEnd = GetHeadPosition(headArea);
        Vector3 localBodyEnd = transform.InverseTransformPoint(bodyEnd);
        Vector3 localHeadEnd = transform.InverseTransformPoint(headEnd);
        Initialize(transform, localBodyEnd, transform, _sprite, _material, _chainCount, _chainWidth, _preserveAspect, _chainPadding);
        _endLocalPoint = localHeadEnd;
        return _mesh != null;
    }

    public static void GetBodyEndpoints(BoxCollider2D bodyArea, out Vector3 bottom, out Vector3 top)
    {
        Vector2 center = bodyArea.offset;
        bottom = bodyArea.transform.TransformPoint(center + Vector2.down * bodyArea.size.y * 0.5f);
        top = bodyArea.transform.TransformPoint(center + Vector2.up * bodyArea.size.y * 0.5f);
    }

    public static Vector3 GetHeadPosition(Collider2D headArea)
    {
        return headArea.transform.TransformPoint(headArea.offset);
    }

    void OnDrawGizmos()
    {
        Transform body = transform.Find("BodyArea");
        Transform head = transform.Find("HeadArea");
        if (body == null || head == null || !body.TryGetComponent<BoxCollider2D>(out var bodyArea) ||
            !head.TryGetComponent<Collider2D>(out var headArea))
            return;

        GetBodyEndpoints(bodyArea, out Vector3 bottom, out Vector3 top);
        Vector3 headCenter = GetHeadPosition(headArea);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.color = Color.green;
        Gizmos.matrix = body.localToWorldMatrix;
        Gizmos.DrawWireCube(bodyArea.offset, bodyArea.size);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawLine(bottom, top);

        Gizmos.color = Color.cyan;
        Vector3 renderStart = _initialized && _start == transform && _end == transform
            ? transform.TransformPoint(_startLocalPoint) : bottom;
        Vector3 renderEnd = _initialized && _start == transform && _end == transform
            ? transform.TransformPoint(_endLocalPoint) : headCenter;
        Gizmos.DrawLine(renderStart, renderEnd);
        Gizmos.DrawWireSphere(renderStart, 0.04f);
        Gizmos.DrawWireSphere(renderEnd, 0.04f);

        Gizmos.color = Color.yellow;
        Gizmos.matrix = head.localToWorldMatrix;
        if (headArea is BoxCollider2D headBox)
            Gizmos.DrawWireCube(headBox.offset, headBox.size);
        else if (headArea is CircleCollider2D headCircle)
            Gizmos.DrawWireSphere(headCircle.offset, headCircle.radius);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireSphere(headCenter, 0.07f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(top, "Charm axis (BodyArea)");
        UnityEditor.Handles.Label(headCenter, "Clasp snap (HeadArea)");
        UnityEditor.Handles.Label(renderStart, "Chain render start");
#endif
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
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

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ChainRenderer))]
public class ChainRendererEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var chain = (ChainRenderer)target;
        Transform body = chain.transform.Find("BodyArea");
        Transform head = chain.transform.Find("HeadArea");
        if (body == null || head == null || !body.TryGetComponent<BoxCollider2D>(out var bodyArea) ||
            !head.TryGetComponent<Collider2D>(out _))
            return;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("체인 길이 조절", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.HelpBox("위쪽과 잠금장치는 고정하고 BodyArea를 아래로 늘리거나 줄입니다. 단위는 BodyArea의 로컬 단위입니다.", UnityEditor.MessageType.Info);
        using (new UnityEditor.EditorGUI.DisabledScope(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var serializedBody = new UnityEditor.SerializedObject(bodyArea);
            serializedBody.Update();
            var size = serializedBody.FindProperty("m_Size");
            var offset = serializedBody.FindProperty("m_Offset");
            Vector2 oldSize = size.vector2Value;
            UnityEditor.EditorGUI.BeginChangeCheck();
            float length = UnityEditor.EditorGUILayout.DelayedFloatField("BodyArea 길이", oldSize.y);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                length = Mathf.Max(0.01f, length);
                Vector2 newOffset = offset.vector2Value;
                newOffset.y += (oldSize.y - length) * 0.5f;
                offset.vector2Value = newOffset;
                size.vector2Value = new Vector2(oldSize.x, length);
                serializedBody.ApplyModifiedProperties();
                UnityEditor.SceneView.RepaintAll();
            }
        }
    }
}
#endif
