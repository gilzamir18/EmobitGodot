import numpy as np
from .feelings import *


targets =['ILLNESS', 'PAIN', 'SMELLING', 'SHINE', 'SATISFACTION', 'DISTRESS', 'RESTLESSNESS']



class LimbicSystem:
  il_fear = 0.0
  pa_fear = 0.0
  joy = 1.0
  oldfeeling = None
  il_decay_rate = 0.00005
  pa_decay_rate = 0.01
  joy_increasing = 1.0
  joy_decreasing = 0.1

  il_fear_c = 0
  pa_fear_c = 0
  joy_c = 0


  def add_joy(value):
    LimbicSystem.joy = LimbicSystem.add_emo(LimbicSystem.joy, value, LimbicSystem.joy_c)
    LimbicSystem.joy_c += 1

  def add_ilfear(value):
    LimbicSystem.il_fear = LimbicSystem.add_emo(LimbicSystem.il_fear, value, LimbicSystem.il_fear_c)
    LimbicSystem.il_fear_c += 1

  def add_pafear(value):
    LimbicSystem.pa_fear = LimbicSystem.add_emo(LimbicSystem.pa_fear, value, LimbicSystem.pa_fear_c)
    LimbicSystem.pa_fear_c += 1

  def add_emo(em, nem, c=0):
    '''
    Combina o valor anterior da emoção (em) com o novo valor (nem).
    '''
    if c <= 0:
      return np.clip(em + nem, 0, 1)
    else:
      return np.clip(em + nem * 1/c, 0, 1)

  def GetAction(obs):
    action = np.zeros(7)
    
    p_joy = LimbicSystem.joy

    _, _, illness2, _ = extract_lastfeeling(obs, "ILLNESS", frame=4)
    _, minsmell, smell2, _ = extract_lastfeeling(obs, "SMELLING", frame=4)
    _, minshine, shine2, _ = extract_lastfeeling(obs, "SHINE", frame=4)
    _, _, pain2, _ = extract_lastfeeling(obs, "PAIN", frame=4)
    
    if LimbicSystem.oldfeeling is not None:
      illness1, smell1, shine1, pain1 = LimbicSystem.oldfeeling
    else:
      _, _, illness1, _ = extract_lastfeeling(obs, "ILLNESS", frame=4)
      _, _, pain1, _ = extract_lastfeeling(obs, "PAIN", frame=4)
      _, minsmell, smell1, _ = extract_lastfeeling(obs, "SMELLING", frame=4)
      _, minshine, shine1, _ = extract_lastfeeling(obs, "SHINE", frame=4)

    delta = illness2 - illness1
    deltaPain = pain2 - pain1
    deltaShine = shine2 - shine1
    deltaSmell = smell2 - smell1

    if smell1 >= minsmell:
      LimbicSystem.joy = LimbicSystem.add_emo(LimbicSystem.joy, LimbicSystem.joy_increasing, LimbicSystem.joy_c)
      LimbicSystem.joy_c += 1
    if shine1 >= minshine:
      LimbicSystem.joy = LimbicSystem.add_emo(LimbicSystem.joy, LimbicSystem.joy_increasing, LimbicSystem.joy_c)
      LimbicSystem.joy_c += 1
    if illness1 > 0:
      LimbicSystem.joy = np.clip(LimbicSystem.joy - LimbicSystem.joy_decreasing, 0, 1)
      LimbicSystem.joy_c = 0
    if pain1 > 0:
      LimbicSystem.joy = np.clip(LimbicSystem.joy - LimbicSystem.joy_decreasing, 0, 1)
      LimbicSystem.joy_c = 0

    if smell1 < minsmell and shine1 < minshine:
      LimbicSystem.joy = np.clip(LimbicSystem.joy - 0.1 * LimbicSystem.joy_decreasing, 0, 1)

    painWeight = 1
    illnessWeight = 2

    if delta > 0:
      LimbicSystem.il_fear = LimbicSystem.add_emo(LimbicSystem.il_fear, 10 * delta, LimbicSystem.il_fear_c)
      LimbicSystem.il_fear_c += 1
      illnessWeight = 2
    elif delta < 0:
      LimbicSystem.il_fear = np.clip(LimbicSystem.il_fear - 0.1 * delta, 0, 1)
      illnessWeight = 2
      LimbicSystem.il_fear_c = 0
    elif illness2 < 0.05:
      LimbicSystem.il_fear = np.clip( LimbicSystem.il_fear - LimbicSystem.il_decay_rate, 0, 1)
      LimbicSystem.il_fear_c = 0
    else:
      LimbicSystem.il_fear = LimbicSystem.add_emo(LimbicSystem.il_fear, 0.1, LimbicSystem.il_fear_c)
      LimbicSystem.il_fear_c += 1
      illnessWeight = 2

    if deltaPain > 0:
      LimbicSystem.pa_fear = LimbicSystem.add_emo(LimbicSystem.pa_fear, 10 * deltaPain, LimbicSystem.pa_fear_c)
      LimbicSystem.pa_fear_c += 1
      painWeight = 2    
    elif deltaPain < 0:
      LimbicSystem.pa_fear = np.clip(LimbicSystem.pa_fear - 0.1 * deltaPain, 0, 1)
      painWeight = 2
      LimbicSystem.pa_fear_c = 0
    elif pain2 < 0.05:
      LimbicSystem.pa_fear = np.clip( LimbicSystem.pa_fear - LimbicSystem.pa_decay_rate, 0, 1 )
      LimbicSystem.pa_fear_c = 0
    else:
      painWeight = 2
      LimbicSystem.pa_fear = LimbicSystem.add_emo(LimbicSystem.pa_fear, 0.1, LimbicSystem.pa_fear_c)
      LimbicSystem.pa_fear_c += 1

    if (p_joy > 0):
      k = 0.01 * p_joy
      LimbicSystem.il_fear = np.clip(LimbicSystem.il_fear-k, 0, 1)
      LimbicSystem.pa_fear = np.clip(LimbicSystem.pa_fear-k, 0, 1)
      LimbicSystem.il_fear_c = 0
      LimbicSystem.pa_fear_c = 0
    
    LimbicSystem.il_fear = np.clip(LimbicSystem.il_fear, 0, 1)
    LimbicSystem.pa_fear = np.clip(LimbicSystem.pa_fear, 0, 1)

    if LimbicSystem.il_fear > LimbicSystem.joy:
      action[0] = illnessWeight
      action[-1] = 1.2
    else:
      painWeight = 1.0
      action[2] = 1
      action[3] = 1
      action[4] = 1
      action[5] = 1
      action[-1] = 1

    action[1] = painWeight

    #print(f"Fear(IL, PA) = {LimbicSystem.il_fear:0.2f}, {LimbicSystem.pa_fear:0.2f}")
    #print(f"Joy = {LimbicSystem.joy}")
    LimbicSystem.oldfeeling = illness2, smell2, shine2, pain2
    return action

  def reset():
      LimbicSystem.joy = 1.0
      LimbicSystem.joy_c = 0
      LimbicSystem.fear = 0
      LimbicSystem.il_fear_c = 0
      LimbicSystem.pa_fear_c = 0
      LimbicSystem.pa_fear = 0.0
      LimbicSystem.il_fear = 0.0
      LimbicSystem.oldfeeling = None
