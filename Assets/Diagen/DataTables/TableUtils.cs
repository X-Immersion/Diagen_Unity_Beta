#if UNITY_EDITOR

using System.Text;
using System.Collections.Generic;
using UnityEditor;

namespace TableUtils
{
    public static class CSVUtils
    {
        public static string[] SplitCSVLine(string line)
        {
            List<string> fields = new List<string>();

            // Using regex to extract everything inside "(...)" into list and replacing "(...)" fields with index of list element
            StringBuilder currentField = new StringBuilder();
            bool inQuotes = false;
            int openParenthesis = 0;
            List<string> parenthesisFields = new List<string>();

            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\[([^]]*)\]");
            var matches = regex.Matches(line);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                parenthesisFields.Add(match.Groups[1].Value);
                line = line.Replace(match.Value, $"[{parenthesisFields.Count - 1}]");
            }

            foreach (char c in line)
            {
                if (c == ',' && !inQuotes && openParenthesis == 0)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                    }
                    else if (c == '[')
                    {
                        openParenthesis++;
                    }
                    else if (c == ']')
                    {
                        openParenthesis--;
                    }
                    currentField.Append(c);
                }
            }

            // Add the last field if any
            if (currentField.Length > 0)
            {
                fields.Add(currentField.ToString().Trim());
            }

            // Replace placeholders with original parenthesis fields
            for (int i = 0; i < fields.Count; i++)
            {
                foreach (var field in parenthesisFields)
                {
                    fields[i] = fields[i].Replace($"[{parenthesisFields.IndexOf(field)}]", $"[{field}]");
                }
            }

            return fields.ToArray();
        }

        public static string GetSaveFilePath(string filename = "Table")
        {
            return EditorUtility.SaveFilePanel("Save CSV", "", $"{filename}.csv", "csv");
        }

        public static string GetLoadFilePath()
        {
            return EditorUtility.OpenFilePanel("Load CSV", "", "csv");
        }
    }
}

#endif
