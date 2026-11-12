using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public Sprite characterImage;
    public string character_Name;

    [TextArea(2, 5)]
    public string dialogueText;
    
    public bool isEnd;

    
}

[System.Serializable]
public class DialogueBifurcation
{
    public Sprite characterImage;

    [TextArea(2, 5)]
    public string bifuractionText;

    
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] dialogueLines;

    public DialogueBifurcation[] dialogueBifurcations;

    public Receipt_Data receiptData;
}
