using Unity.VisualScripting;
using UnityEngine;

public class Result : MonoBehaviour
{
    [SerializeField] Receipt_Data receipt_data;
    Check_Board check;

    int clover;
    int note;
    int number;
    int rainbow;

    int heart;
    int normal;

    int main = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        check = FindAnyObjectByType<Check_Board>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Result_Kiring()
    {
        int correct = 0;
        int incorrect = 0;
        heart = check.GetCount(LayerMask.NameToLayer("Main_Kiring"), "Heart");
        normal = check.GetCount(LayerMask.NameToLayer("Main_Kiring"), "Normal");

        clover = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Clover");
        note = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Note");
        rainbow = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Rainbow");
        number = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Number");

        // 기획 공식: 일치 = 레시피 개수까지 인정, 불일치 = 레시피보다 더 단 개수
        correct   += Mathf.Min(clover,  receipt_data.clover);
        incorrect += Mathf.Max(0, clover  - receipt_data.clover);

        correct   += Mathf.Min(note,    receipt_data.note);
        incorrect += Mathf.Max(0, note    - receipt_data.note);

        correct   += Mathf.Min(rainbow, receipt_data.rainbow);
        incorrect += Mathf.Max(0, rainbow - receipt_data.rainbow);

        correct   += Mathf.Min(number,  receipt_data.number);
        incorrect += Mathf.Max(0, number  - receipt_data.number);

        switch (receipt_data.main_kiring)
            {
                case Receipt_Data.Main_Kiring.Normal:
                if (normal == 1)
                    correct += 1;
                else
                    incorrect += 1;
                break;
                case Receipt_Data.Main_Kiring.Heart:
                    if (heart == 1)
                    correct += 1;
                else
                    incorrect += 1;
                break;
            }

        

        float sum = (float)(correct - incorrect) /
            (float)(receipt_data.clover + receipt_data.note + receipt_data.rainbow + receipt_data.number + 1);

        // 레시피 요소 총 개수 (= 분모와 같은 값)
        int total = receipt_data.clover + receipt_data.note + receipt_data.rainbow + receipt_data.number + 1;

        // 판매가 = 요소마다 $3 (기획: 모든 요소 판매가격 $3)
        float salePrice = total * 3f;

        float earned;
        string grade;
        if (sum >= 1f)                    // 100%: 판매가 + 판매가의 10% 팁
        {
            earned = salePrice * 1.1f;
            grade = "Perfect (Tip)";
        }
        else if (sum >= 0.8f - 0.0001f)   // 80% 이상: 판매가만
        {
            earned = salePrice;
            grade = "Pass";
        }
        else                              // 80% 미만: 환불 (수익 없음)
        {
            earned = 0f;
            grade = "Refund";
        }

        CapitalSystem capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null)
        {
            float before = capital.Current;
            capital.Add(earned);
            Debug.Log($"[판정 수익] {grade}: {before:0.00} → {capital.Current:0.00} ({(earned > 0 ? "+" + earned.ToString("0.00") : "수익 없음")})");
        }
        else
        {
            Debug.LogWarning("[판정 수익] 씬에 CapitalSystem이 없습니다. GameSystems 프리팹을 배치해 주세요.");
        }

        Debug.Log($"[판정 상세] 놓은 것: normal={normal}, heart={heart}, clover={clover}, note={note}, number={number}, rainbow={rainbow}" +
                  $" / 레시피: main={receipt_data.main_kiring}, clover={receipt_data.clover}, note={receipt_data.note}, number={receipt_data.number}, rainbow={receipt_data.rainbow}" +
                  $" / correct={correct}, incorrect={incorrect}, sum={sum:0.00}");
    }
}
