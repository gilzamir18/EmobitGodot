using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using System.Linq;
using Godot;
using System;
using static TorchSharp.torch.optim;

namespace ai4u;

public partial class ContinuousMLPPPOAlgorithm: Node
{

    [Export]
    private int numOfEpochs = 1;

    [Export]
    private int batchSize = 32;

    [Export]
    private float learningRate = 0.00025f;

    [Export]
    private float gamma = 0.99f;
    
    [Export]
    private float lambda = 0.95f;
    
    [Export]
    private float clipParam = 0.2f; 

    [Export]
    private float maxGradNorm = 5;
    
    public float LearningRate => learningRate;
    private readonly object updateLock = new object();

    public int GetBatchSize()
    {
        return batchSize;
    }

    public (float criticLoss, float policyLoss) Update(ContinuousMLPPPO model, ContinuousMLPPPOMemory memory, int batchSize, bool accumulate = false)
    {
        lock (updateLock)
        {
            var states = torch.stack(memory.states).detach();
         
            if (states.size(0) < batchSize)
            {
                GD.PrintErr("Size of the sample set is less than batch size!");
            }
            
            var actions = torch.stack(memory.actions).detach();
            var rewards = torch.tensor(memory.rewards.ToArray()).detach();
            var dones = torch.tensor(memory.dones.Select(d => d ? 1f : 0f).ToArray()).detach();

            var oldValues = model.oldPolicy.Evaluate(states).squeeze().detach();

            if (oldValues.dim() == 0)
            {
                oldValues = oldValues.reshape(1);
            }

            var advantages = ComputeAdvantages(rewards, oldValues, dones).detach();

            // Normalizar vantagens
            advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8);

            var oldLogProbs = ComputeLogProbs(model.oldPolicy, states, actions).detach();

            float totalPolicyLoss = 0;
            float totalValueLoss = 0;
            int numBatches = (int)Math.Ceiling((double)states.size(0) / batchSize);
            for (int i = 0; i < numOfEpochs; i++)
            {
                for (int j = 0; j < numBatches; j++)
                {
                    var indices = torch.randint(0, states.size(0), new torch.Size(batchSize));
                    var batchStates = states.index_select(0, indices);
                    var batchActions = actions.index_select(0, indices);
                    var batchAdvantages = advantages.index_select(0, indices);
                    var batchOldLogProbs = oldLogProbs.index_select(0, indices);
                    var batchRewards = rewards.index_select(0, indices);

                    var newLogProbs = ComputeLogProbs(model.policy, batchStates, batchActions);
                    var values = model.policy.Evaluate(batchStates).squeeze();
                    var ratio = (newLogProbs - batchOldLogProbs).exp();
                    var surr1 = ratio * batchAdvantages;
                    var surr2 = ratio.clamp(1 - clipParam, 1 + clipParam) * batchAdvantages;

                    var policyLoss = -torch.min(surr1, surr2).mean();
                    var valueLoss = (values - batchRewards).pow(2).mean();
                    if (float.IsNaN(policyLoss.item<float>()) || float.IsNaN(valueLoss.item<float>()))
                    {
                        GD.PrintErr("WARN: Loss is NaN. Size of the sample: " + states.size(0));
                        continue; // Skip this batch if NaN is detected
                    }
                    totalPolicyLoss += policyLoss.item<float>();
                    totalValueLoss += valueLoss.item<float>();

                    if (!accumulate)
                    {
                        model.optimizer.zero_grad();
                    }

                    (policyLoss + valueLoss).backward();

                    // Aplica clip_grad_norm
                    if (maxGradNorm > 0)
                    {
                        nn.utils.clip_grad_norm_(model.policy.parameters(), maxGradNorm);
                    }
                    if (!accumulate)
                    {
                        model.optimizer.step();
                    }
                }
            }

            if (!accumulate)
            {
                model.oldPolicy.load_state_dict(model.policy.state_dict());
            }

            return (totalValueLoss / numOfEpochs, totalPolicyLoss / numOfEpochs);
        }
    }

    public void ApplyGradients(ContinuousMLPPPO  model)
    {
        lock (updateLock)
        {
            model.optimizer.step();
            model.optimizer.zero_grad();
            model.oldPolicy.load_state_dict(model.policy.state_dict());
        }
    }

    private Tensor ComputeAdvantages(Tensor rewards, Tensor values, Tensor dones)
    {
        var advantages = torch.zeros_like(rewards);
        var gae = torch.tensor(0.0f);
        var nextValue = torch.tensor(0.0f);
        
        for (long t = rewards.size(0) - 1; t >= 0; t--)
        {
            nextValue = t + 1 < rewards.size(0) ? values[t + 1] : torch.tensor(0.0f);
            var delta = rewards[t] + gamma * nextValue * (1 - dones[t]) - values[t];
            gae = delta + gamma * lambda * (1 - dones[t]) * gae;
            advantages[t] = gae;
        }
        return advantages;
    }

    private Tensor ComputeLogProbs(ContinuousActorCritic policy, Tensor states, Tensor actions)
    {
        var (mean, logStd) = policy.Act(states);
        var std = logStd.exp();
        var var = std.pow(2);
        var logProbs = -0.5 * ((actions - mean).pow(2) / var + 2 * logStd + torch.log(2 * torch.tensor(System.Math.PI)));
        return logProbs.sum(1, keepdim: true);
    }
}