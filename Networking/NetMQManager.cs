using System.Collections.Generic;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

using Object = UnityEngine.Object;

namespace HallOfGodsAI.Networking
{
	public class NetMQServerManager
	{
		private NetMQServer server;

		public delegate void MessageHandler(MessageType type, byte data);
		public event MessageHandler OnMessageRecieved;
		// {
		//     add => server.OnMessageRecieved += value;
		//     remove => server.OnMessageRecieved -= value;
		// }

		public bool loaded = false;

		public void Load()
		{
			// Unload();
			if (loaded == true) return;
			server = new NetMQServer();
			server.Start();
			loaded = true;
		}

		public void Unload()
		{
			if (loaded == false) return;
			server.Stop();
			loaded = false;
		}

		public void SendMessage(Envs.Step<byte[]> message)
		{
			if (server != null && loaded)
			{
				server.Send(message);
			}
		}

		public void SendMessage(MessageType type, byte[] message)
		{
			if (server != null && loaded)
			{
				server.Send(type, message);
			}
		}

		public void Debug()
		{
			if (server != null && loaded)
			{
				server.Debug();
			}
		}

		public void UpdateLoop()
		{
			if (server != null && loaded)
			{
				// server.UpdateLoop();
				byte[] message;
				if (!server.InboundMessageQueue.TryDequeue(out message)) return;
				OnMessageRecieved?.Invoke((MessageType)Enum.ToObject(typeof(MessageType), message[0]), message[1]);
			}
		}
	}
}
