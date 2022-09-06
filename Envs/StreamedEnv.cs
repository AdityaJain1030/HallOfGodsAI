// using System;
// using UnityEngine;
// using Modding;

// namespace HallOfGodsAI.Envs
// {
// 	public abstract class StreamedEnv<TAction, TObservation> : Env<TAction, TObservation>
// 		where TAction : Enum
// 	{

// 		private Networking.NetMQServerManager serverManager = new();

// 		public StreamedEnv(Vector3 ObservationSize, string Name):
// 			base(ObservationSize, Name)
// 		{
// 			ModHooks.HeroUpdateHook += serverManager.UpdateLoop;
// 			serverManager.OnMessageRecieved += OnMessageRecieved;
// 			OnResetDone += ResetDoneHandler;
// 			OnStepDone += StepDoneHandler;
// 		}
// 		private void StepDoneHandler(Step<TObservation> step)
// 		{
// 			serverManager.SendMessage(step);
// 		}

// 		private void ResetDoneHandler(TObservation obs)
// 		{
// 			serverManager.SendMessage(Networking.MessageType.Reset, obs);
// 		}


// 		private void OnMessageRecieved(Networking.MessageType type, byte data)
// 		{
// 			// HallOfGodsAI.Instance.Log($"Message recieved: {type}");
// 			// HallOfGodsAI.Instance.Log($"Message data: {data}");

// 			if (type == Networking.MessageType.Step)
// 			{
// 				TAction action = (TAction)(object)data;
// 				Step(action);
// 			}
// 			else if (type == Networking.MessageType.Reset)
// 			{
// 				Reset();
// 			}
// 			else if (type == Networking.MessageType.Init)
// 			{
// 				serverManager.SendMessage(Networking.MessageType.Init, new byte[] { 0 });
// 			}
// 			// Debug();
// 		}
// 	}
// }