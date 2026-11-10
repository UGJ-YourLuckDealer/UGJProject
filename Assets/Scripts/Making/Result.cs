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

        receipt_data = Customer_Data.receipt;
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

        if (clover == receipt_data.clover)
            correct = clover;
        else
            incorrect = Mathf.Abs(clover - receipt_data.clover);

        if (note == receipt_data.note)
            correct += note;
        else
            incorrect += Mathf.Abs(note - receipt_data.note);

        if (rainbow == receipt_data.rainbow)
            correct += rainbow;
        else
            incorrect += Mathf.Abs(rainbow - receipt_data.rainbow);

        if (number == receipt_data.number)
            correct += number;
        else
            incorrect += Mathf.Abs(number - receipt_data.number);

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
            Debug.Log($"[판정 수익] {grade}: {before:0.00} → {capital.Current:0.00} (+{earned:0.00})");
        }
        else
        {
            Debug.LogWarning("[판정 수익] 씬에 CapitalSystem이 없습니다. GameSystems 프리팹을 배치해 주세요.");
        }

        Debug.Log("Result: " + sum);
        Debug.Log(check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Clover"));
        Debug.Log(heart);
        Debug.Log(note);
        Debug.Log(rainbow);
    }
}
