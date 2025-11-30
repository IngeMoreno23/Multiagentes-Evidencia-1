import agentpy as ap
import socket
import json
import time
import threading
import math
import numpy as np

class Bird(ap.Agent):
    def setup(self):
        self.pos = np.random.uniform(0, self.model.size, 2)
        self.vel = np.random.uniform(-1, 1, 2)

    def step(self):
        neighbors = [a for a in self.model.birds if a is not self]

        # Cohesión
        center = sum((a.pos for a in neighbors)) / len(neighbors)
        coh = (center - self.pos) * 0.01

        # Alineación
        avg_vel = sum((a.vel for a in neighbors)) / len(neighbors)
        ali = (avg_vel - self.vel) * 0.05

        # Separación
        sep = 0
        for a in neighbors:
            diff = self.pos - a.pos
            dist = math.sqrt((diff**2).sum())
            if dist < 2:
                sep += diff * (1.0 / (dist + 1e-5))
        sep *= 0.05

        self.vel += coh + ali + sep
        # Limitar velocidad
        speed = math.sqrt((self.vel**2).sum())
        if speed > 1.5:
            self.vel = self.vel / speed * 1.5

        self.pos += self.vel

        # Bordes
        self.pos = self.pos % self.model.size


class FlockModel(ap.Model):
    def setup(self):
        self.size = self.p.size
        self.N = self.p.N
        self.birds = ap.AgentList(self, self.N, Bird)

    def step(self):
        self.birds.step()

    def get_state(self):
        # Dict { id: {pos: [x,y], vel: [vx,vy]} }
        state = {}
        for i, b in enumerate(self.birds):
            state[i] = {
                "pos": b.pos.tolist(),
                "vel": b.vel.tolist()
            }
        return state

class FlockServer:
    def __init__(self, model, host="127.0.0.1", port=1110, tick_time=0.05):
        self.model = model
        self.tick_time = tick_time
        self.host = host
        self.port = port
        self.client = None

    def start(self):
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.bind((self.host, self.port))
        s.listen(1)
        print("Esperando conexión en", self.host, self.port)

        self.client, addr = s.accept()
        print("Cliente conectado:", addr)

        self.client.send(b"READY\n")

        threading.Thread(target=self.listen_cmds, daemon=True).start()

        while True:
            self.model.step()
            state = self.model.get_state()

            msg = json.dumps(state).encode("utf-8")
            self.client.send(msg + b"\n")

            time.sleep(self.tick_time)

    def listen_cmds(self):
        while True:
            try:
                data = self.client.recv(1024)
                if not data:
                    break
                cmd = data.decode("utf-8").strip()
                print("Cliente dice:", cmd)
            except:
                break

if __name__ == "__main__":
    params = {
        "N": 20,
        "size": 50
    }

    model = FlockModel(parameters=params)
    model.setup()

    server = FlockServer(model)
    server.start()
