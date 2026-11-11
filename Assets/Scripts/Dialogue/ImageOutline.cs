
using UnityEngine;
using UnityEngine.UI;

public class ImageOutline : MonoBehaviour
{
    [SerializeField] private float outlineScale = 1.05f;
    [SerializeField] private Material outlineMaterial;

    private Image originalImage;
    private Image outlineImage;
    GameObject outlineObject;
    private bool setoutline = false;
    void Awake()
    {
        originalImage = GetComponent<Image>();

        // 외곽선용 오브젝트 생성
        outlineObject = new GameObject(
            "WhiteOutline",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        outlineObject.transform.SetParent(transform.parent, false);

        RectTransform originalRect = GetComponent<RectTransform>();
        RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();

        // 원본 RectTransform 복사
        outlineRect.anchorMin = originalRect.anchorMin;
        outlineRect.anchorMax = originalRect.anchorMax;
        outlineRect.pivot = originalRect.pivot;
        outlineRect.anchoredPosition = originalRect.anchoredPosition + new Vector2(43f, 0f);
        outlineRect.sizeDelta = originalRect.sizeDelta;
        outlineRect.localRotation = originalRect.localRotation;

        // 외곽선 크기 확대
        outlineRect.localScale = originalRect.localScale * outlineScale;

        // 흰색 실루엣 생성
        outlineImage = outlineObject.GetComponent<Image>();

        outlineImage.sprite = originalImage.sprite;
        outlineImage.color = Color.white;
        outlineImage.material = Instantiate(outlineMaterial);
        outlineImage.useSpriteMesh = true;
        outlineImage.preserveAspect = originalImage.preserveAspect;
        outlineImage.raycastTarget = false;
        outlineImage.maskable = originalImage.maskable;

        // 원본 이미지 뒤에 배치
        outlineObject.transform.SetSiblingIndex(
            transform.GetSiblingIndex()
        );
        outlineImage.gameObject.SetActive(false);
    }

    public void Show_Outline()
    {
        outlineImage.gameObject.SetActive(true);
    }

    public void Hide()
    {
        outlineImage.gameObject.SetActive(false);
    }
}
