using NetMQ;
using NetMQ.Sockets;
using AsyncIO;
using WebSocketSharp;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Modding;
using GlobalEnums;
using InControl;

namespace HallOfGodsAI.Envs
{
	public enum ActionSpace
	{
		MoveLeft,
		MoveRight,
		AttackLeft,
		AttackRight,
		AttackUp,
		AttackDown,
		Jump,
		CancelJump,
		DashLeft,
		DashRight,
		CastLeft,
		CastRight,
		CastUp,
		CastDown,
		None
		// TODO: Implement these eventually
		// Heal,
		// CancelHeal,
		// ChargeNailArt,
		// NailArtLeft,
		// NailArtRight,
		// NailArtUp,
		// NailArtDashLeft,
		// NailArtDashRight
	}
	public class HornetEnv : Env<ActionSpace, byte[]>
	{
		public static HornetEnv _instance = null;

		public static HornetEnv Instance()
		{

			if (_instance == null)
			{
				_instance = new HornetEnv();

			}
			return _instance;
		}

		// internal Networking.NetMQServerManager serverManager = new();
		internal WebSocket ws;
		internal string scene_name = "GG_Hornet_1";
		internal string gate_name = "door_dreamEnter";
		internal Utils.GameObservation curObs;
		internal bool curDone = false;
		internal Utils.InputDeviceShim inputDevice = new();
		internal float curReward = 0f;

		internal Utils.HitboxReaderManager obsManager = new();

		private static float TimeScaleDuringFrameAdvance = 0f;

		internal GameObject playerDeathPrefab;

		private class PlayerDeathMono : MonoBehaviour
		{
			private void Start()
			{
				PlayerDeathHook();
			}
			private void PlayerDeathHook()
			{
				// if (eventAlreadyReceived) return;
				HallOfGodsAI.Instance.Log("Player died");
				HornetEnv.Instance().curReward -= 100;
				HornetEnv.Instance().curDone = true;
				var step = new Step<byte[]>()
				{
					observation = HornetEnv.Instance().curObs.Flatten(),
					done = HornetEnv.Instance().curDone,
					reward = HornetEnv.Instance().curReward,
					info = ""
				};
				HornetEnv.Instance().InvokeStepDone(step);
			}
		}

		public HornetEnv() :
			base(new Vector3(212, 120, 1), "Hornet")
		{
			InputManager.AttachDevice(inputDevice);
		}

		private void ResetDoneHandler(byte[] obs)
		{
			HallOfGodsAI.Instance.Log("Reset Done Handler");
			Send(Networking.MessageType.Reset, obs);
		}

		public void Setup()
		{
			playerDeathPrefab = HeroController.instance.heroDeathPrefab;
			playerDeathPrefab.AddComponent<PlayerDeathMono>();
			// playerDeathPrefab.gameObject.Awa
			// serverManager.Load();
			ws = new WebSocket("ws://locahost:4096");
			// server.Bind("tcp://*:5555");
			// ModHooks.HeroUpdateHook += UpdateLoop;
			ws.OnMessage += (sender, e) =>
			{
				if (e.IsBinary)
					OnMessageRecieved((Networking.MessageType)e.RawData[0],
					e.RawData[1]);
			};
			ModHooks.AfterTakeDamageHook += TakeDamageHook;
			ModHooks.OnReceiveDeathEventHook += EnemyDeathHook;
			On.HealthManager.TakeDamage += DealDamageHook;
			// On.HealthManager.Die += PlayerDeathHook;
			OnResetDone += ResetDoneHandler;
			OnStepDone += StepDoneHandler;

			// PlayMakerFSM fsm = GameObject.Find("Knight").transform.Find("Hero Death").gameObject;
		}

		// public void UpdateLoop()
		// {
		// 	if (ws != null)
		// 	{
		// 		// server.UpdateLoop();
		// 		byte[] message;
		// 		if (!server.TryReceiveFrameBytes(out message)) return;
		// 		// if (!server.InboundMessageQueue.TryDequeue(out message)) return;
		// 		OnMessageRecieved((Networking.MessageType)Enum.ToObject(typeof(Networking.MessageType), message[0]), message[1]);
		// 	}
		// }

		private void StepDoneHandler(Step<byte[]> step)
		{
			Send(step);
		}

		private void OnMessageRecieved(Networking.MessageType type, byte data)
		{
			// HallOfGodsAI.Instance.Log($"Message recieved: {type}");
			// HallOfGodsAI.Instance.Log($"Message data: {data}");

			if (type == Networking.MessageType.Step)
			{
				ActionSpace action = (ActionSpace)data;
				Step(action);
			}
			else if (type == Networking.MessageType.Reset)
			{
				Reset();
			}
			else if (type == Networking.MessageType.Init)
			{
				Send(Networking.MessageType.Init, new byte[] { 0 });
			}
			// Debug();
		}

		public void UnloadManagers()
		{
			// serverManager.Unload();
			ws.Close();
			obsManager.Unload();
		}

		private void ChangeScene()
		{
			SceneLoad load = ReflectionHelper.GetField<GameManager, SceneLoad>(GameManager.instance, "sceneLoad");
			if (load != null)
			{
				load.Finish += () =>
				{
					LoadScene();
				};
			}
			else LoadScene();
		}

		private void LoadScene()
		{
			GameManager.instance.StopAllCoroutines();
			ReflectionHelper.SetField<GameManager, SceneLoad>(GameManager.instance, "sceneLoad", null);
			GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
			{
				IsFirstLevelForPlayer = false,
				SceneName = scene_name,
				HeroLeaveDirection = GatePosition.door,
				EntryGateName = gate_name,
				EntryDelay = 0f,
				PreventCameraFadeOut = true,
				WaitForSceneTransitionCameraFade = true,
				Visualization = GameManager.SceneLoadVisualizations.Default,
				AlwaysUnloadUnusedAssets = false
			});
		}

		private void DoAction(ActionSpace action)
		{
			switch (action)
			{
				case ActionSpace.MoveLeft:
					inputDevice.DoWalk(true);
					break;
				case ActionSpace.MoveRight:
					inputDevice.DoWalk(false);
					break;
				case ActionSpace.AttackLeft:
					inputDevice.DoAttack(0);
					break;
				case ActionSpace.AttackRight:
					inputDevice.DoAttack(1);
					break;
				case ActionSpace.AttackUp:
					inputDevice.DoAttack(2);
					break;
				case ActionSpace.AttackDown:
					inputDevice.DoAttack(3);
					break;
				case ActionSpace.Jump:
					inputDevice.DoJump();
					break;
				case ActionSpace.DashLeft:
					inputDevice.DoDash(true);
					break;
				case ActionSpace.DashRight:
					inputDevice.DoDash(false);
					break;
				case ActionSpace.CastLeft:
					inputDevice.DoCast(0);
					break;
				case ActionSpace.CastRight:
					inputDevice.DoCast(1);
					break;
				case ActionSpace.CastUp:
					inputDevice.DoCast(2);
					break;
				case ActionSpace.CastDown:
					inputDevice.DoCast(3);
					break;
				default:
					break;
			}
		}

		public override void Reset(int seed = -1)
		{
			curDone = false;
			curReward = 0;
			EndFreezeFrame();
			ChangeScene();
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnResetLoaded;
		}

		public override void Step(ActionSpace action)
		{
			curDone = false;
			curReward = 0;
			DoAction(action);
			AdvanceSteps(15);
		}

		private void OnResetLoaded(Scene scene, LoadSceneMode mode)
		{
			obsManager.Load();
			StartFreezeFrame();
			AdvanceSteps(100, false);
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnResetLoaded;
			// ModHooks.DealDamageHook += DealDamageHook;
		}

		private int TakeDamageHook(int hazardType, int damage)
		{
			//get percentage of total health taken
			curReward -= damage * 100 / 9;
			return damage;
		}

		private void EnemyDeathHook(EnemyDeathEffects _, bool eventAlreadyReceived, ref float? attackDirection, ref bool resetDeathEvent, ref bool spellBurn, ref bool isWatery)
		{
			if (eventAlreadyReceived) return;
			curReward += 100;
			curDone = true;
			InvokeStepDone(new Step<byte[]>()
			{
				observation = curObs.Flatten(),
				done = curDone,
				reward = curReward,
				info = ""
			});
		}

		// private void PlayerDeathHook()
		// {
		// 	// if (eventAlreadyReceived) return;
		// 	curReward -= 100;
		// 	curDone = true;
		// 	InvokeStepDone(new Step<byte[]>()
		// 	{
		// 		observation = curObs.Flatten(),
		// 		done = curDone,
		// 		reward = curReward,
		// 		info = ""
		// 	});
		// }

		private void DealDamageHook(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
		{
			orig(self, hitInstance);
			curReward += hitInstance.DamageDealt * 100 / (self.hp == 0 ? 1 : self.hp);
		}

		#region Freeze Frame
		public void StartFreezeFrame()
		{
			if (Time.timeScale != 0)
			{
				Time.timeScale = 0f;
				TimeScaleDuringFrameAdvance = Utils.TimeScaleManager.CurrentTimeScale;
				Utils.TimeScaleManager.CurrentTimeScale = 0;
				Utils.TimeScaleManager.TimeScaleActive = true;
			}
		}

		public void EndFreezeFrame()
		{
			if (Time.timeScale == 0)
			{
				Utils.TimeScaleManager.CurrentTimeScale = TimeScaleDuringFrameAdvance;
				Time.timeScale = Utils.TimeScaleManager.CurrentTimeScale;
			}
		}

		private void AdvanceSteps(int frames, bool invokeStep = true)
		{
			GameManager.instance.StartCoroutine(Advance(frames, invokeStep));

		}

		private void Send(Envs.Step<byte[]> step)
		{
			byte[] message = new byte[5 + step.observation.Length];
			message[0] = Convert.ToByte(step.done);
			Buffer.BlockCopy(BitConverter.GetBytes(step.reward), 0, message, 1, 4);
			Buffer.BlockCopy(step.observation, 0, message, 5, step.observation.Length);
			Send(Networking.MessageType.Step, message);
			// _netMqPublisher.OutboundMessageQueue.Enquesue(step);
		}

		private void Send(Networking.MessageType type, byte[] bytes)
		{
			byte[] message = new byte[bytes.Length + 1];
			message[0] = (byte)type;
			Buffer.BlockCopy(bytes, 0, message, 1, bytes.Length);
			ws.Send(message);
		}

		private IEnumerator Advance(int frames, bool invokeStep = true)
		{
			Time.timeScale = 10f;
			// int j = 0;
			for (int i = 0; i < frames; i++)
			{
				yield return new WaitForFixedUpdate();
				// HallOfGodsAI.Instance.Log("Advancing frame: " + ++j);
			}
			Time.timeScale = 0;
			var hitboxes = obsManager.GetHitboxes();
			curObs = Utils.ObservationParser.RenderAllHitboxes(hitboxes);
			inputDevice.ResetState();
			if (invokeStep)
			{
				InvokeStepDone(new Step<byte[]>()
				{
					observation = curObs.Flatten(),
					done = curDone,
					reward = curReward,
					info = ""
				});
			}
			else
			{
				HallOfGodsAI.Instance.Log("Reset done");
				InvokeResetDone(curObs.Flatten());
			}
		}
		#endregion

		public override void Close()
		{
			ModHooks.AfterTakeDamageHook -= TakeDamageHook;
			ModHooks.OnReceiveDeathEventHook -= EnemyDeathHook;
			On.HealthManager.TakeDamage -= DealDamageHook;
			// serverManager.OnMessageRecieved -= OnMessageRecieved;
			OnResetDone -= ResetDoneHandler;
			OnStepDone -= StepDoneHandler;
			UnloadManagers();
		}

		public void Debug()
		{
			// Debug.Log("Debug");
			// HallOfGodsAI.Instance.Log("State: " + state);
			// HallOfGodsAI.Instance.Log("InboundMessageQueue.Count: " + InboundMessageQueue.Count);
			// HallOfGodsAI.Instance.Log("OutboundMessageQueue.Count: " + OutboundMessageQueue.Count);
		}

	}
}