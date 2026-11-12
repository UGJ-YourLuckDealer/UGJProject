using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Customer_Data
{
    public static Receipt_Data receipt;

    public static int customer;

    public static int heart;
    public static int normal;

    public static int clover;
    public static int number;
    public static int rainbow;
    public static int note;
    public static int A;
    public static int B;

    public static int state;

    public static bool isfinish;

    public static void ResetStaticState()
    {
        receipt = null;
        customer = 0;
        heart = 10;
        normal = 10;
        clover = 10;
        number = 10;
        rainbow = 10;
        note = 10;
        A = 10;
        B = 10;
        state = 0;
        isfinish = true;
    }

    public static void ResetCustomer()
    {
        Debug.Log("Reset static Values");
        isfinish = true;
        while (customer > 0)
            customer--;
    }

    public static void IncreaseCustomer()
    {
        
        customer++;
    }

    //public enum State
    //{
    //    Best,
    //    Good,
    //    Bad
    //}
    //public State state;

    public bool IsFinished()
    {
        return isfinish;
    }

    public void Finished()
    {
        isfinish = true;
    }

    public void NotFinished()
    {
        isfinish = false;
    }

    public static void TipState(int tipState)
    {

        if (state < tipState)
        {
            while (state != tipState)
                state++;
        }

        else if (state > tipState)
        {
            while (state != tipState)
                state--;
        }
    }

    public int Get_TipState()
    {
        return state;
    }

    public static void Set_CustomerReceipt(DialogueData dialogue)
    {
        receipt = dialogue.receiptData;
    }

    public int Get_Account(GameObject target)
    {
        int category = 0;
        if (target.CompareTag("Heart"))
            category = heart;

        if (target.CompareTag("Normal"))
            category = normal;

        if (target.CompareTag("Clover"))
            category = clover;

        if (target.CompareTag("Number"))
            category = number;

        if (target.CompareTag("Rainbow"))
            category = rainbow;

        if (target.CompareTag("Note"))
            category = note;

        if (target.CompareTag("A"))
            category = A;

        if (target.CompareTag("B"))
            category = B;

        return category;
    }

    public void Decrease_Account(GameObject target)
    {
        if (target.CompareTag("Normal"))
            normal--;

        if (target.CompareTag("Heart"))
            heart--;

        if (target.CompareTag("Clover"))
            clover--;

        if (target.CompareTag("Number"))
            number--;

        if (target.CompareTag("Rainbow"))
            rainbow--;

        if (target.CompareTag("Note"))
            note--;

        if (target.CompareTag("A"))
            A--;

        if (target.CompareTag("B"))
            B--;
    }

    public void InCrease_Account(GameObject target)
    {
        if (target.CompareTag("Normal"))
            normal++;

        if (target.CompareTag("Heart"))
            heart++;

        if (target.CompareTag("Clover"))
            clover++;

        if (target.CompareTag("Number"))
            number++;

        if (target.CompareTag("Rainbow"))
            rainbow++;

        if (target.CompareTag("Note"))
            note++;

        if (target.CompareTag("A"))
            A++;

        if (target.CompareTag("B"))
            B++;
    }
}
