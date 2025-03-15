using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public class NewPathfinding : MonoBehaviour
{
    public void RunFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {
        PrecomputedData.GetXZ(endWorldPosition, out int endX, out int endZ);
        
        FlowFieldScheduler flowFieldJobScheduler = new FlowFieldScheduler();
        flowFieldJobScheduler.ScheduleFlowFieldJobs(200, endX, endZ, _runFullFlow);

        WriteDataToCSV("output.csv");
    }

    public void WriteDataToCSV(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the header
            writer.WriteLine("index,GoToIndex,integrationCost,GoFrom,GoTo");

            // Write each flowNode's data
            for (int i = 0; i < PrecomputedData.gridArray.Length; i++)
            {
                if (PrecomputedData.gridArray[i].goToIndex<0)
                {
                    string line = $"{PrecomputedData.gridArray[i].index},{PrecomputedData.gridArray[i].goToIndex},{PrecomputedData.gridArray[i].integrationCost},{PrecomputedData.gridArray[i].x}:{PrecomputedData.gridArray[i].z},0";
                    writer.WriteLine(line);
                } else
                {
                    string line = $"{PrecomputedData.gridArray[i].index},{PrecomputedData.gridArray[i].goToIndex},{PrecomputedData.gridArray[i].integrationCost},{PrecomputedData.gridArray[i].x}:{PrecomputedData.gridArray[i].z},{PrecomputedData.gridArray[PrecomputedData.gridArray[i].goToIndex].x}:{PrecomputedData.gridArray[PrecomputedData.gridArray[i].goToIndex].z}";
                    writer.WriteLine(line);
                }
                
            }
        }
    }
}
