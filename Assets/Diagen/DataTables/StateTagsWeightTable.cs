#if UNITY_EDITOR
    using UnityEditor;
    using TableUtils;
#endif

using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

using DiagenCommonTypes;

[CreateAssetMenu(fileName = "StateTagsWeight", menuName = "Diagen Assets/State Tags Weight Table")]
public class StateTagsWeightTable : ScriptableObject
{
    [SerializeField]
    private StateTagsWeight[] stateTagsWeightArray = new StateTagsWeight[0];

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
                List<StateTagsWeight> stateTagsWeightList = new List<StateTagsWeight>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = CSVUtils.SplitCSVLine(lines[i]);
                    StateTagsWeight stateTagsWeight = new StateTagsWeight
                    {
                        Name = values[0].Trim('\"'),
                        Weight = int.Parse(values[1].Trim('\"'))
                    };
                    stateTagsWeightList.Add(stateTagsWeight);
                }

                stateTagsWeightArray = stateTagsWeightList.ToArray();
                Debug.Log($"CSV loaded from: {filePath}");
            }
        }

        [ContextMenu("Save CSV")]
        public void SaveCSV()
        {
            string filePath = CSVUtils.GetSaveFilePath("StateTagsWeight");
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
            csv.AppendLine("Name,Weight");

            // Add rows
            foreach (var row in stateTagsWeightArray)
            {
                csv.AppendLine($"{row.Name},\"{row.Weight}\"");
            }

            return csv.ToString();
        }
    #endif

    public StateTagsWeight[] GetInitialStates()
    {
        return stateTagsWeightArray;
    }


    public List<Tag> GetTagsWeight(List<string> availableTags)
    {
        List<Tag> tagsWeight = new List<Tag>();
        foreach (var tag in availableTags)
        {
            bool found = false;
            foreach (var row in stateTagsWeightArray)
            {
                if (tag == row.Name)
                {
                    tagsWeight.Add(
                        new Tag
                        {
                            tag = tag,
                            weight = row.Weight
                        }
                    );
                    found = true;
                    break;
                }
            }

            // Add weight tag of 1, if it is not found in the stateTagsWeightArray
            if (found) continue;
            tagsWeight.Add(
                new Tag
                {
                    tag = tag,
                    weight = 1
                }
            );
        }

        // Sort by weight in descending order
        tagsWeight.Sort((a, b) => b.weight.CompareTo(a.weight));

        return tagsWeight;
    }

    public StateTagsWeight FindTag(string tagName)
    {
        // Find the tag and return its weight
        return stateTagsWeightArray != null ? System.Array.Find(stateTagsWeightArray, e => e.Name == tagName) : null;
    }
}
