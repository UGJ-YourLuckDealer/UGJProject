using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI.Extensions;

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
    [SerializeField] Vector2 makeKeyringTooltipOffset;

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
        isWaiting = true;
        

        customer = Customer_Data.customer;

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
        Debug.Log("customer:" + customer);

        if (Customer_Data.state == 0)
            bifurcation = dialogueData[customer].dialogueBifurcations[0];
        if (Customer_Data.state == 1)
            bifurcation = dialogueData[customer].dialogueBifurcations[1];
        if (Customer_Data.state == 2)
            bifurcation = dialogueData[customer].dialogueBifurcations[2];

        characterName.text = line.character_Name;
        color.a = 255f;
        color.b = color.g = color.r = 255f;
        characterImage.color = color;
        textPanel.SetActive(true);
        DisplayLastDialogue();

        Customer_Data.IncreaseCustomer();
        customer = Customer_Data.customer;
    }

    public void ReStartDialogue()
    {

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


        isWaiting = false;
        textPanel.SetActive(true);

        TextPanelScaleUp panelScaleUp = textPanel.GetComponent<TextPanelScaleUp>();

        panelScaleUp.StartScaleUp();


        typingCoroutine = StartCoroutine(TypeDialogue(line.dialogueText));

    }

    void DisplayLastDialogue()
    {
        isWaiting = false;
        typingCoroutine = StartCoroutine(TypeDialogue(bifurcation.bifuractionText));
        characterImage.sprite = bifurcation.characterImage;
    }

    public void StartDialogue()
    {

        Debug.Log("StartDialogue");
        AudioService.Instance.PlaySfx("sfx_ui_btn_click");

        line = dialogueData[customer].dialogueLines[currentIndex++];

        characterName.text = line.character_Name;
        color.a = 255f;


        StartCoroutine(CharacterImageColor());
        characterImage.color = color;

    }

    //CharacterImageColor Black -> White
    IEnumerator CharacterImageColor()
    {
        characterImage.sprite = line.characterImage;

        float elapsed = 0f;

        while (elapsed < 3f)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / 3f);
            t = Mathf.SmoothStep(0f, 1f, t);

            color.b = color.g = color.r = t;
            characterImage.color = color;

            yield return null;
        }

        characterImage.color = Color.white;

        DisplayDialogue();


    }

    public void NextDialogue()
    {
        Vector3 a = new Vector3(0f, 220f);

        if (line.isEnd)
        {
            Customer_Data.isfinish = false;
            isWaiting = true;

            ToolTip.Instance.SetTooltip("Make Keyring!", makeKeyringTooltipOffset);
            // GuideTooltip.Instance.Show(GameObject.Find("ToolTip").transform, "");

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

        AudioService.Instance.PlaySfx("sfx_ui_btn_click");

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


