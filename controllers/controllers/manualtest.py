from math import trunc
import bemaker
import BMEnv
import gymnasium as gym
import numpy as np
from controller1.controller import DonutGymController
import time
import sys

emomodel = None

if len(sys.argv) > 1:
   emomodel = sys.argv[1].strip()

DonutGymController.emomodel = emomodel
DonutGymController.train_mode = False
try:
    from msvcrt import getch
    inputch = lambda : ''.join(map(chr, getch()))
except ImportError:
    def getch():
        import sys, tty, termios            
        fd = sys.stdin.fileno()
        old_settings = termios.tcgetattr(fd)
        try:
            tty.setraw(fd)
            ch = sys.stdin.read(1)
        finally:
            termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
        return ch
    inputch = lambda : ''.join(getch())

env = gym.make("BMEnv-v0", controller_class=DonutGymController, rid='0', config=dict(server_IP='127.0.0.1', server_port=8080))

print("\n\n\n\nWARNING:::> In Project Settings, option Player, set run in background to true.\n\n\n\n")

obs, info = env.reset()
reward_sum = 0
actions = {'W':[0.5, 0.0, 0.0, 0.0], 'S':[0.0, 0.0, 0.0, 0.0], 'A':[0.0, -0.25, 0.0, 0.0], 
            'D': [0.0, 0.25, 0.0, 0.0]}
while True:
    action = inputch().upper()
    if action in actions:
      action = actions[action]  
      obs, reward, done, truncate, info = env.step(action)
      
      #print("GOAL: ", goal, " MINV: ", minv, "VALUE: ", v, " MAXV: ", maxv, " RWD: ", reward)

      print("VISION ", info['vision'][1::2]) 
      print(reward)
      reward_sum += reward
      env.render()
      if done:
        print("Testing Reward ", reward_sum)
        reward_sum = 0
        obs, info = env.reset()
        #time.sleep(1)
    elif action == 'Q':
      break
    elif action == 'R':
      env.reset()