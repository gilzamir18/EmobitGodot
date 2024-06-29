import ai4u
from ai4u.controllers import BasicGymController
import AI4UEnv
import gymnasium as gym
import numpy as np
from stable_baselines3 import SAC
from stable_baselines3.sac import MultiInputPolicy, MlpPolicy
from stable_baselines3.common.callbacks import CheckpointCallback
import torch
from threading import Thread
from torch.utils.data import DataLoader
from torch.utils.data import Dataset
from torch import nn
from queue import Queue
from collections import deque
from predmodule import *

rModel = None
rModel_bkp = None
Qt = None

# Get cpu, gpu or mps device for training.
device = (
    "cuda"
    if torch.cuda.is_available()
    else "mps"
    if torch.backends.mps.is_available()
    else "cpu"
)

queue = Queue()

model = None

checkpoint_callback = CheckpointCallback(save_freq=100000, save_path='./logs/', name_prefix='rl_model')

expected_reward = None
preview_expected_reward = None
preview_obs = None

total_steps = 0


def reset_callback(last_obs, info):
    global expected_reward, preview_expected_reward, preview_obs
    preview_expected_reward = None
    expected_reward = None
    preview_obs = None

def step_callback(last_obs, action, info):
    global expected_reward, preview_expected_reward, preview_obs, total_steps, rModel

    if total_steps > 0 and total_steps % 200000 == 0:
        torch.save(rModel.state_dict(), f"models_{total_steps}")

    expected_reward = rModel(last_obs).cpu().detach().item()
    if preview_expected_reward is not None:
        BasicGymController.add_field("r'[t]", preview_expected_reward)

    ndaction = np.array([action])
    if model is not None:
        qvalue, _ = model.critic.forward(torch.from_numpy(last_obs).cuda(), torch.from_numpy(ndaction).cuda())
        qvalue = qvalue.cpu().detach().item()
        BasicGymController.add_field('qvalue', qvalue)
        BasicGymController.add_field('reward', info['reward'])

    if preview_obs is not None:
        try:
            sample = (preview_obs, info['reward'])
            queue.put(sample)
        except:
            queue.put("halt")

    preview_expected_reward = expected_reward
    preview_obs = last_obs
    total_steps += 1

BasicGymController.step_callback = step_callback
BasicGymController.reset_callback = reset_callback

env = gym.make("AI4UEnv-v0", rid='0', config=dict(server_IP='127.0.0.1', server_port=8080, buffer_size=81920))
policy_kwargs = dict(net_arch=[1024, 512], use_expln=True, optimizer_class=torch.optim.AdamW)
model = SAC(MlpPolicy, env, learning_starts=1000, policy_kwargs=policy_kwargs, tensorboard_log='tflog', verbose=1)
model.set_env(env)


rModel = NeuralNetwork(env.observation_space.shape[0])
rModel_bkp = NeuralNetwork(env.observation_space.shape[0])

training = Thread(target = train_loop, args=(queue, rModel, rModel_bkp))
training.start()


print("Training....")
try:
    model.learn(total_timesteps=2100000, callback=checkpoint_callback, log_interval=5,)
except:
    queue.put("halt")
model.save("sac1m")
print("Trained...")
del model # remove to demonstrate saving and loading
print("Train finished!!!")

