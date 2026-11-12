using UnityEngine;
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

        if (_loadedReceipt.clover != 0)
        {
            receipt.Add("Clover: " + _loadedReceipt.clover);
        }

        if (_loadedReceipt.note != 0)
        {
            receipt.Add("Note: " + _loadedReceipt.note);
        }

        if (_loadedReceipt.number != 0)
        {
            receipt.Add("Number: " + _loadedReceipt.number);
        }

        if (_loadedReceipt.rainbow != 0)
        {
            receipt.Add("Rainbow: " + _loadedReceipt.rainbow);
        }

        switch (_loadedReceipt.clasp)
        {
            case Receipt_Data.Clasp.A:
                receipt.Add("Clasp: A");
                break;
            case Receipt_Data.Clasp.B:
                receipt.Add("Clasp: B");
                break;
            case Receipt_Data.Clasp.Null:
                break;
        }

    }

    private void Start()
    {
<<<<<<< Updated upstream
        for(int i=0;i< receipt.Count;i++)
        {
            TMP_Text newText = Instantiate(receiptPrefab, receiptParent);

            newText.text = receipt[i].ToString();
        }    
=======
        //for (int i = 0; i < receipt.Count; i++)
        //{
        //    TMP_Text newText = Instantiate(receiptPrefab, receiptParent);

        //    newText.text = receipt[i].ToString();
        //}

>>>>>>> Stashed changes

        
    }
}
