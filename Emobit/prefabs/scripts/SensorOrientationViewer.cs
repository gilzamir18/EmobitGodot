using Godot;
using System;
using ai4u;

public partial class SensorOrientationViewer : Control
{

	[Export]
	private OrientationSensor orientationSensor;

	[Export]
	private string title = "Orientation Sensor";

	private HSlider angleSlider;
	private HSlider distanceSlider;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		angleSlider = GetNode("Angle") as HSlider;
		distanceSlider = GetNode("Distance") as HSlider;
		(GetNode("LabelTitle") as Label).Text = title;
	
		if (orientationSensor.Normalized)
		{
			angleSlider.MinValue = -1;
			angleSlider.MaxValue = 1;

			distanceSlider.MinValue = 0;
			distanceSlider.MaxValue = 1;
		}
		else
		{
			angleSlider.MinValue = -1;
			angleSlider.MaxValue = 1;

			distanceSlider.MinValue = 0;
			distanceSlider.MaxValue = orientationSensor.MaxDistance;
		}
	
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (orientationSensor.LastFloatArrayValue != null)
		{
			if (orientationSensor.Info == InfoType.ANGLE)
			{
				angleSlider.Value = orientationSensor.LastFloatArrayValue[0];
			}
			else if (orientationSensor.Info == InfoType.DIST)
			{
				distanceSlider.Value = orientationSensor.LastFloatArrayValue[0];		
			}
			else
			{
				angleSlider.Value = orientationSensor.LastFloatArrayValue[0];
				distanceSlider.Value = orientationSensor.LastFloatArrayValue[1];
			}
		}
	}
}
