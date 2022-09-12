using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Modding;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace HallOfGodsAI.Utils
{
	public class BossFightManager
	{
		private BossSceneController.SetupEventDelegate? setupEvent;

		public BossLoadedEventDelegate OnSetupEvent;
		public FightEndedEventDelegate FightEndedEvent;
		public StepEndedEventDelegate StepDoneEvent;

		public delegate void BossLoadedEventDelegate();
		public delegate void FightEndedEventDelegate(bool won);
		public delegate void StepEndedEventDelegate();

		public float TimeScaleDuringFrameAdvance = 0f;

		public void Load()
		{
			On.BossSceneController.Awake += RecordSetup;
			On.GameManager.BeginSceneTransition += GetRewards;
			USceneManager.activeSceneChanged += Cleanup;
		}

		public void Unload()
		{
			setupEvent = null;

			On.BossSceneController.Awake -= RecordSetup;
			On.GameManager.BeginSceneTransition -= GetRewards;
			USceneManager.activeSceneChanged -= Cleanup;
		}

		private void RecordSetup(On.BossSceneController.orig_Awake orig, BossSceneController self)
		{
			if (!BossSequenceController.IsInSequence)
			{
				setupEvent = BossSceneController.SetupEvent;
				SkipToBeginningOfFight();
			}

			orig(self);
		}

		private void Cleanup(Scene prev, Scene next)
		{
			if (next.name == "GG_Workshop" || BossSequenceController.IsInSequence)
			{
				setupEvent = null;
			}
		}
		private void GetRewards(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
		{
			string currentSceneName = self.sceneName;
			if (
				info.SceneName == "GG_Workshop"
				&& setupEvent != null
				&& (
					HeroController.instance.heroDeathPrefab.activeSelf // Death returning
					|| ( // Success returning
						StaticVariableList.GetValue<bool>("finishedBossReturning")
					)
				)
			)
			{
				FightEndedEvent?.Invoke(!HeroController.instance.heroDeathPrefab.activeSelf);
				BossSceneController.SetupEvent = setupEvent;
				setupEvent = null;
				StaticVariableList.SetValue("finishedBossReturning", false);

				HeroController.instance.EnableRenderer();
				HeroController.instance.EnterWithoutInput(true);
				HeroController.instance.AcceptInput();
				HeroController.instance.gameObject.LocateMyFSM("Dream Return").FsmVariables.FindFsmBool("Dream Returning").Value = false;

				info.SceneName = currentSceneName;
				info.EntryGateName = "door_dreamEnter";
			}

			orig(self, info);
		}

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
				TimeScaleDuringFrameAdvance = 0;
			}
		}

		public void AdvanceSteps(int frames)
		{
			GameManager.instance.StartCoroutine(Advance(frames));
		}

		public void SkipToBeginningOfFight()
		{
			GameManager.instance.StartCoroutine(AdvanceToBeginning());
		}

		private IEnumerator AdvanceToBeginning()
		{
			yield return new WaitForSeconds(2f);
			OnSetupEvent?.Invoke();

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

			StepDoneEvent?.Invoke();
		}
		#endregion



	}
}