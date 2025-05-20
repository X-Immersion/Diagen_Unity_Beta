//#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

using DiagenLayoutTypes;
using DiagenCommonTypes;

namespace Diagen
{
    public class DiagenLayoutEvent : EditorWindow
    {
        private bool showModelParameters = false;
        private bool showPromptParameters = false;
        private int selectedEvent = 0;
        private bool inProgress = false;
        private DiagenAPI diagenAPI;
        public void SetDiagenAPI(DiagenAPI api)
        {
            diagenAPI = api;
        }
        private DiagenEvent lastDiagenEvent = new DiagenEvent();
        private Vector2 scrollPositionEventResult = Vector2.zero;
        private Vector2 scrollPositionSessionInfo = Vector2.zero;
        public Settings EventsTab(Settings settings)
        {

            EventSessionsPair eventSessionsPair = new EventSessionsPair();

            GUILayout.Label("Event Generation", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            // Model Parameters
            GUILayout.BeginVertical(GUILayout.Width(Screen.width / 2-10));
            showModelParameters = EditorGUILayout.Foldout(showModelParameters, "Model Parameters", true);
            if (showModelParameters)
            {
                settings = DiagenLayoutCommon.DisplayModelParameters(settings);
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Prompt Parameters
            GUILayout.BeginVertical(GUILayout.Width(Screen.width / 2-10));
            showPromptParameters = EditorGUILayout.Foldout(showPromptParameters, "Prompt Parameters", true);
            if (showPromptParameters)
            {
                settings = DiagenLayoutCommon.DisplayPromptParameters(settings);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            
            settings = DiagenLayoutCommon.AgentDropdown(settings);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            string eventName = EventsDropdown(settings.sessionStates[settings.selectedAgentSession], settings);

            // Disable the button if eventName is null or empty
            GUI.enabled = !string.IsNullOrEmpty(eventName);

            bool generateEvent = GUILayout.Button("Execute Event", GUILayout.Width(150));

            // Re-enable GUI after the button
            GUI.enabled = true;

            if (generateEvent)
            {
                eventSessionsPair = DiagenTrigger.TriggerDiagenEvent(settings.sessionStates, eventName, settings.llmGenerationSettings.agentName, settings.optionTables);
                if (eventSessionsPair != null)
                {
                    settings.sessionStates = eventSessionsPair.sessionStates;
                    lastDiagenEvent = eventSessionsPair.diagenEvent;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("Event Results", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            var task = new Task<Settings>(() => settings);
            task = DisplayEventResult(settings, lastDiagenEvent);
            task.ContinueWith(t => settings = t.Result, TaskScheduler.FromCurrentSynchronizationContext());
            // settings = DisplayEventResult(settings, lastDiagenEvent);
            
            GUILayout.Space(20);
            GUILayout.Label("Session Info Results", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            scrollPositionSessionInfo = EditorGUILayout.BeginScrollView(scrollPositionSessionInfo, GUILayout.Height(150));
            DiagenLayoutCommon.DisplaySessionState(settings);
            EditorGUILayout.EndScrollView();

            return settings;
        }

        private string EventsDropdown(Session sessionState, Settings settings)
        {
            // Get the list of available events
            List<DiagenEvent> events = DiagenTrigger.ListAvailableEvents(sessionState, settings.optionTables);
    
            // Check if events is null or empty
            if (events == null || events.Count == 0)
            {
                return string.Empty; // Return an empty string if no events are available
            }
    
            // Extract event names
            string[] eventNames = events.Select(e => e.Name).ToArray();
    
            // Ensure selectedEvent is within the valid range
            selectedEvent = Mathf.Clamp(selectedEvent, 0, eventNames.Length - 1);
    
            // Add dropdown for events
            selectedEvent = EditorGUILayout.Popup("Select Event:", selectedEvent, eventNames);
    
            return eventNames[selectedEvent];
        }

        private async Task<Settings> DisplayEventResult(Settings settings, DiagenEvent diagenEvent)
        {

            if (diagenEvent == null || string.IsNullOrEmpty(diagenEvent.Name))
            {
                GUILayout.Label("No event executed", EditorStyles.label);
            }
            else
            {
                scrollPositionEventResult = EditorGUILayout.BeginScrollView(scrollPositionEventResult, GUILayout.Height(230));
                GUILayout.BeginHorizontal();
                GUILayout.Label("Event Name:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                GUILayout.Label(diagenEvent.Name, EditorStyles.label);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Say Verbatim:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(true);
                var SayVerbatimHeight = EditorStyles.textArea.CalcHeight(new GUIContent(diagenEvent.SayVerbatim), 500);
                diagenEvent.SayVerbatim = EditorGUILayout.TextArea(diagenEvent.SayVerbatim, EditorStyles.textArea, GUILayout.MaxWidth(500), GUILayout.Height(SayVerbatimHeight));
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(5);
                // Button to add Verbatime to History
                if (!string.IsNullOrEmpty(diagenEvent.SayVerbatim))
                {
                    if (GUILayout.Button(new GUIContent("+","Add to History"), GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        List<History> history = new List<History>();
                        // Append diagenEvent.SayVerbatime to history with Agent Name
                        history.Add(new History { Name = settings.llmGenerationSettings.agentName, Message = diagenEvent.SayVerbatim });
                        settings.sessionStates[settings.selectedAgentSession] = DiagenSession.UpdateHistory(settings.sessionStates[settings.selectedAgentSession], history, -1, -1);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                

                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Instruction:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(true);
                var instructionHeight = EditorStyles.textArea.CalcHeight(new GUIContent(diagenEvent.Instruction), 500);
                diagenEvent.Instruction = EditorGUILayout.TextArea(diagenEvent.Instruction, EditorStyles.textArea, GUILayout.MaxWidth(500), GUILayout.Height(instructionHeight));
                EditorGUI.EndDisabledGroup();
                if (!string.IsNullOrEmpty(diagenEvent.Instruction))
                {
                    GUILayout.Space(5);
                    EditorGUI.BeginDisabledGroup(inProgress);
                    if (GUILayout.Button(new GUIContent("↺", "Convert to Verbatime"), GUILayout.Width(100), GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        inProgress = true;
                        // Set the animation running when generating text
                        if (diagenAPI != null)
                        {
                            diagenAPI.SetRunning(true);
                            diagenAPI.SetError(false);
                            diagenAPI.SetQuestion(false);
                        }

                        // Create instruction Prompt 
                        List<Prompt> instructionPrompt = DiagenLlm.CreateInstructionPrompt(settings.sessionStates[settings.selectedAgentSession], settings.llmGenerationSettings.agentName, settings.llmGenerationSettings.playerName, diagenEvent.Instruction, settings.optionTables);
                        // Check if ctx is exceeded
                        int promptTokenCount = DiagenLlm.PromptTokenCount(instructionPrompt);
                        if (settings.llmServerSettings.contextSize < promptTokenCount)
                        {
                            Debug.LogWarning("[Event Generation] Context size exceeded. The LLM might not be able to process all of your input. Count: " + promptTokenCount + " > " + settings.llmServerSettings.contextSize);
                            // return null;
                        }

                        diagenEvent.SayVerbatim = "";

                        var progress = new System.Progress<string>(chunk =>
                        {
                            if (chunk == "[DONE]" || chunk == "data: [DONE]")
                            {
                                inProgress = false;
                                if (diagenAPI != null)
                                {
                                    diagenAPI.SetRunning(false);
                                }
                                Repaint();
                                return;
                            }

                            chunk = chunk.Split(new string[] { "data: " }, System.StringSplitOptions.None)[1];

                            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore
                            };

                            try
                            {
                                LlmStreamResponse llmResponse = JsonConvert.DeserializeObject<LlmStreamResponse>(chunk, jsonSettings);

                                if (llmResponse.choices[0].delta != null && !string.IsNullOrEmpty(llmResponse.choices[0].delta.content))
                                {
                                    diagenEvent.SayVerbatim += llmResponse.choices[0].delta.content;
                                }

                                if (!string.IsNullOrEmpty(diagenEvent.SayVerbatim) && chunk.Contains("\"finish_reason\""))
                                {
                                    inProgress = false;
                                    if (diagenAPI != null)
                                    {
                                        diagenAPI.SetRunning(false);
                                    }
                                }

                                Repaint();
                            }
                            catch (JsonSerializationException ex)
                            {
                                Debug.LogError($"Error processing LLM response: {ex.Message}");
                                inProgress = false;
                                if (diagenAPI != null)
                                {
                                    diagenAPI.SetRunning(false);
                                    diagenAPI.SetError(true);
                                }
                            }
                        });

                        await DiagenSubsystem.GenerateTextStream(settings.llmServerProcess, settings.llmServerSettings, settings.llmGenerationSettings, instructionPrompt, progress);
                    }
                }
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                

                GUILayout.BeginHorizontal();
                GUILayout.Label("Return Trigger:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(true);
                var returnTriggerHeight = EditorStyles.textArea.CalcHeight(new GUIContent(diagenEvent.ReturnTrigger), 500);
                diagenEvent.ReturnTrigger = EditorGUILayout.TextArea(diagenEvent.ReturnTrigger, EditorStyles.textArea, GUILayout.MaxWidth(500), GUILayout.Height(returnTriggerHeight));
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Repeatable:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Toggle(diagenEvent.Repeatable, "", GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                List<BoxInfo> enableStateTags = new List<BoxInfo>();
                diagenEvent.EnableStateTags.ToList().ForEach(tag => enableStateTags.Add(new BoxInfo { title = tag, content = "" }));
                GUILayout.Label("Enable State Tags:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                DiagenLayoutCommon.DisplayColumns(4, enableStateTags);
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                List<BoxInfo> disableStateTags = new List<BoxInfo>();
                diagenEvent.DisableStateTags.ToList().ForEach(tag => disableStateTags.Add(new BoxInfo { title = tag, content = "" }));
                GUILayout.Label("Disable State Tags:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
                DiagenLayoutCommon.DisplayColumns(4, disableStateTags);
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.EndScrollView();
            }


            return settings;
        }
        
        private void UpdateNPCDialogue(string dialogue)
        {
            DiagenTriggerEvent npc = FindObjectOfType<DiagenTriggerEvent>();
            if (npc != null)
            {
                npc.SetDialogue(dialogue);
                Debug.Log("Updated NPC dialogue with: " + dialogue);
            }
            else
            {
                Debug.LogError("DiagenTriggerEvent not found in the scene!");
            }
        }
    }
}
//#endif