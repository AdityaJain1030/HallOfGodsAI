namespace HallOfGodsAI.Envs
{
	public struct Step<TObservation> 
	{
		public TObservation observation;
		public float reward;
		public bool done;
		public string info;
		public void Deconstruct(out TObservation observation, out float reward, out bool done, out string info)
		{
			observation = this.observation;
			reward = this.reward;
			done = this.done;
			info = this.info;
		}
	}
}