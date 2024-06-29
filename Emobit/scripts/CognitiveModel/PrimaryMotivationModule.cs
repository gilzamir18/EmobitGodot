using ai4u.ext;
using System;
using System.Linq;

namespace CognusProject;

public sealed class PrimaryMotivationModule
{
    private static float[] previousHomeostaticDistance;


    private PrimaryMotivationModule()
    {

    }

    public static void Attention(HomeostaticVariable[] variables, float[] hC, bool radProtected)
    {
        if (previousHomeostaticDistance == null)
        {
            previousHomeostaticDistance = new float[variables.Length];
            for (int i = 0; i < variables.Length; i++)
            {
                previousHomeostaticDistance[i] = variables[i].DistanceToCentroid;
            }
        }
        
        for (int i = 0; i < variables.Length; i++)
        {
            if (i == 0 && radProtected)
            {
                hC[0] = 0;
                continue;
            }

            float u = variables[i].Check() ? 1 : 0;
            
            var distanceToCentroid = variables[i].DistanceToCentroid;
            var delta = distanceToCentroid - previousHomeostaticDistance[i];
            previousHomeostaticDistance[i] = distanceToCentroid;
        
            float v = delta >= 0 ? 1 : 0;

            hC[i] = u + (1 - u) * (1 - 3 * v);
        }
    }

    public static void ResetPrimaryMotivationData(HomeostaticVariable[] variables)
    {
        previousHomeostaticDistance = new float[variables.Length];
        for (int i = 0; i < variables.Length; i++)
        {
            previousHomeostaticDistance[i] = variables[i].DistanceToCentroid;
        }
    }
}
