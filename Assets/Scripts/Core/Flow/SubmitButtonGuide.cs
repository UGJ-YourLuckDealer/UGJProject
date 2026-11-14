using UnityEngine;
using UnityEngine.EventSystems;

public class SubmitButtonGuide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UnityEngine.UI.Button button;
    private ObjectDragManager manager;
    private GameObject guideText;
    private bool hovering;

    public void Initialize(UnityEngine.UI.Button submitButton)
    {
        button = submitButton;
        var guide = transform.Find("Guide Text");
        if (guide != null)
        {
            guideText = guide.gameObject;
            foreach (var graphic in guide.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.raycastTarget = false;
            guideText.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[SubmitButtonGuide] Submit Button에 Guide Text 자식이 없습니다.", this);
        }
        button.interactable = false;
    }

    private void Update()
    {
        if (button == null) return;
        if (manager == null) manager = FindAnyObjectByType<ObjectDragManager>();
        button.interactable = manager != null && manager.CurrentChain != null;
        UpdateGuide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        UpdateGuide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        UpdateGuide();
    }

    private void OnDisable()
    {
        hovering = false;
        if (guideText != null) guideText.SetActive(false);
    }

    private void UpdateGuide()
    {
        if (guideText != null && button != null)
        {
            bool visible = button.IsActive() && button.IsInteractable() && !hovering;
            if (guideText.activeSelf != visible) guideText.SetActive(visible);
        }
    }
}
