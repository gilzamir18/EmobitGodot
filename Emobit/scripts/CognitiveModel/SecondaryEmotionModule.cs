using ai4u;
using ai4u.ext;
using Godot;
using System;
using System.Collections.Generic;
using ai4u.math;

namespace CognusProject;

public partial class SecondaryEmotionModule : Node
{


	[Export]
	private bool enableHopeAndSurprise = false;

	private PipelineSensor pipeline;

	private BasicAgent agent;

	private float qValue_tm1 = 0;
	private float maxTD = float.NegativeInfinity;
	private float minTD = float.PositiveInfinity;

	private float maxRW = float.NegativeInfinity;
	private float minRW = float.PositiveInfinity;


	private float maxD = float.NegativeInfinity;
	private float minD = float.PositiveInfinity;


	private Dictionary<string, Emotion> emotions;
	private List<HomeostaticVariable> emotionValue;


	private float previewPredictionOfReward = float.NegativeInfinity;
	

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

		if (enableHopeAndSurprise)
		{
			emotions["hopeR"] = new Emotion("hopeR", new int[]{}, true, 0);
			emotions["surpriseD+"] = new Emotion("surpriseD+", new int[]{}, true, 0);
			emotions["surpriseD-"] = new Emotion("surpriseD-", new int[]{}, false, 0);
		}


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

			if (enableHopeAndSurprise)
			{
				HomeostaticVariable surpriseRPlus = new HomeostaticVariable();
				surpriseRPlus.name = "surpriseD+";
				surpriseRPlus.rangeMin = 0;
				surpriseRPlus.rangeMax = 1;
				surpriseRPlus.minValue = 0;
				surpriseRPlus.maxValue = 1;
				surpriseRPlus.Value = emotions["surpriseD+"].Intensity;

				HomeostaticVariable surpriseRMinus = new HomeostaticVariable();
				surpriseRMinus.name = "surpriseD-";
				surpriseRMinus.rangeMin = 0;
				surpriseRMinus.rangeMax = 1;
				surpriseRMinus.minValue = 0;
				surpriseRMinus.maxValue = 1;
				surpriseRMinus.Value = emotions["surpriseD-"].Intensity;

				HomeostaticVariable hopeR = new HomeostaticVariable();
				hopeR.name = "hopeR";
				hopeR.rangeMin = 0;
				hopeR.rangeMax = 1;
				hopeR.minValue = 0;
				hopeR.maxValue = 1;
				hopeR.Value = emotions["hopeR"].Intensity;

				emotionValue.Add(surpriseRPlus);
				emotionValue.Add(surpriseRMinus);
				emotionValue.Add(hopeR);
			}
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


			if (reward > maxRW)
			{
				maxRW = reward;
			}
			if (reward < minRW)
			{
				minRW = reward;
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

				emotions["distrTD"].AddValue(Math.Abs(TD)/d);
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
				if (enableHopeAndSurprise)
				{
					emotionValue[6].Value = emotions["surpriseD+"].Intensity;
					emotionValue[7].Value = emotions["surpriseD-"].Intensity;
					emotionValue[8].Value = emotions["hopeR"].Intensity;
				}
			}

			if (enableHopeAndSurprise)
			{
				if (agent.ContainsField("r'[t]"))
				{
					var rtl = agent.GetFieldArgAsFloat("r'[t]");

					if (previewPredictionOfReward == float.NegativeInfinity)
					{
						previewPredictionOfReward = reward;
					}

					if ( rtl > 0)
					{
						if (maxRW > 0)
						{
							emotions["hopeR"].AddValue(rtl/maxRW);
						}
						else
						{
							emotions["hopeR"].AddValue(AI4UMath.Clip(rtl, 0, 1));
						}
					}
					else
					{
						emotions["hopeR"].AddValue(-0.01f, true);
					}

					var D = previewPredictionOfReward - reward;

					if (D > maxD)
					{
						maxD = D;
					}
					
					if (D < minD)
					{
						minD = D;
					}


					if (D > 0)
					{
						if (maxD > 0)
						{
							emotions["surpriseD+"].AddValue(D/maxD);
						}
						else
						{
							emotions["surpriseD+"].AddValue(AI4UMath.Clip(D, 0, 1));
						}

						emotions["joyApprx"].AddValue(0.01f);
						emotions["surpriseD-"].AddValue(-0.01f, true);
					}
					else if (D < 0)
					{
						if (minD < 0)
						{
							emotions["surpriseD-"].AddValue(D/minD);

						}
						else
						{
							emotions["surprise-"].AddValue(AI4UMath.Clip(Math.Abs(D), 0, 1));
						}
						emotions["surpriseD+"].AddValue(-0.01f, true);
						emotions["fearRad"].AddValue(0.01f);
						emotions["distrGoal"].AddValue(0.01f);
					}
					else
					{
						emotions["surpriseD-"].AddValue(-0.01f, true);
						emotions["surpriseD+"].AddValue(-0.01f, true);
					}

					previewPredictionOfReward = reward;
				}
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
		if (enableHopeAndSurprise)
		{
			emotions["hopeR"].Reset();
			emotions["surpriseD+"].Reset();
			emotions["surpriseD-"].Reset();
		}
	}
}
