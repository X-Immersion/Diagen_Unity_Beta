#if UNITY_EDITOR
    using UnityEditor;
    using TableUtils;
#endif

using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

using DiagenCommonTypes;

// [CreateAssetMenu(fileName = "Dialogue", menuName = "Diagen Assets/Dialogue")]
public class DialogueTable : ScriptableObject
{
    public History[] historyArray = new History[0];

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
                List<History> historyList = new List<History>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = CSVUtils.SplitCSVLine(lines[i]);
                    History message = new History
                    {
                        Name = values[0].Trim('\"'),
                        Message = values[1].Trim('\"'),
                    };
                    historyList.Add(message);
                }

                historyArray = historyList.ToArray();
                Debug.Log($"CSV loaded from: {filePath}");
            }

        }

        [ContextMenu("Save CSV")]
        public void SaveCSV()
        {
            string filePath = CSVUtils.GetSaveFilePath("Dialogue");
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
            csv.AppendLine("Speaker,Sentence");

            // Add rows
            foreach (var sentence in historyArray)
            {
                csv.AppendLine($"\"{sentence.Name}\",\"{sentence.Message}\"");
            }

            return csv.ToString();
        }
    #endif

    public History[] GetHistory()
    {
        return historyArray;
    }

}
