using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue UI References")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float lettersPerSecond = 30f;

    private bool isDialogueActive = false;
    private DiagenTriggerEvent currentSpeaker = null;
    private Coroutine typingCoroutine = null;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ensure the dialogue box is inactive at the start
        dialogueBox.SetActive(false);
    }

    public bool IsDialogueActive => isDialogueActive;
    public DiagenTriggerEvent CurrentSpeaker => currentSpeaker;

    public void OpenDialogue(DiagenTriggerEvent speaker)
    {
        currentSpeaker = speaker;
        isDialogueActive = true;
        dialogueBox.SetActive(true);
        dialogueText.text = "";
    }

    public void CloseDialogue()
    {
        if (isDialogueActive)
        {
            Debug.Log("Ending dialogue...");
            dialogueBox.SetActive(false);
            isDialogueActive = false;
            currentSpeaker = null;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
        }
    }

    public void SetDialogueProcessing()
    {
        dialogueText.text = "...";
    }

    public void DisplayDialogue(string content, System.Action onComplete = null)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        typingCoroutine = StartCoroutine(TypeDialogue(content, onComplete));
    }

    private IEnumerator TypeDialogue(string content, System.Action onComplete)
    {
        dialogueText.text = "";
        dialogueText.color = Color.black;

        Debug.Log("Typing dialogue...");

        foreach (char letter in content)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        Debug.Log("Finished typing dialogue.");

        // Wait for a few seconds before auto-closing
        yield return new WaitForSeconds(3f);
        
        // Auto-close if dialogue is still the same
        CloseDialogue();
        
        onComplete?.Invoke();
    }

    // New method to update dialogue text progressively without animation
    public void DisplayTextProgressively(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // Update the dialogue text immediately without animation
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }
}
