using System;
using UnityEngine;
using UnityEngine.UI;

public class StartCutsceneDialogue : MonoBehaviour
{
    [Serializable]
    private struct DialogueLine
    {
        [TextArea(2, 5)]
        public string text;
        public Sprite portrait;
    }

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button advanceButton;

    [Header("Dialogue Content")]
    [SerializeField]
    private DialogueLine[] dialogueLines =
    {
        new DialogueLine { text = "I finally made it to the forest ruins..." },
        new DialogueLine { text = "I should stay sharp. Something feels off." }
    };

    [Header("Text Style")]
    [SerializeField] private Font dialogueFont;
    [SerializeField] private int dialogueFontSize = 28;
    [SerializeField] private Color dialogueTextColor = Color.white;

    private int currentIndex;
    private bool isCutsceneActive;

    private void Awake()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(AdvanceDialogue);
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }
    }

    private void Start()
    {
        BeginCutscene();
    }

    private void OnDestroy()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceDialogue);
        }
    }

    public void BeginCutscene()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            EndCutscene();
            return;
        }

        isCutsceneActive = true;
        currentIndex = 0;

        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(true);
        }

        ApplyTextStyle();
        ShowCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!isCutsceneActive)
        {
            return;
        }

        currentIndex++;
        if (currentIndex >= dialogueLines.Length)
        {
            EndCutscene();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = dialogueLines[currentIndex];

        if (dialogueText != null)
        {
            dialogueText.text = line.text;
        }

        if (portraitImage != null)
        {
            bool hasPortrait = line.portrait != null;
            portraitImage.sprite = line.portrait;
            portraitImage.enabled = hasPortrait;
        }
    }

    private void ApplyTextStyle()
    {
        if (dialogueText == null)
        {
            return;
        }

        if (dialogueFont != null)
        {
            dialogueText.font = dialogueFont;
        }

        dialogueText.fontSize = Mathf.Max(8, dialogueFontSize);
        dialogueText.color = dialogueTextColor;
    }

    private void EndCutscene()
    {
        isCutsceneActive = false;

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
    }
}
