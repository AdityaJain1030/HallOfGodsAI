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
        internal Envs.HornetEnv _env = Envs.HornetEnv.Instance();

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

            _env.OnStepDone += (step) =>
            {
                Log("Steppped");
            };
            
            ModHooks.HeroUpdateHook += () => {
                if (Input.GetKeyDown(KeyCode.F2))
                {
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
                    // _env.UnloadManagers();
                }
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    _env.Reset();
                }
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    _env.Debug();
                    // ModHooks.HeroUpdateHook -= _env.TestBlockingUpdate;
                    // ModHooks.HeroUpdateHook += _env.TestBlockingUpdate;
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