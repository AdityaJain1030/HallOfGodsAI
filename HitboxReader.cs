// ADAPTED FROM DEBUGMOD

using System.Collections.Generic;
using UnityEngine;
using GlobalEnums;

namespace HallOfGodsAI.Utils
{
    public enum HitboxType : int
    {
        Knight,
        Enemy,
        Attack,
        Terrain
    }

    public class HitboxReader : MonoBehaviour
    {

        public readonly SortedDictionary<HitboxType, HashSet<Collider2D>> colliders = new()
		{
			{HitboxType.Knight, new HashSet<Collider2D>()},
			{HitboxType.Enemy, new HashSet<Collider2D>()},
			{HitboxType.Attack, new HashSet<Collider2D>()},
			{HitboxType.Terrain, new HashSet<Collider2D>()}
		};

        private void Start()
        {
            foreach (Collider2D collider2D in Resources.FindObjectsOfTypeAll<Collider2D>())
            {
				AddHitbox(collider2D);
			}
        }

        public void UpdateHitbox(GameObject go)
		{
			foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>(true))
			{
				AddHitbox(col);
			}
		}

		private void AddHitbox(Collider2D collider2D)
		{
			if (collider2D == null)
			{
				return;
			}

			if (collider2D is BoxCollider2D or PolygonCollider2D or EdgeCollider2D or CircleCollider2D)
			{
				GameObject go = collider2D.gameObject;
				if (collider2D.GetComponent<DamageHero>() || collider2D.gameObject.LocateMyFSM("damages_hero"))
				{
					colliders[HitboxType.Enemy].Add(collider2D);
				}
				else if (go.layer == (int)PhysLayers.TERRAIN)
				{
					colliders[HitboxType.Terrain].Add(collider2D);
				}
				else if (go == HeroController.instance?.gameObject && !collider2D.isTrigger)
				{
					colliders[HitboxType.Knight].Add(collider2D);
				}
				else if (go.GetComponent<DamageEnemies>() || go.LocateMyFSM("damages_enemy") || go.name == "Damager" && go.LocateMyFSM("Damage"))
				{
					colliders[HitboxType.Attack].Add(collider2D);
				}
			}
		}
    }
}
