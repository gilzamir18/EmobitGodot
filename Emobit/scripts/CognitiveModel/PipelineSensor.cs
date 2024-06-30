using Godot;
using ai4u;
using System;
using ai4u.ext;
using System.Collections.Generic;
using System.Text;

namespace CognusProject;

public partial class PipelineSensor: Sensor
{

    [Export]
    private HomeostaticHUD homeostaticHUD;

    [Export]
    private HomeostaticHUD emotionHUD;

    [Export]
    private SecondaryEmotionModule secondaryEmotionModule;

    [Export]
    private int predefinedBehavior = -1;

    [Export]
    private Node3D radiation;

    
    public bool RadiationEnabled {get; set;} = true;

    public bool RadiationOn {get; set;} = false;

    [Export]
    public int MinDistToStartRadiation {get; set;} = 3;
    [Export]
    public int MaxDistToStartRadiation {get; set;} = 5;

    private int distToStartRadiation;


    public static int ILLNESS = 0, PAIN = 1, FRUIT_SMELL = 2, FRUIT_BRIGHT = 3, SATISFACTION = 4, FRUSTRATION = 5, TIREDNESS = 6;

    private OrientationSensor fruitSensor;
    private OrientationSensor radiationSensor;

    private RayCastingSensor rayCastingSensor;

    private GroupTouchSensor collisionWithWallSensor;

    private ActionSensor actionSensor;

    private HistoryStack<float> history;

    private HomeostaticVariable[] variables;

    private Vector3 previousPosition;

    private RigidBody3D agentBody;

    private Dictionary<string, Emotion> emotions;
    private List<HomeostaticVariable> emotionalValues;

    internal HomeostaticVariable illness, pain, fruitBright, fruitSmell, satisfaction, frustration, tiredness;
    

    private float[] hC;
    private float[] w;

    private float lastReward = 0;

    private float sumOfRewards = 0;

    private float[] lastActions;

    private float[] visionData;

    public override void OnSetup(Agent agent)
    {   
        this.agent = (RLAgent) agent;

        this.agent.OnStepEnd += UpdateReward;

        agentBody = (RigidBody3D) this.agent.GetAvatarBody();

        fruitSensor = GetNode<OrientationSensor>("FruitSensor");
        radiationSensor = GetNode<OrientationSensor>("RadiationSensor");
        rayCastingSensor = GetNode<RayCastingSensor>("RaycastingSensor");
        actionSensor = GetNode<ActionSensor>("ActionSensor");
        collisionWithWallSensor = GetNode<GroupTouchSensor>("CollisionWithWallSensor");
    
        fruitSensor.OnSetup(agent);
        radiationSensor.OnSetup(agent);
        rayCastingSensor.OnSetup(agent);
        actionSensor.OnSetup(agent);
        collisionWithWallSensor.OnSetup(agent);
        
        lastActions =  new float[actionSensor.shape[0]];
        visionData = new float[rayCastingSensor.shape[0]];


        SetupHomestaticVariables();
        ResetHomestaticVariables();
        variables = new HomeostaticVariable[]{illness, pain, fruitSmell, fruitBright, satisfaction, frustration, tiredness};

        type = SensorType.sfloatarray;
        shape = new int[]{ stackedObservations * (variables.Length * 5 + rayCastingSensor.shape[0] +  actionSensor.shape[0] + 1) };
        rangeMin = -1;
        rangeMax = 1;

        if (homeostaticHUD != null)
        {
            homeostaticHUD.SetupVariables(variables);
        }

        hC = new float[variables.Length];
        w = new float[variables.Length];

        SetupPrimaryEmotions();
        
        if (secondaryEmotionModule != null)
        {
            secondaryEmotionModule.SetPipeline(this);
            secondaryEmotionModule.SetAgent(this.agent);
            secondaryEmotionModule.SetEmotions(emotions, emotionalValues);
        }

        if (emotionHUD != null)
        {
            emotionHUD.SetupVariables(emotionalValues.ToArray());
        }
        history = new HistoryStack<float>(shape[0]);
    }


    public bool BodyProtected
    {
        get
        {
            return illness.maxValue >= 0.99f;
        }
    }

    public void UpdateReward(RLAgent agent)
    {
        //GD.Print("Sum of rewards " + sumOfRewards);
        agent.AddReward(sumOfRewards);
        sumOfRewards = 0;
    }


    public override void OnReset(Agent agent)
    {
        if (secondaryEmotionModule != null)
        {
            secondaryEmotionModule.Reset();
        }

        radiation.Visible = false;

        if (GD.RandRange(0, 9)<=7)
        {
            RadiationEnabled = true;
        }
        else
        {
            RadiationEnabled = false;
        }

        RadiationOn = false;

        distToStartRadiation = GD.RandRange(MinDistToStartRadiation, MaxDistToStartRadiation);
        //GD.Print(RadiationEnabled + " ::: " + distToStartRadiation);
        


        fruitSensor.OnReset(agent);
        radiationSensor.OnReset(agent);
        rayCastingSensor.OnReset(agent);
        actionSensor.OnReset(agent);
        collisionWithWallSensor.OnReset(agent);

        lastActions =  new float[actionSensor.shape[0]];
        visionData = new float[rayCastingSensor.shape[0]];
        sumOfRewards = 0;
        lastReward = 0;
        ResetHomestaticVariables();
        foreach(var em in emotions)
        {
            em.Value.Reset();
            if (em.Key == "fearRad")
            {
                emotionalValues[0].Value = 0;
            }
            else if (em.Key == "fearPain")
            {
                emotionalValues[1].Value = 0;
            }
            else if (em.Key == "joyApprx")
            {
                emotionalValues[2].Value = 0;
            } else if (em.Key == "distrGoal")
            {
                emotionalValues[3].Value = 0;
            }
        }

        if (homeostaticHUD != null)
        {
            homeostaticHUD.UpdateSliders();
        }

        if (emotionHUD != null)
        {
            emotionHUD.UpdateSliders();
        }


        hC = new float[variables.Length];
        w = new float[variables.Length];
        history = new HistoryStack<float>(shape[0]);
        previousPosition = agentBody.Position;

        PerceptionModule.ResetPerceptionData();
        PrimaryMotivationModule.ResetPrimaryMotivationData(variables);
        PrimaryEmotionModule.ResetPrimaryEmotionData(variables);
        RewardModule.ResetRewardData(variables);
    }

    public override SensorType GetDataType()
    {
        return SensorType.sfloatarray;
    }

    public override float[] GetFloatArrayValue()
    {
        Percept();
        for (int i = 0; i < variables.Length; i++)
        {
            history.Push(variables[i].Value);
            history.Push(variables[i].minValue);
            history.Push(variables[i].maxValue);
            history.Push(hC[i]);
            history.Push(w[i]);
        }
        history.Push(this.lastReward);
        for (int i = 0; i < lastActions.Length; i++)
        {
            history.Push(lastActions[i]);
        }
        //StringBuilder sb = new StringBuilder();
        //int c = 0;
        for (int i = 0; i < visionData.Length; i++)
        {
            history.Push(visionData[i] / 255.0f);
            //sb.Append(visionData[i].ToString() + "  ");
            //c++;
            /*
            if (c > 60)
            {
                c = 0;
                sb.Append('\n');
            }*/
        }
        //GD.Print(sb.ToString());
        return history.Values;
    }

    private void Percept()
    {
        float[] fruitData = fruitSensor.GetFloatArrayValue();
        var fruitDirection = fruitData[0];
        var fruitDist = fruitData[1];
        
        float radiationDist = radiationSensor.GetFloatArrayValue()[0];

        if (RadiationEnabled)
        {
            if (!RadiationOn && radiationDist >= 0 && radiationDist < distToStartRadiation)
            {
                RadiationOn = true;
                radiation.Visible = true;
            }
            else if (!RadiationOn)
            {
                radiationDist = -1;
            }
        }
        else
        {
            radiationDist = -1;
        }

        
        visionData = rayCastingSensor.GetFloatArrayValue();
        
        lastActions = actionSensor.GetFloatArrayValue();


        Vector3 newpos = agentBody.Position;
        float agentDist = newpos.DistanceTo(previousPosition);
        previousPosition = newpos;


        float collisionWall = collisionWithWallSensor.GetFloatArrayValue()[0];


        PerceptionModule.Percept(variables, fruitDirection, fruitDist, radiationDist, agentDist, visionData, lastActions, collisionWall, (float)agent.GetPhysicsProcessDeltaTime());
        PrimaryMotivationModule.Attention(variables, hC, BodyProtected);
        
        PrimaryEmotionModule.Evaluate(emotions, variables);

        if (this.secondaryEmotionModule != null)
        {
            this.secondaryEmotionModule.Update(emotions);
        }

        this.lastReward = RewardModule.Rewarding(emotions, variables, w, hC)/500;
        this.sumOfRewards += this.lastReward;

        emotionalValues[0].Value = emotions["fearRad"].Intensity;
        emotionalValues[1].Value = emotions["fearPain"].Intensity;
        emotionalValues[2].Value = emotions["joyApprx"].Intensity;
        emotionalValues[3].Value = emotions["distrGoal"].Intensity;

        //GD.Print(emotions["fearPain"].Accumulator);

        if (emotionHUD != null)
        {
            emotionHUD.UpdateSliders();
        }

        if (homeostaticHUD != null)
        {
            homeostaticHUD.UpdateSliders();
        }
    }


    private void SetupPrimaryEmotions()
    {
        emotions = new();
        emotions["fearRad"] = new Emotion("fearRad", new int[]{ILLNESS}, false);
        emotions["fearPain"] = new Emotion("fearPain", new int[]{PAIN}, false);
        emotions["joyApprx"] = new Emotion("joyApprx", new int[]{SATISFACTION}, true);
        emotions["distrGoal"] = new Emotion("distrGoal", new int[]{FRUSTRATION}, false);

        HomeostaticVariable fearRad = new HomeostaticVariable();
        fearRad.name = "fearRad";
        fearRad.minValue = 0;
        fearRad.maxValue = 1;
        fearRad.rangeMin = 0;
        fearRad.rangeMax = 1;
        fearRad.Value = emotions["fearRad"].Intensity;

        HomeostaticVariable fearPain = new HomeostaticVariable();
        fearPain.name = "fearPain";
        fearPain.minValue = 0;
        fearPain.maxValue = 1;
        fearPain.rangeMin = 0;
        fearPain.rangeMax = 1;
        fearPain.Value = emotions["fearPain"].Intensity;


        HomeostaticVariable joyApprx = new HomeostaticVariable();
        joyApprx.name = "joyApprx";
        joyApprx.minValue = 0;
        joyApprx.maxValue = 1;
        joyApprx.rangeMin = 0;
        joyApprx.rangeMax = 1;
        joyApprx.Value = emotions["joyApprx"].Intensity;


        HomeostaticVariable distrGoal = new HomeostaticVariable();
        distrGoal.name = "distrGoal";
        distrGoal.minValue = 0;
        distrGoal.maxValue = 1;
        distrGoal.rangeMin = 0;
        distrGoal.rangeMax = 1;
        distrGoal.Value = emotions["distrGoal"].Intensity;

        //this.emotionalValues = new HomeostaticVariable[]{fearRad, fearPain, joyApprx};

        this.emotionalValues = new();
        this.emotionalValues.Add(fearRad);
        this.emotionalValues.Add(fearPain);
        this.emotionalValues.Add(joyApprx);
        this.emotionalValues.Add(distrGoal);
    }

    private void SetupHomestaticVariables()
    {
        pain = new HomeostaticVariable();
        pain.name = "Pain";

        illness = new HomeostaticVariable();
        illness.name = "Illness";

        fruitSmell = new HomeostaticVariable();
        fruitSmell.name = "FruitSmell";

        fruitBright = new HomeostaticVariable();
        fruitBright.name = "FruitBright";

        satisfaction = new HomeostaticVariable();
        satisfaction.name = "Satisfaction";

        frustration = new HomeostaticVariable();
        frustration.name = "Frustration";

        tiredness = new HomeostaticVariable();
        tiredness.name = "Tiredness";
    }

    private void ResetHomestaticVariables()
    {
        // ILLNESS = 0, PAIN = 1, FRUIT_SMELL = 2, FRUIT_BRIGHT = 3, SATISFACTION = 4, FRUSTRATION = 5, TIREDNESS = 6;
        /*int[,] options = new int[,]{ {0, 0, 1, 1, 1, 1, 1}, //rota mais distante 
                                     {0, 0, 1, 1, 1, 0, 1}, //rota mais longa sem muita convicção
                                     {3, 0, 1, 1, 1, 1, 1}, //rota mais curta sem muita convicação
                                     {3, 0, 1, 1, 1, 0, 1}, //rota mais curta com muita convicção
                                     {3, 0, 1, 1, 1, 0, 1}, //rota mais curta com muita convicção
                                    };*/
            int[,] options = new int[,]{ {0, 0, 1, 1, 1, 1, 1}, //rota mais distante 
                                     {3, 0, 1, 1, 1, 1, 1}, //rota mais curta
                                     {3, 0, 1, 1, 1, 0, 1}, //rota mais curta
                                   };

            int idx = 0;
            if (predefinedBehavior >= 0)
            {
                idx = predefinedBehavior;
            }
            else
            {
                 idx = GD.RandRange(0, 2);
            }


            pain.minValue = 0;
            pain.maxValue = new float[]{0.05f, 0.5f, 0.9f, 1.0f}[options[idx, PAIN] ]; 
            pain.rangeMin = 0;
            pain.rangeMax = 1;
            pain.Value = 0;
            pain.SetCentroid(0.0f);

            illness.minValue = 0.0f;
            illness.maxValue = new float[]{0.01f, 0.5f, 0.9f, 1.0f}[options[idx, ILLNESS]];
            illness.rangeMin = 0;
            illness.rangeMax = 1;
            illness.Value = 0;
            illness.SetCentroid(0.0f);

            fruitSmell.minValue = new float[]{0.0f, 0.1f, 0.3f, 0.9f}[options[idx, FRUIT_SMELL]];
            fruitSmell.maxValue = 1.0f;
            fruitSmell.rangeMax = 1.0f;
            fruitSmell.rangeMin = 0.0f;
            fruitSmell.SetCentroid(1.0f);
            fruitSmell.Value = 0;

            fruitBright.minValue = new float[]{0.0f, 0.1f, 0.3f, 0.9f}[options[idx, FRUIT_BRIGHT]];
            fruitBright.maxValue = 1.0f;
            fruitBright.rangeMax = 1.0f;
            fruitBright.rangeMin = 0.0f;
            fruitBright.SetCentroid(1.0f);
            fruitBright.Value = 0;

            satisfaction.minValue = new float[]{0.0f, 0.1f, 0.5f, 0.9f}[options[idx, SATISFACTION]];
            satisfaction.maxValue = 1.0f;
            satisfaction.rangeMin = 0;
            satisfaction.rangeMax = 1;
            satisfaction.SetCentroid(1.0f);
            satisfaction.Value = 0.0f;

            frustration.minValue = 0.0f;
            frustration.maxValue = new float[]{0.1f, 0.5f, 0.9f, 1.0f}[options[idx, FRUSTRATION]];
            frustration.rangeMin = 0;
            frustration.rangeMax = 1;
            frustration.SetCentroid(0);
            frustration.Value = 0.0f;
        
            tiredness.minValue = 0.0f;
            tiredness.maxValue = new float[]{0.1f, 0.5f, 0.9f, 1.0f}[options[idx, TIREDNESS]];
            tiredness.rangeMin = 0;
            tiredness.rangeMax = 1;
            tiredness.SetCentroid(0);
            tiredness.Value = 0.0f;

            if (illness.maxValue >= 0.99f)
            {
                (agentBody.GetNode<MeshInstance3D>("MeshInstance3D").GetSurfaceOverrideMaterial(0) as StandardMaterial3D).AlbedoColor = new Color(1f, 0.3f, 0.3f, 1.0f);
            }
            else
            {
                (agentBody.GetNode<MeshInstance3D>("MeshInstance3D").GetSurfaceOverrideMaterial(0) as StandardMaterial3D).AlbedoColor = new Color(1f, 1.0f, 0.0f, 1.0f);
            }
    }
}




