#if UNITY_EDITOR
    using UnityEditor;
#endif

using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;

using DiagenCommonTypes;

namespace Diagen
{
    public class DiagenSubsystem : MonoBehaviour
    {
        public static DiagenSubsystem Instance { get; private set; }
        public static Process globalLlmServerProcess = null;
        public static List<Session> sessionStates = new List<Session>();
        public LlmServerSettings llmServerSettings;
        public LlmGenerationSettings llmGenerationSettings;

        public static string RemoteSession;

        private async void Awake()
        {
            if (Instance == null && Instance != this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Initialize settings using default values
                llmServerSettings = new LlmServerSettings();
                llmGenerationSettings = new LlmGenerationSettings();

                RemoteSession = await InitRemoteSession(llmServerSettings.apiKey);
            }
            else
            {
                Destroy(gameObject);
            }
        }



        public static async Task<string> InitRemoteSession(string apiKey)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Api-Key {apiKey}");

                    var payload = new
                    {
                        session_id = Guid.NewGuid().ToString()
                    };

                    var response = await client.PostAsync(
                        "https://diagen-api.xandimmersion.com/init_session",
                        new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                    );

                    string json = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError("Failed to init session: " + response.ReasonPhrase + "\n" + json);
                        return null;
                    }

                    var result = JsonConvert.DeserializeObject<SessionInitResponse>(json);
                    Debug.Log("Session initialized: " + result.session_id);
                    RemoteSession = result.session_id;

                    return result.session_id;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during session init: " + ex.Message);
                return null;
            }
        }



        // Remote Generation : settings.llmServerProcess, settings.llmServerSettings, settings.llmGenerationSettings, conversationPrompt, progress
        // LlmServerSettings llmServerSettings, LlmGenerationSettings llmGenerationSettings, List<Prompt> conversationPrompt, IProgress<string> progress
        public static async Task GenerateTextStream(Process llmServerProcess, LlmServerSettings llmServerSettings, LlmGenerationSettings llmGenerationSettings, List<Prompt> conversationPrompt, IProgress<string> progress)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Api-Key {llmServerSettings.apiKey}");

                    var coreDescription = string.Join("\n", conversationPrompt.Select(p => $"{p.role}: {p.content}"));
                    Debug.Log($"[DEBUG] Core Prompt Sent:\n{coreDescription}");
                    //Debug.Log($"[PLayer Name] :\n{llmGenerationSettings.playerName}");

                    var payload = new
                    {
                        npc_name = llmGenerationSettings.agentName,
                        player_name = llmGenerationSettings.playerName,
                        core_description = string.Join("\n", conversationPrompt.Select(p => $"{p.role}: {p.content}")),
                        language = "en",

                        llama_generation_params = new
                        {
                            max_tokens = llmGenerationSettings.nPredict,
                            temperature = llmGenerationSettings.temperature,
                            stream = true
                        },

                        session_id = RemoteSession
                    };

                    var request = new HttpRequestMessage(HttpMethod.Post, "https://diagen-api.xandimmersion.com/generate-stream")
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError("Failed to stream response: " + response.ReasonPhrase);
                        return;
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            Debug.Log($"[STREAM] Raw line: {line}");

                            if (!string.IsNullOrEmpty(line))
                            {
                                if (line.StartsWith("data: "))
                                    line = line.Substring("data: ".Length);

                                var fakeJson = new
                                {
                                    choices = new[]
                                    {
                                        new
                                        {
                                            delta = new
                                            {
                                                content = line
                                            },
                                            finish_reason = (string)null
                                        }
                                    }
                                };

                                string wrappedLine = "data: " + JsonConvert.SerializeObject(fakeJson);
                                progress.Report(wrappedLine);
                            }
                        }

                        // Simulate end-of-stream
                        progress.Report("data: [DONE]");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during streaming from remote API: " + ex.Message);
            }
        }

        public static void SetSessions(List<Session> sessions)
        {
            sessionStates = sessions;
        }

        public static List<Session> GetSessions()
        {
            return sessionStates;
        }

        public class PromptResponse
        {
            public string response { get; set; }
        }

        public class SessionInitResponse
        {
            public string session_id { get; set; }
            public string message { get; set; }
        }
    }
}