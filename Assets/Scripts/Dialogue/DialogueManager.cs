using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public DialogueData[] dialogueData;

    //Main_Data mainData;
    public int customer = 1;

    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI B_ButtonText;
    public UnityEngine.UI.Image characterImage;
    Color color;

    public GameObject nextButton;

    public GameObject textPanel;

    [SerializeField] private int currentIndex = 0;

    private float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public DialogueLine line;
    public DialogueBifurcation bifurcation;

    private bool isWaiting = false;

    private bool isEndDialogue = false;

    private void Start()
    {
        Debug.Log("Start");
        isWaiting = false;

        Debug.Log(Customer_Data.isfinish);

        if (Customer_Data.isfinish == true)
            StartDialogue();
        else
            LastDialogue();

        Customer_Data.Set_CustomerReceipt(dialogueData[customer]);
    }

    private void Update()
    {

        if (Mouse.current.leftButton.wasPressedThisFrame && !isWaiting)
        {
            if (isTyping == true)
            {
                isTyping = false;
                StopCoroutine(typingCoroutine);


                if (Customer_Data.isfinish == true)
                    dialogueText.text = line.dialogueText;
                else if (Customer_Data.isfinish != true)
                    dialogueText.text = bifurcation.bifuractionText;


                if (!line.isEnd && Customer_Data.isfinish == true)
                    nextButton.SetActive(true);

            }
            else
            {
                if (Customer_Data.isfinish == true)
                {
                    Debug.Log("NextDialogue");
                    NextDialogue();
                }
                else if (Customer_Data.isfinish != true)
                {
                    if (isEndDialogue == true)
                        return;

                    EndDialogue();
                    isEndDialogue = true;
                }

            }
        }
    }

    public void LastDialogue()
    {


        TextPanelScaleUp panelScaleUp = textPanel.GetComponent<TextPanelScaleUp>();

        panelScaleUp.StartScaleUp();
        Debug.Log(currentIndex);
        Debug.Log(customer);

        bifurcation = dialogueData[customer - 1].dialogueBifurcations[currentIndex];

        characterName.text = line.character_Name;
        color.a = 255f;
        color.b = color.g = color.r = 255f;
        characterImage.color = color;
        textPanel.SetActive(true);
        DisplayLastDialogue();
    }

    public void ReStartDialogue()
    {
        isWaiting = false;
        isEndDialogue = false;
        //customer++;

        if (customer < dialogueData.Length)
        {
            line = dialogueData[customer].dialogueLines[currentIndex];
            StartDialogue();

            color.a = 255f;
            color.b = color.g = color.r = 255f;
            characterImage.color = color;
            return;
        }

    }
    void DisplayDialogue()
    {
        typingCoroutine = StartCoroutine(TypeDialogue(line.dialogueText));
        characterImage.sprite = line.characterImage;
    }

    void DisplayLastDialogue()
    {
        typingCoroutine = StartCoroutine(TypeDialogue(bifurcation.bifuractionText));
        characterImage.sprite = bifurcation.characterImage;
    }

    public void StartDialogue()
    {
        textPanel.SetActive(true);
        Debug.Log("StartDialogue");
        TextPanelScaleUp panelScaleUp = textPanel.GetComponent<TextPanelScaleUp>();

        panelScaleUp.StartScaleUp();

        line = dialogueData[customer].dialogueLines[currentIndex++];

        characterName.text = line.character_Name;
        color.a = 255f;
        color.b = color.g = color.r = 255f;
        characterImage.color = color;
        DisplayDialogue();
    }

    public void NextDialogue()
    {


        if (line.isEnd)
        {
            Customer_Data.isfinish = false;
            isWaiting = true;
            return;
        }


        TextPanelScaleUp panelScaleUp = textPanel.GetComponent<TextPanelScaleUp>();
        panelScaleUp.StartScaleUp();

        if (currentIndex < dialogueData[customer].dialogueLines.Length)
        {
            line = dialogueData[customer].dialogueLines[currentIndex++];
            DisplayDialogue();
        }
        else
        {
            EndDialogue();
            Debug.Log("Dialogues is END");
            return;
        }


    }

    public void EndDialogue()
    {
        if (!line.isEnd && Customer_Data.isfinish == true)
            return;

        Customer_Data.isfinish = true;

        currentIndex = 0;

        color.a = 0f;
        dialogueText.text = "";
        characterImage.sprite = null;
        characterImage.color = color;

        textPanel.SetActive(false);

        nextButton.SetActive(false);

        isWaiting = true;

        Invoke(nameof(ReStartDialogue), 2f);
    }

    IEnumerator TypeDialogue(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        nextButton.SetActive(false);

        foreach (char letter in text)
        {
            dialogueText.text += letter;

            yield return new WaitForSeconds(typingSpeed);
        }

        if (!line.isEnd)
            nextButton.SetActive(true);
        isTyping = false;
        typingCoroutine = null;
    }
}


