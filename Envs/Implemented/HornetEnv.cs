using NetMQ;
using NetMQ.Sockets;
using AsyncIO;
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
	public class HornetEnv : StreamedEnv<ActionSpace, byte[]>
	{
		internal Networking.NetMQServerManager serverManager = new();
		internal string scene_name = "GG_Hornet_2";
		internal string gate_name = "door_dreamEnter";
		internal Utils.GameObservation curObs;
		internal bool curDone = false;
		internal Utils.InputDeviceShim inputDevice = new();
		internal float curReward = 0f;
		internal Utils.HitboxReaderManager obsManager = new();

		private static float TimeScaleDuringFrameAdvance = 0f;


		public HornetEnv() :
			base(new Vector3(212, 120, 1), "Hornet")
		{
			InputManager.AttachDevice(inputDevice);
		}

		private void ResetDoneHandler(byte[] obs)
		{
			serverManager.SendMessage(Networking.MessageType.Reset, obs);
		}

		public void Setup()
		{
			serverManager.Load();
			ModHooks.HeroUpdateHook += serverManager.UpdateLoop;
			serverManager.OnMessageRecieved += OnMessageRecieved;
			OnResetDone += ResetDoneHandler;
			OnStepDone += StepDoneHandler;
			ModHooks.AfterTakeDamageHook += TakeDamageHook;
			ModHooks.OnReceiveDeathEventHook += EnemyDeathHook;
			On.HealthManager.TakeDamage += DealDamageHook;
			ModHooks.BeforePlayerDeadHook += PlayerDeathHook;
		}

		private void StepDoneHandler(Step<byte[]> step)
		{
			serverManager.SendMessage(step);
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
				serverManager.SendMessage(Networking.MessageType.Init, new byte[] { 0 });
			}
			// Debug();
		}

		public void UnloadManagers()
		{
			serverManager.Unload();
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

		protected override void Reset(int seed = -1)
		{
			curDone = false;
			curReward = 0;
			ChangeScene();
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnResetLoaded;
		}

		protected override void Step(ActionSpace action)
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
			var hitboxes = obsManager.GetHitboxes();
			curObs = Utils.ObservationParser.RenderAllHitboxes(hitboxes);
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnResetLoaded;
			// ModHooks.DealDamageHook += DealDamageHook;
			InvokeResetDone(curObs.Flatten());
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

		private void PlayerDeathHook()
		{
			// if (eventAlreadyReceived) return;
			curReward -= 100;
			curDone = true;
			InvokeStepDone(new Step<byte[]>()
			{
				observation = curObs.Flatten(),
				done = curDone,
				reward = curReward,
				info = ""
			});
		}

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

		private IEnumerator Advance(int frames, bool invokeStep = true)
		{
			Time.timeScale = 1f;
			for (int i = 0; i < frames; i++)
			{
				yield return new WaitForFixedUpdate();
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
		}
		#endregion

		protected override void Close()
		{
			ModHooks.AfterTakeDamageHook -= TakeDamageHook;
			ModHooks.OnReceiveDeathEventHook -= EnemyDeathHook;
			On.HealthManager.TakeDamage -= DealDamageHook;
			serverManager.OnMessageRecieved -= OnMessageRecieved;
			OnResetDone -= ResetDoneHandler;
			OnStepDone -= StepDoneHandler;
			ModHooks.HeroUpdateHook -= serverManager.UpdateLoop;
			UnloadManagers();
		}

		public void Debug() 
		{
			// Debug.Log("Debug");
			serverManager.Debug();
		}

	}
}