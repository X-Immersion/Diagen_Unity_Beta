using Debug = UnityEngine.Debug;
using DiagenCommonTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Diagen
{
    public class DiagenLlm : MonoBehaviour
    {
        public static List<Prompt> CreateConversationPrompt(Session sessionState, string agentName, string playerName, string description, bool useHistory, string message, int maxCharacters, OptionTables optionTables, int startIndex = 0, int endIndex = -1)
        {
            List<Prompt> conversationPrompt = new List<Prompt>();
            if (description == null || description == "")
            {
                conversationPrompt.Add(
                    new Prompt
                    {
                        role = "system",
                        content = CreateAgentDescription(sessionState, agentName, playerName, optionTables, maxCharacters)
                    }
                );
            }
            else
            {
                conversationPrompt.Add(
                    new Prompt
                    {
                        role = "system",
                        content = description
                    }
                );
            }

            if (useHistory)
            {
                List<Prompt> historyPrompt = CreateHistoryPrompts(sessionState, startIndex, endIndex);
                if (historyPrompt.Count > 0)
                {
                    conversationPrompt.AddRange(historyPrompt);
                }
            }

            conversationPrompt.Add(
                new Prompt
                {
                    role = "user",
                    content = message
                }
            );

            return conversationPrompt;
        }

        public static List<Prompt> CreateInstructionPrompt(Session sessionState, string agentName, string playerName, string instruction, OptionTables optionTables, int maxCharacters = 10000, string description = "")
        {
            List<Prompt> instructionPrompt = new List<Prompt>();
            if (description == null || description == "")
            {
                instructionPrompt.Add(
                    new Prompt
                    {
                        role = "system",
                        content = CreateAgentDescription(sessionState, agentName, playerName, optionTables, maxCharacters)
                    }
                );

            }
            else
            {
                instructionPrompt.Add(
                    new Prompt
                    {
                        role = "system",
                        content = description
                    }
                );
            }

            Debug.Log($"description: {instructionPrompt[0].content}");

            // Add instruction context to system prompt
            instructionPrompt[0].content += DiagenUtilities.ReplaceCharWithEscapedChar(
                ". The user gives you an instruction on what you should say and you say it. " +
                "An example user input looks as follows: 'Instruction: You say you don't want to listen to the negotiator no more'. " +
                "Your response should be: 'I cannot listen to this non sense no more'. " +
                "Stay in Character and do not make up stories. Base your response only on the given context or data."
            );


            instructionPrompt.Add(
                new Prompt
                {
                    role = "user",
                    content = instruction
                }
            );

            return instructionPrompt;
        }

        public static List<Prompt> CreateFillerPrompt(Session sessionState, string agentName, string playerName, int maxCharacters, OptionTables optionTables, int startIndex = 0, int endIndex = -1)
        {
            // System prompt for filler
            List<Prompt> fillerPrompt = new List<Prompt>();
            fillerPrompt.Add(
                new Prompt
                {
                    role = "system",
                    content = CreateAgentDescription(sessionState, agentName, playerName, optionTables, maxCharacters)
                }
            );

            // Last two History prompts for filler
            List<Prompt> historyPrompt = CreateHistoryPrompts(sessionState, startIndex, endIndex);
            if (historyPrompt.Count > 1)
            {
                fillerPrompt.Add(historyPrompt[historyPrompt.Count - 2]);
                fillerPrompt.Add(historyPrompt[historyPrompt.Count - 1]);
            }

            // Add filler context to system prompt
            fillerPrompt[0].content += DiagenUtilities.ReplaceCharWithEscapedChar(".\nGenerate a short filler phrase as if talking to yourself.\n" +
                "Obey these strict instructions: \n" +
                "* The response should be easily understood when spoken aloud. \n" +
                "* Avoid actions, gestures, or sounds like sighs or laughter. \n" +
                "* Stick to simple, spoken words. \n" +
                "* Only return the filler phrase without any additional text or context.");

            return fillerPrompt;
        }


        public static List<Prompt> CreateTopicPrompt(List<Topic> topicPairs, string agentName, string playerName, string message)
        {
            List<Prompt> topicPrompt = new List<Prompt>();

            // Find the topic pair for the given Agent
            string systemPrompt = "Context: The following sentence is part of a conversation. " +
                                playerName + " \'s sentence: \"" + message +
                                "\"Choose the one topic from the list below that is the most clearly related to " + playerName + "'s sentence. " +
                                "If no topic has a strong and direct relationship, respond with 'None'. " +
                                "Do not explain your choice. Respond only with the topic number or 'None'. " +
                                "Topics:\n ";

            for (int i = 0; i < topicPairs.Count; i++)
            {
                systemPrompt += (i + 1) + ". " + topicPairs[i].Description + "\n ";
            }
            systemPrompt += (topicPairs.Count + 1) + ". None of these topics\n";

            systemPrompt = DiagenUtilities.ReplaceCharWithEscapedChar(systemPrompt);

            topicPrompt.Add(new Prompt { role = "system", content = systemPrompt });
            return topicPrompt;
        }


        public static List<Prompt> CreateTopicValidationPrompt(Topic selectedTopic, string agentName, string playerName, string message)
        {
            List<Prompt> topicPrompt = new List<Prompt>();

            string systemPrompt = "Evaluate if a sentence can directly be categorized into a given Category.\n " +
                    "Respond with a single number between 1 and 100 based on the strength of the relationship. The higher the value, the stronger the relationship is.\n " +
                    playerName + "'s sentence: \'" + message + "\'\n " +
                    "Category: \"" + selectedTopic.Description + "\"\n" +
                    "Only provide a single number.";

            systemPrompt = DiagenUtilities.ReplaceCharWithEscapedChar(systemPrompt);
            topicPrompt.Add(new Prompt { role = "system", content = systemPrompt });
            return topicPrompt;
        }

        public static string CreateAgentDescription(Session sessionState, string agentName, string playerName, OptionTables optionTables, int maxCharacters)
        {
            // iterate over sessionState EnabledTags
            if (sessionState.EnableStateTags.Count == 0 || optionTables.characterInformationTable == null)
            {
                return $"Your name is {agentName} and you are talking to {playerName}. You have to talk in the first perspective, with dialogue.";
            }

            List<DescriptionInfo> characterInfo = optionTables.characterInformationTable.FindCharacterInformationFromTag(sessionState.EnableStateTags, optionTables.stateTagsWeightTable); // Already sorted descendingly
            string description = "";
            for (int i = 0; i < characterInfo.Count; i++)
            {
                description += characterInfo[i].Description;
                if (description.Length > maxCharacters)
                {
                    break;
                }
            }
            description += "Only respond in spoken words. Do not make up stories. Base your response only on the given context or data.";
            return description;
        }

        public static List<Prompt> CreateHistoryPrompts(Session sessionState, int startIndex = 0, int endIndex = -1)
        {
            List<Prompt> historyPrompt = new List<Prompt>();
            // Iterate over State.history and add to History
            if (endIndex == -1 || endIndex > sessionState.History.Count)
            {
                endIndex = sessionState.History.Count;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                if (sessionState.History[i].Name == sessionState.AgentName)
                {
                    historyPrompt.Add(new Prompt { role = "assistant", content = DiagenUtilities.ReplaceCharWithEscapedChar(sessionState.History[i].Message) });
                }
                else
                {
                    historyPrompt.Add(new Prompt { role = "user", content = DiagenUtilities.ReplaceCharWithEscapedChar(sessionState.History[i].Message) });
                }
            }
            return historyPrompt;
        }

        // public static Session AddHistoryToSession(Session sessionState, string playerName, string message, string response)
        // {
        //     sessionState.History.Add(new History { Name = playerName, Message = message });
        //     sessionState.History.Add(new History { Name = sessionState.AgentName, Message = response }); 
        //     return sessionState;
        // }

        public static bool CheckFullSentence(string text)
        {
            // Replace "..." with "{ELLIPSIS}"
            text = text.Replace("...", "{ELLIPSIS}");

            // Remove leading punctuation marks (".", "!", "?", ";") from SubPartSentence
            text = text.TrimStart('.', '!', '?', ';');

            // Replace specific patterns using regex
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<\|.+?\|>|\(.+?\)|\[.+?\]|\*.+?\*", "");

            // check if text has sentence ending with TEXT(R"([\.\!\?\;]+)")
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\.\!\?\;]") && text.Length > 1)
            {
                return true;
            }
            return false;
        }

        public static (string fullSentence, string restSentence) CleanSentence(string text)
        {
            text = text.Replace("...", "{ELLIPSIS}");

            // Remove TEXT(R"(<\|.+?\|>|\(.+?\)|\[.+?\]|\*.+?\*)") with TEXT("")
            text = Regex.Replace(text, @"<\|.+?\|>|\(.+?\)|\[.+?\]|\*.+?\*", "");

            // Split on punctuation marks
            var split = Regex.Split(text, @"([\.\!\?\;])", RegexOptions.None);

            // Combine the first part and the punctuation mark
            string fullSentence = split.Length > 1 ? split[0] + split[1] : text;
            string restSentence = split.Length > 2 ? string.Join("", split.Skip(2)) : "";

            // Replace "{ELLIPSIS}" with "..."  
            fullSentence = fullSentence.Replace("{ELLIPSIS}", "...");
            restSentence = restSentence.Replace("{ELLIPSIS}", "...");

            // Remove new lines at beginning and end of full sentence
            fullSentence = Regex.Replace(fullSentence, @"\s+", " ").Trim();

            return (fullSentence, restSentence);
        }

        public static int PromptTokenCount(List<Prompt> prompts)
        {
            int tokenCount = 0;
            foreach (Prompt prompt in prompts)
            {
                tokenCount += TokenCount(prompt.content);
            }
            return (int)(tokenCount * 1.4); // We add 40% to the token count to account for the model's tokenization
        }

        public static int TokenCount(string text)
        {
            return text.Split(' ').Length;
        }

        /// <summary>
        /// Generates text from an input using the LLM server.
        /// </summary>
        /// <returns>A tuple containing the generated text, whether generation is done, and whether an error occurred.</returns>
        public static async Task<(string text, bool isDone, bool isError)> GenerateTextAsync(
            Session session,
            string agentName,
            string playerName,
            string description,
            string message,
            OptionTables optionTables,
            LlmServerSettings llmServerSettings,
            LlmGenerationSettings llmGenerationSettings,
            Process llmServerProcess,
            int maxCharacters = 10000)
        {
            // Create conversation prompt
            List<Prompt> conversationPrompt = CreateConversationPrompt(
                session,
                agentName,
                playerName,
                description,
                true,
                message,
                maxCharacters,
                optionTables);

            // Check token count
            int promptTokenCount = DiagenLlm.PromptTokenCount(conversationPrompt);
            if (llmServerSettings.contextSize < promptTokenCount)
            {
                UnityEngine.Debug.LogWarning($"[LLM Generation] Context size exceeded. Count: {promptTokenCount} > {llmServerSettings.contextSize}");
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
                    conversationPrompt,
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
        /// Generates text from an input using the LLM server and provides real-time updates as chunks are received.
        /// </summary>
        /// <param name="onChunkProcessed">Callback that provides the current text, completion status, and error status after each chunk.</param>
        /// <returns>A task that completes when text generation is finished.</returns>
        public static async Task GenerateTextAsyncChunks(
            Session session,
            string agentName,
            string playerName,
            string description,
            string message,
            OptionTables optionTables,
            LlmServerSettings llmServerSettings,
            LlmGenerationSettings llmGenerationSettings,
            Process llmServerProcess,
            int maxCharacters,
            Action<string, bool, bool> onChunkProcessed)
        {
            // Create conversation prompt
            List<Prompt> conversationPrompt = CreateConversationPrompt(
                session,
                agentName,
                playerName,
                description,
                true,
                message,
                maxCharacters,
                optionTables);

            // Check token count
            int promptTokenCount = DiagenLlm.PromptTokenCount(conversationPrompt);
            if (llmServerSettings.contextSize < promptTokenCount)
            {
                UnityEngine.Debug.LogWarning($"[LLM Generation] Context size exceeded. Count: {promptTokenCount} > {llmServerSettings.contextSize}");
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
                    conversationPrompt,
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