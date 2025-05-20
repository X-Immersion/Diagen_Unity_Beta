#if UNITY_EDITOR
    using UnityEditor;
    using TableUtils;
#endif

using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DiagenCommonTypes;



[CreateAssetMenu(fileName = "EventsInfo", menuName = "Diagen Assets/Events Table")]
public class EventsTable : ScriptableObject
{
    public DiagenEvent[] eventsArray = new DiagenEvent[0];

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
                List<DiagenEvent> eventsList = new List<DiagenEvent>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = CSVUtils.SplitCSVLine(lines[i]);
    
                    // Convert the array to a List for easier resizing
                    List<string> valuesList = values.ToList();

                    // Ensure the list has at least 7 elements by adding empty values
                    while (valuesList.Count < 7)
                    {
                        valuesList.Add("");  // Fill missing fields with empty strings
                    }

                    DiagenEvent evt = new DiagenEvent
                    {
                        Name = string.IsNullOrEmpty(valuesList[0]) ? "UnnamedEvent" : valuesList[0].Trim('\"'),
                        SayVerbatim = valuesList[1].Trim('\"'),  // Empty string if missing
                        Instruction = valuesList[2].Trim('\"'),
                        ReturnTrigger = valuesList[3].Trim('\"'),
                        Repeatable = bool.TryParse(valuesList[4].Trim('\"'), out bool repeat) ? repeat : false,
                        EnableStateTags = string.IsNullOrEmpty(valuesList[5]) ? new string[] {""} : valuesList[5].Trim('[', ']', '\"').Split(new[] { "\"\",\"\"" }, System.StringSplitOptions.None),
                        DisableStateTags = string.IsNullOrEmpty(valuesList[6]) ? new string[] {""} : valuesList[6].Trim('[', ']', '\"').Split(new[] { "\"\",\"\"" }, System.StringSplitOptions.None),
                    };

                    eventsList.Add(evt);
                }



                eventsArray = eventsList.ToArray();
                Debug.Log($"CSV loaded from: {filePath}");
            }
        }

        [ContextMenu("Save CSV")]
        public void SaveCSV()
        {
            string filePath = CSVUtils.GetSaveFilePath("Events");
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
            csv.AppendLine("Name,SayVerbatim,Instruction,ReturnTrigger,Repeatable,EnableStateTags,DisableStateTags");

            // Add rows
            foreach (var evt in eventsArray)
            {
                string enableStates = $"\"[\"\"{string.Join("\"\",\"\"", evt.EnableStateTags)}\"\"]\"";
                string disableStates = $"\"[\"\"{string.Join("\"\",\"\"", evt.DisableStateTags)}\"\"]\"";
                csv.AppendLine($"{evt.Name},\"{evt.SayVerbatim}\",\"{evt.Instruction}\",\"{evt.ReturnTrigger}\",\"{evt.Repeatable}\",{enableStates},{disableStates}");
            }

            return csv.ToString();
        }
#endif

    public List<DiagenEvent> GetEvents()
    {
        return eventsArray != null ? eventsArray.ToList() : new List<DiagenEvent>();
    }

    public DiagenEvent FindEvent(string eventName)
    {
        return eventsArray != null ? System.Array.Find(eventsArray, e => e.Name == eventName) : null;
    }

}