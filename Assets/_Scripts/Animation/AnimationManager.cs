using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct MoveInstancesJob : IJobParallelFor
{
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float playbackFramesPerSecond;
    [ReadOnly] public int numFrames; // Total frame count for looping
    [ReadOnly] public NativeList<EnemyData> enemyDataList;
    [ReadOnly] public quaternion fixedXRotation;
    [ReadOnly] public bool animationIsIdle;
    [ReadOnly] public uint enemyType;

    public NativeArray<InstanceDataGpu> instanceData;

    public void Execute(int i)
    {
        float3 position = float3.zero;
        Quaternion rotation = Quaternion.identity;

        if (i < enemyDataList.Length)
        {
            EnemyData tempEnemy = enemyDataList[i];

            if (tempEnemy.enemyType == enemyType)
            {
                if (tempEnemy.IsAttacking && !animationIsIdle)
                {
                    position = tempEnemy.Position;
                    rotation = tempEnemy.Rotation;
                }

                if (!tempEnemy.IsAttacking && animationIsIdle)
                {
                    position = tempEnemy.Position;
                    rotation = tempEnemy.Rotation;
                }
            }
        }


        float3 scale = new float3(1, 1, 1);

        quaternion finalRotation = math.mul(rotation, fixedXRotation);

        InstanceDataGpu oldData = instanceData[i];

        // Calculate frame increment based on speed and time
        float frameIncrement = playbackFramesPerSecond * deltaTime;
        float newFrame = oldData.AnimationFrame + frameIncrement;

        // Loop animation frame number
        if (numFrames > 0)
        {
            newFrame = math.fmod(newFrame, (float)numFrames);
            if (newFrame < 0) newFrame += numFrames;
        }
        else
        {
            newFrame = 0; // Default to frame 0 if no frames exist
        }

        instanceData[i] = new InstanceDataGpu
        {
            Matrix = float4x4.TRS(position, finalRotation, scale), // Use the combined finalRotation
            AnimationFrame = newFrame
        };
    }
}

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;

    [SerializeField] private List<BakedDataReference> animationList = new List<BakedDataReference>();
    [SerializeField] private List<InstancingIndividualEnemy> instanceScripts = new List<InstancingIndividualEnemy>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var animation in animationList)
        {
            var handler = new InstancingIndividualEnemy(animation, enemyManager);
            instanceScripts.Add(handler);
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var instance in instanceScripts)
        {
            instance.CallUpdate();
        }
    }
}
