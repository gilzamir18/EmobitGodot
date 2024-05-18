import ai4u
import AI4UEnv
import gymnasium as gym
import numpy as np
from stable_baselines3 import SAC
from stable_baselines3.sac import MultiInputPolicy, MlpPolicy
import time
import torch
import sys
from ai4u.onnxutils import read_json_file
from ai4u.onnxutils import sac_export_to
from ai4u.controllers import BasicGymController
from predmodule import *
from queue import Queue
from collections import deque

queue = Queue()
expected_reward = None
preview_expected_reward = None
preview_obs = None

rModel = None
rModel_bkp = None
total_steps = 0

def reset_callback(last_obs, info):
    global expected_reward, preview_expected_reward, preview_obs
    preview_expected_reward = None
    expected_reward = None
    preview_obs = None

def step_callback(last_obs, action, info):
    global expected_reward, preview_expected_reward, preview_obs, total_steps, rModel

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
        sample = (preview_obs, info['reward'])
        queue.put(sample)

    preview_expected_reward = expected_reward
    preview_obs = last_obs
    total_steps += 1

path = "sac1m.zip"
model = None

if len(sys.argv) > 1:
   path = sys.argv[1].strip()

BasicGymController.step_callback = step_callback
BasicGymController.reset_callback = reset_callback

env = gym.make("AI4UEnv-v0", rid='0', config=dict(server_IP='127.0.0.1', server_port=8080))

obs, info = env.reset()

model = SAC.load(path, custom_objects={'action_space': env.action_space, 'observation_space': env.observation_space}) 

rModel = NeuralNetwork(env.observation_space.shape[0])
rModel_bkp = NeuralNetwork(env.observation_space.shape[0])

rModel.load_state_dict(torch.load("models_2000000"))
rModel_bkp.load_state_dict(rModel.state_dict())

reward_sum = 0
while True:
    action, _states = model.predict(obs, deterministic=False)
    obs, reward, done, truncate, info = env.step(action[0])
    #print(reward)
    reward_sum += reward
    if done:
      print("Testing Reward: ", reward_sum)
      reward_sum = 0
      obs, truncate = env.reset()
      done = False
     # time.sleep(1)
