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
using System;
using DiagenLayoutTypes;
using DiagenCommonTypes;

namespace Diagen
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections;

    public class GifEditorWindow : EditorWindow
    {
        private Texture2D[] gifFrames;
        private int currentFrame = 0;
        private float frameRate = 0.1f; // Adjust for speed
        private float nextFrameTime;

        [MenuItem("Window/Gif Viewer")]
        public static void ShowWindow()
        {
            GifEditorWindow window = GetWindow<GifEditorWindow>();
            window.titleContent = new GUIContent("GIF Viewer");
            window.position = new Rect(10, Screen.height - 200, 200, 200); // Bottom-left corner
        }

        private void OnEnable()
        {
            LoadGifFrames();
            EditorApplication.update += UpdateFrame;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateFrame;
        }

        private void LoadGifFrames()
        {
            // Load GIF frames from Resources folder (place images in "Assets/Resources/GifFrames/")
            gifFrames = new Texture2D[10]; // Change according to the number of frames
            for (int i = 0; i < gifFrames.Length; i++)
            {
                gifFrames[i] = Resources.Load<Texture2D>($"Assets/Diagen/Images/frame_{i}");
            }
        }

        private void UpdateFrame()
        {
            if (gifFrames == null || gifFrames.Length == 0)
                return;

            if (EditorApplication.timeSinceStartup >= nextFrameTime)
            {
                currentFrame = (currentFrame + 1) % gifFrames.Length;
                nextFrameTime = (float)EditorApplication.timeSinceStartup + frameRate;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (gifFrames == null || gifFrames.Length == 0 || gifFrames[currentFrame] == null)
            {
                EditorGUILayout.LabelField("No GIF frames found!");
                return;
            }

            GUILayout.Space(position.height - 180); // Position it lower
            GUI.DrawTexture(new Rect(10, position.height - 150, 128, 128), gifFrames[currentFrame], ScaleMode.ScaleToFit);
        }
    }


    public class DiagenLayoutOption : EditorWindow
    {
        private string newAgentName = "";
        private bool showModelParameters = false;
        private bool showPromptParameters = false;
        private Vector2 scrollPosition;
        private float totalHeight;

        public Settings SessionOptionsTab(Settings settings)
        {
            totalHeight = 0;

            GUILayout.BeginHorizontal();
            // Model Parameters
            GUILayout.BeginVertical(GUILayout.Width(Screen.width / 2-10));
            showModelParameters = EditorGUILayout.Foldout(showModelParameters, "Model Parameters", true);
            if (showModelParameters)
            {
                settings.llmServerSettings.gpuLayers = EditorGUILayout.IntField("GPU Layers (-1 for auto):", settings.llmServerSettings.gpuLayers);
                settings.llmServerSettings.contextSize = EditorGUILayout.IntField("Context Size:", settings.llmServerSettings.contextSize);
                settings.llmServerSettings.flashAttention = EditorGUILayout.Toggle("Enable Flash Attention:", settings.llmServerSettings.flashAttention);
                settings.llmServerSettings.gpuSelect = EditorGUILayout.IntField("GPU Select (-1 for auto):", settings.llmServerSettings.gpuSelect);
                settings.llmServerSettings.utilization = EditorGUILayout.Slider("Utilization:", settings.llmServerSettings.utilization, 0.1f, 1.0f);
                totalHeight += EditorGUIUtility.singleLineHeight * 5 + 20; // Adjust based on the number of elements
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Prompt Parameters
            GUILayout.BeginVertical(GUILayout.Width(Screen.width / 2-10));
            showPromptParameters = EditorGUILayout.Foldout(showPromptParameters, "Prompt Parameters", true);
            if (showPromptParameters)
            {
                settings.llmGenerationSettings.nPredict = EditorGUILayout.IntField("Length (token per generation):", settings.llmGenerationSettings.nPredict);
                settings.llmGenerationSettings.temperature = EditorGUILayout.Slider("Temperature:", settings.llmGenerationSettings.temperature, 0.1f, 2.0f);
                settings.llmGenerationSettings.topK = EditorGUILayout.IntField("Top K:", settings.llmGenerationSettings.topK);
                settings.llmGenerationSettings.topP = EditorGUILayout.Slider("Top P:", settings.llmGenerationSettings.topP, 0.1f, 1.0f);
                settings.llmGenerationSettings.frequencyPenalty = EditorGUILayout.Slider("Frequency Penalty:", settings.llmGenerationSettings.frequencyPenalty, 0.0f, 2.0f);
                settings.llmGenerationSettings.presencePenalty = EditorGUILayout.Slider("Presence Penalty:", settings.llmGenerationSettings.presencePenalty, 0.0f, 2.0f);
                totalHeight += EditorGUIUtility.singleLineHeight * 6 + 20; // Adjust based on the number of elements
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            totalHeight += 10;


            GUILayout.Label("Session Parameters", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.Space(10);
            // Field to select asset of type TopicTable
            settings.optionTables.characterInformationTable = (CharacterInformationTable)EditorGUILayout.ObjectField("Character Information Table", settings.optionTables.characterInformationTable, typeof(CharacterInformationTable), false);

            GUILayout.Space(5);
            // Field to select asset of type EventTable
            settings.optionTables.eventsTable = (EventsTable)EditorGUILayout.ObjectField("Event Table", settings.optionTables.eventsTable, typeof(EventsTable), false);

            GUILayout.Space(5);
            // Field to select asset of type CharacterInformationTable
            settings.optionTables.topicTable = (TopicTable)EditorGUILayout.ObjectField("Topic Table", settings.optionTables.topicTable, typeof(TopicTable), false);
            
            GUILayout.Space(5);
            // Field to select asset of type State Tags weight
            settings.optionTables.stateTagsWeightTable = (StateTagsWeightTable)EditorGUILayout.ObjectField("State Tags Weight", settings.optionTables.stateTagsWeightTable, typeof(StateTagsWeightTable), false);

            GUILayout.Space(10);

            GUILayout.Label("Initialize Agents", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            GUILayout.Space(10);
            // Input field for Agent Name
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            newAgentName = EditorGUILayout.TextField("Agent Name:", newAgentName);

            // D�sactiver le bouton si newAgentName est vide
            GUI.enabled = !string.IsNullOrEmpty(newAgentName);

            // Initialiser le bouton Agent
            bool initAgent = GUILayout.Button("Initialize", GUILayout.Width(150));

            // R�activer l'interaction des autres �l�ments UI
            GUI.enabled = true;

            if (initAgent)
            {
                settings.sessionStates = DiagenSession.InitSession(settings.sessionStates, newAgentName);
                settings.sessionStates = DiagenSession.BindAllStateTagsToAgent(settings.sessionStates, newAgentName, settings.optionTables);

                // D�finir le Agent s�lectionn� sur le nouvellement cr��
                settings.selectedAgentSession = settings.sessionStates.Count - 1;
                settings.llmGenerationSettings.agentName = settings.sessionStates[settings.selectedAgentSession].AgentName;

                // Show a pop-up message
                EditorUtility.DisplayDialog("Agent Initialization", $"Agent {newAgentName} initialized", "OK");
            }


            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (settings.sessionStates.Count > 0)
            {
                GUILayout.Label("Select Agent", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
                GUILayout.Space(5);

                // Dropdown with all Agents
                settings = DiagenLayoutCommon.AgentDropdown(settings);

                // Ensure selectedAgentSession is within valid range
                if (settings.selectedAgentSession >= settings.sessionStates.Count)
                {
                    settings.selectedAgentSession = settings.sessionStates.Count - 1; // Reset to last valid index
                }

                if (settings.selectedAgentSession >= 0) // Only proceed if there's at least one valid session
                {
                    // Multi Select that shows all state tags and Enabled state tags are selected
                    List<Tag> allStateTags = new List<Tag>();
                    List<string> enabledStateTags = new List<string>();

                    string agentName = settings.sessionStates[settings.selectedAgentSession].AgentName;

                    allStateTags = DiagenStateTags.GetAgentAllStateTags(settings.sessionStates, agentName);
                    enabledStateTags = DiagenStateTags.GetAgentEnabledStateTags(settings.sessionStates, agentName);

                    GUILayout.Space(5);
                    GUILayout.Label("State Tags", new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });

                    GUILayout.Space(5);

                    // Show all state tags
                    int elementsPerCurrentRow = 4;
                    for (int i = 0; i < allStateTags.Count; i += elementsPerCurrentRow)
                    {
                        GUILayout.BeginHorizontal();
                        for (int j = 0; j < elementsPerCurrentRow; j++)
                        {
                            if (i + j >= allStateTags.Count) break;
                            var tag = allStateTags[i + j];
                            bool enabled = enabledStateTags.Contains(tag.tag);
                            bool newEnabled = EditorGUILayout.ToggleLeft(tag.tag, enabled, GUILayout.Width(200));
                            if (newEnabled != enabled)
                            {
                                if (newEnabled)
                                {
                                    settings.sessionStates = DiagenStateTags.AppendStateTags(settings.sessionStates, agentName, new List<Tag> { tag });
                                }
                                else
                                {
                                    settings.sessionStates = DiagenStateTags.RemoveStateTags(settings.sessionStates, agentName, new List<Tag> { tag });
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                    // If enabled state tags changed, call EnableTopics function
                    if (enabledStateTags != DiagenStateTags.GetAgentEnabledStateTags(settings.sessionStates, agentName))
                    {
                        settings.sessionStates[settings.selectedAgentSession] = DiagenTopic.EnableTopics(settings.sessionStates[settings.selectedAgentSession], agentName, settings.optionTables);
                    }
                }

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();

                // Update Session button
                bool updateSession = GUILayout.Button("Update Agent States");
                if (updateSession)
                {
                    settings.sessionStates = DiagenSession.BindAllStateTagsToAgent(settings.sessionStates, settings.llmGenerationSettings.agentName, settings.optionTables);
                    settings.sessionStates = DiagenSession.UpdateSessionWithOptions(settings.sessionStates, settings.llmGenerationSettings.agentName, settings.optionTables);
                }

                // Reset Session button
                bool resetSession = GUILayout.Button("Reset Agent States");
                if (resetSession)
                {
                    settings.sessionStates = DiagenSession.ResetSession(settings.sessionStates, settings.llmGenerationSettings.agentName);
                    settings.sessionStates = DiagenSession.BindAllStateTagsToAgent(settings.sessionStates, settings.llmGenerationSettings.agentName, settings.optionTables);
                }

                GUILayout.Space(10);

                // Remove Session button
                bool removeSession = GUILayout.Button("Remove Agent");
                if (removeSession)
                {
                    settings.sessionStates = DiagenSession.RemoveSession(settings.sessionStates, settings.llmGenerationSettings.agentName);

                    // Ensure valid index after removal
                    if (settings.sessionStates.Count > 0)
                    {
                        settings.selectedAgentSession = Math.Max(0, settings.selectedAgentSession - 1); // Set to previous valid index
                        settings.llmGenerationSettings.agentName = settings.sessionStates[settings.selectedAgentSession].AgentName;
                    }
                    else
                    {
                        settings.selectedAgentSession = 0;
                        settings.llmGenerationSettings.agentName = "";
                    }
                }
                GUILayout.EndHorizontal();
            }


            GUILayout.Space(10);

            GUILayout.Label("Connect to Server", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.Space(5);
            settings.llmServerSettings.apiKey = EditorGUILayout.TextField("API Key", settings.llmServerSettings.apiKey);

            if (GUILayout.Button("Initialize Remote Session"))
            {
                _ = DiagenSubsystem.InitRemoteSession(settings.llmServerSettings.apiKey);
            }

            GUILayout.Space(10);

            //EditorGUI.BeginDisabledGroup(settings.llmServerProcess != null && !settings.llmServerProcess.HasExited);
            /*/
            if (GUILayout.Button("Start LLM Server"))
            {
                settings.llmServerProcess = DiagenSubsystem.StartLlamaServer(settings.llmServerSettings, settings.llmServerProcess);

                // Log swerve server address
                Debug.Log("Server Address: " + settings.llmServerSettings.serverAddress + ":" + settings.llmServerSettings.serverPort);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(settings.llmServerProcess == null || settings.llmServerProcess.HasExited);
            if (GUILayout.Button("Stop LLM Server"))
            {
                settings.llmServerProcess = DiagenSubsystem.StopLlamaServer(settings.llmServerProcess);
                Debug.Log("CLosed server at: " + settings.llmServerSettings.serverAddress + ":" + settings.llmServerSettings.serverPort);

            }
            /*/
            EditorGUI.EndDisabledGroup();

            return settings;
        }


    }


}
#endif