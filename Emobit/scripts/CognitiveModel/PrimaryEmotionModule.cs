using ai4u.ext;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CognusProject;

public sealed class PrimaryEmotionModule
{

    private static float[] dMinusOne;

    private PrimaryEmotionModule()
    {

    }

    public static void Evaluate(Dictionary<string, Emotion> emotions, HomeostaticVariable[] variables)
    {
        if (dMinusOne == null)
        {
            CreateDistanceTracker(variables);
        }

        foreach(var emost in emotions.Keys)
        {
            var em = emotions[emost];
            int[] sts = em.Stimulus;

            for (int i = 0; i < sts.Length; i++)
            {
                var v = variables[sts[i]];
                
                var dt = v.DistanceToCentroid;

                var delta = dt - dMinusOne[i];

                dMinusOne[i] = dt;

                /*if (em.Code == "fearPain")
                {
                    GD.Print("Delta: " + delta);
                }*/
                if (delta > 0)
                {
                    em.AddValue(10*delta);
                }
                else if (delta < 0)
                {
                    em.AddValue(delta, true);
                }
                else
                {
                    em.AddValue(-0.2f, true);
                }
            }
        }
    }

    public static void ResetPrimaryEmotionData(HomeostaticVariable[] variables)
    {
        CreateDistanceTracker(variables);
    }

    private static void CreateDistanceTracker(HomeostaticVariable[] vars)
    {
        dMinusOne = new float[vars.Length];
        for (int i = 0; i < vars.Length; i++)
        {
            dMinusOne[i] = vars[i].DistanceToCentroid;
        }
    }
}
