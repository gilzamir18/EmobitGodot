import ai4u
from ai4u.utils import address_port_generator, rid_generator, start_envs_windows
from ai4u.controllers import BasicGymController
import AI4UEnv
import gymnasium as gym
import numpy as np
from stable_baselines3 import PPO
from stable_baselines3.ppo import MultiInputPolicy, MlpPolicy
from stable_baselines3.common.callbacks import CheckpointCallback
import torch
from stable_baselines3.common.env_util import make_vec_env
import time

model = None
checkpoint_callback = CheckpointCallback(save_freq=100000, save_path='./logs/', name_prefix='rl_model')

ai4u_config = dict(server_IP='127.0.0.1',
                   server_port=8080, 
                   buffer_size=819200,
                   timeout=-1,
                   observation_space=gym.spaces.Box(low=-1, high=1, shape=(904,), dtype=float),
                   action_space=gym.spaces.Box(low=np.array([0,-1, -1, -1]), high=np.array([1, 1, 1, 1]), shape=(4,), dtype=float),
                   port_generator=address_port_generator(8080, 4))

vec_env = make_vec_env("AI4UEnv-v0", n_envs=4, env_kwargs=dict(rid=list(rid_generator(0, 4)), config=ai4u_config))

policy_kwargs = dict(net_arch=[1024, 512], use_expln=True, optimizer_class=torch.optim.AdamW)

model = PPO(MlpPolicy, vec_env,  policy_kwargs=policy_kwargs, tensorboard_log='tflog', verbose=1)
model.set_env(vec_env)

start_envs_windows(paths=["Demo1.console.exe"]*4, rids=list(rid_generator(0, 4)), host='127.0.0.1', port_generator=address_port_generator(8080, 4))

time.sleep(10)

print("Training....")
model.learn(total_timesteps=2100000, callback=checkpoint_callback, log_interval=5)
model.save("ppo_model")
print("Trained...")
del model # remove to demonstrate saving and loading
print("Train finished!!!")

