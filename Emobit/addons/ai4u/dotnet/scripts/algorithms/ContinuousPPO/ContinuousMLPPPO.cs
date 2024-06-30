using System;
using System.Collections.Generic;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
using static TorchSharp.torch;
using static TorchSharp.torch.nn.functional;
using System.Linq;
using Godot;

namespace ai4u;

public class ContinuousMLPPPOMemory
{
    public List<Tensor> states = new List<Tensor>();
    public List<Tensor> actions = new List<Tensor>();
    public List<float> rewards = new List<float>();
    public List<bool> dones = new List<bool>();

    public void Clear()
    {
        states.Clear();
        actions.Clear();
        rewards.Clear();
        dones.Clear();
    }
}

public class ContinuousActorCritic : Module
{

    private readonly Sequential actorMean;
    private readonly Sequential actorLogStd;
    private readonly Sequential critic;

    public Sequential Actor => actorMean;
    public Sequential Critic => critic;

    public ContinuousActorCritic(int inputSize, int[] hiddenSize, int actionSize, Module<Tensor, Tensor> activation = null) : base(nameof(ActorCritic))
    {
        if (activation == null)
        {
            activation = ReLU();
        }
        actorMean = Sequential();
        actorMean.append("input", Linear(inputSize, hiddenSize[0]));
        actorMean.append("relu_input", activation);

        actorLogStd = Sequential();
        actorLogStd.append("input", Linear(inputSize, hiddenSize[0]));
        actorLogStd.append("relu_input", activation);

        critic = Sequential();
        critic.append("input", Linear(inputSize, hiddenSize[0]));
        critic.append("relu_input", activation);

        if (hiddenSize.Length == 1)
        {
            actorMean.append("hidden", Linear(hiddenSize[0], hiddenSize[0]));
            actorMean.append(activation);
            actorMean.append("output", Linear(hiddenSize[0], actionSize));

            actorLogStd.append("hidden", Linear(hiddenSize[0], hiddenSize[0]));
            actorLogStd.append(activation);
            actorLogStd.append("output", Linear(hiddenSize[0], actionSize));
            actorLogStd.append("log_std", Tanh());

            critic.append("hidden", Linear(hiddenSize[0], hiddenSize[0]));
            critic.append(activation);
            critic.append("output", Linear(hiddenSize[0], 1));
        }
        else
        {
            for (int i = 0; i < hiddenSize.Length-1; i++)
            {
                actorMean.append("hidden" + i, Linear(hiddenSize[i], hiddenSize[i+1]));
                actorMean.append(activation);

                actorLogStd.append("hidden" + i, Linear(hiddenSize[i], hiddenSize[i+1]));
                actorLogStd.append(activation);

                critic.append("hidden" + i, Linear(hiddenSize[i], hiddenSize[i+1]));
                critic.append(activation);
            }
            actorMean.append("output", Linear(hiddenSize[hiddenSize.Length-1], actionSize));

            actorLogStd.append("output", Linear(hiddenSize[hiddenSize.Length-1], actionSize));
            actorLogStd.append("log_std", Tanh());
        
            critic.append("output", Linear(hiddenSize[hiddenSize.Length-1], 1));
        }
        RegisterComponents();
    }

    public (Tensor mean, Tensor logStd) Act(Tensor input)
    {
        var mean = actorMean.forward(input);
        var logStd = actorLogStd.forward(input);
        return (mean, logStd);
    }

    public Tensor Evaluate(Tensor input)
    {
        return critic.forward(input);
    }

    public void Save(string prefix="model", string path="")
    {
        GD.Print(System.IO.Path.Join(path, prefix + "_critic.dat"));
        actorMean.save(System.IO.Path.Join(path, prefix + "_actor.dat"));
        critic.save(System.IO.Path.Join(path, prefix + "_critic.dat"));
    }

    public void Load(string prefix="model", string path="")
    {
        actorMean.load (System.IO.Path.Join(path, prefix + "_actor.dat"));
        critic.load(System.IO.Path.Join(path, prefix + "_critic.dat"));
    }
}


public partial class ContinuousMLPPPO: Node
{

    private Dictionary<ActivationFunction, Module<Tensor, Tensor> > activations;

    internal ContinuousActorCritic policy;
    internal ContinuousActorCritic oldPolicy;
    internal torch.optim.Optimizer optimizer;

    [Export]
    private Agent agent;


    [ExportGroup("Model Parameters")]
    
    [Export]
    private int[] hiddenSize = new int[]{32};

    [Export]
    private ActivationFunction activationFunction = ActivationFunction.ReLU;

    [ExportGroup("Training Mode")]
    [Export]
    private bool trainingMode = true;
    [Export]
    internal ContinuousMLPPPOAlgorithm algorithm;
    [Export]
    internal bool shared = false;

    public int NumberOfEnvs {get; set;} = 0;

    private int inputSize = 2;
    private int outputSize = 4;

    public int InputSize => inputSize;

    private bool built = false;

    public bool IsInTrainingMode => trainingMode;

    private Module<Tensor, Tensor> activation;

    internal void Build(int numInputs, int numOutputs)
    {
        if (!built)
        {

            activations = new Dictionary<ActivationFunction, Module<Tensor, Tensor>>();
            activations[ActivationFunction.ReLU] = ReLU();
            activations[ActivationFunction.Sigmoid] = Sigmoid();
            activations[ActivationFunction.Tanh] = Tanh();

            activation = activations[activationFunction];

            if (trainingMode && algorithm == null)
            {
                algorithm = new ContinuousMLPPPOAlgorithm();
                AddChild(algorithm);
            }
            this.inputSize = numInputs;
            this.outputSize = numOutputs;
        
            policy = new ContinuousActorCritic(inputSize, hiddenSize, outputSize, activation);
            oldPolicy = new ContinuousActorCritic(inputSize, hiddenSize, outputSize, activation);
            if (algorithm != null && optimizer == null)
            {
                GD.PrintRich("Warning: this model is in training mode!");
                optimizer = torch.optim.Adam(policy.parameters(), algorithm.LearningRate);
            }
            built = true;
            if (shared)
            {
                GetTree().Root.GetNode<ContinuousMLPPPOAsyncSingleton>("MLPPPOAsyncSingleton").Model = this;
            }
        }
    }

    public void Save()
    {
        policy.Save();
    }

    public void SyncWith(MLPPPO model)
    {
        policy.Actor.load_state_dict(model.policy.Actor.state_dict());
        policy.Critic.load_state_dict(model.policy.Critic.state_dict());
        oldPolicy.Actor.load_state_dict(model.oldPolicy.Actor.state_dict());
        oldPolicy.Critic.load_state_dict(model.oldPolicy.Critic.state_dict());
    }

    public Tensor SelectAction(Tensor state)
    {
        var (mean, logStd) = policy.Act(state);
        var std = logStd.exp();
        var action = normal(mean, std);
        return clamp(action, -1 , 1);
    }

    public void Load(string prefix="model", string path="")
    {
        policy.Load(prefix, path);
        oldPolicy.Load(prefix, path);
    }
}
