using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Diagen; // Add this namespace reference to access DiagenSubsystem
using DiagenCommonTypes;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

public class DiagenTriggerEvent : MonoBehaviour
{

    [Header("Dialogue UI References")]
    [SerializeField] private TextMeshProUGUI dialogueText; // TMP text element for dialogue

    [Header("Dialogue Settings")]
    [SerializeField, TextArea(3, 10)] private List<string> dialogueLines; // Stores multiple lines
    
    [SerializeField] public GameObject player;
    private string dialogueContent; // Default text
    private bool interacting = false;
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool movingHorizontally = true; // Alternates between X and Y movement

    [Header("Session")]
    public string NpcName = "Oriane";
    private LlmServerSettings llmServerSettings = new LlmServerSettings();
    private LlmGenerationSettings llmGenerationSettings = new LlmGenerationSettings();

    [SerializeField]
    private List<Session> sessionStates = new List<Session>();
    private int sessionIndex = 0;
    private OptionTables optionTables;


    [SerializeField]
    public TopicTable topicTable;
    [SerializeField]
    public EventsTable eventsTable;
    [SerializeField]
    public StateTagsWeightTable stateTagsWeightTable;
    [SerializeField]
    public CharacterInformationTable characterInformationTable;
    [SerializeField]
    public List<string> enabledStateTags = new List<string>();

    [Header("LLM Settings")]
    public bool detectTopics = false;

    private bool inProgress = false;

    private int listEvent = 0;
    private bool invitation = true;


    private void Start()
    {
        initialPosition = transform.position;
        llmServerSettings.Init();

        // Get the global States
        sessionStates = DiagenSession.GetGlobalSessions();

        // load all table into option table
        optionTables = new OptionTables();
        if (topicTable != null) optionTables.topicTable = topicTable;
        if (eventsTable != null) optionTables.eventsTable = eventsTable;
        if (stateTagsWeightTable != null) optionTables.stateTagsWeightTable = stateTagsWeightTable;
        if (characterInformationTable != null) optionTables.characterInformationTable = characterInformationTable;

        sessionStates = DiagenSession.InitSession(sessionStates, NpcName);
        sessionStates = DiagenSession.BindAllStateTagsToAgent(sessionStates, NpcName, optionTables);
        sessionStates = DiagenSession.EnableTags(sessionStates, NpcName, enabledStateTags);
        sessionStates[sessionIndex] = DiagenTopic.EnableTopics(sessionStates[sessionIndex], NpcName, optionTables);
        sessionIndex = DiagenSession.GetSessionIndex(sessionStates, NpcName);

        // Set the global states
        DiagenSession.SetGlobalSessions(sessionStates);

        // set initial values for llmGenerationSettings
        llmGenerationSettings.agentName = NpcName;
        llmGenerationSettings.playerName = "Newcomer";
        llmGenerationSettings.includeHistory = true;
        llmGenerationSettings.nPredict = 128;
        llmGenerationSettings.temperature = 2.0f;
        llmGenerationSettings.topK = 50;
        llmGenerationSettings.topP = 0.9f;
        llmGenerationSettings.frequencyPenalty = 0.8f;
        llmGenerationSettings.presencePenalty = 0.3f;

        UnityEngine.Debug.Log("NPC Initialized");
    }

    // End event 
    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    public void SetDialogue(string newDialogue)
    {
        dialogueContent = newDialogue; // Update the stored dialogue contents

        if (dialogueText != null)
        {
            dialogueText.text = dialogueContent; // Update UI if dialogue is active
        }
        DialogueManager.Instance.DisplayDialogue(newDialogue);
        UnityEngine.Debug.Log("NPC dialogue updated: " + newDialogue);
    }


    private async void OnMouseDown() // Click on the NPC to trigger dialogue
    {
        UnityEngine.Debug.Log("NPC Clicked!");
        await Interact();
    }

    public async Task Interact()
    {

        // Check if the session is suspicious, then you should always take suspicious
        List<Session> sessionStates = DiagenSession.GetGlobalSessions();

        UnityEngine.Debug.Log("Interact() function was called!");

        if (inProgress || interacting)
        {
            UnityEngine.Debug.Log("Interaction already in progress...");
            return;
        }

        // Log enabledState tags
        //UnityEngine.Debug.Log("Session Enabled State Tags: " + string.Join(", ", sessionStates[sessionIndex].EnableStateTags));
        
        // Close dialogue if this NPC is currently speaking
        if (DialogueManager.Instance.IsDialogueActive && DialogueManager.Instance.CurrentSpeaker == this)
        {
            DialogueManager.Instance.CloseDialogue();
            return;
        }
        
        interacting = true;
        dialogueContent = ""; // Reset dialogue content
    

        // Open dialogue with this NPC as speaker
        DialogueManager.Instance.OpenDialogue(this);
        List<DiagenEvent> events = DiagenTrigger.ListAvailableEvents(sessionStates[sessionIndex], optionTables);

        DiagenEvent selectedEvent = events[listEvent];

        // Increment listEvent for next iteration
        listEvent += 1;
        if (listEvent >= events.Count)
        {
            listEvent = 0;
        }

        UnityEngine.Debug.Log("Selected event: " + selectedEvent.Name);
        await TriggerEvent(selectedEvent);
    }

    private async Task TriggerEvent(DiagenEvent selectedEvent)
    {        
        // if selectedEvent.SayVerbatim is not null, set dialogueContent to selectedEvent.SayVerbatim  
        if (selectedEvent.SayVerbatim != null && selectedEvent.SayVerbatim != "")
        {
            dialogueLines.Add(selectedEvent.SayVerbatim);
            SetDialogue(selectedEvent.SayVerbatim);
            DialogueManager.Instance.DisplayDialogue(selectedEvent.SayVerbatim);
            await Task.Delay(10000);
            interacting = false;
        }
        
        // Replace the instruction text generation with the chunk-based method
        if (selectedEvent.Instruction != null && selectedEvent.Instruction != "")
        {
            DialogueManager.Instance.SetDialogueProcessing();
            UnityEngine.Debug.Log("Instruction: " + selectedEvent.Instruction);
            inProgress = true;
            
            // Example - Event Execution: Usage Chunk by Chunk
            // Use GenerateTextFromInstructionAsyncChunks instead
            await DiagenTrigger.GenerateTextFromInstructionAsyncChunks(
                sessionStates[sessionIndex],
                llmGenerationSettings.agentName,
                llmGenerationSettings.playerName,
                selectedEvent.Instruction,
                optionTables,
                llmServerSettings,
                llmGenerationSettings,
                null,
                (currentText, isDone, isError) => {
                    // Update dialogue text with each incoming chunk
                    UnityEngine.Debug.Log("Current Text: " + currentText);
                    DialogueManager.Instance.DisplayTextProgressively(currentText);
                    
                    // Handle completion or error
                    if (isDone)
                    {
                        if (isError)
                        {
                            UnityEngine.Debug.LogError("Error generating text from instruction");
                        }
                        else if (!string.IsNullOrEmpty(currentText))
                        {
                            dialogueLines.Add(currentText);
                        }
                        else
                        {
                            DialogueManager.Instance.DisplayTextProgressively("I have nothing more to say.");
                            dialogueLines.Add("I have nothing more to say.");
                        }
                    }
                }
            );
            
            inProgress = false;
            await Task.Delay(10000); // Wait for user to read the text
            DialogueManager.Instance.CloseDialogue();
            interacting = false;

            foreach (var actionEvent in selectedEvent.ActionEvents)
            {
                actionEvent.Execute();
            }
            //D.ExecuteActionEvent

            
        }
        
        // Get global sessions
        sessionStates = DiagenSession.GetGlobalSessions();

        // Execute Event
        EventSessionsPair eventSessionsPair = DiagenTrigger.TriggerDiagenEvent(sessionStates, selectedEvent.Name, NpcName, optionTables);
        sessionStates = eventSessionsPair.sessionStates;
        DiagenEvent diagenEvent = eventSessionsPair.diagenEvent;

        // Update global session again
        DiagenSession.SetGlobalSessions(sessionStates);

        UnityEngine.Debug.Log("sessionStates "+ sessionStates[sessionIndex].EnableStateTags);
    }

    private bool IsPlayerClose()
    {
        if (player == null) return false; // Ensure the player reference is valid
        return Vector3.Distance(transform.position, player.transform.position) < 2.5f;
    }

    private void Cleanup()
    {
        //DiagenSubsystem.StopLlamaServer();
    }


}

