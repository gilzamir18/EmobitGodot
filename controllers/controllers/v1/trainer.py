import ai4u
from ai4u.controllers import BasicGymController
import AI4UEnv
import gymnasium as gym
import numpy as np
from stable_baselines3 import SAC
from stable_baselines3.sac import MultiInputPolicy, MlpPolicy
from stable_baselines3.common.callbacks import CheckpointCallback


model = None

checkpoint_callback = CheckpointCallback(save_freq=100000, save_path='./logs/', name_prefix='rl_model')

env = gym.make("AI4UEnv-v0", rid='0', config=dict(server_IP='127.0.0.1', server_port=8080))
policy_kwargs = dict(net_arch=[1024, 512])
model = SAC(MlpPolicy, env, learning_starts=10000, policy_kwargs=policy_kwargs, tensorboard_log='tflog', verbose=1)
model.set_env(env)
print("Training....")
model.learn(total_timesteps=5000000, callback=checkpoint_callback, log_interval=5)
model.save("sac1m")
print("Trained...")
del model # remove to demonstrate saving and loading
print("Train finished!!!")

