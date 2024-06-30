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

public class MLPPPOMemory
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

public class ActorCritic : Module
{
    private readonly Sequential actor;
    private readonly Sequential critic;

    public Sequential Actor => actor;
    public Sequential Critic => critic;

    public ActorCritic(int inputSize, int[] hiddenSize, int actionSize, torch.nn.Module<Tensor, Tensor> activation = null) : base(nameof(ActorCritic))
    {
        if (activation == null)
        {
            activation = ReLU();
        }
        actor = Sequential();
        actor.append("input", Linear(inputSize, hiddenSize[0]));
        actor.append("relu_input", activation);

        critic = Sequential();
        critic.append("input", Linear(inputSize, hiddenSize[0]));
        critic.append("relu_input", activation);

        if (hiddenSize.Length == 1)
        {
            actor.append("hidden", Linear(hiddenSize[0], hiddenSize[0]));
            actor.append(activation);
            actor.append("logits", Linear(hiddenSize[0], actionSize));
            actor.append("output", Softmax(1));

            critic.append("hidden", Linear(hiddenSize[0], hiddenSize[0]));
            critic.append(activation);
            critic.append("output", Linear(hiddenSize[0], 1));
        }
        else
        {
            for (int i = 0; i < hiddenSize.Length-1; i++)
            {
                actor.append("hidden" + i, Linear(hiddenSize[i], hiddenSize[i+1]));
                actor.append(activation);

                critic.append("hidden" + i, Linear(hiddenSize[i], hiddenSize[i+1]));
                critic.append(activation);
            }
            actor.append("logits", Linear(hiddenSize[hiddenSize.Length-1], actionSize));
            actor.append("output", Softmax(1));
            critic.append("output", Linear(hiddenSize[hiddenSize.Length-1], 1));
        }

        RegisterComponents();
    }

    public Tensor Act(Tensor input)
    {
        return actor.forward(input);
    }

    public Tensor Evaluate(Tensor input)
    {
        return critic.forward(input);
    }

    public void Save(string prefix="model", string path="")
    {
        GD.Print(System.IO.Path.Join(path, prefix + "_critic.dat"));
        actor.save(System.IO.Path.Join(path, prefix + "_actor.dat"));
        critic.save(System.IO.Path.Join(path, prefix + "_critic.dat"));
    }

    public void Load(string prefix="model", string path="")
    {
        actor.load (System.IO.Path.Join(path, prefix + "_actor.dat"));
        critic.load(System.IO.Path.Join(path, prefix + "_critic.dat"));
    }
}


public partial class MLPPPO: Node
{
    private Dictionary<ActivationFunction, Module<Tensor, Tensor> > activations;
    internal ActorCritic policy;
    internal ActorCritic oldPolicy;
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
    internal MLPPPOAlgorithm algorithm;
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
            
            if (trainingMode)
            {
                algorithm = new MLPPPOAlgorithm();
                AddChild(algorithm);
            }
            this.inputSize = numInputs;
            this.outputSize = numOutputs;
        
            policy = new ActorCritic(inputSize, hiddenSize, outputSize, activation);
            oldPolicy = new ActorCritic(inputSize, hiddenSize, outputSize, activation);
            if (algorithm != null && optimizer == null)
            {
                GD.PrintRich("Warning: this model is in training mode!");
                optimizer = torch.optim.Adam(policy.parameters(), algorithm.LearningRate);
            }
            built = true;
            if (shared)
            {
                GetTree().Root.GetNode<MLPPPOAsyncSingleton>("MLPPPOAsyncSingleton").Model = this;
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
        var actionProbs = policy.Act(state);
        var action = torch.multinomial(actionProbs, 1);
        return action;
    }

    public void Load(string prefix="model", string path="")
    {
        policy.Load(prefix, path);
        oldPolicy.Load(prefix, path);
    }
}
