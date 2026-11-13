using System.Runtime.InteropServices;
using UnityEngine;


[CreateAssetMenu(fileName = "Receipt", menuName = "Receipt Data")]
public class Receipt_Data : ScriptableObject
{
    public enum Main_Kiring
    {
        Normal,
        [InspectorName("키링 체인")]
        Heart
    }

    public Main_Kiring main_kiring;

    public int clover;
    public int note;
    public int number;
    public int rainbow;

    public enum Special
    {
        Null,
        Four,
        Seven,
        NoneFour,
        NoneSeven
    }

    public Special special;

    public enum Clasp
    {
        Null,
        [InspectorName("D형 잠금장치")]
        A,
        [InspectorName("O형 잠금장치")]
        B
    }

    public Clasp clasp;
}
