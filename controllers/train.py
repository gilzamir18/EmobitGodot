import sys
import os
from threading import Thread

if __name__ == "__main__":
    args = sys.argv
    config = {}
    with open( args[1].strip() ) as source:
        for line in source:
            data = line.strip().split("=")
            k = data[0].strip()
            if k != '':
                config[k] = data[1].strip()

    test_cmd = "python3 " + os.path.join(config['controller_action'], "appgym_sb3train.py")
    hasemotion = config['controller_emotion'] != 'None'
    if hasemotion:
        test_cmd2 = "python3 " + os.path.join(config['controller_emotion'], "emoagent.py")
    t1 = Thread(target = lambda:os.system(test_cmd))
    t1.start()
    if hasemotion:
        t2 = Thread(target = lambda:os.system(test_cmd2))
        t2.start()
        t2.join()
    t1.join()
