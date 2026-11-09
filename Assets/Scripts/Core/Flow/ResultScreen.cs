using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>Result 씬: 마지막 게임 결과(승패·남은 자본·처리한 손님 수)를 표시한다.</summary>
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;

    private void Start()
    {
        var result = SceneFlow.LastResult;
        if (result == null)
        {
            if (titleText != null) titleText.text = "No Result";
            if (detailText != null) detailText.text = "Start from the Title scene.";
            return;
        }

        var r = result.Value;
        if (titleText != null) titleText.text = r.isWin ? "WIN" : "LOSE";
        if (detailText != null)
            detailText.text = $"Money  {r.capital.ToString("0.00", CultureInfo.InvariantCulture)}\nCustomers  {r.customersFinished}";
    }
}
