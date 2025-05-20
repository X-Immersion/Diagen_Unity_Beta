#if UNITY_EDITOR

using System.Security.AccessControl;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Diagen;
using DiagenCommonTypes;
using DiagenLayoutTypes;

namespace DiagenLayoutTypes
{

    public class DiagenLayoutCommon : EditorWindow
    {
        public static Settings AgentDropdown(Settings settings)
        {
            // Ensure sessionStates has at least one Agent
            if (settings.sessionStates == null || settings.sessionStates.Count == 0)
            {
                settings.sessionStates = new List<Session>
                {
                    new Session { AgentName = "Agent" } // Default Agent
                };
            }

            // Dropdown with all Agents
            string[] agentNames = new string[settings.sessionStates.Count];
            for (int i = 0; i < settings.sessionStates.Count; i++)
            {
                agentNames[i] = settings.sessionStates[i].AgentName;
            }

            settings.llmGenerationSettings.playerName = EditorGUILayout.TextField("User Name:", settings.llmGenerationSettings.playerName);

            GUILayout.Space(5);

            settings.selectedAgentSession = System.Array.IndexOf(agentNames, settings.llmGenerationSettings.agentName);
            if (settings.selectedAgentSession < 0) settings.selectedAgentSession = 0; // Default to first Agent if not found

            int selectedAgent = EditorGUILayout.Popup("Select Agent:", settings.selectedAgentSession, agentNames);
            settings.llmGenerationSettings.agentName = agentNames[selectedAgent];

            GUILayout.Space(5);
            return settings;
        }

        public static void DisplaySessionState(Settings settings)
        {
            // Ensure sessionStates has at least one Agent
            if (settings.sessionStates == null || settings.sessionStates.Count == 0)
            {
                settings.sessionStates = new List<Session>
                {
                    new Session { AgentName = "Placeholder" } // Default Agent
                };
            }

            string[] agentNames = new string[settings.sessionStates.Count];
            for (int i = 0; i < settings.sessionStates.Count; i++)
            {
                agentNames[i] = settings.sessionStates[i].AgentName;
            }

            settings.selectedAgentSession = System.Array.IndexOf(agentNames, settings.llmGenerationSettings.agentName);
            if (settings.selectedAgentSession < 0) settings.selectedAgentSession = 0; // Default to first Agent if not found

            GUILayout.BeginHorizontal();
            GUILayout.Label("Agent Name:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
            EditorGUI.BeginDisabledGroup(true);
            var agentNameHeight = EditorStyles.textArea.CalcHeight(new GUIContent(settings.sessionStates[settings.selectedAgentSession].AgentName), 500);
            settings.sessionStates[settings.selectedAgentSession].AgentName = EditorGUILayout.TextArea(settings.sessionStates[settings.selectedAgentSession].AgentName, EditorStyles.textArea, GUILayout.MaxWidth(500), GUILayout.Height(agentNameHeight));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            List<BoxInfo> enableStateTags = new List<BoxInfo>();
            settings.sessionStates[settings.selectedAgentSession].EnableStateTags.ForEach(tag => enableStateTags.Add(new BoxInfo { title = tag, content = "" }));
            GUILayout.Label("Enable State Tags:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
            DisplayColumns(8, enableStateTags, 600f);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("All State Tags:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
            List<BoxInfo> tags = new List<BoxInfo>();
            settings.sessionStates[settings.selectedAgentSession].AllStateTags.ForEach(tag => tags.Add(new BoxInfo { title = tag.tag, content = "Weight: " + tag.weight.ToString() }));
            DisplayColumns(8, tags, 600f);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            List<BoxInfo> enableTopics = new List<BoxInfo>();
            settings.sessionStates[settings.selectedAgentSession].EnableTopics.ForEach(topic => enableTopics.Add(new BoxInfo { title = topic.Name, content = topic.Description }));
            GUILayout.Label("Enable Topics:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
            DisplayColumns(8, enableTopics, 500f);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            List<BoxInfo> executedDiagenEvent = new List<BoxInfo>();
            settings.sessionStates[settings.selectedAgentSession].ExecutedDiagenEvent.ForEach(e => executedDiagenEvent.Add(new BoxInfo { title = e, content = "" }));
            GUILayout.Label("Executed Diagen Event:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
            DisplayColumns(8, executedDiagenEvent, 600f);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            // GUILayout.BeginHorizontal();
            // GUILayout.Label("History:", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }, GUILayout.Width(150));
            // EditorGUI.BeginDisabledGroup(true);

            // string history = string.Join(", ", settings.sessionStates[settings.selectedAgentSession].History.Select(h => h.Message));
            // var historyHeight = EditorStyles.textArea.CalcHeight(new GUIContent(history), 500);
            // history = EditorGUILayout.TextArea(history, EditorStyles.textArea, GUILayout.MaxWidth(500), GUILayout.Height(historyHeight));
            // EditorGUI.EndDisabledGroup();
            // GUILayout.EndHorizontal();
            // GUILayout.Space(5);
        }

        public static void SaveSettings(Settings settings, string sessionStatesFilePath)
        {
            // Save Options Table
            string json = JsonUtility.ToJson(settings.optionTables);
            EditorPrefs.SetString("DiagenOptionTables", json);

            // Save SessionStates
            DiagenSession.SaveSessionStates(sessionStatesFilePath, settings.sessionStates);

            // Save settings.llmServerSettings
            string llmServerSettingsJson = JsonUtility.ToJson(settings.llmServerSettings);
            EditorPrefs.SetString("DiagenLlmServerSettings", llmServerSettingsJson);

            // Save settings.llmGenerationSettings
            string llmGenerationSettingsJson = JsonUtility.ToJson(settings.llmGenerationSettings);
            EditorPrefs.SetString("DiagenLlmGenerationSettings", llmGenerationSettingsJson);
        }

        public static Settings LoadSettings(Settings settings = null, string sessionStatesFilePath = "")
        {
            string json = EditorPrefs.GetString("DiagenOptionTables", JsonUtility.ToJson(new OptionTables()));
            settings.optionTables = JsonUtility.FromJson<OptionTables>(json);


            string llmGenerationSettingsJson = EditorPrefs.GetString("DiagenLlmGenerationSettings", JsonUtility.ToJson(new LlmGenerationSettings()));
            settings.llmGenerationSettings = JsonUtility.FromJson<LlmGenerationSettings>(llmGenerationSettingsJson);

            string llmServerSettingsJson = EditorPrefs.GetString("DiagenLlmServerSettings", JsonUtility.ToJson(new LlmServerSettings()));
            settings.llmServerSettings = JsonUtility.FromJson<LlmServerSettings>(llmServerSettingsJson);

            if (File.Exists(sessionStatesFilePath))
            {
                settings.sessionStates = DiagenSession.LoadSessionStates(sessionStatesFilePath);
            }
            else
            {
                settings.sessionStates = new List<Session>();
            }

            if (settings.sessionStates == null)
            {
                settings.sessionStates = new List<Session>();
            }

            settings.llmServerSettings.Init(); // Initialize paths safely

            return settings;
        }

        public static void DisplayColumns(int elementsPerRow = 4, List<BoxInfo> elements = null, float maxRowWidth=400f)
        {
            if (elements == null || elements.Count == 0) return;

            float minElementWidth = 80f; // Minimum width for an element
            float maxElementWidth = 150f; // Maximum width for an element

            // Determine the longest element text size
            float longestTextWidth = elements.Max(e => GetTextWidth(e.title));
            float elementWidth = Mathf.Clamp(longestTextWidth + 20, minElementWidth, maxElementWidth);

            // Calculate max elements that fit within 400px
            int maxElementsPerRow = Mathf.Max(1, Mathf.FloorToInt(maxRowWidth / elementWidth));
            int elementsPerCurrentRow = Mathf.Min(elementsPerRow, maxElementsPerRow);

            GUILayout.BeginVertical();
            for (int i = 0; i < elements.Count; i += elementsPerCurrentRow)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(maxRowWidth));
                for (int j = 0; j < elementsPerCurrentRow; j++)
                {
                    if (i + j < elements.Count)
                    {
                        GUILayout.BeginVertical(GUILayout.Width(elementWidth));
                        DisplayBox(elements[i + j]);
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            GUILayout.EndVertical();
        }

        public static Settings DisplayModelParameters(Settings settings)
        {
            EditorGUI.BeginDisabledGroup(settings.llmServerProcess == null || settings.llmServerProcess.HasExited);
            settings.llmServerSettings.gpuLayers = EditorGUILayout.IntField("GPU Layers (-1 for auto):", settings.llmServerSettings.gpuLayers);
            settings.llmServerSettings.contextSize = EditorGUILayout.IntField("Context Size:", settings.llmServerSettings.contextSize);
            settings.llmServerSettings.flashAttention = EditorGUILayout.Toggle("Enable Flash Attention:", settings.llmServerSettings.flashAttention);
            settings.llmServerSettings.gpuSelect = EditorGUILayout.IntField("GPU Select (-1 for auto):", settings.llmServerSettings.gpuSelect);
            settings.llmServerSettings.utilization = EditorGUILayout.Slider("Utilization:", settings.llmServerSettings.utilization, 0.1f, 1.0f);

            if (settings.llmServerProcess == null || settings.llmServerProcess.HasExited)
            {
                EditorGUILayout.HelpBox("Start Model in Setup first", MessageType.Warning);
            }
            else if (GUILayout.Button("Reload Model"))
            {
                //settings.llmServerProcess = DiagenSubsystem.RestartLlamaServer(settings.llmServerSettings, settings.llmServerProcess);
            }
            EditorGUI.EndDisabledGroup();

            return settings;
        }

        public static Settings DisplayPromptParameters(Settings settings)
        {
            settings.llmGenerationSettings.nPredict = EditorGUILayout.IntField("Length (token per generation):", settings.llmGenerationSettings.nPredict);
            settings.llmGenerationSettings.temperature = EditorGUILayout.Slider("Temperature:", settings.llmGenerationSettings.temperature, 0.1f, 2.0f);
            settings.llmGenerationSettings.topK = EditorGUILayout.IntField("Top K:", settings.llmGenerationSettings.topK);
            settings.llmGenerationSettings.topP = EditorGUILayout.Slider("Top P:", settings.llmGenerationSettings.topP, 0.1f, 1.0f);
            settings.llmGenerationSettings.frequencyPenalty = EditorGUILayout.Slider("Frequency Penalty:", settings.llmGenerationSettings.frequencyPenalty, 0.0f, 2.0f);
            settings.llmGenerationSettings.presencePenalty = EditorGUILayout.Slider("Presence Penalty:", settings.llmGenerationSettings.presencePenalty, 0.0f, 2.0f);

            return settings;
        }

        // Function to estimate text width (Unity does not provide direct text width measuring in GUILayout)
        private static float GetTextWidth(string text)
        {
            return text.Length * 7f; // Approximate width per character
        }


        public static void DisplayBox(BoxInfo element)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(new GUIContent(element.title, element.content), new GUIStyle(EditorStyles.miniLabel) { fontSize = 12 });
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }


    
    [System.Serializable]
    public class Settings
    {
        [SerializeField]
        public List<Session> sessionStates = new List<Session>();
        [SerializeField]
        public LlmServerSettings llmServerSettings = new LlmServerSettings();
        [SerializeField]
        public LlmGenerationSettings llmGenerationSettings = new LlmGenerationSettings();
        [SerializeField]
        public OptionTables optionTables = new OptionTables();
        public Process llmServerProcess;
        public int selectedAgentSession = 0;

        public void Init()
        {
            llmServerSettings.Init(); // Initialize paths safely
        }
    }
    
    [System.Serializable]
    public class BoxInfo
    {
        public string title;
        public string content;
    }
}
#endif