using ai4u;
using Godot;
using System;
using System.Collections.Generic;

namespace CognusProject;

public partial class SecondaryEmotionModule : Node
{

	private PipelineSensor pipeline;

	private BasicAgent agent;

	public void SetPipeline(PipelineSensor sensor)
	{
		this.pipeline = sensor;
	}

	public void SetAgent(BasicAgent agent)
	{
		this.agent = agent;
	}

	public void Update(Dictionary<string, Emotion> emotions)
	{
		if (agent.ContainsField("qvalue"))
		{
			float value = agent.GetFieldArgAsFloat("qvalue");
			GD.Print(value);
		}
	}

	public void Reset()
	{

	}
}
