from argparse import Action
from concurrent.futures import thread
from enum import Enum
from re import T
import gym
from gym.spaces import Discrete, Box
import numpy as np
import zmq
import asyncio
import websockets
import time
import cv2

from stable_baselines3 import PPO
from stable_baselines3.common.env_util import make_vec_env
# from stable_baselines3.common.env_util import make_vec_env

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

		self.loop = asyncio.get_event_loop()
		self.socket = self.loop.run_until_complete(websockets.connect("ws://localhost:3000/e"))

		self.action_space = Discrete(14,)
		self.observation_shape = (212, 120, 1)
		self.observation_space = Box(low=0, high=255, shape=self.observation_shape, dtype=np.uint8)

		# self.loop.run_until_complete(self.socket.send(bytearray((0, 0))))
		# if (self.loop.run_until_complete(self.socket.recv()) == bytearray((0, 0))):
		# 	print("Connected to Hollow Knight")
	
	def step(self, action):
		print("attempting step")
		assert action >= 0 and action < 14, "Invalid action"
		self.loop.run_until_complete(self.socket.send(bytearray((1, action))))
		bytes = self.loop.run_until_complete(self.socket.recv())
		time.sleep(0.1)
		# RETRIES = 3
		# while RETRIES > 0:
		# 	try:
		# 		bytes = self.loop.run_until_complete(asyncio.wait_for(self.socket.recv(), timeout=1000))
		# 	except:
		# 		RETRIES -= 1
		# 		if RETRIES == 0:
		# 			return None
		# 		self.loop.run_until_complete(self.socket.send(bytearray((1, action))))
			

		# 	if self.socket.poll(3000) & zmq.POLLIN != 0:
		# 		bytes = self.socket.recv()
		# 		break
		# 	RETRIES -= 1
		# 	self.socket.setsockopt(zmq.LINGER, 0)
		# 	self.socket.close()
		# 	if RETRIES == 0:
		# 		return None
		# 	self.socket = self.ctx.socket(zmq.REQ)
		# 	self.socket.connect('tcp://127.0.0.1:' + self.port)
		# 	self.socket.send(bytearray((1, action)))
		print('recv')
		# print(bytes[0:10])
		done = (bytes[1] == 1)
		[reward] = struct.unpack('f', bytes[2:6])
		obs = np.frombuffer(bytes[6:], dtype=np.uint8).reshape(self.observation_shape)
		return obs, reward, done, {}
		# return 
	
	def reset(self):
		bytes = self.loop.run_until_complete(self.socket.recv())
		# print("Attempting Reset")
		# self.loop.run_until_complete(self.socket.send(bytearray((2, 0))))
		# bytes = self.loop.run_until_complete(self.socket.recv())
		# RETRIES = 3
		# while RETRIES > 0:
		# 	try:
		# 		print("test ran")
		# 		bytes = self.loop.run_until_complete(asyncio.wait_for(self.socket.recv(), timeout=1000))
		# 	except:
		# 		RETRIES -= 1
		# 		if RETRIES == 0:
		# 			return None
		# 		print("resending")
		# 		self.loop.run_until_complete(self.socket.send(bytearray((2, 0))))
		# 	if self.socket.poll(3000) & zmq.POLLIN != 0:
		# 		bytes = self.socket.recv()
		# 		break
		# 	RETRIES -= 1
		# 	self.socket.setsockopt(zmq.LINGER, 0)
		# 	self.socket.close()
		# 	if RETRIES == 0:
		# 		return None
		# 	self.socket = self.ctx.socket(zmq.REQ)
		# 	self.socket.connect('tcp://127.0.0.1:' + self.port)
		# 	self.socket.send(bytearray((2, 0)))
		# if (len(bytes) == 25445):
		# 	bytes = bytes[5:]
		obs = np.frombuffer(bytes[1:], dtype=np.uint8).reshape(self.observation_shape)
		return obs
	def seed(seed):
		return
	
	def close(self):
		self.socket.close()
		return


	# def reset(self):
env = make_vec_env(HallOfGodsEnv, n_envs=1)
# env = HallOfGodsEnv()
model = PPO("CnnPolicy", env, verbose=1)
model.learn(total_timesteps=5000000)
print('done')
model.save("PPO_Hornet_1")
# # def tryReceieve(socket, ctx, timeout = 3000, RETRIES = 3):
# 	# RETRIES = 5
	


	
		

obs = env.reset()

dead = False
while not dead:
	action, _states = model.predict(obs)
	obs, rewards, dones, info = env.step(action)
	dead = dones[0]
	# if dones[0] == True:
	# 	env.reset()
		# break

# print(obs)
# print(dead)

# print(obs.shape)

# for i in range(100):
# 	obs, reward, done, _ = env.step(env.action_space.sample())
# 	if (obs.shape != (212, 120, 1)):
# 		print(obs.shape)
# 		break
# model = DQN('CnnPolicy', env, verbose=1)
# model.learn(total_timesteps=1000)

# obs = en
# for i in range(20):
#     action, _states = model.predict(obs, deterministic=True)
#     obs, reward, done, info = env.step(action)
#     env.render()
#     if done:
#       obs = env.reset()

# time.sleep(4)
# for i in range(10):