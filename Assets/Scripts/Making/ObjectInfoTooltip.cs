using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
public class ObjectInfoTooltip : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text accountText;



    [Header("Settings")]
    [SerializeField] private float offset = 20f;

    private Canvas canvas;

    Customer_Data data;

    private void Awake()
    {
        data = new Customer_Data();
        canvas = GetComponent<Canvas>();

        tooltipPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!tooltipPanel.gameObject.activeSelf)
            return;

        if (Mouse.current == null)
            return;

        UpdatePosition();
    }

    public void Show(GameObject target)
    {
        int account = data.Get_Account(target);
        


        nameText.text = "Name: " + target.tag;
        typeText.text = "Category: " + LayerMask.LayerToName(target.layer);
        priceText.text = "Sell Price: 1$";
        accountText.text = "Account: "+ account;

        tooltipPanel.gameObject.SetActive(true);

        UpdatePosition();
    }
    public void Hide()
    {
        tooltipPanel.gameObject.SetActive(false);
    }
    void UpdatePosition()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Camera uiCamera = null;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosition, uiCamera, out Vector2 localPosition);

        if(mousePosition.x > Screen.width/2f)
        {
            tooltipPanel.pivot = new Vector2(1f, 0f);
            localPosition.x += -offset;
        }
        else
        {
            tooltipPanel.pivot = new Vector2(0f, 0f);
            localPosition.x += offset;
        }

        if(mousePosition.y > Screen.height/2f)
        {
            tooltipPanel.pivot = new Vector2(tooltipPanel.pivot.x, 1f);
            localPosition.y -= offset;
        }
        else
        {
            tooltipPanel.pivot = new Vector2(tooltipPanel.pivot.x, 0f);
            localPosition.y += offset;
        }



            tooltipPanel.anchoredPosition = localPosition;
    }
}
