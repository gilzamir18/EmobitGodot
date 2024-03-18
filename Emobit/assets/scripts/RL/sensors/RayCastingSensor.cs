using Godot;
using System;
using System.Collections.Generic;
using System.Collections;
using ai4u;
using System.Text;

namespace ai4u
{	
	public struct Ray
	{
		private Vector3 origin;
		private Vector3 direction;
		private Vector3 endPoint;
		
		public Ray(Vector3 o, Vector3 d)
		{
			this.origin = o;
			this.direction = d.Normalized();
			this.endPoint = origin + direction;
		}
		
		public Vector3 Origin
		{
			get
			{
				return origin;
			}
		}
		
		public Vector3 Direction
		{
			get
			{
				return direction;
			}
		}
		
		public Vector3 EndPoint
		{
			get
			{
				return endPoint;
			}
		}
		
		public float GetDist(Vector3 q)
		{
			return (q - origin).Length();
		}
	}

	public partial class RayCastingSensor : Sensor
	{
		
		[Export]
		public int[] groupCode;
		[Export]
		public string[] groupName;
		
		[Export]
		public int noObjectCode;
	
		[Export]
		public int hSize = 10;
		
		[Export]
		public int vSize = 10;
		
		[Export]
		public float horizontalShift = 0;
		[Export]
		public float verticalShift = 0;
	
		[Export]
		public NodePath eyePath;
		public Node3D eye;

		[Export]
		public float visionMaxDistance = 500f;
		
		[Export]
		public float fieldOfView = 90.0f;

		[Export]
		public bool debugEnabled = false;

		[Export]
		public bool flattened = false;


		[Export] 
		private Color debugColor = new Color(1, 0, 0, 1);
		
		[Export]
		private Color debugNoTagColor = new Color(1, 1, 1, 1);

		[Export]
		private Color debugBackgroundColor = new Color(0, 1, 0, 1);

	 	[Export]
		private float debugThickness = 1.0f;

		[Export]
		private float debugOriginSize = 10;

		[Export]
		private bool excludeSelf = true;

		private Dictionary<string, int> mapping;
		private Ray[,] raysMatrix = null;
		private HistoryStack<float> history;
		private PhysicsDirectSpaceState3D spaceState;
		

		private LineDrawer lineDrawer;

		public override void OnSetup(Agent agent) 
		{

			if (debugEnabled)
			{
				lineDrawer = GetNode<LineDrawer>("/root/LineDrawer");
			}

			type = SensorType.sfloatarray;
			if (!flattened)
			{
				shape = new int[2]{hSize,  vSize};
				history = new HistoryStack<float>(stackedObservations * shape[0] * shape[1]);
			}
			else
			{
				shape = new int[1]{hSize * vSize};
				history = new HistoryStack<float>(stackedObservations * shape[0]);
			}
			
			agent.AddResetListener(this);
			
			mapping = new Dictionary<string, int>();

			this.agent = (BasicAgent) agent;
			this.spaceState = (this.agent.GetAvatarBody() as PhysicsBody3D).GetWorld3D().DirectSpaceState;

			if (this.eyePath != null && this.eyePath != "") {
				this.eye = GetNode(this.eyePath) as Node3D;
			} else {
				this.eye = this.agent.GetAvatarBody() as Node3D;
			}
			raysMatrix = new Ray[hSize, vSize];
		}

		public override float[] GetFloatArrayValue()
		{
			var aim = eye.GlobalTransform.Basis;			
			Vector3 forward = aim.Z.Normalized();
			Vector3 up = aim.Y.Normalized();
			Vector3 right = aim.X.Normalized();
			UpdateRaysMatrix(eye.GlobalTransform.Origin, forward, up, right, fieldOfView);
			if (debugEnabled)
			{
				lineDrawer.Redraw();
			}
			return history.Values;
		}

		private void UpdateRaysMatrix(Vector3 position, Vector3 forward, Vector3 up, Vector3 right, float fieldOfView = 45.0f)
		{
			float vangle = 2 * fieldOfView / hSize;
			float hangle = 2 * fieldOfView / vSize;

			
			float iangle = -fieldOfView;
			
			var debugline = 0;
			for (int i = 0; i < hSize; i++)
			{
				var k1 = 1;
				if (hSize <= 1)
				{
					k1 = 0;
				}
				var fwd = forward.Rotated(up, Mathf.DegToRad( (iangle * k1 + horizontalShift)  + vangle * i) ).Normalized();
				for (int j = 0; j < vSize; j++)
				{
					var k2 = 1;
					if (vSize <= 1)
					{
						k2 = 0;
					}
					var direction = fwd.Rotated(right, Mathf.DegToRad( (iangle * k2 + verticalShift) + hangle * j)).Normalized();
					raysMatrix[i, j] =  new Ray(position, direction);
					UpdateView(i, j, debugline);
					debugline ++;
				}
			}
		}
		
		public void UpdateView(int i, int j, int debug_line = 0)
		{
			var myray = raysMatrix[i,j];
			
			//var query = PhysicsRayQueryParameters3D.Create(myray.Origin, myray.Origin + myray.Direction*visionMaxDistance, 2147483647);
			

			var query = PhysicsRayQueryParameters3D.Create(myray.Origin, myray.Origin + myray.Direction*visionMaxDistance, 2147483647);
			
			if (excludeSelf)
			{
				var rb = agent.GetAvatarBody() as PhysicsBody3D;
				query.Exclude = new Godot.Collections.Array<Rid> { rb.GetRid() };
			}

			var result = this.spaceState.IntersectRay( query);//new Godot.Collections.Array { agent.GetBody() }
			
			bool isTagged = false;
			float t = -1;
			if (result.Count > 0)
			{
				t = myray.GetDist((Vector3)result["position"]);					
				
				Node3D gobj = (Node3D) result["collider"];

				var groups = gobj.GetGroups();

				if (t <= visionMaxDistance)
				{
					foreach(string g in groups)
					{
						if (mapping.ContainsKey(g))
						{
							int code = mapping[g];
							history.Push(code);
							isTagged = true;
							break;
						}
					}
				}
				if (!isTagged)
				{
					history.Push(noObjectCode);
				}				
			}
			else
			{
				history.Push(noObjectCode);
			}
			if (debugEnabled)
			{
				if (isTagged) {
					lineDrawer.Draw_Line3D(debug_line, myray.Origin, myray.Origin + myray.Direction * visionMaxDistance, debugColor, debugBackgroundColor, debugThickness, debugOriginSize);
				} else 
				{
					lineDrawer.Draw_Line3D(debug_line, myray.Origin, myray.Origin + myray.Direction * visionMaxDistance, debugNoTagColor, debugBackgroundColor, debugThickness, debugOriginSize);					
				}
			}			
		}
		
		public override void OnReset(Agent agent) {
			if (!flattened)
			{
				history = new HistoryStack<float>(stackedObservations * shape[0] * shape[1]);
			}
			else
			{
				history = new HistoryStack<float>(stackedObservations * shape[0]);
			}
			mapping = new Dictionary<string, int>();
			
			for (int o = 0; o < groupName.Length; o++ )
			{
				var code = groupCode[o];
				var name = groupName[o];
				mapping[name] = code;
			}
			raysMatrix = new Ray[hSize, vSize];
		} 
	}
}
