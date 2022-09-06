using System;
using System.Collections;
using UnityEngine;
using Modding;

namespace HallOfGodsAI.Envs
{
	public abstract class Env<TAction, TObservation> : IDisposable
		where TAction : Enum
	{
		public int ActionSize { get; set; }
		public Vector3 ObservationSize { get; set; }
		public string Name { get; set; }
		public delegate void ResetDone(TObservation observation);
		public delegate void StepDone(Step<TObservation> step);
		public event ResetDone OnResetDone;
		public event StepDone OnStepDone;

		public Env(Vector3 ObservationSize, string Name)
		{
			this.ActionSize = Enum.GetNames(typeof(TAction)).Length;
			this.ObservationSize = ObservationSize;
			this.Name = Name;
		}

		public abstract void Reset(int seed = -1);
		public abstract void Step(TAction action);
		public abstract void Close();

		protected void InvokeResetDone(TObservation observation)
		{
			OnResetDone?.Invoke(observation);
		}
		protected void InvokeStepDone(Step<TObservation> step)
		{
			OnStepDone?.Invoke(step);
		}

		public void Dispose()
		{
			Close();
		}

		
	}
}