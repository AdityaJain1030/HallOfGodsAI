using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace HallOfGodsAI
{
    internal class HallOfGodsAI : Mod
    {
        internal static HallOfGodsAI Instance { get; private set; }
        internal Envs.NewHornetEnv _env = Envs.NewHornetEnv.Instance();

        public HallOfGodsAI() : base("HallOfGodsAI") { }

        public override string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public override void Initialize()
        {
            Log("Initializing");

            Instance = this;

            Log("Initialized");
            // ilRecordKillForJournal = new ILHook(origRecordKillForJournal, ilRecordKillForJournalHook);

            _env.OnStepDone += (step) =>
            {
                // Log("Steppped");
            };
            
            ModHooks.HeroUpdateHook += () => {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    var statues = UObject.FindObjectsOfType<BossStatue>();
                    foreach (var statue in statues)
                    {
                        Log(statue.gameObject.name);
                    }
                    // BossStatueCompletionStates.;
                    // _env.StartFreezeFrame();
                }
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    // _env.EndFreezeFrame();
                }
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    _env.Setup();
                }
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    _env.Close();
                }
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    _env.Reset();
                }
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    _env.Step(Envs.ActionSpace.MoveLeft);
                }
            };
            // SceneManager.

            // On.HeroController.EnableRenderer += (orig, self) =>
            // {
            //     orig(self);
            //     _env.Load();
            //     // self.gameObject.AddComponent<HornetController>();
            // };
        }
    }
}