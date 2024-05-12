using ai4u.ext;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CognusProject;

public sealed class RewardModule
{

    private RewardModule()
    {

    }

    private static  float beforeJoy =  0;
    private static float beforeDistr = 0;

    public static float Rewarding(Dictionary<string, Emotion> emotions, HomeostaticVariable[] variables, float[] w, float[] hC)
    {
        var fearRad = emotions["fearRad"];
        var fearPain = emotions["fearPain"];
        var joyApprx = emotions["joyApprx"];
        var distrGoal = emotions["distrGoal"];
        
        w[0] = w[1] = w[2] = w[3] = w[4] = w[5] = w[6] = 1;
        if (fearRad.Intensity > 0)
        {
            w[0] = 2;
            w[2] = w[3] = w[4] = 0;
        }

        if (fearPain.Intensity > 0)
        {
            w[1] = 0;
        }
        else
        {
            w[1] = 1;
        }

        var difjoy = joyApprx.Intensity - beforeJoy;
        if (difjoy > 0)
        {
            w[4] = 2;
        }
        beforeJoy = joyApprx.Intensity;

        var difdistr = distrGoal.Intensity - beforeDistr;
        if (difdistr > 0)
        {
            w[PipelineSensor.FRUSTRATION] = 2;
        }
        beforeDistr = distrGoal.Intensity;

        return w[0] * hC[0] + w[1] * hC[1] + w[2] * hC[2] + w[3] * hC[3] + w[4] * hC[4] + w[5] * hC[5] + w[6] * hC[6];
    }

    public static void ResetRewardData(HomeostaticVariable[] variables)
    {
        beforeJoy = 0;
        beforeDistr = 0;
    }
}
