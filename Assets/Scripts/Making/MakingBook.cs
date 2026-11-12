using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MakingBook : MonoBehaviour
{
    /// <summary>
    /// Making Scene에서 바로 테스트용 Default값 변수
    /// </summary>
    public Receipt_Data receipt_data;
    public TMP_Text receiptPrefab;
    public Transform receiptParent;

    List<string> receipt = new List<string>();
    Receipt_Data _loadedReceipt;

    private void Awake()
    {
        if (Customer_Data.receipt != null)
        {
            Debug.Log("Counter Scene으로 부터 Static으로 Target 레시피 데이터 받아옴");
            _loadedReceipt = Customer_Data.receipt;
        }
        else
        {
            Debug.LogWarning("Counter Scene으로 부터 Static으로 Target 레시피 데이터 받아오지 못함, Default 레시피 데이터 사용");
            _loadedReceipt = receipt_data;
        }

        switch (_loadedReceipt.main_kiring)
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
        //for (int i = 0; i < receipt.Count; i++)
        //{
        //    TMP_Text newText = Instantiate(receiptPrefab, receiptParent);

        //    newText.text = receipt[i].ToString();
        //}


    }
}
