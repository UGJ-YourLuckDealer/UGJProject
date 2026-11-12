<<<<<<< Updated upstream
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

    int a;
    int b;

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

        a = check.GetCount(LayerMask.NameToLayer("Clasp"), "A");
        b = check.GetCount(LayerMask.NameToLayer("Clasp"), "B");

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

        switch (receipt_data.clasp)
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
                if (a == 0 && b==0)
                    correct += 1;
                else
                    incorrect += 1;
                break;
        }



        float sum = (float)(correct - incorrect) /
            (float)(receipt_data.clover + receipt_data.note + receipt_data.rainbow + receipt_data.number + 1 + 1);

        // ·¹½ÃÇÇ ¿ä¼Ò ÃÑ °³¼ö (= ºĞ¸ğ¿Í °°Àº °ª)
        int total = receipt_data.clover + receipt_data.note + receipt_data.rainbow + receipt_data.number + 1 + 1;

        // ÆÇ¸Å°¡ = ¿ä¼Ò¸¶´Ù $3 (±âÈ¹: ¸ğµç ¿ä¼Ò ÆÇ¸Å°¡°İ $3)
        float salePrice = total * 3f;

        float earned;
        string grade;
        if (sum >= 1f)                    // 100%: ÆÇ¸Å°¡ + ÆÇ¸Å°¡ÀÇ 10% ÆÁ
        {
            earned = salePrice * 1.1f;
            grade = "Perfect (Tip)";
        }
        else if (sum >= 0.8f - 0.0001f)   // 80% ÀÌ»ó: ÆÇ¸Å°¡¸¸
        {
            earned = salePrice;
            grade = "Pass";
        }
        else                              // 80% ¹Ì¸¸: È¯ºÒ (¼öÀÍ ¾øÀ½)
        {
            earned = 0f;
            grade = "Refund";
        }

        CapitalSystem capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null)
        {
            float before = capital.Current;
            capital.Add(earned);
            Debug.Log($"[ÆÇÁ¤ ¼öÀÍ] {grade}: {before:0.00} ¡æ {capital.Current:0.00} (+{earned:0.00})");
        }
        else
        {
            Debug.LogWarning("[ÆÇÁ¤ ¼öÀÍ] ¾À¿¡ CapitalSystemÀÌ ¾ø½À´Ï´Ù. GameSystems ÇÁ¸®ÆÕÀ» ¹èÄ¡ÇØ ÁÖ¼¼¿ä.");
        }

        Debug.Log("Result: " + sum);
        Debug.Log(check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Clover"));
        Debug.Log(heart);
        Debug.Log(note);
        Debug.Log(rainbow);
    }
}
=======
using Unity.VisualScripting;
using UnityEngine;

public class Result : MonoBehaviour
{
    public Receipt_Data receipt_data;
    Check_Board check;

    int clover;
    int note;
    int number;
    int rainbow;

    int heart;
    int normal;

    int a;
    int b;

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


        if(heart == 0 && normal == 0)
        {
            Debug.LogError("Keyring is not exist");
            return;
        }    


        clover = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Clover");
        note = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Note");
        rainbow = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Rainbow");
        number = check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Number");

        a = check.GetCount(LayerMask.NameToLayer("Clasp"), "A");
        b = check.GetCount(LayerMask.NameToLayer("Clasp"), "B");

        correct += Mathf.Min(clover, receipt_data.clover);
        incorrect += Mathf.Max(0, clover - receipt_data.clover);

        correct += Mathf.Min(note, receipt_data.note);
        incorrect += Mathf.Max(0, note - receipt_data.note);

        correct += Mathf.Min(rainbow, receipt_data.rainbow);
        incorrect += Mathf.Max(0, rainbow - receipt_data.rainbow);

        correct += Mathf.Min(number, receipt_data.number);
        incorrect += Mathf.Max(0, number - receipt_data.number);

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

        switch (receipt_data.clasp)
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
                if (a == 0 && b==0)
                    correct += 1;
                else
                    incorrect += 1;
                break;
        }



        float sum = (float)(correct - incorrect) /
            (float)(receipt_data.clover + receipt_data.note + receipt_data.rainbow + receipt_data.number + 1 + 1);

        // ë ˆì‹œí”¼ ìš”ì†Œ ì´ ê°œìˆ˜ (= ë¶„ëª¨ì™€ ê°™ì€ ê°’)
        int total = receipt_data.clover + receipt_data.note + receipt_data.rainbow + receipt_data.number + 1 + 1;

        // íŒë§¤ê°€ = ìš”ì†Œë§ˆë‹¤ $3 (ê¸°íš: ëª¨ë“  ìš”ì†Œ íŒë§¤ê°€ê²© $3)
        float salePrice = total * 3f;

        float earned;
        string grade;
        JudgementResult judgement = new JudgementResult();
        if (sum >= 1f)                    // 100%: íŒë§¤ê°€ + íŒë§¤ê°€ì˜ 10% íŒ
        {
            earned = salePrice * 1.1f;
            grade = "Perfect (Tip)";
            judgement.grade = JudgementGrade.Perfect;
        }
        else if (sum >= 0.8f - 0.0001f)   // 80% ì´ìƒ: íŒë§¤ê°€ë§Œ
        {
            earned = salePrice;
            grade = "Pass";
            judgement.grade = JudgementGrade.Pass;
        }
        else                              // 80% ë¯¸ë§Œ: í™˜ë¶ˆ (ìˆ˜ìµ ì—†ìŒ)
        {
            earned = 0f;
            grade = "Refund";
            judgement.grade = JudgementGrade.Refund;
        }

        CapitalSystem capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null)
        {
            float before = capital.Current;
            capital.Add(earned);
            Debug.Log($"[íŒì • ìˆ˜ìµ] {grade}: {before:0.00} â†’ {capital.Current:0.00} (+{earned:0.00})");
        }
        else
        {
            Debug.LogWarning("[íŒì • ìˆ˜ìµ] ì”¬ì— CapitalSystemì´ ì—†ìŠµë‹ˆë‹¤. GameSystems í”„ë¦¬íŒ¹ì„ ë°°ì¹˜í•´ ì£¼ì„¸ìš”.");
        }

        // íŒ ìœ ë¬´ í‘œì‹œìš© íŒì • ê²°ê³¼ ì•Œë¦¼ (ìˆ˜ìµì€ ìœ„ì—ì„œ ì´ë¯¸ ë”í–ˆìœ¼ë¯€ë¡œ ê¸ˆì•¡ì€ ë„£ì§€ ì•ŠìŒ)
        judgement.matched = correct;
        judgement.mismatched = incorrect;
        judgement.total = total;
        judgement.ratio = sum;
        GameEvents.RaiseKeyringJudged(null, judgement);

        Debug.Log("Result: " + sum);
        Debug.Log(check.GetCount(LayerMask.NameToLayer("Sub_Acrill"), "Clover"));
        Debug.Log(heart);
        Debug.Log(note);
        Debug.Log(rainbow);
    }
}
>>>>>>> Stashed changes
