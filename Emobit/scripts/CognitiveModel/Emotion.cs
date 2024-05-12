using System;
using System.Data.SqlTypes;
using System.Linq;
using Godot;
using static ai4u.math.AI4UMath;

namespace CognusProject;


public partial class Emotion
{
    private float accumulator;

    private float initialValue;

    private float decay;

    private float maxAccumulator = 2500;

    private string code;

    public string Code => code;

    private int[] stimulus;

    private bool positive;

    public int[] Stimulus => stimulus.ToArray();

    public int Signal => positive ? 1 : -1;

    public bool IsPositive => positive;


    public Emotion(string code, int[] stimulus, bool positive, float decay = 0.01f, float initialValue=0.0f, float maxAccumulator = 2500)
    {
        this.code = code;
        this.initialValue = initialValue;
        this.accumulator = initialValue;
        this.decay = decay;
        this.stimulus = stimulus;
        this.positive = positive;
        this.maxAccumulator = maxAccumulator;
    }

    public void Reset()
    {
        accumulator = initialValue;
    }

    public void SetZero()
    {
        accumulator = 0;
    }

    public float MaxAccumulator => maxAccumulator;

    public float Accumulator
    {
        get
        {
            return this.accumulator;
        }
    }

    public float Intensity
    {
        get
        {
            if (accumulator > 0)
            {
                return  Clip(0.1f * (float)Math.Log2(accumulator), 0, 1);
            }
            else
            {
                return 0;
            }
        }
    }

    public void Decay()
    {
        accumulator = Clip(accumulator - decay, 0, 1);
    }

    public void AddValue(float v, bool plan = false)
    {
        v = Clip(v, -1, 1);
        if (plan)
        {
            if (v > 0)
            {
                accumulator = Clip(accumulator + v, 0, maxAccumulator);
            }
            else if (v < 0)
            {
                accumulator = Clip(accumulator + v * Math.Abs(accumulator), 0, maxAccumulator);
            }
        }
        else
        {
            if (v > 0)
            {
                accumulator += (float)Math.Pow(2, 10 * v);
                accumulator = Clip(accumulator, 0, maxAccumulator);
            }
            else if (v < 0)
            {
                accumulator += (float)Math.Pow(2, 10 * (-v) );
                accumulator = Clip(accumulator, 0, maxAccumulator);
            }
        }
    }
}
