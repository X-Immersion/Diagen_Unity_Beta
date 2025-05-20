using UnityEngine;
using System.Collections.Generic;
using System.IO;
using DiagenCommonTypes;
using System;

namespace Diagen
{    
    public class DiagenSession : MonoBehaviour 
    {
        [System.Serializable]
        private class SessionStateListWrapper
        {
            public Session[] SessionStates;
        }

        public static List<Session> GetGlobalSessions()
        {

            return DiagenSubsystem.GetSessions();
        }

        public static List<Session> GetGlobalSessionByName(string agentName)
        {
            List<Session> sessionStates = DiagenSubsystem.GetSessions();
            Session sessionState = GetSessionByName(sessionStates, agentName);

            if (sessionState != null)
            {
                return new List<Session> { sessionState };
            }
            else
            {
                return new List<Session>();
            }
        }

        public static void SetGlobalSessions(List<Session> sessionStates)
        {
            DiagenSubsystem.SetSessions(sessionStates);
        }

        public static void SetGlobalSessionByName(string agentName, Session sessionState)
        {
            List<Session> sessionStates = DiagenSubsystem.GetSessions();
            sessionStates = UpdateSessionByName(sessionStates, agentName, sessionState);
            DiagenSubsystem.SetSessions(sessionStates);
        }

        public static List<Session> InitSession(List<Session> sessionStates, string agentName)
        {
            Session sessionState = GetSessionByName(sessionStates, agentName);

            if (sessionState != null)
            {
                sessionState = ResetSessionState(sessionState);
                sessionStates = UpdateSessionByName(sessionStates, agentName, sessionState);
            }
            else 
            {
                Session newSessionState = new Session();
                newSessionState.AgentName = agentName;
                sessionStates.Add(newSessionState);
            }
            return sessionStates;
        }

        public static Session GetSessionByName(List<Session> sessionStates, string agentName)
        {
            return sessionStates.Find(state => state.AgentName == agentName);
        }

        public static List<Session> RemoveSession (List<Session> sessionStates, string agentName)
        {
            Session sessionState = GetSessionByName(sessionStates, agentName);

            if (sessionState != null)
            {
                sessionStates.Remove(sessionState);
            }
            return sessionStates;
        }

        public static List<Session> UpdateSessionByName(List<Session> sessionStates, string agentName, Session sessionState)
        {
            int index = sessionStates.FindIndex(state => state.AgentName == agentName);
            if (index >= 0)
            {
                sessionStates[index] = sessionState;
            }
            return sessionStates;
        }

        public static List<Session> UpdateSessionWithOptions(
            List<Session> sessionStates, 
            string agentName,
            OptionTables optionTables)
        {
            // Find the session state for the given Agent
            Session sessionState = GetSessionByName(sessionStates, agentName);

            if (sessionState != null)
            {
                // Remove state tags if not in AllStateTags
                sessionState.EnableStateTags.RemoveAll(tag => sessionState.AllStateTags.FindIndex(t => t.tag == tag) < 0);

                // Update enabled topics if they dropped out of the list
                sessionState = DiagenTopic.EnableTopics(sessionState, agentName, optionTables);
                
                // Overwrite the session state in the list of session states
                sessionStates = UpdateSessionByName(sessionStates, agentName, sessionState);
            }
            return sessionStates;
        }

        public static List<Session> ResetSession(
            List<Session> sessionStates, 
            string agentName)
        {
            // Find the session state for the given Agent
            Session sessionState = GetSessionByName(sessionStates, agentName);

            if (sessionState != null)
            {
                // Reset the session state
                sessionState = ResetSessionState(sessionState);

                // Overwrite the session state in the list of session states
                sessionStates = UpdateSessionByName(sessionStates, agentName, sessionState);
            }
            else 
            {
                // Initialize the session
                Session newSessionState = new Session();
                newSessionState.AgentName = agentName;

                // Add the new session state to the list of session states
                sessionStates.Add(newSessionState);
            }
            return sessionStates;
        }

        private static Session ResetSessionState(Session sessionState)
        {
            // Reset the session state
            sessionState.EnableStateTags = new List<string>();
            sessionState.AllStateTags = new List<Tag>();
            sessionState.EnableTopics = new List<Topic>();
            sessionState.ExecutedDiagenEvent = new List<string>();
            sessionState.History = new List<History>();

            return sessionState;
        }


        public static List<Session> BindAllStateTagsToAgent(
            List<Session> sessionStates,
            string agentName,
            OptionTables optionTables)
            {
                if (optionTables == null)
                {
                    Debug.Log("BindAllStateTagsToAgent: optionTables is null.");
                    optionTables = new OptionTables();
                    // return sessionStates;
                }

                if (optionTables.characterInformationTable == null)
                {
                    Debug.Log("BindAllStateTagsToAgent: characterInformationTable is null.");
                    optionTables.characterInformationTable = new CharacterInformationTable();
                    // return sessionStates;
                }

                if (optionTables.topicTable == null)
                {
                    Debug.Log("BindAllStateTagsToAgent: topicTable is null.");
                    optionTables.topicTable = new TopicTable();
                    // return sessionStates;
                }

                if (optionTables.stateTagsWeightTable == null)
                {
                    Debug.Log("BindAllStateTagsToAgent: stateTagsWeightTable is null.");
                    // Make empty options table
                    optionTables.stateTagsWeightTable = new StateTagsWeightTable();
                    // return sessionStates;
                }

                if (optionTables.eventsTable == null)
                {
                    Debug.Log("BindAllStateTagsToAgent: eventsTable is null.");
                    optionTables.eventsTable = new EventsTable();
                    // return sessionStates;
                }

                List<string> availableTags = new List<string>();

                // Get all tags from optionTables character information table
                availableTags.AddRange(optionTables.characterInformationTable.GetAvailableTags() ?? new List<string>());
                availableTags.AddRange(optionTables.topicTable.GetAvailableTags() ?? new List<string>());

                // Remove duplicates
                availableTags = new List<string>(new HashSet<string>(availableTags));

                List<Tag> tagsWeight = optionTables.stateTagsWeightTable.GetTagsWeight(availableTags) ?? new List<Tag>();

                // Find the session state for the given Agent
                Session sessionState = GetSessionByName(sessionStates, agentName);

                if (sessionState == null)
                {
                    UnityEngine.Debug.LogError($"BindAllStateTagsToAgent: No session found for Agent '{agentName}'.");
                    return sessionStates;
                }

                sessionState.AllStateTags = tagsWeight;

                return sessionStates;
            }


    public static Session UpdateHistory(
            Session sessionState, 
            List<History> history,
            int insertIndex = -1,
            int keepNEntries = 8)
        {
            // Find the session state for the given Agent
            if (sessionState != null)
            {
                if (insertIndex >= 0)
                {
                    sessionState.History.InsertRange(insertIndex, history);
                }
                else
                {
                    sessionState.History.AddRange(history);
                }

                // Ensure the history does not exceed the specified number of entries
                if (sessionState.History.Count > keepNEntries && keepNEntries > 0)
                {
                    sessionState.History.RemoveRange(0, sessionState.History.Count - keepNEntries);
                }
            }
            else
            {
                Debug.LogError("No Session state provided");
            }

            return sessionState;
        }

        public static Session ResetHistory(Session sessionState)
        {
            sessionState.History = new List<History>();
            return sessionState;
        }

        public static Session UpdateEnabledTopics(
            Session sessionState, 
            List<Topic> topics, 
            bool enable, 
            bool onlyFirst = false)
        {
            if (enable)
            {
                if (onlyFirst)
                {
                    Debug.LogError("Enable only first topic not supported for enabling topics");
                }
                else
                {
                    sessionState.EnableTopics.AddRange(topics);
                }
            }
            else
            {
                if (onlyFirst && topics.Count > 0)
                {
                    sessionState.EnableTopics.Remove(topics[0]);
                }
                else
                {
                    foreach (var topic in topics)
                    {
                        sessionState.EnableTopics.Remove(topic);
                    }
                }
            }
            return sessionState;
        }

        public static bool SaveSessionStates(string saveFileName, List<Session> sessionStates)
        {
            try
            {
                var wrapper = new SessionStateListWrapper { SessionStates = sessionStates.ToArray() };
                string json = JsonUtility.ToJson(wrapper, true); // `true` adds pretty printing for readability
                System.IO.File.WriteAllText(saveFileName, json);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to save session states: " + ex.Message);
                return false;
            }
        }

        public static List<Session> LoadSessionStates(string saveFileName)
        {
            try
            {
                // Read the JSON string from the file
                string json = File.ReadAllText(saveFileName);
                
                // Deserialize the JSON into the wrapper class
                var wrapper = JsonUtility.FromJson<SessionStateListWrapper>(json);
                
                // Convert the array back to a List<Session> and return it
                return wrapper?.SessionStates != null ? new List<Session>(wrapper.SessionStates) : new List<Session>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to load session states: " + ex.Message);
                return new List<Session>();
            }
        }

        public static int GetSessionIndex(List<Session> sessionStates, string agentName)
        {
            return sessionStates.FindIndex(state => state.AgentName == agentName);
        }

        public static List<Session> EnableTags(
            List<Session> sessionStates, 
            string agentName, 
            List<string> tags)
        {
            // Find the session state for the given Agent
            Session sessionState = GetSessionByName(sessionStates, agentName);

            if (sessionState != null)
            {
                // Add the tags to the list of enabled state tags
                sessionState.EnableStateTags.AddRange(tags);
                
                // Overwrite the session state in the list of session states
                sessionStates = UpdateSessionByName(sessionStates, agentName, sessionState);
            }
            return sessionStates;
        }

    }
}
