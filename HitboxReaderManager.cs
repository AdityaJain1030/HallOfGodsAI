using System.Collections.Generic;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace HallOfGodsAI.Utils
{
    public class HitboxReaderManager
    {
        private HitboxReader _hitboxReader;
        public bool loaded = false;

        public void Load()
        {
            Unload();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged +=
                CreateHitboxReader;

            ModHooks.ColliderCreateHook += UpdateHitboxReader;

            CreateHitboxReader();
            loaded = true;
        }

        public void Unload()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -=
                CreateHitboxReader;

            ModHooks.ColliderCreateHook -= UpdateHitboxReader;
            DestroyHitboxReader();
        }

        private void CreateHitboxReader(Scene current, Scene next) =>
            CreateHitboxReader();

        private void CreateHitboxReader()
        {
            DestroyHitboxReader();
            if (GameManager.instance.IsGameplayScene())
            {
                _hitboxReader = new GameObject().AddComponent<HitboxReader>();
            }
        }

        private void DestroyHitboxReader()
        {
            if (_hitboxReader != null)
            {
                Object.Destroy (_hitboxReader);
                _hitboxReader = null;
            }
        }

        private void UpdateHitboxReader(GameObject go)
        {
            if (_hitboxReader != null)
            {
                _hitboxReader.UpdateHitbox (go);
            }
        }

        public SortedDictionary<HitboxType, HashSet<Collider2D>> GetHitboxes()
        {
            return _hitboxReader?.colliders
                ?? new SortedDictionary<HitboxType, HashSet<Collider2D>>()
                {
                    { HitboxType.Knight, new HashSet<Collider2D>() },
                    { HitboxType.Enemy, new HashSet<Collider2D>() },
                    { HitboxType.Attack, new HashSet<Collider2D>() },
                    { HitboxType.Terrain, new HashSet<Collider2D>() }
                };
        }
    }
}
