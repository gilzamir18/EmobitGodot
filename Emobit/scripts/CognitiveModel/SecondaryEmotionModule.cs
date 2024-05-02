using ai4u;
using ai4u.ext;
using Godot;
using System;
using System.Collections.Generic;

namespace CognusProject;

public partial class SecondaryEmotionModule : Node
{

	private PipelineSensor pipeline;

	private BasicAgent agent;

	private float qValue_tm1 = 0;
	private float maxTD = float.NegativeInfinity;
	private float minTD = float.PositiveInfinity;

	private Dictionary<string, Emotion> emotions;
	private List<HomeostaticVariable> emotionValue;

	public void SetPipeline(PipelineSensor sensor)
	{
		this.pipeline = sensor;
		
	}

	public void SetAgent(BasicAgent agent)
	{
		this.agent = agent;
	}

	public void SetEmotions(Dictionary<string, Emotion> emotions, List<HomeostaticVariable> emotionValue)
	{
		emotions["joyTD"] = new Emotion("joyTD", new int[]{}, true, 0);
		emotions["distrTD"] = new Emotion("distrTD", new int[]{}, false, 0);

		if (emotionValue != null)
		{
			HomeostaticVariable joyTD = new HomeostaticVariable();
			joyTD.name = "joyTD";
			joyTD.rangeMin = 0;
			joyTD.rangeMax = 1;
			joyTD.minValue = 0;
			joyTD.maxValue = 1;
			joyTD.Value = emotions["joyTD"].Intensity;

			HomeostaticVariable distrTD = new HomeostaticVariable();
			distrTD.name = "distrTD";
			distrTD.rangeMin = 0;
			distrTD.rangeMax = 1;
			distrTD.minValue = 0;
			distrTD.maxValue = 1;
			distrTD.Value = emotions["distrTD"].Intensity;

			emotionValue.Add(joyTD);
			emotionValue.Add(distrTD);
		}

		this.emotionValue = emotionValue;
		this.emotions = emotions;
	}

	public void Update(Dictionary<string, Emotion> emotions)
	{
		if (agent.ContainsField("qvalue") && agent.ContainsField("reward"))
		{
			float qvalue_t = agent.GetFieldArgAsFloat("qvalue");
			float reward = agent.GetFieldArgAsFloat("reward");
			var TD = 0.99f * (reward + qvalue_t) - qValue_tm1;
			qValue_tm1 = qvalue_t;

			if (TD > maxTD)
			{
				maxTD = TD;
			}
			if (TD < minTD)
			{
				minTD = TD;
			}

			if (TD > 0)
			{
				float d = 1;
				if (maxTD > 0)
				{
					d = maxTD;
				}

				emotions["joyTD"].AddValue(TD/d);
				emotions["joyApprx"].AddValue(0.01f);
			}
			else 
			{
				emotions["joyTD"].AddValue(-0.01f, true);
			}

			if (TD < 0)
			{
				float d = 1;
				if (minTD < 0)
				{
					d = Math.Abs(minTD);
				}

				emotions["distrTD"].AddValue(TD/d);
				emotions["fearPain"].AddValue(0.0001f);
				emotions["fearRad"].AddValue(0.0001f);
				emotions["distrGoal"].AddValue(0.001f);
			}
			else
			{
				emotions["joyTD"].AddValue(-0.01f, true);
			}

			if (emotionValue != null)
			{
				emotionValue[4].Value = emotions["joyTD"].Intensity;
				emotionValue[5].Value = emotions["distrTD"].Intensity;
			}
		}
	}

	public void Reset()
	{
		this.qValue_tm1 = 0;
		this.minTD = float.PositiveInfinity;
		this.maxTD = float.NegativeInfinity;
		emotions["joyTD"].Reset();
		emotions["distrTD"].Reset();
	}
}
