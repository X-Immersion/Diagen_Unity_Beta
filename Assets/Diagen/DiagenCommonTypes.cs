
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace DiagenCommonTypes
{
    public class LlmServerSettings
    {
        public string serverAddress = "http://localhost";
        public int serverPort = 8002;
        public const string LlamaServerExecutable = "diagen-server.exe";
        public string llamaServerPath; // Initialized in Init method
        public string modelPath; // Initialized in Init method
        public int gpuLayers = -1;
        public int contextSize = 512;
        public bool flashAttention = false;
        public int gpuSelect = -1;
        public float utilization = 0.7f;
        public string LLMmodel = "baseXI3.gguf";
        public string apiKey = "DiFfIY8V.OtXo0aj6l1oPhceGRGG3ndWW7b8OUeVt";
        public string sessionId;

        public void Init()
        {
            llamaServerPath = Path.Combine(Application.dataPath, "Diagen", "Local", LlamaServerExecutable);
            modelPath = Path.Combine(Application.dataPath, "Diagen", "Local", "models", LLMmodel);
        }
    }

    public class Prompt
    {
        public string role = "";
        public string content = "";
    }

    public class LlmGenerationSettings
    {
        public string message = "";
        public string description = "";
        public string agentName = "Agent";
        public string playerName = "Player";
        public bool includeHistory = true;

        public int nPredict = 32;
        public float temperature = 2.0f;
        public int topK = 50;
        public float topP = 0.9f;
        public float frequencyPenalty = 0.8f;
        public float presencePenalty = 0.3f;
    }

    public class LlmGenerationParams
    {
        public List<Prompt> messages = new List<Prompt>();
        public int n_predict = 32;
        public float temperature = 0.7f;
        public int top_k = 50;
        public float top_p = 0.9f;
        public float frequency_penalty = 0.8f;
        public float presence_penalty = 0.3f;
        public bool cache_prompt = true;
        public bool stream = true;
    }

    public class OptionTables
    {
        public TopicTable topicTable;
        public EventsTable eventsTable;
        public StateTagsWeightTable stateTagsWeightTable;
        public CharacterInformationTable characterInformationTable;
    }

    [System.Serializable]
    public class Session
    {
        public string AgentName;
        public List<string> EnableStateTags = new List<string>();
        public List<Tag> AllStateTags = new List<Tag>();
        public List<Topic> EnableTopics = new List<Topic>();
        public List<string> ExecutedDiagenEvent = new List<string>();
        public List<History> History = new List<History>();
    }

    [System.Serializable]
    public class History
    {
        public string Name;
        public string Message;
    }
    

    [System.Serializable]
    public class Tag
    {
        public string tag;
        public int weight;
    }

    [System.Serializable]
    public class DiagenEvent
    {
        public string Name;
        public string[] StateTags; // The stateTags needed to be activated to use the trigger.
        [TextArea(3, 10)]
        public string SayVerbatim;
        [TextArea(3, 10)]
        public string Instruction;
        public string ReturnTrigger;
        public bool Repeatable;
        public bool GlobalStateTags; // NEW: Determines if this event affects all NPCs in the session
        public string[] EnableStateTags;
        public string[] DisableStateTags;

        public List<ActionEvent> ActionEvents = new List<ActionEvent>();

        public void ExecuteActionEvent()
        {
            foreach (var actionEvent in ActionEvents)
            {
                actionEvent.Execute();
            }
        }
    }

    public class EventSessionsPair
    {
        public DiagenEvent diagenEvent;
        public List<Session> sessionStates;
    }

    [System.Serializable]
    public class Topic
    {
        public string Name;
        public string[] StateTags;
        [TextArea(3, 10)]
        public string Description;

        public float Threshold;

        public string ReturnTrigger;
    }
    [System.Serializable]
    public class CharacterInformation
    {
        public string Name;
        public string[] StateTags;
        [TextArea(3, 10)]
        public string Description;
    }

    public class DescriptionInfo
    {
        public string Description;
        public int Weight;
    }

    [System.Serializable]
    public class StateTagsWeight
    {
        public string Name;
        public int Weight;
    }

    // Class for llm response: "{\"choices\":[{\"finish_reason\":\"stop\",\"index\":0,\"message\":{\"content\":\"The audacity of these petty kingdoms.  \\\"weapons\\\", please\",\"role\":\"assistant\"}}],\"created\":1737720139,\"model\":\"gpt-3.5-turbo-0613\",\"object\":\"chat.completion\",\"usage\":{\"completion_tokens\":128,\"prompt_tokens\":184,\"total_tokens\":312},\"id\":\"chatcmpl-k1s0HMgrvFxHg8Nkk1yAsT59KHpzKm3C\"}"
    [System.Serializable]
    public class LlmResponse
    {
        public List<Choice> choices;
        public long created;
        public string model;

        [JsonProperty("object")]
        public string objectType; // Use a different name for the field
        public Usage usage;
        public string id;
    }

    [System.Serializable]
    public class Choice
    {
        public string finish_reason;
        public int index;
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
        public string role;
    }

    [System.Serializable]
    public class Usage
    {
        public int completion_tokens;
        public int prompt_tokens;
        public int total_tokens;
    }

    // LLm Streaming response
    //{"choices":[{"finish_reason":null,"index":0,"delta":{"content":"("}}],"created":1737985795,"id":"chatcmpl-flb4xhtHROn0N2CCzEGUSPzVvxZL0qOp","model":"gpt-3.5-turbo-0613","object":"chat.completion.chunk"}
    [System.Serializable]
    public class LlmStreamResponse
    {
        public List<StreamingChoice> choices;
        public long created;
        public string model;

        [JsonProperty("object")]
        public string objectType; // Use a different name for the field
        public string id;
    }

    [System.Serializable]
    public class StreamingChoice
    {
        public string finish_reason;
        public int index;
        public Delta delta;
    }

    [System.Serializable]
    public class Delta
    {
        public string content;
    }
}