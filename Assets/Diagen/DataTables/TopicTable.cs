#if UNITY_EDITOR
    using UnityEditor;
    using TableUtils;
#endif

using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

using DiagenCommonTypes;

[CreateAssetMenu(fileName = "Topics", menuName = "Diagen Assets/Topic Detection Table")]
public class TopicTable : ScriptableObject
{
    public Topic[] topicsArray = new Topic[0];

#if UNITY_EDITOR
        [ContextMenu("Load CSV")]
        public void LoadCSV()
        {
            // Implementation to load from CSV
            string filePath = CSVUtils.GetLoadFilePath();

            if (!string.IsNullOrEmpty(filePath))
            {
                // Read the file
                string[] lines = File.ReadAllLines(filePath);

                // Skip the first line as it contains headers
                List<Topic> topicsList = new List<Topic>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = CSVUtils.SplitCSVLine(lines[i]);
                    Topic topic = new Topic
                    {
                        Name = values[0].Trim('\"'),
                        StateTags = values[1].Trim('[', ']', '\"').Split(new[] { "\"\",\"\"" }, System.StringSplitOptions.None),
                        Description = values[2].Trim('\"'),
                        Threshold = Mathf.Clamp(float.Parse(values[3].Trim('\"')), 0f, 1f),
                        ReturnTrigger = values[4].Trim('\"')
                    };
                    topicsList.Add(topic);
                }

                topicsArray = topicsList.ToArray();
                Debug.Log($"CSV loaded from: {filePath}");
            }

        }

        [ContextMenu("Save CSV")]
        public void SaveCSV()
        {
            string filePath = CSVUtils.GetSaveFilePath("Topics");
            if (!string.IsNullOrEmpty(filePath))
            {
                // Generate CSV content
                string csv = GenerateCSV();
                
                // Write to file
                File.WriteAllText(filePath, csv);
                // Refresh the AssetDatabase to show the new file in the Project window
                AssetDatabase.Refresh();

                Debug.Log($"CSV saved to: {filePath}");
            }
        }
        private string GenerateCSV()
        {
            // Add headers
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Name,StateTags,Description,Threshold,ReturnTrigger");

            // Add rows
            foreach (var topic in topicsArray)
            {
                string stateTags = $"\"[\"\"{string.Join("\"\",\"\"", topic.StateTags)}\"\"]\"";
                csv.AppendLine($"{topic.Name},{stateTags},\"{topic.Description}\",\"{topic.Threshold}\",\"{topic.ReturnTrigger}\"");
            }

            return csv.ToString();
        }
#endif

    public Topic[] GetTopics()
    {
        return topicsArray;
    }


    public List<string> GetAvailableTags()
    {
        if (topicsArray == null)
        {
            return new List<string>();
        }

        List<string> availableTags = new List<string>();
        foreach (var topic in topicsArray)
        {   
            foreach (var tag in topic.StateTags)
            {
                if (!availableTags.Contains(tag))
                {
                    availableTags.Add(tag);
                }
            }
        }
        return availableTags;
    }

    public List<Topic> FindTopicFromTag(List<string> activateTags)
    {
        if (topicsArray == null)
        {
            return new List<Topic>();
        }

        List<Topic> topicList = new List<Topic>();
        // Find all elements inside characterInformationsArray that contain all active Tags inside its own StateTags
        foreach (var topic in topicsArray)
        {
            bool requiredTagsExist = true;
            foreach (var tag in topic.StateTags)
            {
                if (!activateTags.Contains(tag))
                {
                    requiredTagsExist = false;
                    break;
                }
            }

            if (requiredTagsExist)
            {
                topicList.Add(topic);
            }
        }
        return topicList;
    }

    public Topic FindTopic(string topicName)
    {
        return topicsArray != null ? System.Array.Find(topicsArray, t => t.Name == topicName) : null;
    }


}

