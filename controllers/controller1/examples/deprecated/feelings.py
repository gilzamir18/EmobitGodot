import numpy as np

FEELINGS = 7
FEELINGS_POS = 11
FEELINGS_PROPS_LEN = 4
FRAME_SIZE = 39
FEELINGS_PROPS = ['ACTIVE', 'MIN_VALUE', 'VALUE', 'MAX_VALUE']
FEELINGS = ['ILLNESS', 'PAIN', 'SMELLING', 'SHINE', 'SATISFACTION', 'DISTRESS', 'RESTLESSNESS'];
FEELINGS_IDX = {name: idx for idx, name in enumerate(FEELINGS)}


def extract_lastfeeling(obs, feeling="ILLNESS", frame=4):
    start =  ((frame-1) * FRAME_SIZE) + FEELINGS_POS  + FEELINGS_IDX[feeling] * FEELINGS_PROPS_LEN
    goal = obs[start]
    value = obs[start + 1]
    min_value = obs[start + 2]
    max_value = obs[start + 3]
    return goal, min_value, value, max_value
