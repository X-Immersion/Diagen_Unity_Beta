using Debug = UnityEngine.Debug;
using DiagenCommonTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Diagen
{    
    public class DiagenTrigger : MonoBehaviour 
    {
        public static EventSessionsPair TriggerDiagenEvent(List<Session> sessionStates, string EventName, string agentName, OptionTables optionTables)
        {
            DiagenEvent diagenEvent = optionTables.eventsTable.FindEvent(EventName);
            if (diagenEvent == null)
            {
                Debug.LogError($"TriggerDiagenEvent: Event {EventName} not found.");
                return null;
            }

            if (diagenEvent.GlobalStateTags)
            {
                // Execute the event for all sessions
                sessionStates = ExecuteEvent(sessionStates, agentName, diagenEvent);
            }
            else
            {
                // Find the session for the triggering agent
                Session sessionState = DiagenSession.GetSessionByName(sessionStates, agentName);
                if (sessionState == null)
                {
                    Debug.LogError($"TriggerDiagenEvent: No session found for agent {agentName}");
                    return null;
                }

                // Execute the event for the triggering agent only
                sessionState = ExecuteEvent(sessionState, diagenEvent);
                
                // Overwrite the session state in the session list
                sessionStates = DiagenSession.UpdateSessionByName(sessionStates, agentName, sessionState);
            }

            

            Debug.Log($"Event {EventName} executed. Global: {diagenEvent.GlobalStateTags}");
            return new EventSessionsPair { sessionStates = sessionStates, diagenEvent = diagenEvent };
        }

        public static List<DiagenEvent> ListAvailableEvents(Session sessionState, OptionTables optionTables)
        {
            if (optionTables == null || optionTables.eventsTable == null || sessionState == null)
            {
                return new List<DiagenEvent>();
            }

            List<DiagenEvent> availableEvents = new List<DiagenEvent>();

            var allEvents = optionTables.eventsTable.GetEvents() ?? new List<DiagenEvent>();
            var activateTags = sessionState.EnableStateTags ?? new List<string>();

            foreach (var diagenEvent in allEvents)
            {
                bool requiredTagsExist = true;

                foreach (var tag in diagenEvent.StateTags)
                {
                    if (!activateTags.Contains(tag))
                    {
                        requiredTagsExist = false;
                        break;
                    }
                }

                if (requiredTagsExist)
                {
                    availableEvents.Add(diagenEvent);
                }
            }

            return availableEvents;
        }



        public static DiagenEvent FindEvent(string eventName, OptionTables optionTables)
        {
            // Find the event in the events table
            return optionTables.eventsTable.FindEvent(eventName);
        }

        private static Session ExecuteEvent(Session sessionState, DiagenEvent diagenEvent)
        {
            // Check if the event is repeatable
            if (!diagenEvent.Repeatable && sessionState.ExecutedDiagenEvent.Contains(diagenEvent.Name))
            {
                Debug.LogError($"ExecuteEvent: Event {diagenEvent.Name} not repeatable for {sessionState.AgentName}");
                return sessionState;
            }

            if (!diagenEvent.GlobalStateTags)
            {
                // Apply changes ONLY to the triggering NPC
                sessionState = ApplyTagChanges(sessionState, diagenEvent);
            }
            else
            {
                // With input type Session, we can only apply changes to the given NPC
                Debug.LogWarning($"[Event Generation] GlobalStateTags are only applied to the given sessionState. Pass a list of sessions to apply to all NPCs in the session.");
                sessionState = ApplyTagChanges(sessionState, diagenEvent);
            }

            return sessionState;
        }

        private static List<Session> ExecuteEvent(List<Session> sessionStates, string agentName, DiagenEvent diagenEvent)
        {

            Session sessionState = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (sessionState == null)
            {
                Debug.LogError($"ExecuteEvent: No session found for agent {agentName}");
                return null;
            }

            // Check if the event is repeatable
            if (!diagenEvent.Repeatable && sessionState.ExecutedDiagenEvent.Contains(diagenEvent.Name))
            {
                Debug.LogError($"ExecuteEvent: Event {diagenEvent.Name} not repeatable for {sessionState.AgentName}");
                return sessionStates;
            }

            if (!diagenEvent.GlobalStateTags)
            {
                // Apply changes ONLY to the triggering NPC
                sessionState = ApplyTagChanges(sessionState, diagenEvent);

                // update the session state in the list
                sessionStates = DiagenSession.UpdateSessionByName(sessionStates, agentName, sessionState);
            }
            else
            {
                for (int i = 0; i < sessionStates.Count; i++)
                {
                    sessionStates[i] = ApplyTagChanges(sessionStates[i], diagenEvent); // This updates the list
                }
            }
            return sessionStates;
            
        }

        // Helper function to apply tag changes
        private static Session ApplyTagChanges(Session session, DiagenEvent diagenEvent)
        {
            if (diagenEvent.EnableStateTags.Length > 0)
            {
                session = DiagenStateTags.AppendStateTagsOnState(DiagenStateTags.GetTags(diagenEvent.EnableStateTags.ToList()), session);
            }
            if (diagenEvent.DisableStateTags.Length > 0)
            {
                session = DiagenStateTags.RemoveStateTagsFromState(DiagenStateTags.GetTags(diagenEvent.DisableStateTags.ToList()), session);
            }

            // Mark event as executed
            session.ExecutedDiagenEvent.Add(diagenEvent.Name);
            session.ExecutedDiagenEvent = new List<string>(new HashSet<string>(session.ExecutedDiagenEvent));

            return session;
        }

        /// <summary>
        /// Generates text from an instruction using the LLM server.
        /// </summary>
        /// <returns>A tuple containing the generated text, whether generation is done, and whether an error occurred.</returns>
        public static async Task<(string text, bool isDone, bool isError)> GenerateTextFromInstructionAsync(
            Session session,
            string agentName,
            string playerName,
            string instruction,
            OptionTables optionTables,
            LlmServerSettings llmServerSettings,
            LlmGenerationSettings llmGenerationSettings,
            Process llmServerProcess,
            int maxCharacters = 10000,
            string description = "")
        {
            // Create instruction prompt
            List<Prompt> instructionPrompt = DiagenLlm.CreateInstructionPrompt(
                session,
                agentName,
                playerName,
                instruction,
                optionTables,
                maxCharacters,
                description);

            // Check token count
            int promptTokenCount = DiagenLlm.PromptTokenCount(instructionPrompt);
            if (llmServerSettings.contextSize < promptTokenCount)
            {
                UnityEngine.Debug.LogWarning($"[Event Generation] Context size exceeded. Count: {promptTokenCount} > {llmServerSettings.contextSize}");
            }

            // Variables to track the result
            string resultText = "";
            bool isDone = false;
            bool isError = false;

            // Create TaskCompletionSource to signal when done
            var tcs = new TaskCompletionSource<bool>();

            // Create progress callback
            var progress = new System.Progress<string>(chunk =>
            {
                try
                {
                    // Split chunk on first occurrence of "data: " to get the actual data
                    if (!chunk.Contains("data: "))
                    {
                        return;
                    }

                    chunk = chunk.Split(new string[] { "data: " }, StringSplitOptions.None)[1];

                    if (chunk == "[DONE]")
                    {
                        isDone = true;
                        tcs.TrySetResult(true);
                        return;
                    }

                    JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };

                    LlmStreamResponse llmResponse = JsonConvert.DeserializeObject<LlmStreamResponse>(chunk, jsonSettings);

                    // Process the response content
                    if (llmResponse.choices[0].delta != null && !string.IsNullOrEmpty(llmResponse.choices[0].delta.content))
                    {
                        resultText += llmResponse.choices[0].delta.content;
                    }

                    // Check if finished
                    if (llmResponse.choices[0].finish_reason != null && llmResponse.choices[0].finish_reason != "")
                    {
                        isDone = true;
                        tcs.TrySetResult(true);
                    }
                }
                catch (JsonSerializationException ex)
                {
                    UnityEngine.Debug.LogError($"Error processing LLM response: {ex.Message}");
                    isError = true;
                    tcs.TrySetResult(true);
                }
            });

            // Start generation and create a timeout task
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(30)); // 30 second timeout

            try
            {
                // Start the generation
                Task generateTask = DiagenSubsystem.GenerateTextStream(
                    llmServerProcess,
                    llmServerSettings,
                    llmGenerationSettings,
                    instructionPrompt,
                    progress);

                // Wait for either generation to complete, completion signal, or timeout
                Task completedTask = await Task.WhenAny(generateTask, tcs.Task, timeoutTask);

                // If timed out, mark error
                if (completedTask == timeoutTask && !isDone && !isError)
                {
                    UnityEngine.Debug.LogError("Text generation timed out");
                    isError = true;
                }

                // Wait for the generate task to complete regardless (to avoid unhandled exceptions)
                if (completedTask != generateTask)
                {
                    await generateTask;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error during text generation: {ex.Message}");
                isError = true;
            }

            return (resultText, isDone, isError);
        }

        /// <summary>
        /// Generates text from an instruction using the LLM server and provides real-time updates as chunks are received.
        /// </summary>
        /// <param name="onChunkProcessed">Callback that provides the current text, completion status, and error status after each chunk.</param>
        /// <returns>A task that completes when text generation is finished.</returns>
        public static async Task GenerateTextFromInstructionAsyncChunks(
            Session session,
            string agentName,
            string playerName,
            string instruction,
            OptionTables optionTables,
            LlmServerSettings llmServerSettings,
            LlmGenerationSettings llmGenerationSettings,
            Process llmServerProcess,
            Action<string, bool, bool> onChunkProcessed,
            int maxCharacters = 10000,
            string description = "")
        {
            // Create instruction prompt
            List<Prompt> instructionPrompt = DiagenLlm.CreateInstructionPrompt(
                session,
                agentName,
                playerName,
                instruction,
                optionTables,
                maxCharacters,
                description);

            // Check token count
            int promptTokenCount = DiagenLlm.PromptTokenCount(instructionPrompt);

            if (llmServerSettings.contextSize < promptTokenCount)
            {
                UnityEngine.Debug.LogWarning($"[Event Generation] Context size exceeded. Count: {promptTokenCount} > {llmServerSettings.contextSize}");
            }

            // Variables to track the result
            string resultText = "";
            bool isDone = false;
            bool isError = false;

            // Create TaskCompletionSource to signal when done
            var tcs = new TaskCompletionSource<bool>();

            // Create progress callback
            var progress = new System.Progress<string>(chunk =>
            {
                try
                {
                    UnityEngine.Debug.Log($"[Event Generation] Chunk: {chunk}");
                    // Split chunk on first occurrence of "data: " to get the actual data
                    if (!chunk.Contains("data: "))
                    {
                        return;
                    }

                    chunk = chunk.Split(new string[] { "data: " }, StringSplitOptions.None)[1];

                    if (chunk == "[DONE]")
                    {
                        isDone = true;
                        onChunkProcessed(resultText, true, false); // Final callback with complete text
                        tcs.TrySetResult(true);
                        return;
                    }

                    JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };

                    LlmStreamResponse llmResponse = JsonConvert.DeserializeObject<LlmStreamResponse>(chunk, jsonSettings);

                    // Process the response content
                    if (llmResponse.choices[0].delta != null && !string.IsNullOrEmpty(llmResponse.choices[0].delta.content))
                    {
                        resultText += llmResponse.choices[0].delta.content;
                        
                        // Call the callback with the current text (not done, no error)
                        onChunkProcessed(resultText, false, false);
                    }

                    // Check if finished
                    if (llmResponse.choices[0].finish_reason != null && llmResponse.choices[0].finish_reason != "")
                    {
                        isDone = true;
                        onChunkProcessed(resultText, true, false); // Final callback with complete text
                        tcs.TrySetResult(true);
                    }
                }
                catch (JsonSerializationException ex)
                {
                    UnityEngine.Debug.LogError($"Error processing LLM response: {ex.Message}");
                    isError = true;
                    onChunkProcessed(resultText, true, true); // Signal error
                    tcs.TrySetResult(true);
                }
            });

            // Start generation and create a timeout task
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(30)); // 30 second timeout

            try
            {
                // Start the generation
                Task generateTask = DiagenSubsystem.GenerateTextStream(
                    llmServerProcess,
                    llmServerSettings,
                    llmGenerationSettings,
                    instructionPrompt,
                    progress);

                // Wait for either generation to complete, completion signal, or timeout
                Task completedTask = await Task.WhenAny(generateTask, tcs.Task, timeoutTask);

                // If timed out, mark error
                if (completedTask == timeoutTask && !isDone && !isError)
                {
                    UnityEngine.Debug.LogError("Text generation timed out");
                    isError = true;
                    onChunkProcessed(resultText, true, true); // Signal timeout error
                }

                // Wait for the generate task to complete regardless (to avoid unhandled exceptions)
                if (completedTask != generateTask)
                {
                    await generateTask;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error during text generation: {ex.Message}");
                isError = true;
                onChunkProcessed(resultText, true, true); // Signal exception error
            }
        }
    }
}