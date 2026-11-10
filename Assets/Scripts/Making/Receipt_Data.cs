using UnityEngine;


[CreateAssetMenu(fileName = "Receipt", menuName = "Receipt Data")]
public class Receipt_Data : ScriptableObject
{
    public enum Main_Kiring
    {
        Normal,
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
        A,
        B
    }

    public Clasp clasp;
}
