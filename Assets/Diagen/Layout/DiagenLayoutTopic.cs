#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;

using DiagenCommonTypes;
using DiagenLayoutTypes;

namespace Diagen
{
    public class DiagenLayoutTopic : EditorWindow
    {
        private Topic lastDetectedTopic = new Topic();
        private bool inProgress = false;
        private DiagenAPI diagenAPI;
        public void SetDiagenAPI(DiagenAPI api)
        {
            diagenAPI = api;
        }

        public async Task<Settings> TopicDetectionTab(Settings settings)
        {
            // Dropdown with all Agents
            GUILayout.Label("Character Names", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.Space(10);

            settings = DiagenLayoutCommon.AgentDropdown(settings);
            GUILayout.Space(10);
            
            GUILayout.Label("Enabled Topics", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.Space(10);

            if (settings.sessionStates[settings.selectedAgentSession].EnableTopics.Count > 0)
            {
                List<BoxInfo> enableTopics = new List<BoxInfo>();
                settings.sessionStates[settings.selectedAgentSession].EnableTopics.ForEach(topic => enableTopics.Add(new BoxInfo { title = topic.Name, content = topic.Description }));
                DiagenLayoutCommon.DisplayColumns(8, enableTopics, 700f);
            }
            else {
                GUILayout.Label("No topics enabled - Add tags in Options menu", new GUIStyle(EditorStyles.miniLabel) { fontSize = 12 });
            }

            GUILayout.Space(10);

            GUILayout.Label("Topic Detection", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            GUILayout.Space(10);

            // Display a message text field
            GUILayout.Label("Message", new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            // settings.llmGenerationSettings.description = EditorGUILayout.TextArea(settings.llmGenerationSettings.description, textAreaStyle, GUILayout.Height(100), GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(GUILayout.Height(60));
            settings.llmGenerationSettings.message = EditorGUILayout.TextArea(settings.llmGenerationSettings.message, textAreaStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Button detect topics
            GUI.enabled = (inProgress == false && settings.sessionStates[settings.selectedAgentSession].EnableTopics.Count > 0);
            if (GUILayout.Button(inProgress ? "Detecting topic..." : "Detect Topics"))
            {
                Debug.Log("Detecting topic...");
                inProgress = true;
                // Set the animation running when generating text
                if (diagenAPI != null)
                {
                    diagenAPI.SetRunning(true);
                    diagenAPI.SetError(false);
                    diagenAPI.SetQuestion(false);
                }
                lastDetectedTopic = await DiagenTopic.CallTopicDetection(settings.llmServerProcess, settings.llmServerSettings, settings.sessionStates, settings.llmGenerationSettings.agentName, settings.llmGenerationSettings.playerName, settings.llmGenerationSettings.message, settings.optionTables);
                inProgress = false;
                // Set the animation to stop when done generating text
                if (diagenAPI != null)
                {
                    diagenAPI.SetRunning(false);
                    diagenAPI.SetError(false);
                    if (lastDetectedTopic == null)
                    {
                        diagenAPI.SetQuestion(true);
                    }
                    else
                    {
                        diagenAPI.SetQuestion(false);
                    }
                }
                Repaint();
            }

            // if Topic is not null, display the topic
            if (lastDetectedTopic != null && lastDetectedTopic.Name != null)
            {

                GUILayout.Space(10);    
                GUILayout.Label("Detected Topic");
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name: ", GUILayout.Width(100));
                GUILayout.Space(10);
                // Disable field
                EditorGUI.BeginDisabledGroup(true);
                lastDetectedTopic.Name = EditorGUILayout.TextField(lastDetectedTopic.Name, GUILayout.Width(500));
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Description:", GUILayout.Width(100));
                GUILayout.Space(10);
                EditorGUI.BeginDisabledGroup(true);
                lastDetectedTopic.Description = EditorGUILayout.TextField(lastDetectedTopic.Description, GUILayout.Width(500));
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(5);
                Repaint();
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.Label("No topic detected", new GUIStyle(EditorStyles.miniLabel) { fontSize = 12 });
            }


            return settings;
        }
    }
}
#endif