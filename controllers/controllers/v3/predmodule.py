import numpy as np
import torch
from threading import Thread
from torch.utils.data import DataLoader
from torch.utils.data import Dataset
from torch import nn
from queue import Queue
from collections import deque

#Classe para datasets
class MyDataset(Dataset):
    def __init__(self, array_de_entradas, array_de_saidas):
        self.array_de_entradas = array_de_entradas
        self.array_de_saidas = array_de_saidas

    def __len__(self):
        return len(self.array_de_entradas)

    def __getitem__(self, idx):
        entrada = self.array_de_entradas[idx]
        saida = self.array_de_saidas[idx]
        sample = (entrada, saida)
        return sample


def train_loop(qin, model, model_bkp):
    samples_x = deque(maxlen=100)
    samples_y = deque(maxlen=100)
    loss_fn = nn.MSELoss()
    k = 0
    model_bkp.load_state_dict(model.state_dict())
    optimizer = torch.optim.Adam(model.parameters(), lr=0.0003)
    while True:
        try:
            sample = qin.get()
            if sample == "halt":
                print("Trainer is closed ...")
                break
            samples_x.append(sample[0])
            samples_y.append(sample[1])
            if len(samples_x) >= 64:
                s_y = np.reshape(np.array(samples_y, dtype=np.float32), (len(samples_y), 1))
                dataloader = DataLoader(MyDataset(np.array(samples_x, dtype=np.float32), s_y), batch_size=64, shuffle=True)
                model_bkp.train()
                mean_loss = 0
                total = 0
                for batch, (x, y) in enumerate(dataloader):
                    pred = model_bkp(x)
                    loss = loss_fn(pred, y)
                    loss.backward()
                    optimizer.step()
                    optimizer.zero_grad()
                    mean_loss += loss.item()
                    total += 1
                    #if batch % 100 == 0:
                    #    loss, current = loss.item(), (batch + 1) * len(x)
                    #    print(f"Imagination Performance::loss: {loss:>7f}  [{current:>5d}/{size:>5d}]")
                model.load_state_dict(model_bkp.state_dict())
                if k > 0 and k % 100 == 0:
                    print("MEAN LOSS OF IMAGINATION MODEL: ", mean_loss/total)
            k += 1
        except KeyboardInterrupt:
            break

# Define imaginationbrain
class NeuralNetwork(nn.Module):
    def __init__(self, input_size):
        super().__init__()
        self.flatten = nn.Flatten()
        self.linear_relu_stack = nn.Sequential(
            nn.Linear(input_size, 512),
            nn.ReLU(),
            nn.Linear(512, 512),
            nn.ReLU(),
            nn.Linear(512, 1)
        )

    def forward(self, x):
        x = self.flatten(torch.Tensor(x))
        logits = self.linear_relu_stack(x)
        return logits