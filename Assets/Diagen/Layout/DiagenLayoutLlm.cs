#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using System.Threading.Tasks;

using DiagenCommonTypes;
using DiagenLayoutTypes;


namespace Diagen
{
    public class DiagenLayoutLlm : EditorWindow
    {
        private bool showModelParameters = false;
        private bool showPromptParameters = false;
        private int keepNHistory = 8;
        private Vector2 scrollPosition;
        private float totalHeight;
        private bool inProgress = false;
        
        // Reference to the DiagenAPI instance
        private DiagenAPI diagenAPI;
        
        // Method to set the DiagenAPI reference
        public void SetDiagenAPI(DiagenAPI api)
        {
            diagenAPI = api;
        }

        public async Task<Settings> LlmOptionsTab(Settings settings)
        {
            totalHeight = 0;

            GUILayout.BeginHorizontal();
            // Model Parameters
            GUILayout.BeginVertical(GUILayout.Width(Screen.width / 2-10));
            showModelParameters = EditorGUILayout.Foldout(showModelParameters, "Model Parameters", true);
            if (showModelParameters)
            {
                settings = DiagenLayoutCommon.DisplayModelParameters(settings);
                totalHeight += EditorGUIUtility.singleLineHeight * 6 + 20; // Adjust based on the number of elements
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Prompt Parameters
            GUILayout.BeginVertical(GUILayout.Width(Screen.width / 2-10));
            showPromptParameters = EditorGUILayout.Foldout(showPromptParameters, "Prompt Parameters", true);
            if (showPromptParameters)
            {
                settings = DiagenLayoutCommon.DisplayPromptParameters(settings);
                totalHeight += EditorGUIUtility.singleLineHeight * 6 + 20; // Adjust based on the number of elements
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            totalHeight += 10;

            // Dropdown with all Agents
            settings = DiagenLayoutCommon.AgentDropdown(settings);
            totalHeight += EditorGUIUtility.singleLineHeight;

            if (settings.sessionStates.Count > 0)
            {
                GUILayout.Label(settings.llmGenerationSettings.agentName + " Description:", EditorStyles.boldLabel);
                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                textAreaStyle.wordWrap = true;
                GUILayout.BeginVertical(GUILayout.Height(60));
                settings.llmGenerationSettings.description = EditorGUILayout.TextArea(settings.llmGenerationSettings.description, textAreaStyle, GUILayout.ExpandHeight(true));
                GUILayout.EndVertical();
                totalHeight += 60;
                if (GUILayout.Button("Fetch Description"))
                {
                    // Get Agent session
                    Session sessionState = settings.sessionStates.Find(state => state.AgentName == settings.llmGenerationSettings.agentName);
                    settings.llmGenerationSettings.description = DiagenLlm.CreateAgentDescription(sessionState, settings.llmGenerationSettings.agentName, settings.llmGenerationSettings.playerName, settings.optionTables, 100000);
                }
                totalHeight += EditorGUIUtility.singleLineHeight;

                var task = new Task<Settings>(() => settings);
                await displayConversation(settings);
                
                GUILayout.Space(10);
                totalHeight += 10;

                EditorGUI.BeginDisabledGroup(inProgress);
                if (GUILayout.Button("Generate Text"))
                {
                    inProgress = true;
                    
                    if (diagenAPI != null)
                    {
                        diagenAPI.SetRunning(true);
                        diagenAPI.SetError(false);
                        diagenAPI.SetQuestion(false);
                    }
                    
                    // The 'message' for GenerateTextAsyncChunks is the current user input
                    string currentUserMessage = settings.llmGenerationSettings.message;

                    // Prepare history: Add user message and an empty placeholder for agent's response
                    List<History> historyTurn = new List<History>();
                    historyTurn.Add(new History { Name = settings.llmGenerationSettings.playerName, Message = currentUserMessage });
                    historyTurn.Add(new History { Name = settings.llmGenerationSettings.agentName, Message = "" }); // Placeholder for LLM response
                    settings.sessionStates[settings.selectedAgentSession] = DiagenSession.UpdateHistory(settings.sessionStates[settings.selectedAgentSession], historyTurn, -1, keepNHistory);
                    
                    // Reset input message field
                    settings.llmGenerationSettings.message = "";

                    // Variables for UI batching logic (mimicking old fullText and newTokenCount)
                    string currentBatchTextForUI = "";
                    string previousTotalTextFromStream = ""; // To calculate deltas if needed, or use accumulated directly
                    int deltasInCurrentBatchUI = 0;
                    int broadcastNTokenCountUI = 5; // From original code, adjust as needed

                    try
                    {
                        await DiagenLlm.GenerateTextAsyncChunks(
                            settings.sessionStates[settings.selectedAgentSession],
                            settings.llmGenerationSettings.agentName,
                            settings.llmGenerationSettings.playerName,
                            settings.llmGenerationSettings.description,
                            currentUserMessage, // Pass the user's current message for prompt creation
                            settings.optionTables,
                            settings.llmServerSettings,
                            settings.llmGenerationSettings,
                            settings.llmServerProcess,
                            100000, // maxCharacters
                            (accumulatedText, isDone, isError) => {
                                if (isError)
                                {
                                    Debug.LogError("Error during text generation stream.");
                                    inProgress = false;
                                    if (diagenAPI != null) { diagenAPI.SetRunning(false); diagenAPI.SetError(true); }
                                    Repaint();
                                    return;
                                }

                                string newDelta = "";
                                if (accumulatedText.Length > previousTotalTextFromStream.Length)
                                {
                                    newDelta = accumulatedText.Substring(previousTotalTextFromStream.Length);
                                }
                                previousTotalTextFromStream = accumulatedText;

                                if (!string.IsNullOrEmpty(newDelta))
                                {
                                    currentBatchTextForUI += newDelta;
                                    deltasInCurrentBatchUI++;
                                }

                                if (deltasInCurrentBatchUI >= broadcastNTokenCountUI)
                                {
                                    if (DiagenLlm.CheckFullSentence(currentBatchTextForUI))
                                    {   
                                        // Append the validated batch to the last history message (agent's response)
                                        settings.sessionStates[settings.selectedAgentSession].History[^1].Message += currentBatchTextForUI;
                                        currentBatchTextForUI = ""; // Reset the batch
                                    }
                                    deltasInCurrentBatchUI = 0; // Reset delta count for the next batch check
                                }
                                
                                // For a smoother display, one might update a temporary display string with `accumulatedText`
                                // and only commit to `History[^1].Message` based on the batching logic.
                                // However, to keep UI logic similar, we only update history based on batching.
                                // If you want immediate visual feedback of every character, you'd update History[^1].Message = accumulatedText here.
                                // For now, let's ensure the last placeholder reflects the ongoing generation if not batched yet.
                                // A simple way is to update the last history item with the full accumulated text,
                                // but the batching logic above tries to commit only "full sentences" from batches.
                                // Let's ensure the last history item always reflects the latest accumulated text for live update.
                                settings.sessionStates[settings.selectedAgentSession].History[^1].Message = accumulatedText;


                                if (isDone)
                                {
                                    Debug.Log("Finished LLM Stream via GenerateTextAsyncChunks.");
                                    // Ensure any remaining text in currentBatchTextForUI is appended if the batching logic didn't catch it.
                                    // This is covered by setting History[^1].Message = accumulatedText;
                                    // if (!string.IsNullOrEmpty(currentBatchTextForUI)) {
                                    //    settings.sessionStates[settings.selectedAgentSession].History[^1].Message += currentBatchTextForUI;
                                    // }
                                    // Final text is already in accumulatedText
                                    settings.sessionStates[settings.selectedAgentSession].History[^1].Message = accumulatedText;

                                    inProgress = false;
                                    if (diagenAPI != null) diagenAPI.SetRunning(false);
                                }
                                Repaint(); // Redraw the window to show updates
                            }
                        );
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Exception calling GenerateTextAsyncChunks: {ex.Message}");
                        inProgress = false;
                        if (diagenAPI != null) { diagenAPI.SetRunning(false); diagenAPI.SetError(true); }
                        Repaint();
                    }
                }   
                EditorGUI.EndDisabledGroup();
                totalHeight += EditorGUIUtility.singleLineHeight;
            }

            return settings;
        }

        public async Task<Settings> displayConversation(Settings settings)
        {
            GUILayout.BeginHorizontal();
            settings.llmGenerationSettings.includeHistory = EditorGUILayout.Toggle("Include History:", settings.llmGenerationSettings.includeHistory);
            GUILayout.FlexibleSpace();
            // slider to set keepNHistory
            keepNHistory = EditorGUILayout.IntSlider("# of History Elements:", keepNHistory, 1, 20);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("Reset History", GUILayout.Width(100)))
            {
                settings.sessionStates[settings.selectedAgentSession].History.Clear();
            }
            GUILayout.Space(5);
            totalHeight += EditorGUIUtility.singleLineHeight + 5;

            if (settings.llmGenerationSettings.includeHistory == false)
            {

                keepNHistory = 1;

            }

            // if (showHistory)
            {
                int startIdx = settings.sessionStates[settings.selectedAgentSession].History.Count - keepNHistory;
                if (startIdx < 0)
                {
                    startIdx = 0;
                }

                float scrollViewHeight = 140;
                if (totalHeight + scrollViewHeight > 800)
                {
                    scrollViewHeight = 800 - totalHeight;
                }

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollViewHeight));
                for (int i = 0; i < settings.sessionStates[settings.selectedAgentSession].History.Count; i++)
                {
                    if (i < startIdx) // Skip elements before startIdx
                    {
                        continue;
                    }

                    // Agent history displayed on right
                    if (settings.sessionStates[settings.selectedAgentSession].History[i].Name != settings.llmGenerationSettings.playerName)
                    {
                        // Show Box with Agent Name and Message
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", GUILayout.Width(100)); // Empty space element
                        GUILayout.FlexibleSpace();

                        // Add remove button
                        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                        buttonStyle.normal.textColor = Color.red;
                        if (GUILayout.Button(new GUIContent("X", "Delete Text"), buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            settings.sessionStates[settings.selectedAgentSession].History.RemoveAt(i);
                            break;
                        }
                        settings.sessionStates[settings.selectedAgentSession].History[i].Message = EditorGUILayout.TextArea(settings.sessionStates[settings.selectedAgentSession].History[i].Message, EditorStyles.textArea, GUILayout.MaxWidth(500), GUILayout.ExpandHeight(true));
                        
                        GUILayout.BeginVertical();
                        GUILayout.Label(settings.sessionStates[settings.selectedAgentSession].History[i].Name, EditorStyles.boldLabel, GUILayout.Width(100));
                        // Add two buttons: One "+" with tooltip "Generate Filler" and one with a regenerate icon to regenerate the message
                        GUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(settings.llmServerProcess == null || settings.llmServerProcess.HasExited);
                        if (GUILayout.Button(new GUIContent("+", "Generate Filler"), GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            List<Prompt> fillerPrompt = DiagenLlm.CreateFillerPrompt(settings.sessionStates[settings.selectedAgentSession], settings.llmGenerationSettings.agentName, settings.llmGenerationSettings.playerName, 10000, settings.optionTables, 0, i + 1);
                            List<History> history = new List<History>();
                            history.Add(new History { Name = settings.llmGenerationSettings.agentName, Message = "" });
                            settings.sessionStates[settings.selectedAgentSession] = DiagenSession.UpdateHistory(settings.sessionStates[settings.selectedAgentSession], history, i + 1, -1);
                            
                            // Reset message
                            settings.llmGenerationSettings.message = "";

                        var progress = new System.Progress<string>(chunk =>
                        {
                            // Skip [DONE] line
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

                            // Append raw line to last history message
                            settings.sessionStates[settings.selectedAgentSession].History[^1].Message += chunk;
                            Repaint();
                        });


                            // Ensure there is a placeholder for the response
                            settings.sessionStates[settings.selectedAgentSession].History.Add(new History
                            {
                                Name = settings.llmGenerationSettings.agentName,
                                Message = ""
                            });

                            await DiagenSubsystem.GenerateTextStream(settings.llmServerProcess, settings.llmServerSettings, settings.llmGenerationSettings, fillerPrompt, progress);
                        }
                        if (GUILayout.Button(new GUIContent("↺","Regenerate Text"), GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            List<Prompt> conversationPrompt = DiagenLlm.CreateConversationPrompt(settings.sessionStates[settings.selectedAgentSession], settings.llmGenerationSettings.agentName, settings.llmGenerationSettings.playerName, settings.llmGenerationSettings.description, settings.llmGenerationSettings.includeHistory, settings.llmGenerationSettings.message, 100000, settings.optionTables, 0, i);
                            
                            // Check if ctx is exceeded
                            int promptTokenCount = DiagenLlm.PromptTokenCount(conversationPrompt);
                            if (settings.llmServerSettings.contextSize < promptTokenCount)
                            {
                                Debug.LogWarning("[LLM Generation] Context size exceeded. The LLM might not be able to process all of your input. Count: " + promptTokenCount + " > " + settings.llmServerSettings.contextSize);
                                // return null;
                            }

                            settings.sessionStates[settings.selectedAgentSession].History[i].Message = "";
                            
                            // Reset message
                            settings.llmGenerationSettings.message = "";

                            var progress = new System.Progress<string>(chunk =>
                            {
                                // split chunk on first occurance of "data: " to get the actual data
                                chunk = chunk.Split(new string[] { "data: " }, System.StringSplitOptions.None)[1];
                                
                                // Parse chunk as LlmStreamResponse
                                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                                {
                                    MissingMemberHandling = MissingMemberHandling.Ignore
                                };

                                try
                                {
                                    LlmStreamResponse llmResponse = JsonConvert.DeserializeObject<LlmStreamResponse>(chunk, jsonSettings);

                                    // Append to latest History element
                                    settings.sessionStates[settings.selectedAgentSession].History[i].Message += llmResponse.choices[0].delta.content;

                                    // if finish reason not null, add to history
                                    if (llmResponse.choices[0].finish_reason != null && llmResponse.choices[0].finish_reason != "")
                                    {
                                        Debug.Log("Finished LLM Stream");
                                    }

                                    // Force the window to redraw
                                    Repaint();
                                }
                                catch (JsonSerializationException)
                                {
                                    //Error
                                }
                            });

                            await DiagenSubsystem.GenerateTextStream(settings.llmServerProcess, settings.llmServerSettings, settings.llmGenerationSettings, conversationPrompt, progress);
                        }
                        EditorGUI.EndDisabledGroup();
                        // Move down button
                        if (GUILayout.Button(new GUIContent("▼", "Move Down"), GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            if (i < settings.sessionStates[settings.selectedAgentSession].History.Count - 1)
                            {
                                var temp = settings.sessionStates[settings.selectedAgentSession].History[i];
                                settings.sessionStates[settings.selectedAgentSession].History[i] = settings.sessionStates[settings.selectedAgentSession].History[i + 1];
                                settings.sessionStates[settings.selectedAgentSession].History[i + 1] = temp;
                            }
                        }
                        // Move up button
                        if (GUILayout.Button(new GUIContent("▲", "Move Up"), GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            if (i > 0)
                            {
                                var temp = settings.sessionStates[settings.selectedAgentSession].History[i];
                                settings.sessionStates[settings.selectedAgentSession].History[i] = settings.sessionStates[settings.selectedAgentSession].History[i - 1];
                                settings.sessionStates[settings.selectedAgentSession].History[i - 1] = temp;
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }
                    else 
                    // Player history displayed on left
                    {
                        // Show Box with Player Name and Message
                        GUILayout.BeginHorizontal();

                        GUILayout.BeginVertical();
                        GUILayout.Label(settings.sessionStates[settings.selectedAgentSession].History[i].Name, EditorStyles.boldLabel, GUILayout.Width(100));
                        // Add two buttons: One "+" with tooltip "Generate Filler" and one with a regenerate icon to regenerate the message
                        GUILayout.BeginHorizontal();
                        // Move down button
                        if (GUILayout.Button(new GUIContent("▼", "Move Down"), GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            if (i < settings.sessionStates[settings.selectedAgentSession].History.Count - 1)
                            {
                                var temp = settings.sessionStates[settings.selectedAgentSession].History[i];
                                settings.sessionStates[settings.selectedAgentSession].History[i] = settings.sessionStates[settings.selectedAgentSession].History[i + 1];
                                settings.sessionStates[settings.selectedAgentSession].History[i + 1] = temp;
                            }
                        }
                        // Move up button
                        if (GUILayout.Button(new GUIContent("▲", "Move Up"), GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            if (i > 0)
                            {
                                var temp = settings.sessionStates[settings.selectedAgentSession].History[i];
                                settings.sessionStates[settings.selectedAgentSession].History[i] = settings.sessionStates[settings.selectedAgentSession].History[i - 1];
                                settings.sessionStates[settings.selectedAgentSession].History[i - 1] = temp;
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        
                        settings.sessionStates[settings.selectedAgentSession].History[i].Message = EditorGUILayout.TextArea(settings.sessionStates[settings.selectedAgentSession].History[i].Message, EditorStyles.textArea, GUILayout.MaxWidth(500));

                        // Add a button with a trash can icon in red to delete the message
                        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                        buttonStyle.normal.textColor = Color.red;
                        if (GUILayout.Button(new GUIContent("X", "Delete Text"), buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            settings.sessionStates[settings.selectedAgentSession].History.RemoveAt(i);
                            break;
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("", GUILayout.Width(100)); // Empty space element
                        GUILayout.EndHorizontal();
                    }
                    // Space
                    GUILayout.Space(5);
                }
                EditorGUILayout.EndScrollView();
                totalHeight += scrollViewHeight;
            }

            // Add new message
            GUILayout.BeginHorizontal();
            GUILayout.Label(settings.llmGenerationSettings.playerName, EditorStyles.boldLabel, GUILayout.Width(100));
            settings.llmGenerationSettings.message = EditorGUILayout.TextArea(settings.llmGenerationSettings.message, EditorStyles.textArea, GUILayout.MaxWidth(500));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(100)); // Empty space element
            GUILayout.EndHorizontal();
            totalHeight += EditorGUIUtility.singleLineHeight;

            return settings;
        }

        private void SaveHistoryToCSV(List<History> history)
        {
            string filePath = EditorUtility.SaveFilePanel("Save History to CSV", "", "history.csv", "csv");
            if (!string.IsNullOrEmpty(filePath))
            {
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("Speaker,Message");

                foreach (var entry in history)
                {
                    csv.AppendLine($"\"{entry.Name}\",\"{DiagenUtilities.ReplaceCharWithEscapedChar(entry.Message)}\"");
                }

                File.WriteAllText(filePath, csv.ToString());
                Debug.Log($"History saved to: {filePath}");
            }
        }
    }

}
#endif