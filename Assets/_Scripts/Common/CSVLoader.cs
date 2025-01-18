using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CSVLoader
{
    public Dictionary<string, List<float>> LoadCSV(TextAsset csvFile)
    {
        Dictionary<string, List<float>> data = new Dictionary<string, List<float>>();
        string[] lines = csvFile.text.Split('\n');

        foreach (string line in lines)
        {
            string[] parts = line.Split('\t'); // Assuming tab-separated values
            if (parts.Length < 2) continue;

            string key = parts[0];
            List<float> values = parts.Skip(1).Select(float.Parse).ToList();
            data[key] = values;
        }

        return data;
    }
}