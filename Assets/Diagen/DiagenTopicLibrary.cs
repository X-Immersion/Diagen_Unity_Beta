using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System;
using System.Text;
using DiagenCommonTypes;

namespace Diagen 
{   
    public class DiagenTopic : MonoBehaviour
    {
        public static async Task<Topic> CallTopicDetection(Process llmServerProcess, LlmServerSettings llmServerSettings, List<Session> sessionStates, string agentName, string playerName, string message, OptionTables optionTables)
        {
            // Agent session
            Session sessionState = DiagenSession.GetSessionByName(sessionStates, agentName);

            // Get topics from topicsTable that are enabled in session
            List<Topic> enabledTopics = GetEnabledTopics(sessionState, optionTables);

            if (enabledTopics.Count == 0)
            {
                Debug.LogWarning("[Topic Detection] No topics enabled.");
                return null;
            }

            // Create topic prompt
            List<Prompt> topicPrompt = DiagenLlm.CreateTopicPrompt(enabledTopics, agentName, playerName, message);

            // Check context size
            int promptTokenCount = DiagenLlm.PromptTokenCount(topicPrompt);
            if (llmServerSettings.contextSize < promptTokenCount)
            {
                Debug.LogWarning($"[Topic Detection] Context size exceeded. Count: {promptTokenCount} > {llmServerSettings.contextSize}");
            }

            // Create LLM settings for detection
            LlmGenerationSettings detectionSettings = new LlmGenerationSettings
            {
                description = "",
                nPredict = 20,
                temperature = 0.0f,
                topK = 0,
                topP = 0.9f,
                frequencyPenalty = 0.0f,
                presencePenalty = 0.0f
            };

            // --- DETECTION PHASE ---
            StringBuilder detectionBuilder = new StringBuilder();

            var detectionProgress = new Progress<string>(chunk =>
            {
                detectionBuilder.Append(chunk);
            });

            await DiagenSubsystem.GenerateTextStream(
                llmServerProcess,
                llmServerSettings,
                detectionSettings,
                topicPrompt,
                detectionProgress
            );

            string detectionResponse = detectionBuilder.ToString();
            Match match = Regex.Match(detectionResponse, @"\d+");
            int topicId = -1;

            if (match.Success)
            {
                topicId = int.Parse(match.Value);
                Debug.Log("[Topic Detection] Extracted Topic ID: " + topicId);
            }
            else
            {
                Debug.LogWarning("[Topic Detection] No number found in the response.");
                return null;
            }

            if (topicId >= enabledTopics.Count + 1)
            {
                Debug.Log("[Topic Detection] No topic detected.");
                return null;
            }

            // --- VALIDATION PHASE ---
            LlmGenerationSettings validationSettings = new LlmGenerationSettings
            {
                description = "",
                nPredict = 20,
                temperature = 0.0f,
                topK = 0,
                topP = 0.9f,
                frequencyPenalty = 0.0f,
                presencePenalty = 0.0f
            };

            List<Prompt> validationPrompt = DiagenLlm.CreateTopicValidationPrompt(enabledTopics[topicId - 1], agentName, playerName, message);
            StringBuilder validationBuilder = new StringBuilder();

            var validationProgress = new Progress<string>(chunk =>
            {
                validationBuilder.Append(chunk);
            });

            await DiagenSubsystem.GenerateTextStream(
                llmServerProcess,
                llmServerSettings,
                validationSettings,
                validationPrompt,
                validationProgress
            );

            string validationResponse = validationBuilder.ToString();
            Match validationMatch = Regex.Match(validationResponse, @"\d+");
            int validationScore = -1;

            if (validationMatch.Success)
            {
                validationScore = int.Parse(validationMatch.Value);
                Debug.Log("[Topic Validation] Extracted Validation: " + validationScore);
            }
            else
            {
                Debug.Log("[Topic Validation] No topic detected.");
                return null;
            }

            // Final decision
            if (enabledTopics[topicId - 1].Threshold * 100 <= validationScore)
            {
                return enabledTopics[topicId - 1];
            }
            else
            {
                Debug.Log($"Topic might have been detected with score: {validationScore}, but under the threshold: {enabledTopics[topicId - 1].Threshold * 100}");
                return null;
            }
        }


        public static Session EnableTopics(Session sessionState, string agentName, OptionTables optionTables)
        {
            // Ensure sessionState is not null; return a new Session if needed
            if (sessionState == null)
            {
                return new Session(); // Or return null if that fits your logic
            }

            // Ensure optionTables is not null
            if (optionTables == null || optionTables.topicTable == null)
            {
                sessionState.EnableTopics = new List<Topic>(); // No topics enabled
                return sessionState;
            }

            // Ensure EnableStateTags is not null
            if (sessionState.EnableStateTags == null)
            {
                sessionState.EnableTopics = new List<Topic>(); // No topics enabled
                return sessionState;
            }

            // Fetch topics safely
            sessionState.EnableTopics = optionTables.topicTable.FindTopicFromTag(sessionState.EnableStateTags)?.ToList()
                                        ?? new List<Topic>();

            return sessionState;
        }

        public static List<Topic> GetEnabledTopics(Session sessionState, OptionTables optionTables)
        {
            return sessionState.EnableTopics;
        }
    }
}