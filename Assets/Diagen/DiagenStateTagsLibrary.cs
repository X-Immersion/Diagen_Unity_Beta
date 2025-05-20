
using UnityEngine;
using System.Collections.Generic;
using DiagenCommonTypes;

namespace Diagen
{    
    public class DiagenStateTags : MonoBehaviour 
    {

        public static Session AppendStateTagsOnState(List<Tag> StateTags, Session sessionState)
        {
            // Append the tags to EnableStateTags if they are not already present
            foreach (Tag tag in StateTags)
            {
                if (!sessionState.EnableStateTags.Contains(tag.tag))
                {
                    sessionState.EnableStateTags.Add(tag.tag);
                }
            }
            // Log number of ellements added
            Debug.Log($"Added {StateTags.Count} tags to EnableStateTags");
            return sessionState;
        }

        public static Session RemoveStateTagsFromState(List<Tag> StateTags, Session sessionState)
        {
            int RemovedElementCount = 0;
            // Remove the tags from EnableStateTags if they are present
            foreach (Tag tag in StateTags)
            {
                if (sessionState.EnableStateTags.Contains(tag.tag))
                {
                    sessionState.EnableStateTags.Remove(tag.tag);
                    RemovedElementCount++;
                }
            }
            // Log number of ellements removed
            Debug.Log($"Removed {RemovedElementCount} tags from EnableStateTags");
            return sessionState;
        }

        public static List<Session> SetStateTags(List<Session> sessionStates, string agentName, List<Tag> StateTags)
        {
            // Find the session state for the given Agent
            Session session = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (session != null)
            {
                // Set the tags to EnableStateTags
                session.EnableStateTags = new List<string>();
                foreach (Tag tag in StateTags)
                {
                    session.EnableStateTags.Add(tag.tag);
                }
                // Overwrite the session state in the list of session states
                sessionStates[sessionStates.FindIndex(state => state.AgentName == agentName)] = session;
            }
            else
            {
                Debug.LogError("Session state not found for Agent: " + agentName);
            }
            return sessionStates;
        }

        public static List<Session> AppendStateTags(List<Session> sessionStates, string agentName, List<Tag> StateTags)
        {
            // Find the session state for the given Agent
            Session session = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (session != null)
            {
                // Set the tags to EnableStateTags
                session = AppendStateTagsOnState(StateTags, session);
                // Overwrite the session state in the list of session states
                sessionStates[sessionStates.FindIndex(state => state.AgentName == agentName)] = session;
            }
            else
            {
                Debug.LogError("Session state not found for Agent: " + agentName);
            }
            return sessionStates;
        }

        public static List<Session> RemoveStateTags(List<Session> sessionStates, string agentName, List<Tag> StateTags)
        {
            // Find the session state for the given Agent
            Session session = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (session != null)
            {
                session = RemoveStateTagsFromState(StateTags, session);
                
                // Overwrite the session state in the list of session states
                sessionStates[sessionStates.FindIndex(state => state.AgentName == agentName)] = session;
            }
            else
            {
                Debug.LogError("Session state not found for Agent: " + agentName);
            }
            return sessionStates;
        }

        public static List<Session> ClearStateTags(List<Session> sessionStates, string agentName)
        {
            sessionStates = SetStateTags(sessionStates, agentName, new List<Tag>());
            return sessionStates;
        }

        public static Dictionary<bool, List<Tag>> ContainsStateTags(List<Session> sessionStates, string agentName, List<Tag> StateTagsToSearch)
        {
            bool FoundAllTags = true;
            // Find the session state for the given Agent
            Session session = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (session != null)
            {
                // Check if the tags are present in EnableStateTags
                List<Tag> FoundTags = new List<Tag>();
                List<Tag> NotFoundTags = new List<Tag>();

                foreach (Tag tag in StateTagsToSearch)
                {
                    if (session.EnableStateTags.Contains(tag.tag))
                    {
                        FoundTags.Add(tag);
                    }
                    else
                    {
                        NotFoundTags.Add(tag);
                        FoundAllTags = false;
                    }
                }

                return new Dictionary<bool, List<Tag>> { { FoundAllTags, NotFoundTags } };
            }
            else
            {
                Debug.LogError("Session state not found for Agent: " + agentName);
                return new Dictionary<bool, List<Tag>>();
            }
        }

        public static List<string> GetAgentEnabledStateTags(List<Session> sessionStates, string agentName)
        {
            // Find the session state for the given Agent
            Session session = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (session != null)
            {
                List<Tag> AvailableTags = session.AllStateTags;

                List<string> EnabledTags = new List<string>();

                foreach (Tag tag in AvailableTags)
                {
                    if (session.EnableStateTags.Contains(tag.tag))
                    {
                        EnabledTags.Add(tag.tag);
                    }
                }
                // Return the tags from EnableStateTags
                return EnabledTags;
            }
            else
            {
                Debug.LogError("Session state not found for Agent: " + agentName);
                return new List<string>();
            }
        }

        public static List<Tag> GetAgentAllStateTags(List<Session> sessionStates, string agentName)
        {
            // Find the session state for the given Agent
            Session session = DiagenSession.GetSessionByName(sessionStates, agentName);
            if (session != null)
            {
                // Return the tags from AllStateTags
                return session.AllStateTags;
            }
            else
            {
                Debug.LogError("Session state not found for Agent: " + agentName);
                return new List<Tag>();
            }
        }

        public static List<Tag> GetTags(List<string> tags)
        {
            List<Tag> tagList = new List<Tag>();
            foreach (string tag in tags)
            {
                Tag tagObj = new Tag();
                tagObj.tag = tag;
                tagList.Add(tagObj);
            }
            return tagList;
        }

        private static List<Tag> SortTagsByWeight(List<Tag> tags) // Descending Order
        {
            tags.Sort((x, y) => y.weight.CompareTo(x.weight));
            return tags;
        }
    }
}