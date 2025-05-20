#if UNITY_EDITOR
    using UnityEditor;
    using TableUtils;
#endif

using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using DiagenCommonTypes;

[CreateAssetMenu(fileName = "CharacterInfo", menuName = "Diagen Assets/Character Information Table")]
public class CharacterInformationTable : ScriptableObject
{
    public CharacterInformation[] characterInformationsArray = new CharacterInformation[0];

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
                List<CharacterInformation> characterInformationList = new List<CharacterInformation>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = CSVUtils.SplitCSVLine(lines[i]);
                    CharacterInformation characterInfo = new CharacterInformation
                    {
                        Name = values[0].Trim('\"'),
                        StateTags = values[1].Trim('[', ']', '\"').Split(new[] { "\"\",\"\"" }, System.StringSplitOptions.None),
                        Description = values[2].Trim('\"'),
                    };
                    characterInformationList.Add(characterInfo);
                }

                characterInformationsArray = characterInformationList.ToArray();
                Debug.Log($"CSV loaded from: {filePath}");
            }
        }

        [ContextMenu("Save CSV")]
        public void SaveCSV()
        {
            string filePath = CSVUtils.GetSaveFilePath("CharacterInfo");
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
            csv.AppendLine("Name,StateTags,Description");

            // Add rows
            foreach (var row in characterInformationsArray)
            {
                string stateTags = $"\"[\"\"{string.Join("\"\",\"\"", row.StateTags)}\"\"]\"";
                csv.AppendLine($"{row.Name},{stateTags},\"{row.Description}\"");
            }

            return csv.ToString();
        }
    #endif

    public CharacterInformation[] GetCharacterInformation()
    {
        return characterInformationsArray;
    }
    

    public List<string> GetAvailableTags()
    {
        if (characterInformationsArray == null)
        {
            return new List<string>();
        }

        List<string> tags = new List<string>();
        foreach (var characterInfo in characterInformationsArray)
        {
            foreach (var tag in characterInfo.StateTags)
            {
                if (!tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }
        }
        return tags;
    }

    public List<DescriptionInfo> FindCharacterInformationFromTag(List<string> activateTags, StateTagsWeightTable stateTagsWeightTable )
    {
        List<DescriptionInfo> characterInfoList = new List<DescriptionInfo>();
        // Find all elements inside characterInformationsArray that contain all active Tags inside its own StateTags
        foreach (var characterInfo in characterInformationsArray)
        {
            bool requiredTagsExist = true;
            int weight = 0;
            foreach (var tag in characterInfo.StateTags)
            {
                if (!activateTags.Contains(tag))
                {
                    requiredTagsExist = false;
                    break;
                }
                if (stateTagsWeightTable == null)
                {
                    continue;
                }
                try
                {
                    weight += stateTagsWeightTable.FindTag(tag).Weight;
                }
                catch (System.NullReferenceException)
                {
                    weight += 1;
                }
            }

            if (requiredTagsExist)
            {
                characterInfoList.Add(
                    new DescriptionInfo
                    {
                        Description = characterInfo.Description,
                        Weight = weight
                    }
                );
            }
        }

        // Sort by weight in descending order
        characterInfoList.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        return characterInfoList;
    }
}
