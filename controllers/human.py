import ai4u
import AI4UEnv
import gymnasium as gym
import numpy as np
import sys
from ai4u.controllers import BasicGymController


if len(sys.argv) > 1:
   path = sys.argv[1].strip()


env = gym.make("AI4UEnv-v0", rid='0', config=dict(server_IP='127.0.0.1', server_port=8080, buffer_size=81900))

obs, info = env.reset()

reward_sum = 0

noop = np.array([0, 0, 0, 0])
left = np.array([0, -1, 0, 0])
right = np.array([0, 1, 0, 0])
forward = np.array([1, 0, 0, 0])
backward = np.array([-1, 0, 0, 0])

actions = {'0': noop, '4': left, '6': right, '8': forward, '2': backward}


while True:
    choosen = input()
    action = actions[choosen]
    obs, reward, done, truncate, info = env.step(action)
    print(obs)
    print(reward)
    reward_sum += reward
    if done:
      print("Testing Reward: ", reward_sum)
      reward_sum = 0
      obs, truncate = env.reset()
      done = False
