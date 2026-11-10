using UnityEngine;

public class Customer_Data 
{
    public static Receipt_Data receipt;
    public static void Set_CustomerReceipt(DialogueData dialogue)
    {
        receipt = dialogue.receiptData;
    }

    public void Get_CustomerReceipt()
    {

    }
}
