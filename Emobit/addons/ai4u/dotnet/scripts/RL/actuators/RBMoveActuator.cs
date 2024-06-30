using System.Collections;
using System.Collections.Generic;
using Godot;


namespace ai4u {
	public partial class RBMoveActuator : MoveActuator
	{
		//forces applied on the x, y and z axes.    
		private float move, turn, jump, jumpForward;
		[Export]
		private float moveAmount = 1;
		[Export]
		private float turnAmount = 1;
		[Export]
		private float jumpPower = 1;
		[Export]
		private float jumpForwardPower = 1;
		[Export]
		private float collisionShapeHalfHeight = 1.0f;
		
		[Export]
		private float precision = 0.001f;

		[Export]
		public string floorGroup = "Floor";

		private Agent agent;
		
		private RigidBody3D rBody;
		private PhysicsDirectSpaceState3D spaceState;

		private CollisionShape3D collisionShape;

		public RBMoveActuator()
		{

		}

		public override void OnSetup(Agent agent)
		{

			shape = new int[1]{4};
			isContinuous = true;
			rangeMin = new float[]{0, -1, 0, 0};
			rangeMax = new float[]{1, 1, 1, 1};
			this.agent = agent;
			agent.AddResetListener(this);
			rBody = this.agent.GetAvatarBody() as RigidBody3D;
			this.spaceState = rBody.GetWorld3D().DirectSpaceState;
			foreach (Node child in rBody.GetChildren())
			{
				if (child is CollisionShape3D)
				{
					collisionShape = (CollisionShape3D)child;
					break;
				}
			}
		}


		private bool onGround = false;

		public override bool OnGround
		{
			get
			{
				return onGround;
			}
		}

		private bool CheckOnGround()
		{	
			var query = PhysicsRayQueryParameters3D.Create(collisionShape.GlobalPosition, 
				collisionShape.GlobalPosition + Vector3.Down * collisionShapeHalfHeight, 2147483647);

			var result = this.spaceState.IntersectRay( query );

			if (result.Count > 0)
			{

				var n3d = (Node3D)result["collider"];
				//GD.Print("COLLIDE WITH " + n3d.Name);
				if (n3d.IsInGroup(floorGroup))
				{
					//GD.Print("ONGRONUD");
					return true;
				}
			}
			
			//GD.Print("NOGROUND");

			return false;
		}

		public override void Act()
		{
			if (agent != null && !agent.Done)
			{
				float[] action = agent.GetActionArgAsFloatArray();

				action[0] = ai4u.math.AI4UMath.Clip(Mathf.Remap(action[0], -1, 1, rangeMin[0], rangeMax[0]), rangeMin[0], rangeMax[0]);
				action[1] = ai4u.math.AI4UMath.Clip(Mathf.Remap(action[1], -1, 1, rangeMin[1], rangeMax[1]), rangeMin[1], rangeMax[1]);
				action[2] = ai4u.math.AI4UMath.Clip(Mathf.Remap(action[2], -1, 1, rangeMin[2], rangeMax[2]), rangeMin[2], rangeMax[2]);
				action[3] = ai4u.math.AI4UMath.Clip(Mathf.Remap(action[3], -1, 1, rangeMin[3], rangeMax[3]), rangeMin[3], rangeMax[3]);

				move = action[0];
				turn = action[1];
				jump = action[2];
				jumpForward = action[3];

				if (rBody != null)
				{
					onGround = CheckOnGround();
					
					if (onGround)
					{	
						if (Mathf.Abs(turn) < precision)
						{
							turn = 0;
						}
						
						if (Mathf.Abs(jump) < precision)
						{
							jump = 0;
						}
						
						if (Mathf.Abs(jumpForward) < precision)
						{
							jumpForward = 0;
						}
						
						var velocity = new Vector3(0, 0, 0);
						
						
						velocity.Z += move * moveAmount + jumpForward * jumpForwardPower;
						
						var r = Vector3.Up * turn * turnAmount;
						
						PhysicsServer3D.BodySetState(
							rBody.GetRid(),
							PhysicsServer3D.BodyState.AngularVelocity,
							r
						);
						
						velocity.Y += jump * jumpPower + jumpForward * jumpPower;
						
						velocity = velocity.Rotated(Vector3.Up, rBody.Rotation.Y);
						
						
						PhysicsServer3D.BodySetState(
							rBody.GetRid(),
							PhysicsServer3D.BodyState.LinearVelocity,
							velocity
						);
					}
				}
			}
			move = 0;
			turn = 0;
			jump = 0;
			jumpForward = 0;
		}

		public override void OnReset(Agent agent)
		{
			turn = 0;
			move = 0;
			jump = 0;
			jumpForward = 0;
			onGround = false;
		}
	}
}
