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

public class DiagenConversation : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerInputField; // Assign in Inspector
    [SerializeField] private TMP_Text responseText; // Assign in Inspector
    [SerializeField] private GameObject dialogueBox;

    // Private Variables
    private bool inProgress = false;
    private bool interacting = false;
    private string dialogueContent = "";
    private List<string> dialogueLines = new List<string>();
    private LlmGenerationSettings llmGenerationSettings;
    private LlmServerSettings llmServerSettings;
    private List<Session> sessionStates = new List<Session>();
    private OptionTables optionTables;
    private Coroutine typingCoroutine = null;
    private Session currentSession;
    public string NpcName = "Ivysaur";
    private int sessionIndex = 0;
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


    private void Start()
    {
        // Ensure optionTables is initialized
        optionTables = new OptionTables();
        if (topicTable != null) optionTables.topicTable = topicTable;
        if (eventsTable != null) optionTables.eventsTable = eventsTable;
        if (stateTagsWeightTable != null) optionTables.stateTagsWeightTable = stateTagsWeightTable;
        if (characterInformationTable != null) optionTables.characterInformationTable = characterInformationTable;

        // Ensure llm settings are initialized
        llmGenerationSettings = new LlmGenerationSettings();
        llmServerSettings = new LlmServerSettings();

        // Get the global States
        sessionStates = DiagenSession.GetGlobalSessions();
        sessionStates = DiagenSession.InitSession(sessionStates, NpcName);
        sessionStates = DiagenSession.BindAllStateTagsToAgent(sessionStates, NpcName, optionTables);
        sessionStates = DiagenSession.EnableTags(sessionStates, NpcName, enabledStateTags);
        sessionIndex = DiagenSession.GetSessionIndex(sessionStates, NpcName);
        sessionStates[sessionIndex] = DiagenTopic.EnableTopics(sessionStates[sessionIndex], NpcName, optionTables);

        if (sessionIndex < 0 || sessionIndex >= sessionStates.Count)
        {
            UnityEngine.Debug.LogError($"Invalid sessionIndex at Start: {sessionIndex}, sessionStates count: {sessionStates.Count}");
            return;
        }

        // Set the global states
        DiagenSession.SetGlobalSessions(sessionStates);

        // Ensure dialogue box is hidden at start
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        // Add listener to detect Enter key when input field is active
        if (playerInputField != null)
        {
            playerInputField.onEndEdit.AddListener(OnPlayerInputSubmit);
        }


        if (sessionStates == null || sessionStates.Count == 0)
        {
            UnityEngine.Debug.LogError("sessionStates is empty at Start!");
            return;
        }
    }

    private async void OnPlayerInputSubmit(string inputText)
    {
        // Detect Enter key press (only in Standalone/Editor mode)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            await SendText();
        }
    }

    private async Task SendText()
    {
        if (inProgress || interacting)
        {
            UnityEngine.Debug.Log("Interaction already in progress...");
            return;
        }

        if (playerInputField == null || responseText == null)
        {
            UnityEngine.Debug.LogError("UI elements are not assigned!");
            return;
        }

        if (sessionStates == null || sessionStates.Count == 0)
        {
            UnityEngine.Debug.LogError("sessionStates is null or empty!");
            return;
        }

        if (sessionIndex < 0 || sessionIndex >= sessionStates.Count)
        {
            UnityEngine.Debug.LogError($"Invalid sessionIndex: {sessionIndex}, sessionStates count: {sessionStates.Count}");
            return;
        }

        if (llmGenerationSettings == null || llmServerSettings == null)
        {
            UnityEngine.Debug.LogError("llmGenerationSettings or llmServerSettings is null!");
            return;
        }

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            UnityEngine.Debug.LogWarning("Player input is empty!");
            return;
        }

        sessionStates = DiagenSession.GetGlobalSessions();

        // Start the Topic Detection
        Topic detectedTopic = await DiagenTopic.CallTopicDetection(null, llmServerSettings, sessionStates, NpcName, llmGenerationSettings.playerName, message, optionTables);

        if (detectedTopic != null)
        {
            UnityEngine.Debug.Log("Detected Topic: " + detectedTopic.Name);

            TrustLevelUI trustUI = FindObjectOfType<TrustLevelUI>();
            if (trustUI != null)
            {
                if (detectedTopic.Name == "positive")
                {
                    trustUI.ChangeTrustLevel(0.1f); // Increase trust
                    UnityEngine.Debug.Log("Trust increased by 0.1");

                }
                else if (detectedTopic.Name == "negative")
                {
                    trustUI.ChangeTrustLevel(-0.1f); // Decrease trust
                    UnityEngine.Debug.Log("Trust decreased by 0.1");
                }
            }


        }
        else
        {
            UnityEngine.Debug.Log("No topic detected");
        }

        // Disable the input field while generating a response
        playerInputField.interactable = false;

        // Clear the input field
        playerInputField.text = "";

        interacting = true;
        dialogueContent = ""; // Reset dialogue content

        OpenDialogue();
        SetDialogueProcessing();
        inProgress = true;

        await DiagenLlm.GenerateTextAsyncChunks(
            sessionStates[sessionIndex],
            llmGenerationSettings.agentName,
            llmGenerationSettings.playerName,
            llmGenerationSettings.description,
            message,
            optionTables,
            llmServerSettings,
            llmGenerationSettings,
            null,
            10000,
            (currentText, isDone, isError) =>
            {
                if (responseText != null)
                {
                    responseText.text = currentText; // Display full text immediately
                }

                if (isDone)
                {
                    if (isError)
                    {
                        UnityEngine.Debug.LogError("Error generating text from message");
                    }
                    else if (!string.IsNullOrEmpty(currentText))
                    {
                        dialogueLines.Add(currentText);
                    }
                    else
                    {
                        responseText.text = "I have nothing more to say.";
                        dialogueLines.Add("I have nothing more to say.");
                    }
                }
            }
        );

        inProgress = false;
        await Task.Delay(5000);
        CloseDialogue();
        interacting = false;

        // Re-enable the input field after response is done
        playerInputField.interactable = true;
        playerInputField.ActivateInputField(); // Reactivate for typing

        CheckTrusting();
    }

    private async void CheckTrusting()
    {
        TrustLevelUI trustUI = FindObjectOfType<TrustLevelUI>();

        if (trustUI.trustLevel > 0.5f)
        {
            // Calculate probability: trustLevel / 2
            if (Random.value < trustUI.trustLevel) // Random.value gives a float between 0 and 1 - Replace 0 by Random.value
            {
                List<DiagenEvent> events = DiagenTrigger.ListAvailableEvents(sessionStates[sessionIndex], optionTables);

                if (events.Count > 0) // Ensure the list is not empty
                {
                    DiagenEvent selectedEvent = events[0]; // Select the first event

                    UnityEngine.Debug.Log("Selected event: " + selectedEvent.Name);
                    await TriggerEvent(selectedEvent);
                }
                else
                {
                    UnityEngine.Debug.Log("No available events to trigger.");
                }
            }
            else
            {
                UnityEngine.Debug.Log("Event trigger did not occur (random chance failed).");
            }
        }
    }
    private async Task TriggerEvent(DiagenEvent selectedEvent)
    {
        if (selectedEvent == null)
        {
            UnityEngine.Debug.LogError("TriggerEvent: selectedEvent is null!");
            return;
        }

        UnityEngine.Debug.Log($"TriggerEvent: Processing event '{selectedEvent.Name}'");

        if (!string.IsNullOrEmpty(selectedEvent.SayVerbatim))
        {
            UnityEngine.Debug.Log($"Say Verbatim: {selectedEvent.SayVerbatim}");
            dialogueLines.Add(selectedEvent.SayVerbatim);

            OpenDialogue();
            SetDialogueProcessing();
            inProgress = true;

            responseText.text = selectedEvent.SayVerbatim; // Directly update responseText

            await Task.Delay(5000);
            interacting = false;
        }
        else
        {
            UnityEngine.Debug.Log("No SayVerbatim text found.");
        }

        if (!string.IsNullOrEmpty(selectedEvent.Instruction))
        {
            OpenDialogue();
            SetDialogueProcessing();
            UnityEngine.Debug.Log($"Instruction: {selectedEvent.Instruction}");
            inProgress = true;

            await DiagenTrigger.GenerateTextFromInstructionAsyncChunks(
                sessionStates[sessionIndex],
                llmGenerationSettings.agentName,
                llmGenerationSettings.playerName,
                selectedEvent.Instruction,
                optionTables,
                llmServerSettings,
                llmGenerationSettings,
                null,
                (currentText, isDone, isError) =>
                {

                    if (!string.IsNullOrEmpty(currentText))
                    {
                        responseText.text = currentText; // Display full text immediately
                    }

                    if (isDone)
                    {
                        if (isError)
                        {
                            UnityEngine.Debug.LogError("Error generating text from instruction.");
                        }
                        else if (!string.IsNullOrEmpty(currentText))
                        {
                            dialogueLines.Add(currentText);
                        }
                        else
                        {
                            responseText.text = "I have nothing more to say.";
                            dialogueLines.Add("I have nothing more to say.");
                        }
                    }
                }
            );

            inProgress = false;
            await Task.Delay(5000);
            CloseDialogue();
            interacting = false;

            foreach (var actionEvent in selectedEvent.ActionEvents)
            {
                UnityEngine.Debug.Log($"Executing action event: {actionEvent}");
                actionEvent.Execute();
            }
        }
        else
        {
            UnityEngine.Debug.Log("No Instruction text found.");
        }

        // Refresh global sessions
        sessionStates = DiagenSession.GetGlobalSessions();
        EventSessionsPair eventSessionsPair = DiagenTrigger.TriggerDiagenEvent(sessionStates, selectedEvent.Name, NpcName, optionTables);
        sessionStates = eventSessionsPair.sessionStates;
        DiagenSession.SetGlobalSessions(sessionStates);

        UnityEngine.Debug.Log($"Updated sessionStates for {NpcName}: {string.Join(", ", sessionStates[sessionIndex].EnableStateTags)}");
    }

    private void OpenDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }
        responseText.text = "";
    }

    private void CloseDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    private void SetDialogueProcessing()
    {
        responseText.text = "...";
    }


}
