using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HallOfGodsAI.Utils
{
	public static class ObservationParser
	{
		#region RendererUtils
		public static void RenderHitboxToObservation(Camera camera, Collider2D collider2D, HitboxType type, GameObservation observation)
		{
			if (collider2D == null || !collider2D.isActiveAndEnabled)
			{
				return;
			}
			switch (collider2D)
			{
				case BoxCollider2D boxCollider2D:
					Vector2 halfSize = boxCollider2D.size / 2f;
					Vector2 topLeft = new(-halfSize.x, halfSize.y);
					Vector2 topRight = halfSize;
					Vector2 bottomRight = new(halfSize.x, -halfSize.y);
					Vector2 bottomLeft = -halfSize;

					List<Vector2> boxPoints = new List<Vector2>
					{
						topLeft, topRight, bottomRight, bottomLeft
					};
					PaintPolygonToGameState(boxPoints, camera, collider2D, type, observation);

					break;
				case PolygonCollider2D polygonCollider2D:
					for (int i = 0; i < polygonCollider2D.pathCount; i++)
					{
						List<Vector2> polygonPoints = new(polygonCollider2D.GetPath(i));
						PaintPolygonToGameState(polygonPoints, camera, collider2D, type, observation);
					}
					break;
				case EdgeCollider2D edgeCollider2D:
					PaintPolygonToGameState(new(edgeCollider2D.points), camera, collider2D, type, observation);
					break;
				case CircleCollider2D circleCollider2D:
					Vector2 center = LocalToScreenPoint(camera, circleCollider2D, Vector2.zero);
					Vector2 right = LocalToScreenPoint(camera, collider2D, Vector2.right * circleCollider2D.radius);
					int radius = (int)Math.Round(Vector2.Distance(center, right));
					observation.AddCircleToState(
						center,
						radius,
						(int)type + 1
					);
					break;

			}
		}
		public static Vector2 LocalToScreenPoint(Camera camera, Collider2D collider2D, Vector2 point)
		{
			Vector2 result = camera.WorldToScreenPoint((Vector2)collider2D.transform.TransformPoint(point + collider2D.offset));
			return new Vector2((int)Math.Round(result.x), (int)Math.Round(Screen.height - result.y));
		}

		public static void PaintPolygonToGameState(List<Vector2> points, Camera camera, Collider2D collider2D, HitboxType hitboxType, GameObservation observation)
		{
			List<Vector2> projectedPoints = points.Select(point => LocalToScreenPoint(camera, collider2D, point)).ToList();
			observation.AddPolygonToObservation(projectedPoints, (int)hitboxType + 1);
		}
		#endregion

		public static GameObservation RenderAllHitboxes(SortedDictionary<HitboxType, HashSet<Collider2D>> hitboxes)
		{
			GameObservation observation = new GameObservation(848, 480);

			Camera camera = Camera.main;
			foreach (var pair in hitboxes)
			{
				foreach (Collider2D collider2D in pair.Value)
				{
					RenderHitboxToObservation(camera, collider2D, pair.Key, observation);
				}
			}

			var resizedObservation = observation.ResizeNearestNeighbor(212, 120);
			return resizedObservation;
		}
	}
}