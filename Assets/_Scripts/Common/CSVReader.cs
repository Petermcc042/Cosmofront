using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CSVReader : MonoBehaviour
{

    public List<int> GetLastLineIntegers(string _csvFilePath)
    {
        List<int> result = new List<int>();

        if (File.Exists(_csvFilePath))
        {
            string[] lines = File.ReadAllLines(_csvFilePath);

            if (lines.Length > 0)
            {
                string[] lastLineValues = lines[lines.Length - 1].Split(',');

                foreach (string value in lastLineValues)
                {
                    if (int.TryParse(value, out int intValue))
                    {
                        result.Add(intValue);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse integer: " + value);
                    }
                }
            }
            else
            {
                Debug.LogError("CSV file is empty");
            }
        }
        else
        {
            Debug.LogError("CSV file not found: " + _csvFilePath);
        }

        return result;
    }

    public List<int> GetSecondLastLineFloats(string _csvFilePath)
    {
        List<int> result = new List<int>();

        if (File.Exists(_csvFilePath))
        {
            string[] lines = File.ReadAllLines(_csvFilePath);

            if (lines.Length > 1)
            {
                string[] secondLastLineValues = lines[lines.Length - 2].Split(',');

                foreach (string value in secondLastLineValues)
                {
                    if (int.TryParse(value, out int floatValue))
                    {
                        result.Add(floatValue);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse float: " + value);
                    }
                }
            }
            else
            {
                Debug.LogError("CSV file has only one line");
            }
        }
        else
        {
            Debug.LogError("CSV file not found: " + _csvFilePath);
        }

        return result;
    }

    public List<List<float>> ReadCSVAndCreateColumns(string _csvFilePath)
    {
        List<List<float>> columns = new List<List<float>>();

        if (File.Exists(_csvFilePath))
        {
            // Read all lines from the CSV file
            string[] lines = File.ReadAllLines(_csvFilePath);

            // Remove the last two rows
            List<string> filteredLines = new List<string>(lines);
            filteredLines.RemoveRange(lines.Length - 2, 2);

            // Iterate through each column index
            for (int i = 0; i < filteredLines[0].Split(',').Length; i++)
            {
                List<float> column = new List<float>();

                // Iterate through each row and extract the value for the current column
                foreach (string line in filteredLines)
                {
                    string[] values = line.Split(',');

                    // Parse the value to float
                    if (float.TryParse(values[i], out float floatValue))
                    {
                        column.Add(floatValue);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse float: " + values[i]);
                    }
                }

                // Add the column to the list of columns
                columns.Add(column);
            }
        }
        else
        {
            Debug.LogError("CSV file not found: " + _csvFilePath);
        }

        return columns;
    }

}