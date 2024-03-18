import bemaker
import BMEnv
import gymnasium as gym
import numpy as np
from stable_baselines3 import SAC
from stable_baselines3.sac import MultiInputPolicy, MlpPolicy
from controller import DonutGymController
import time
import torch
import sys


path = "model"

if len(sys.argv) > 1:
   path = sys.argv[1].strip()

env = gym.make("BMEnv-v0", controller_class=DonutGymController, rid='0', config=dict(server_IP='127.0.0.1', server_port=8080))

model = SAC.load(path, custom_objects={'action_space': env.action_space, 'observation_space': env.observation_space}) 
DonutGymController.model = model
DonutGymController.train_mode = False
obs, info = env.reset()

reward_sum = 0
while True:
    action, _states = model.predict(obs, deterministic=False)
    #value = model.critic.forward(torch.from_numpy(obs).cuda(), torch.from_numpy(action).cuda())
    #print("states ", value)
    obs, reward, done, truncate, info = env.step(action)
    #print(reward)
    reward_sum += reward
    if done:
      print("Testing Reward: ", reward_sum)
      reward_sum = 0
      obs, truncate = env.reset()
      done = False
     # time.sleep(1)



