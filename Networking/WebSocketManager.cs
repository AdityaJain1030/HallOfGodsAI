using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace HallOfGodsAI.Networking
{
	public class WebSocketManager : WebSocketBehavior
	{
		public delegate void MessageHandler(MessageType type, byte data);

		public event MessageHandler OnMessageRecieved;

		protected override void OnMessage(MessageEventArgs e)
		{
			if (e.IsBinary)
			{
				byte[] data = e.RawData;
				MessageType type = (MessageType) data[0];
				byte message = data[1];
				OnMessageRecieved?.Invoke(type, message);
			}
		}

		protected override void OnOpen()
		{
			base.OnOpen();
			HallOfGodsAI.Instance.Log("Client Connection Opened");
		}

		protected override void OnClose(CloseEventArgs e)
		{
			base.OnClose(e);
			HallOfGodsAI.Instance.Log("Client Connection Closed");
		}

		public void SendMessage(Envs.Step<byte[]> step)
		{
			byte[] message = new byte[5 + step.observation.Length];
			message[0] = Convert.ToByte(step.done);
			Buffer.BlockCopy(BitConverter.GetBytes(step.reward), 0, message, 1, 4);
			Buffer.BlockCopy(step.observation, 0, message, 5, step.observation.Length);
			SendMessage(Networking.MessageType.Step, message);
		}

		public void SendMessage(MessageType type, byte[] bytes)
		{
			byte[] message = new byte[bytes.Length + 1];
			message[0] = (byte)type;
			Buffer.BlockCopy(bytes, 0, message, 1, bytes.Length);
			Send(message);
		}
	}
}