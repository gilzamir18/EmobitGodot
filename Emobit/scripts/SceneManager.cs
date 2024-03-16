using ai4u;
using Godot;
using System;
using System.Collections.Generic;

struct Layout
{
	public int AgentPosition {get; set;}
	public int RadiationPosition {get; set;}
	public int FruitPosition {get; set;}

	public int Block1 {get; set;}

	public int Block2 {get; set;}

	public int Block3 {get; set;}

	public int Block4 {get; set;}

	public Layout(int agentPosition, int radiationPosition, int fruitPosition, int block1, int block2, int block3, int block4)
	{
		this.AgentPosition = agentPosition;
		this.RadiationPosition = radiationPosition;
		this.FruitPosition = fruitPosition;
		this.Block1 = block1;
		this.Block2 = block2;
		this.Block3 = block3;
		this.Block4 = block4;
	}
}

public partial class SceneManager : Node
{

	[Export]
	private Node3D radiation;
	[Export]
	private Node radiationPositions;

	[Export]
	private RigidBody3D agentBody;

	[Export]
	private Node agentPositions;

	[Export]
	private StaticBody3D fruit;
	[Export]
	private Node fruitPositions;


	private StaticBody3D block1, block2, block3, block4;
	private CollisionShape3D shape1, shape2, shape3, shape4;
	
	[Export]
	private BasicAgent agent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		block1 = GetNode<StaticBody3D>("Block1");
		block2 = GetNode<StaticBody3D>("Block2");
		block3 = GetNode<StaticBody3D>("Block3");
		block4 = GetNode<StaticBody3D>("Block4");

		shape1 = block1.GetNode<CollisionShape3D>("CollisionShape3D");
		shape2 = block2.GetNode<CollisionShape3D>("CollisionShape3D");
		shape3 = block3.GetNode<CollisionShape3D>("CollisionShape3D");
		shape4 = block4.GetNode<CollisionShape3D>("CollisionShape3D");

		agent.beforeTheResetEvent += OnReset;
	}


	public void OnReset(BasicAgent agent)
	{
		Layout l1 = new Layout(0, 0, 0, 0, 1, 0, 1);
		Layout l2 = new Layout(1, 1, 1, 0, 1, 0, 1);
		Layout l3 = new Layout(2, 2, 2, 0, 1, 0, 1);
		Layout l4 = new Layout(3, 3, 3, 0, 1, 0, 1);
		Layout l5 = new Layout(4, 4, 4, 0, 1, 0, 1);
		Layout l6 = new Layout(5, 5, 5, 0, 1, 0, 1);
		Layout l7 = new Layout(6, 6, 6, 0, 1, 0, 1);
		Layout l8 = new Layout(7, 7, 7, 0, 1, 0, 1);
		Layout l9 = new Layout(8, 8, 8, 0, 1, 0, 1);
		Layout l10 = new Layout(9, 9, 9, 0, 1, 0, 1);
		Layout l11 = new Layout(10, 10, 10, 0, 1, 0, 1);
		Layout l12 = new Layout(11, 11, 11, 0, 1, 0, 1);
		Layout l13 = new Layout(12, 12, 12, 0, 1, 1, 0);
		Layout l14 = new Layout(13, 13, 13, 0, 1, 1, 0);
		Layout l15 = new Layout(14, 14, 14, 0, 1, 1, 0);
		Layout l16 = new Layout(15, 15, 15, 0, 1, 1, 0);
		Layout l17 = new Layout(16, 16, 16, 0, 1, 1, 0);
		Layout l18 = new Layout(17, 17, 17, 0, 1, 1, 0);
		Layout l19 = new Layout(18, 18, 18, 0, 1, 1, 0);
		Layout l20 = new Layout(19, 19, 19, 0, 1, 1, 0);
		Layout l21 = new Layout(20, 20, 20, 0, 1, 1, 0);
		Layout l22 = new Layout(21, 21, 21, 0, 1, 1, 0);
		Layout l23 = new Layout(22, 22, 22, 0, 1, 1, 0);
		Layout l24 = new Layout(23, 23, 23, 0, 1, 1, 0);
		Layout l25 = new Layout(24, 24, 24, 0, 1, 1, 0);

		List<Layout> layouts = new(){l1, l2, l3, l4, l5, l6, 
									 l7, l8, l9, l10, l11, l12,
									 l13, l14, l15, l16, l17, l18,
									 l19, l20, l21, l22, l23, l24,
									 l25};
		
		var sl = GD.RandRange(0, layouts.Count - 1);
		Layout l = layouts[sl];
		ResetBlocks(l.Block1, l.Block2, l.Block3, l.Block4);
		ResetAgentPosition(agentBody, l.AgentPosition);
		ResetFruitPosition(fruit, l.FruitPosition);
		ResetRadiationPosition(l.RadiationPosition);
	}


	private void ResetRadiationPosition(int pos)
	{
		Vector3 reference;
		var children = radiationPositions.GetChildren();
		if (children.Count > 0)
		{
			reference = ((Node3D)children[pos]).Position;
		}
		else
		{
			reference = ((Node3D)radiationPositions).GlobalPosition;
		}
		radiation.Position = reference;
	}

	private void ResetBlocks(int b1, int b2, int b3, int b4)
	{
		if (b1 != 0)
		{
			block1.Visible  = true;
			shape1.Disabled = false;
		}
		else
		{
			block1.Visible  = false;
			shape1.Disabled = true;
		}

		if (b2 != 0)
		{
			block2.Visible  = true;
			shape2.Disabled = false;
		}
		else
		{
			block2.Visible  = false;
			shape2.Disabled = true;
		}


		if (b3 != 0)
		{
			block3.Visible  = true;
			shape3.Disabled = false;
		}
		else
		{
			block3.Visible  = false;
			shape3.Disabled = true;
		}


		if (b4 != 0)
		{
			block4.Visible  = true;
			shape4.Disabled = false;
		}
		else
		{
			block4.Visible  = false;
			shape4.Disabled = true;
		}
	}


	private void ResetFruitPosition(StaticBody3D body3D, int pos)
	{
		Vector3 reference;
		var children = fruitPositions.GetChildren();
		if (children.Count > 0)
		{
			reference = ((Node3D)children[pos]).Position;
		}
		else
		{
			reference = ((Node3D)fruitPositions).GlobalPosition;
		}
		body3D.Position = reference;
		body3D.ConstantAngularVelocity = new Vector3(0, 0, 0);
		body3D.ConstantLinearVelocity = new Vector3(0, 0, 0);
	}

	private void ResetAgentPosition(RigidBody3D rBody, int pos)
	{
		Godot.Collections.Array<Node> children;
		children = agentPositions.GetChildren();
		Transform3D reference;
		if (children.Count > 0)
		{
			reference = ((Node3D)children[pos]).GlobalTransform;
		}
		else
		{
			reference = ((Node3D) agentPositions).GlobalTransform;
		}

		Vector3 axis = new Vector3(0, 1, 0); // Or Vector3.Right
		int idx = GD.RandRange(0, 2);
		float rotationAmount = new float[]{0.0f,  Mathf.Pi/9.0f, Mathf.Pi}[idx];

		// Rotate the transform around the X axis by 0.1 radians.
		reference.Basis = new Basis(axis, rotationAmount) * reference.Basis;

		/*var mode = rBody.Mode;
		rBody.Mode = RigidBody3D.ModeEnum.Kinematic;
		rBody.Position = children[idx].position;
		rBody.Mode = mode;*/
		
		PhysicsServer3D.BodySetState(
			rBody.GetRid(),
			PhysicsServer3D.BodyState.Transform,
			reference
		);
		
		PhysicsServer3D.BodySetState(
			rBody.GetRid(),
			PhysicsServer3D.BodyState.AngularVelocity,
			new Vector3(0, 0, 0)
		);	
		
		PhysicsServer3D.BodySetState(
			rBody.GetRid(),
			PhysicsServer3D.BodyState.LinearVelocity,
			new Vector3(0, 0, 0)
		);	
	}
}
