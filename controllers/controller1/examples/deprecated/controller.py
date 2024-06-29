import torch
from torch import nn
import ai4u
from ai4u import agents
import gymnasium as gym
import numpy as np
from collections import deque
import sys

IMG_W = 62
IMG_H = 3
IMG_CH = 4
MAX_STEPS = 1000
ENV_HIST_LEN = 30
ARRAY_SIZE = IMG_CH * 11
EXTRA_FIELDS = 1

# Get cpu, gpu or mps device for training.
device = (
    "cuda"
    if torch.cuda.is_available()
    else "mps"
    if torch.backends.mps.is_available()
    else "cpu"
)

class DonutGymController(agents.BasicController):
    model = None
    train_mode = True
    """
    This basic controller only works with Unity or Godot AI4UTesting
    application or similar environments. For a custom controller,
    create an class based on BasicGymController and put it as argument
    of the command gym.make. For example:

    gym.make("AI4UEnv-v0", controller_class=MyController)

    where MyController is the controller class that you create based
    on ai4uagents.BasicGymController.
    """
    def __init__(self):
        """
        Controller constructor don't have arguments.
        """
        super().__init__()
        self._seed = 0
        self.reward_sum = 0
        self.action_space = gym.spaces.Box(low=np.array([0,-1, 0, 0]),
                               high=np.array([1, 1, 1, 1]),
                               dtype=np.float32)


        input_size = IMG_W*IMG_H*IMG_CH + ARRAY_SIZE+EXTRA_FIELDS
        self.observation_space = gym.spaces.Box(low=-1, high=1,
                                        shape=(input_size, ), dtype=np.float32)
        
        self.env_hist = deque(maxlen=ENV_HIST_LEN)
        self.previous_obs = None
        self.Qt = None
    
        #self.observation_space = gym.spaces.Dict(
        #    {
        #        "array": gym.spaces.Box(-10, 10, shape=(157,), dtype=float),
        #        "vision": gym.spaces.Box(0, 1, shape=(IMG_W * IMG_H * IMG_CH, ), dtype=float),
        #    }
        #)

    def handleNewEpisode(self, info):
        """
        Implement this method if you have an important thing to do 
        after a new episode started.
        """
        print("Begin of the episode....")
        self.reward_sum = 0
        pass

    def handleEndOfEpisode(self, info):
        """
        Implement this method if you have an important thing to do 
        after the current ending. May be  that you want create a 
        new episode with request_restart command to agent.
        """
        self.reward_sum += info['reward']
        print("End Of Episode")
        print("Reward Sum: ", self.reward_sum)
        pass
    def seed(self, s):
        """
        This method prepare the environment with
        random initialization based on seed 's'.
        """
        self._seed = s 
    
    def render(self):
        """
        This method has been maintained to maintain compatibility with 
        the Gym environment standard. It is important that you maintain 
        this method.
        """
        pass

    def close(self):
        """
        Release allocated resources .
        """
        sys.exit(0)

    def handleConfiguration(self, id, max_step, modelmetadata):
        print("Agent configuration: id=", id, " maxstep=", max_step)

    def transform_state(self, info):
        """
        This method transform AI4U data structure to a
        shape supported by OpenGym based environments.
        """
        #print(info)
        if  type(info) is tuple:
            info = info[0]
        if ("vision" in info) and ("array" in info):
            vision = info["vision"]
            vision = np.reshape(vision, (IMG_W*IMG_H*IMG_CH, ))
            vision[0:len(vision):1] = vision[0:len(vision):1]/255
            #print('------------------------------')
            #print(vision)
            #print('------------------------------')
            array = info['array']
            array = np.append(array, vision)
            steps = info['steps']/MAX_STEPS
            #array = np.append(array, self.hope_th)
            array = np.append(array, steps)
            self.reward_sum += info['reward']
            self.last_obs = np.array([array], dtype=np.float32)
            self.last_reward = info['reward']
            self.last_done = info['done']
            self.last_truncated = info['truncated']
            self.last_info = info
            return self.last_obs, self.last_reward, self.last_done,  info['truncated'], info
        else:
            return info

    def reset_behavior(self, info):
        """
        Here you implement whatever is necessary to configure 
        an episode's initial settings and return the first 
        observation that an agent will use to start performing
        actions. In this example, we only extract the initial 
        state from the information sent by the game environment
        implemented in the AI4UTesting code.
        """
        self.hope_th = np.random.choice([0.1, 0.5, 0.9, 1.0])
        vision = info['vision']
        vision = np.reshape(vision, (IMG_W*IMG_H*IMG_CH, ))
        vision[0:len(vision):2] = vision[0:len(vision):2]/255
        vision[1:len(vision):2] = vision[1:len(vision):2]/10
        #print('------------------------------')
        #print(vision)
        #print('------------------------------')
        array = info['array']
        array = np.append(array, vision)
        steps = info['steps']/MAX_STEPS
        array = np.append(array, steps)
        #array = np.append(array, self.hope_th)
        self.env_hist = deque(maxlen=ENV_HIST_LEN)
        self.last_obs = np.array([array], dtype=np.float32)
        self.last_reward = 0
        self.last_done = False
        self.last_truncated = False
        self.last_info = info
        return self.last_obs, info

    def step_behavior(self, action):
        """
        In this method, by changing the values of the 
        actionName and actionArgs attributes,
        you define the action to be performed based on
        the action (action argument) returned by the agent.
        Therefore, this method is called when an agent makes
        a decision, producing the action represented by the
        variable "action".
        """
        self.actionName = "move"
        if type(action) != str:
            if DonutGymController.train_mode:
                action = np.array([action])
            else:
                action = np.array(action)
            actionArgs = action.squeeze()
            self.actionArgs = actionArgs
            #goal = LimbicSystem.GetAction(self.last_obs[0])
            #emotions = np.array([LimbicSystem.joy, 0, LimbicSystem.pa_fear, LimbicSystem.il_fear], dtype=np.float32)
            #self.fields = [("goal", goal), ("emotions", emotions )]
        elif action == 'stop':
            self.actionName = "__stop__"
            self.actionArgs = [0]
        elif action == 'pause':
            self.actionName = "__pause__"
            self.actionArgs = [0]
        elif action == "restart":
               self.actionName = "__restart__"
               self.actionArgs = [0]
        elif action == 'resume':
            self.actionName = "__resume__"
            self.actionArgs = [0]

