using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class MakingBook : MonoBehaviour
{
    public Receipt_Data receipt_data;

    List<string> receipt = new List<string>();

    public TMP_Text receiptPrefab;
    public Transform receiptParent;

    private void Awake()
    {
        receipt_data = Customer_Data.receipt;

        switch(receipt_data.main_kiring)
        {
            case Receipt_Data.Main_Kiring.Normal:
                receipt.Add("Main Kiring: Normal");
                break;
            case Receipt_Data.Main_Kiring.Heart:
                receipt.Add("Main Kiring: Heart");
                break;
        }

        if (Customer_Data.clover != 0)
        {
            receipt.Add("Clover: " + Customer_Data.clover);
        }

        if (Customer_Data.note != 0)
        {
            receipt.Add("Note: " + Customer_Data.note);
        }

        if (Customer_Data.number != 0)
        {
            receipt.Add("Number: " + Customer_Data.number);
        }

        if (Customer_Data.rainbow != 0)
        {
            receipt.Add("Rainbow: " + Customer_Data.rainbow);
        }

        if (Customer_Data.A != 0)
        {
            receipt.Add("Scrap: " + Customer_Data.A);
        }

    }

    private void Start()
    {
        for(int i=0;i< receipt.Count;i++)
        {
            TMP_Text newText = Instantiate(receiptPrefab, receiptParent);

            newText.text = receipt[i].ToString();
        }    

        
    }
}
