using System;
using Optimization;
using System.Xml;

namespace Optimization.Optimizers.Extensions.PSODD
{
	[Attributes.Extension(Description="Stagnation detection and dispersion", AppliesTo=new Type[] {typeof(PSO.PSO)})]
	public class PSODD : Extension
	{
		public class Settings : Optimization.Settings
		{
			[Attributes.Setting("window", 10, Description="The window size (in iterations) to measure stagnation over")]
			public uint Window;
			
			[Attributes.Setting("threshold", 0.00001, Description="The threshold after which to stop the optimization")]
			public double Threshold;
		}
		
		private double d_previousFitness;
		private double d_previousVelocity;
		private double d_currentVelocity;
		private bool d_stop;

		public PSODD(Job job) : base(job)
		{			
			d_previousFitness = 0;
			
			d_previousVelocity = 0;
			d_currentVelocity = 0;
			
			d_stop = false;
		}
		
		public new Settings Configuration
		{
			get
			{
				return (Settings)base.Configuration;
			}
		}
		
		protected override Optimization.Settings CreateSettings()
		{
			return new Settings();
		}
		
		public override bool Finished()
		{
			return !d_stop;
		}
		
		private void RecordVelocity()
		{
			double velocity = 0;

			foreach (Solution sol in Job.Optimizer.Population)
			{
				PSO.Particle p = (PSO.Particle)sol;

				// Calculate normalized average velocity
				double norm = 1.0 / p.Parameters.Count;

				for (int i = 0; i < p.Parameters.Count; ++i)
				{
					double range = (p.Parameters[i].Boundary.Max - p.Parameters[i].Boundary.Min);
					velocity += norm * p.Velocity[i] / range;
				}
			}
			
			d_currentVelocity += velocity;
		}
		
		private void CalculateStop()
		{
			if (!(d_previousFitness == 0 || d_previousVelocity == 0))
			{
				double r;
				
				double fitness = 1 - Job.Optimizer.Best.Fitness.Value / d_previousFitness;
				double velocity = 1 - d_currentVelocity / d_previousVelocity;
	
				r = System.Math.Abs(fitness / velocity);
				
				d_stop = r < Configuration.Threshold;
			}
			
			d_previousFitness = Job.Optimizer.Best.Fitness.Value;

			d_previousVelocity = d_currentVelocity;
			d_currentVelocity = 0;
		}
		
		public override void Update()
		{
			RecordVelocity();
			
			if (Job.Optimizer.CurrentIteration != 0 && Job.Optimizer.CurrentIteration % Configuration.Window == 0)
			{
				CalculateStop();
			}
		}
	}
}
