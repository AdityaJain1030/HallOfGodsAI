from argparse import Action
from enum import Enum
import gym
from gym.spaces import Discrete, Box
import numpy as np
import zmq
import time
import cv2
from stable_baselines3 import DQN
import struct

class Actions(Enum):
	MoveLeft = 0
	MoveRight = 1
	AttackLeft = 2
	AttackRight = 3
	AttackUp = 4
	AttackDown = 5
	Jump = 6
	CancelJump = 7
	DashLeft = 8
	DashRight = 9
	CastLeft = 10
	CastRight = 11
	CastUp = 12
	CastDown = 13
	NOOP = 14

class HallOfGodsEnv(gym.Env):
	def __init__(self):
		super(HallOfGodsEnv, self).__init__()

		self.ctx = zmq.Context()
		self.socket = self.ctx.socket(zmq.REQ)
		self.port = "5555"
		self.socket.connect('tcp://127.0.0.1:' + self.port)

		self.action_space = Discrete(14,)
		self.observation_shape = (212, 120, 1)
		self.observation_space = Box(low=0, high=255, shape=self.observation_shape, dtype=np.uint8)

		self.socket.send(bytearray((0, 0)))
		if (self.socket.recv() == bytearray((0, 0))):
			print("Connected to Hollow Knight")
	
	def step(self, action):
		print("attempting step")
		assert action >= 0 and action < 14, "Invalid action"
		self.socket.send(bytearray((1, action)))
		bytes = self.socket.recv()
		# print(bytes[0:10])
		done = (bytes[1] == 1)
		[reward] = struct.unpack('f', bytes[2:6])
		obs = np.frombuffer(bytes[6:], dtype=np.uint8).reshape(self.observation_shape)
		return obs, reward, done, {}
		# return 
	
	def reset(self):
		self.socket.send(bytearray((2, 0)))
		bytes = self.socket.recv()
		obs = np.frombuffer(bytes[1:], dtype=np.uint8).reshape(self.observation_shape)
		return obs
	
	def close(self):
		self.socket.close()
		self.ctx.term()
		return

	
	# def reset(self):
env = HallOfGodsEnv()
model = DQN("MlpPolicy", env, verbose=1)
model.learn(total_timesteps=20)
model.save('e1')

print("t done")
obs = env.reset()
for i in range(20):
    action, _states = model.predict(obs, deterministic=True)
    obs, reward, done, info = env.step(action)
    env.render()
    if done:
      obs = env.reset()

# time.sleep(4)
# for i in range(10):