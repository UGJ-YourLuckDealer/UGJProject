using UnityEngine;

/// <summary>
/// 월드 오브젝트를 카메라 화면 기준 위치(Viewport 좌표 0~1)에 고정한다.
/// Camera.ViewportToWorldPoint로 위치를 계산하므로 화면 비율(16:9, 4:3 등)이 달라도 화면 안에 보인다.
/// 화면 크기가 바뀌면 다시 계산한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ViewportAnchor : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    [Tooltip("화면 기준 위치. (0,0) = 왼쪽 아래, (1,1) = 오른쪽 위")]
    [SerializeField] private Vector2 viewportPosition = new Vector2(0.5f, 0.5f);

    [Tooltip("계산된 위치에 더할 월드 좌표 오프셋")]
    [SerializeField] private Vector2 worldOffset;

    [Tooltip("오브젝트 크기(Renderer 범위)까지 고려해 화면 밖으로 나가지 않게 한다")]
    [SerializeField] private bool keepInsideScreen = true;

    [Tooltip("화면 가장자리와의 최소 간격 (Viewport 비율, 0.02 = 화면 높이의 2%)")]
    [SerializeField, Range(0f, 0.2f)] private float screenMargin = 0.02f;

    private int lastWidth;
    private int lastHeight;

    public Vector2 ViewportPosition
    {
        get => viewportPosition;
        set { viewportPosition = value; Apply(); }
    }

    private void Start() => Apply();

    private void LateUpdate()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
    }

    public void Apply()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        float depth = transform.position.z - cam.transform.position.z;
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, depth));
        world.x += worldOffset.x;
        world.y += worldOffset.y;
        world.z = transform.position.z;

        if (keepInsideScreen)
        {
            Vector3 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            float margin = (max.y - min.y) * screenMargin;
            Vector2 ext = HalfSize();
            world.x = ClampCenter(world.x, min.x + margin + ext.x, max.x - margin - ext.x);
            world.y = ClampCenter(world.y, min.y + margin + ext.y, max.y - margin - ext.y);
        }

        transform.position = world;

        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    // 자식 포함 Renderer 범위의 절반 크기 (외곽선 등 꺼진 자식은 제외)
    private Vector2 HalfSize()
    {
        bool found = false;
        Bounds b = default;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }
        return found ? (Vector2)b.extents : Vector2.zero;
    }

    // 범위가 화면보다 크면 가운데로
    private static float ClampCenter(float v, float lo, float hi)
    {
        return lo > hi ? (lo + hi) * 0.5f : Mathf.Clamp(v, lo, hi);
    }
}
