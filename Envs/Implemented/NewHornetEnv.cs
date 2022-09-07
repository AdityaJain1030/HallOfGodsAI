using WebSocketSharp;
using WebSocketSharp.Server;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Modding;
using GlobalEnums;
using InControl;

namespace HallOfGodsAI.Envs
{
	public class NewHornetEnv : Env<ActionSpace, byte[]>
	{
		#region Fields
		//networking
		internal WebSocketServer wssr;
		internal Networking.WebSocketManager wsManager;

		//game
		internal string scene_name = "GG_Hornet_1";
		internal string gate_name = "door_dreamEnter";

		//obs
		internal Utils.GameObservation curObs;
		internal bool curDone = false;
		internal float curReward = 0f;
		internal Utils.HitboxReaderManager obsManager = new();

		//input
		internal Utils.InputDeviceShim inputDevice = new();

		//timefreeze
		private static float TimeScaleDuringFrameAdvance = 0f;
		#endregion

		#region Singleton
		private static NewHornetEnv _instance = null;

		public static NewHornetEnv Instance()
		{

			if (_instance == null)
			{
				_instance = new NewHornetEnv();

			}
			return _instance;
		}
		#endregion

		public NewHornetEnv() :
			base(new Vector3(212, 120, 1), "Hornet")
		{
			InputManager.AttachDevice(inputDevice);
		}

		public void Setup()
		{
			StartWebSocketServer();
			AddRewardHooks();
			InitWebSocketCallbacks();
		}

		private void StartWebSocketServer()
		{
			wsManager = new();
			wsManager.OnMessageRecieved += OnMessageRecieved;
			wssr = new(3000);
			wssr.AddWebSocketService<Networking.WebSocketManager>("/e", () => wsManager);
			wssr.Start();
		}

		private void AddRewardHooks()
		{
			ModHooks.AfterTakeDamageHook += TakeDamageHook;
			On.HealthManager.TakeDamage += DealDamageHook;
			On.BossSceneController.DoDreamReturn += PlayerDeadHook;
			On.BossSceneController.CheckBossesDead += BossDeadHook;
		}

		private void RemoveRewardHooks()
		{
			ModHooks.AfterTakeDamageHook -= TakeDamageHook;
			On.HealthManager.TakeDamage -= DealDamageHook;
			On.BossSceneController.DoDreamReturn -= PlayerDeadHook;
			On.BossSceneController.CheckBossesDead -= BossDeadHook;
		}

		#region Reward Hooks
		private int TakeDamageHook(int hazardType, int damage)
		{
			//get percentage of total health taken
			curReward -= damage * 100 / 9;
			return damage;
		}

		private void DealDamageHook(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
		{
			orig(self, hitInstance);
			curReward += hitInstance.DamageDealt * 100 / (self.hp == 0 ? 1 : self.hp);
		}

		private void PlayerDeadHook(On.BossSceneController.orig_DoDreamReturn orig, BossSceneController self)
		{
			orig(self);
			HallOfGodsAI.Instance.Log("Player dead");
			curDone = true;
			curReward -= 100;
			EndFreezeFrame();
			InvokeStepDone(new Step<byte[]>()
			{
				observation = curObs.Flatten(),
				done = curDone,
				reward = curReward,
				info = ""
			});
		}

		private void BossDeadHook(On.BossSceneController.orig_CheckBossesDead orig, BossSceneController self)
		{
			orig(self);
			HallOfGodsAI.Instance.Log("Bosses dead");
			curDone = true;
			curReward += 100;
			EndFreezeFrame();
			InvokeStepDone(new Step<byte[]>()
			{
				observation = curObs.Flatten(),
				done = curDone,
				reward = curReward,
				info = ""
			});
		}
		#endregion

		#region Frame Advance Utils
		public void StartFreezeFrame(int speed)
		{
			if (Time.timeScale != 0)
			{
				Time.timeScale = 0f;
				TimeScaleDuringFrameAdvance = speed;
			}
		}

		public void EndFreezeFrame()
		{
			GameManager._instance.StopCoroutine("Advance");
			if (Time.timeScale == 0)
			{
				Time.timeScale = 1;
			}
		}

		private void AdvanceSteps(int frames, bool invokeStep = true)
		{
			GameManager.instance.StartCoroutine(Advance(frames, invokeStep));
		}

		private IEnumerator Advance(int frames, bool invokeStep = true)
		{
			Time.timeScale = TimeScaleDuringFrameAdvance;
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
				InvokeResetDone(curObs.Flatten());
			}
		}
		#endregion

		#region WebSocket Callbacks
		private void InitWebSocketCallbacks()
		{
			OnResetDone += SendReset;
			OnStepDone += SendStep;
		}

		private void CloseWebSocketCallbacks()
		{
			OnResetDone -= SendReset;
			OnStepDone -= SendStep;
		}

		private void OnMessageRecieved(Networking.MessageType type, byte data)
		{
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
				wsManager.SendMessage(Networking.MessageType.Init, new byte[] { 0 });
			}
		}

		private void SendReset(byte[] obs)
		{
			HallOfGodsAI.Instance.Log("Reset Done Handler");
			wsManager.SendMessage(Networking.MessageType.Reset, obs);
		}

		private void SendStep(Step<byte[]> step)
		{
			wsManager.SendMessage(step);
		}
		#endregion
		private void LoadBossScene()
		{
			var HC = HeroController.instance;
			var GM = GameManager.instance;

			//Copy paste of the FSM that loads a boss from HoG
			PlayerData.instance.dreamReturnScene = "GG_Workshop";
			PlayMakerFSM.BroadcastEvent("BOX DOWN DREAM");
			PlayMakerFSM.BroadcastEvent("CONVO CANCEL");

			HC.ClearMPSendEvents();
			GM.TimePasses();
			GM.ResetSemiPersistentItems();
			HC.enterWithoutInput = true;
			HC.AcceptInput();

			GM.BeginSceneTransition(new GameManager.SceneLoadInfo
			{
				SceneName = scene_name,
				EntryGateName = gate_name,
				EntryDelay = 0,
				Visualization = GameManager.SceneLoadVisualizations.GodsAndGlory,
				PreventCameraFadeOut = true
			});
			GameManager.instance.StartCoroutine(ResetDoneWorker());
		}

		private IEnumerator ResetDoneWorker()
		{
			yield return new WaitForFinishedEnteringScene();
			yield return null;
			yield return new WaitForSeconds(1f); //this line differenciates this function from ApplySettings
			HeroController.instance.AddMPCharge(1);
			HeroController.instance.AddMPCharge(-1);
			obsManager.Load();
			StartFreezeFrame(5);
			yield return Advance(20, false);
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

		#region Gym Overrides
		public override void Reset(int seed = -1)
		{
			curDone = false;
			curReward = 0f;
			LoadBossScene();
		}

		public override void Step(ActionSpace action)
		{
			curDone = false;
			curReward = 0;
			DoAction(action);
			AdvanceSteps(15);
		}

		public override void Close()
		{
			EndFreezeFrame();
			obsManager.Unload();
			RemoveRewardHooks();
			CloseWebSocketCallbacks();
			wssr.Stop();
		}
		#endregion
	}
}