using Unity.VisualScripting;
using UnityEngine;

public class Result : MonoBehaviour
{
    public Receipt_Data receipt_data;

    int clover;
    int note;
    int number;
    int rainbow;

    int heart;
    int normal;

    int a;
    int b;

    int main = 0;

    Receipt_Data _loadedReceipt;

    void Start()
    {
        if (Customer_Data.receipt != null)
        {
            _loadedReceipt = Customer_Data.receipt;
        }
        else
        {
            _loadedReceipt = receipt_data;
        }
    }

    private void CountAttachedMaterials()
    {
        heart = normal = clover = note = rainbow = number = a = b = 0;
        ObjectDragManager manager = FindAnyObjectByType<ObjectDragManager>();
        KeyringMaterialObject chain = manager != null ? manager.CurrentChain : null;
        if (chain == null || chain.keyringMaterialData.category != KeyringMaterialObject.KeyringMaterial.Category.Chain)
            return;

        if (chain.keyringMaterialData.main == Receipt_Data.Main_Kiring.Heart) heart++;
        else if (chain.keyringMaterialData.main == Receipt_Data.Main_Kiring.Normal) normal++;

        foreach (var entry in chain.childs)
        {
            var material = entry.Value;
            if (material.bindedObject == null)
                continue;

            if (material.category == KeyringMaterialObject.KeyringMaterial.Category.Clasp)
            {
                if (material.clasp == Receipt_Data.Clasp.A) a++;
                else if (material.clasp == Receipt_Data.Clasp.B) b++;
            }
            else if (material.category == KeyringMaterialObject.KeyringMaterial.Category.Charm)
            {
                switch (material.charmType)
                {
                    case KeyringMaterialObject.KeyringMaterial.CharmType.Clover: clover++; break;
                    case KeyringMaterialObject.KeyringMaterial.CharmType.Note: note++; break;
                    case KeyringMaterialObject.KeyringMaterial.CharmType.Rainbow: rainbow++; break;
                    case KeyringMaterialObject.KeyringMaterial.CharmType.Number: number++; break;
                }
            }
        }
    }

    public void Result_Kiring()
    {
        int correct = 0;
        int incorrect = 0;
        CountAttachedMaterials();

        correct += Mathf.Min(clover, _loadedReceipt.clover);
        incorrect += Mathf.Max(0, clover - _loadedReceipt.clover);

        correct += Mathf.Min(note, _loadedReceipt.note);
        incorrect += Mathf.Max(0, note - _loadedReceipt.note);

        correct += Mathf.Min(rainbow, _loadedReceipt.rainbow);
        incorrect += Mathf.Max(0, rainbow - _loadedReceipt.rainbow);

        correct += Mathf.Min(number, _loadedReceipt.number);
        incorrect += Mathf.Max(0, number - _loadedReceipt.number);

        switch (_loadedReceipt.main_kiring)
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

        switch (_loadedReceipt.clasp)
        {
            case Receipt_Data.Clasp.A:
                if (a == 1)
                    correct += 1;
                else
                    incorrect += 1;
                break;
            case Receipt_Data.Clasp.B:
                if (b == 1)
                    correct += 1;
                else
                    incorrect += 1;
                break;
            case Receipt_Data.Clasp.Null:
                if (a == 0 && b == 0)
                    correct += 1;
                else
                    incorrect += 1;
                break;
        }



        float sum = (float)(correct - incorrect) /
            (float)(_loadedReceipt.clover + _loadedReceipt.note + _loadedReceipt.rainbow + _loadedReceipt.number + 1 + 1);

        // 레시피 요소 총 개수 (= 분모와 같은 값)
        int total = _loadedReceipt.clover + _loadedReceipt.note + _loadedReceipt.rainbow + _loadedReceipt.number + 1 + 1;

        // 판매가 = 요소마다 $3 (기획: 모든 요소 판매가격 $3)
        float salePrice = total * 3f;

        float earned;
        string grade;
        JudgementResult judgement = new JudgementResult();
        if (sum >= 1f)                    // 100%: 판매가 + 판매가의 10% 팁
        {
            int state = 0;
            earned = salePrice * 1.1f;
            grade = "Perfect (Tip)";
            judgement.grade = JudgementGrade.Perfect;
            Customer_Data.TipState(state);
        }
        else if (sum >= 0.8f - 0.0001f)   // 80% 이상: 판매가만
        {
            int state = 1;
            earned = salePrice;
            grade = "Pass";
            judgement.grade = JudgementGrade.Pass;
            Customer_Data.TipState(state);
        }
        else                              // 80% 미만: 환불 (수익 없음)
        {
            int state = 2;
            earned = 0f;
            grade = "Refund";
            judgement.grade = JudgementGrade.Refund;
            Customer_Data.TipState(state);
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

        // 팁 유무 표시용 판정 결과 알림 (수익은 위에서 이미 더했으므로 금액은 넣지 않음)
        judgement.matched = correct;
        judgement.mismatched = incorrect;
        judgement.total = total;
        judgement.ratio = sum;
        GameEvents.RaiseKeyringJudged(null, judgement);

        Debug.Log("Result: " + sum);
        Debug.Log(clover);
        Debug.Log(heart);
        Debug.Log(note);
        Debug.Log(rainbow);
    }
}
