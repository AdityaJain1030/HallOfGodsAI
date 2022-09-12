using WebSocketSharp;
using WebSocketSharp.Server;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
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
		private string PreviousScene;
		private Vector3 StatuePos;
		private BossStatue statue;

		//obs
		internal Utils.GameObservation curObs;
		internal bool curDone = false;
		internal float curReward = 0f;
		internal Utils.HitboxReaderManager obsManager = new();
		internal Utils.BossFightManager bossFightManager = new();

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
		public void AdvanceSteps(int frames)
		{
			GameManager.instance.StartCoroutine(Advance(frames));
		}
		private IEnumerator Advance(int frames)
		{
			if (TimeScaleDuringFrameAdvance == 0) yield break;

			Time.timeScale = TimeScaleDuringFrameAdvance;
			// int j = 0;
			for (int i = 0; i < frames; i++)
			{
				yield return new WaitForFixedUpdate();
				// HallOfGodsAI.Instance.Log("Advancing frame: " + ++j);
			}
			Time.timeScale = 0;

			OnStep();
		}
		public void Setup()
		{
			StartWebSocketServer();
			AddRewardHooks();
			InitWebSocketCallbacks();
			bossFightManager.Load();
			obsManager.Load();
			bossFightManager.OnSetupEvent += OnSetup;
			// bossFightManager.StepDoneEvent += OnStep;
			bossFightManager.FightEndedEvent += FightEnded;
		}

		private void OnStep()
		{
			var hitboxes = obsManager.GetHitboxes();
			curObs = Utils.ObservationParser.RenderAllHitboxes(hitboxes);
			inputDevice.ResetState();
			InvokeStepDone(new Step<byte[]>()
			{
				observation = curObs.Flatten(),
				done = curDone,
				reward = curReward,
				info = ""
			});
			if (curDone) bossFightManager.EndFreezeFrame();
		}

		private void FightEnded(bool won)
		{
			HallOfGodsAI.Instance.Log("FightEnded: " + won);
			curDone = true;
			if (won) curReward += 100;
			else curReward -= 100;
		}

		private void OnSetup()
		{
			HallOfGodsAI.Instance.Log("EnvStarted");
			var hitboxes = obsManager.GetHitboxes();
			curObs = Utils.ObservationParser.RenderAllHitboxes(hitboxes);
			InvokeResetDone(curObs.Flatten());
			bossFightManager.StartFreezeFrame(5);
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
			// USceneManager.sceneLoaded += SceneLoaded;
			// On.BossSceneController.DoDreamReturn += (orig, self) => {
			// 	orig(self);
			// 	HallOfGodsAI.Instance.Log("Player dead");
			// };
		}

		private void RemoveRewardHooks()
		{
			ModHooks.AfterTakeDamageHook -= TakeDamageHook;
			On.HealthManager.TakeDamage -= DealDamageHook;
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
			// else if (type == Networking.MessageType.Init)
			// {
			// 	wsManager.SendMessage(Networking.MessageType.Init, new byte[] { 0 });
			// }
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
			// LoadBossScene();
		}

		public override void Step(ActionSpace action)
		{
			curDone = false;
			curReward = 0;
			DoAction(action);
			AdvanceSteps(20);
		}

		public override void Close()
		{
			bossFightManager.EndFreezeFrame();
			obsManager.Unload();
			RemoveRewardHooks();
			CloseWebSocketCallbacks();
			wssr.Stop();
			bossFightManager.Unload();
		}
		#endregion

		// private void Cleanup(Scene prev, Scene next) {
		// if (next.name == "GG_Workshop" || BossSequenceController.IsInSequence) {
		// 	setupEvent = null;
		// }
	}
}