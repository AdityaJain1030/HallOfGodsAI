using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// using NumSharp;

namespace HallOfGodsAI.Utils
{
    ///<summary>
    /// A struct that represents an observation of the game.
    ///</summary>
    public struct GameObservation
    {
        public readonly int width;

        public readonly int height;

        public byte[,] state { get; private set; }
		
        public GameObservation(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.state = new byte[width, height];
        }

        ///<summary>
        /// Rasterizes a polygon to the GameObservation
        /// Uses the Efficient Fill Algorithm detailed <a href="http://alienryderflex.com/polygon_fill">here</a>.
        ///</summary>
        ///<param name="polygon">A list of the polygon vertexes</param>
        ///<param name="polygonLayer">The layer the polygon resides on</param>
        public void AddPolygonToObservation(
            List<Vector2> Points,
            int polygonLayer
        )
        {
            int SIZE = Points.Count;
            int IMAGE_TOP = 0;
            int IMAGE_BOT = Screen.height;
            int IMAGE_LEFT = 0;
            int IMAGE_RIGHT = Screen.width;

            int[] nodeX = new int[100];
            int
                i,
                j,
                nodes,
                pixelX,
                pixelY,
                swap;

            for (pixelY = IMAGE_TOP; pixelY < IMAGE_BOT; pixelY++)
            {
                nodes = 0;
                j = SIZE - 1;
                for (i = 0; i < SIZE; i++)
                {
                    if (
                        Points[i].y < pixelY && Points[j].y >= pixelY ||
                        Points[j].y < pixelY && Points[i].y >= pixelY
                    )
                    {
                        nodeX[nodes++] =
                            (
                            int
                            )(Points[i].x +
                            (pixelY - Points[i].y) /
                            (Points[j].y - Points[i].y) *
                            (Points[j].x - Points[i].x));
                    }
                    j = i;
                }
                i = 0;
                while (i < nodes - 1)
                {
                    if (nodeX[i] > nodeX[i + 1])
                    {
                        swap = nodeX[i];
                        nodeX[i] = nodeX[i + 1];
                        nodeX[i + 1] = swap;
                        if (i > 0) i--;
                    }
                    else
                        i++;
                }
                for (i = 0; i < nodes; i += 2)
                {
                    if (nodeX[i] >= IMAGE_RIGHT) break;
                    if (nodeX[i + 1] > IMAGE_LEFT)
                    {
                        if (nodeX[i] < IMAGE_LEFT) nodeX[i] = IMAGE_LEFT;
                        if (nodeX[i + 1] > IMAGE_RIGHT)
                            nodeX[i + 1] = IMAGE_RIGHT;
                        for (pixelX = nodeX[i]; pixelX < nodeX[i + 1]; pixelX++)
                        SetState(pixelX, ((IMAGE_BOT - 1) - pixelY), polygonLayer);
                    }
                }
            }
        }

		///<summary>
		/// Rasterizes a circle to the GameObservation
		/// Uses a crude circle algorithm cuz I was too lazy to figure out scan lines.
		///</summary>
		///<param name="center">The center of the circle</param>
		///<param name="radius">The radius of the circle</param>
		///<param name="circleLayer">The layer the circle resides on</param>

		public void AddCircleToState(Vector2 center, int radius, int circleLayer)
		{
			for (int x = -radius; x < radius; x++)
			{
				int height = (int)Math.Sqrt(radius * radius - x * x);

				for (int y = -height; y < height; y++)
				{
					SetState((int)center.x + x, (int)center.y + y, circleLayer);
				}
			}

		}
	
		public GameObservation ResizeNearestNeighbor(int newWidth, int newHeight)
		{
			GameObservation newObservation = new GameObservation(newWidth, newHeight);
			int x_ratio = (int)((this.width << 16) / newWidth) + 1;
			int y_ratio = (int)((this.height << 16) / newHeight) + 1;

			int x2, y2;
			for (int i = 0; i < newHeight; i++)
			{
				for (int j = 0; j < newWidth; j++)
				{
					x2 = ((j * x_ratio) >> 16);
					y2 = ((i * y_ratio) >> 16);
					newObservation.SetState(j, i, this.state[x2, y2]);
				}
			}
			return newObservation;
		}

        public void SetState(int x, int y, int value)
        {
            if (x < 0) x=0;
            if (y < 0) y=0;
            if (x >= width) x = width - 1;
            if (y >= height) y = height - 1;
            state[x, y] = (byte)(value * 62);
        }

        public void Reset() 
        {
            state = new byte[width, height];
        }

        public byte[] Flatten()
        {
            byte[] data = new byte[width * height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    data[i * height + j] = state[i, j];
                }
            }
            return data;
            // return state.SelectMany(x => x).ToArray();
        }

        // public static GameObservation Normalize(GameObservation observation)
        // {
        //     // return observation.state.astype(np.float32) / 255;
        //     np.linalg.norm()
        // }
	}

    // public struct CurrentTimestep {
    //     public GameObservation observation;
    //     public float reward;
    //     public bool done;
    //     public Envs.Actions action;

    //     public CurrentTimestep(int width, int height, Envs.Actions action = Envs.Actions.None, float reward = 0, bool done = false)
    //     {
    //         this.observation = new GameObservation(width, height);
    //         this.reward = reward;
    //         this.done = done;
    //         this.action = action;
    //     }

    //     public void Reset() {
    //         this.observation.Reset();
    //         this.reward = 0;
    //         this.done = false;
    //         this.action = Envs.Actions.None;
    //     }
    // }
}
