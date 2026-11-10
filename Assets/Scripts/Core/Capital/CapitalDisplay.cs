using TMPro;
using UnityEngine;

/// <summary>현재 자본 텍스트. GameEvents.CapitalChanged를 받아 갱신한다.</summary>
public class CapitalDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [Tooltip("숫자 표시 형식 (화폐 단위 없음, 소수점 2자리)")]
    [SerializeField] private string format = "0.00";

    private void OnEnable()
    {
        GameEvents.CapitalChanged += Show;
    }

    private void OnDisable()
    {
        GameEvents.CapitalChanged -= Show;
    }

    private void Start()
    {
        // 이 UI가 CapitalSystem보다 늦게 켜져 첫 신호를 놓친 경우 대비
        var capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null) Show(capital.Current);
    }

    private void Show(float capital)
    {
        if (moneyText != null) moneyText.text = capital.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }
}
