using Unity.Android.Gradle.Manifest;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Customer_Data 
{
    public static Receipt_Data receipt;

    public static int heart = 10;
    public static int normal = 10;

    public static int clover = 10;
    public static int number = 10;
    public static int rainbow = 10;
    public static int note = 10;
    public static int A = 10;

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

        Debug.Log("°¨¼ÒµÊ");
    }

}
