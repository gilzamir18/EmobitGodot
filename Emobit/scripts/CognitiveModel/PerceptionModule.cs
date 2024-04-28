using ai4u.ext;
using Godot;
using System;
using System.Linq;

namespace CognusProject;

public sealed class PerceptionModule
{

    private static float minFruitDistance = float.PositiveInfinity;

    private static int FRUIT_ID = 200;


    private PerceptionModule()
    {

    }

    public static void Percept(HomeostaticVariable[] variables,
                                                    float fruitDirection, 
                                                    float fruitDistance,
                                                    float radiationDistance,
                                                    float agentDist,
                                                    float[] visionData,
                                                    float[] action,
                                                    float collisionWall)
    {

        if (radiationDistance > 1)
        {
            variables[PipelineSensor.ILLNESS].Value = 1.0f/radiationDistance;
        }
        else if (radiationDistance >= 0)
        {
            variables[PipelineSensor.ILLNESS].Value = 1.0f;
        }
        else
        {
            variables[PipelineSensor.ILLNESS].Value = 0;
        }

        if (collisionWall > 0)
        {
            variables[PipelineSensor.PAIN].AddValue(collisionWall * 0.01f);
        }
        else
        {
            variables[PipelineSensor.PAIN].AddValue(-0.01f);
        }


        if (fruitDistance > 1)
        {
            variables[PipelineSensor.FRUIT_SMELL].Value = 1.0f/fruitDistance;
        }
        else
        {
            variables[PipelineSensor.FRUIT_SMELL].Value = 1.0f;
        }

        variables[PipelineSensor.FRUIT_BRIGHT].Value = (1 + fruitDirection)/2.0f;


        var delta = fruitDistance - minFruitDistance;

        //GD.Print($"{fruitDistance} ==== {minFruitDistance} ==== {delta}");


        if (minFruitDistance == float.PositiveInfinity || 
                (fruitDistance > 0 && delta < 0 && Math.Abs(delta) > 0.000001) )
        {
            minFruitDistance = fruitDistance;
            variables[PipelineSensor.SATISFACTION].AddValue(0.09f);
        }
        else
        {
            variables[PipelineSensor.SATISFACTION].AddValue(-0.01f);
        }

        bool fruitVisible = visionData.Contains(FRUIT_ID);

        if (fruitVisible)
        {
            variables[PipelineSensor.FRUSTRATION].AddValue(-0.02f);
        }
        else
        {
            variables[PipelineSensor.FRUSTRATION].AddValue(0.01f);
        }

        if (agentDist <= 0.1)
        {
            variables[PipelineSensor.TIREDNESS].AddValue(0.01f);
        }
        else
        {
            variables[PipelineSensor.TIREDNESS].AddValue(-0.008f);
        }
    }

    public static void ResetPerceptionData()
    {
        minFruitDistance = float.PositiveInfinity;
    }
}
