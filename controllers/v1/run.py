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

path = "logs/rl_model_1200000_steps.zip"
model = None

if len(sys.argv) > 1:
   path = sys.argv[1].strip()


env = gym.make("AI4UEnv-v0", rid='0', config=dict(server_IP='127.0.0.1', server_port=8080, buffer_size=81900))

#metadatamodel = read_json_file('model.json')
#sac_export_to("model", env)
obs, info = env.reset()

model = SAC.load(path, custom_objects={'action_space': env.action_space, 'observation_space': env.observation_space}) 

reward_sum = 0
while True:
    action, _states = model.predict(obs, deterministic=False)
    obs, reward, done, truncate, info = env.step(action)
    #print(reward)
    reward_sum += reward
    if done:
      print("Testing Reward: ", reward_sum)
      reward_sum = 0
      obs, truncate = env.reset()
      done = False
     # time.sleep(1)
