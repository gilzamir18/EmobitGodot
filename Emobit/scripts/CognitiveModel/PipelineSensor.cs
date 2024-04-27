using Godot;
using ai4u;
using System;
using ai4u.ext;
using System.Collections.Generic;

namespace CognusProject;

public enum BodyProtection {None, Radiation, Random};

public partial class PipelineSensor: Sensor
{

    [Export]
    private HomeostaticHUD homeostaticHUD;

    [Export]
    private HomeostaticHUD emotionHUD;

    [Export]
    private BodyProtection bodyProtection = BodyProtection.None;

    private BodyProtection _bodyProtection;


    public bool HasRadiationProtection
    {
        get
        {
            return _bodyProtection == BodyProtection.Radiation;
        }
    }

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
    private HomeostaticVariable[] emotionalValues;

    private HomeostaticVariable illness, pain, fruitBright, fruitSmell, satisfaction, frustration, tiredness;
    

    private float[] hC;
    private float[] w;

    private float lastReward = 0;

    private float sumOfRewards = 0;

    private float[] lastActions;

    private float[] visionData;

    public override void OnSetup(Agent agent)
    {   

        this.agent = (BasicAgent) agent;

        this.agent.endOfStepEvent += UpdateReward;

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
        if (emotionHUD != null)
        {
            emotionHUD.SetupVariables(emotionalValues);
        }
        history = new HistoryStack<float>(shape[0]);
    }


    public void UpdateReward(BasicAgent agent)
    {
        //GD.Print("Sum of rewards " + sumOfRewards);
        agent.AddReward(sumOfRewards);
        sumOfRewards = 0;
    }


    public override void OnReset(Agent agent)
    {

        if (bodyProtection == BodyProtection.Random)
        {
            if (GD.RandRange(0, 1) == 0)
            {
                _bodyProtection = BodyProtection.None;
            }
            else
            {
                _bodyProtection = BodyProtection.Radiation;
            }
        }
        else
        {
            _bodyProtection = bodyProtection;
        }


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
        history.Push(this._bodyProtection == BodyProtection.Radiation? 1 : 0);
        history.Push(this.lastReward);
        for (int i = 0; i < lastActions.Length; i++)
        {
            history.Push(lastActions[i]);
        }
        for (int i = 0; i < visionData.Length; i++)
        {
            history.Push(visionData[i]);
        }
        return history.Values;
    }

    private void Percept()
    {
        float[] fruitData = fruitSensor.GetFloatArrayValue();
        var fruitDirection = fruitData[0];
        var fruitDist = fruitData[1];
        
        float radiationDist = radiationSensor.GetFloatArrayValue()[0];
        
        visionData = rayCastingSensor.GetFloatArrayValue();
        
        lastActions = actionSensor.GetFloatArrayValue();


        Vector3 newpos = agentBody.Position;
        float agentDist = newpos.DistanceTo(previousPosition);
        previousPosition = newpos;


        float collisionWall = collisionWithWallSensor.GetFloatArrayValue()[0];


        PerceptionModule.Percept(variables, fruitDirection, fruitDist, radiationDist, agentDist, visionData, lastActions, collisionWall);
        PrimaryMotivationModule.Attention(variables, hC);
        PrimaryEmotionModule.Evaluate(emotions, variables);
        this.lastReward = RewardModule.Rewarding(emotions, variables, w, hC)/700.0f;
        this.sumOfRewards += this.lastReward;

        emotionalValues[0].Value = emotions["fearRad"].Intensity;
        emotionalValues[1].Value = emotions["fearPain"].Intensity;
        emotionalValues[2].Value = emotions["joyApprx"].Intensity;

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

        this.emotionalValues = new HomeostaticVariable[]{fearRad, fearPain, joyApprx};
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
            //ILLNESS = 0, PAIN = 1, SMELLING = 2, SHINE=3, SATISFACTION=4, DISTRESS=5
            int[,] options = new int[,]{ {0, 0, 1, 1, 1, 3}, //rota mais distante 
                                         {0, 0, 1, 1, 1, 0}, //rota mais longa sem muita convicção
                                         {3, 0, 1, 1, 1, 3}, //rota mais curta sem muita convicação
                                         {3, 0, 1, 1, 1, 0}, //rota mais curta com muita convicção
                                         {2, 0, 1, 1, 1, 0}, //rota mais curta com muita convicção
                                        };

            int idx = 0;
            if (GD.RandRange(0, 1) == 1)
            {
                idx = GD.RandRange(0, 1);
            }
            else
            {
                idx = GD.RandRange(2, 4);
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
            tiredness.maxValue = 0.5f;//new float[]{0.1f, 0.5f, 0.9f, 1.0f}[Random.Range(0, 4)];
            tiredness.rangeMin = 0;
            tiredness.rangeMax = 1;
            tiredness.SetCentroid(0);
            tiredness.Value = 0.0f;
    }
}




