using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

namespace HallOfGodsAI.Networking
{
	public enum MessageType
	{
		Init,
		Step,
		Reset
	}

	public class NetMQServer
	{
		public enum NetworkingState
		{
			RES,
			REQ,
		}
		private readonly Thread _listenerWorker;
		private bool _listenerCancelled;
		public NetworkingState state = NetworkingState.REQ;
		public ConcurrentQueue<byte[]> InboundMessageQueue = new();
		public ConcurrentQueue<byte[]> OutboundMessageQueue = new();

		private void ListenerWork()
		{
			AsyncIO.ForceDotNet.Force();
			using (var server = new ResponseSocket())
			{
				server.Bind("tcp://*:5555");

				while (!_listenerCancelled)
				{
					if (state == NetworkingState.RES)
					{
						byte[] message;
						if (!OutboundMessageQueue.TryDequeue(out message)) continue;
						HallOfGodsAI.Instance.Log($"Sending message: {(MessageType)message[0]}");
						server.SendFrame(message);
						state = NetworkingState.REQ;
					}
					else if (state == NetworkingState.REQ)
					{
						byte[] message;
						if (!server.TryReceiveFrameBytes(out message)) continue;
						// HallOfGodsAI.Instance.Log(message[0].ToString());
						InboundMessageQueue.Enqueue(message);
						state = NetworkingState.RES;
					}

				}
			}
			NetMQConfig.Cleanup();
		}

		public NetMQServer()
		{
			_listenerWorker = new Thread(ListenerWork);
		}

		public void Start()
		{
			_listenerCancelled = false;
			_listenerWorker.Start();
		}

		public void Stop()
		{
			_listenerCancelled = true;
			_listenerWorker.Join();
		}

		public void Debug() {
			HallOfGodsAI.Instance.Log("State: " + state);
			HallOfGodsAI.Instance.Log("InboundMessageQueue.Count: " + InboundMessageQueue.Count);
			HallOfGodsAI.Instance.Log("OutboundMessageQueue.Count: " + OutboundMessageQueue.Count);
			// HallOfGodsAI.Instance.Log();
		}

		public void Send(Envs.Step<byte[]> step)
		{
			byte[] message = new byte[5 + step.observation.Length];
			message[0] = Convert.ToByte(step.done);
			Buffer.BlockCopy(BitConverter.GetBytes(step.reward), 0, message, 1, 4);
			Buffer.BlockCopy(step.observation, 0, message, 5, step.observation.Length);
			Send(MessageType.Step, message);
			// _netMqPublisher.OutboundMessageQueue.Enquesue(step);
		}

		public void Send(MessageType type, byte[] bytes)
		{
			byte[] message = new byte[bytes.Length + 1];
			message[0] = (byte)type;
			Buffer.BlockCopy(bytes, 0, message, 1, bytes.Length);
			OutboundMessageQueue.Enqueue(message);
		}
	}
}