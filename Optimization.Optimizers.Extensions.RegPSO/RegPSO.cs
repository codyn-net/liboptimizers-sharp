using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.Extensions.RegPSO
{
	[Attributes.Extension(Description="Regrouping Particle Swarm Optimization", AppliesTo = new Type[] {typeof(PSO.PSO)})]
	public class RegPSO : Extension
	{
		private double d_diagonal;
		private Biorob.Math.Expression d_stagnationThreshold;
		private Biorob.Math.Expression d_regroupingFactor;
		private Dictionary<string, object> d_context;

		public RegPSO(Job job) : base(job)
		{
			d_context = new Dictionary<string, object>();
			d_stagnationThreshold = new Biorob.Math.Expression();
			d_regroupingFactor = new Biorob.Math.Expression();
		}

		protected override Optimization.Settings CreateSettings()
		{
			return new Settings();
		}
		
		public new Settings Configuration
		{
			get
			{
				return (Settings)base.Configuration;
			}
		}
		
		public override void Initialize()
		{
			base.Initialize();
			
			d_stagnationThreshold.Parse(Configuration.StagnationThreshold);
			d_regroupingFactor.Parse(Configuration.RegroupingFactor);
			
			d_context["stagnation_threshold"] = d_stagnationThreshold;
			d_context["k"] = 1;
		}
		
		private void CalculateDiagonal()
		{
			d_diagonal = 0;
			
			foreach (Boundary boundary in Job.Optimizer.Boundaries)
			{
				double dd = boundary.Max - boundary.Min;
				d_diagonal += dd * dd;
			}
			
			d_diagonal = System.Math.Sqrt(d_diagonal);
		}
		
		public override void InitializePopulation()
		{
			base.InitializePopulation();

			CalculateDiagonal();
		}
		
		private double EuclideanDistance(Solution s0, Solution s1)
		{
			double ret = 0;

			for (int i = 0; i < s0.Parameters.Count; ++i)
			{
				double dd = s0.Parameters[i].Value - s1.Parameters[i].Value;
				ret += dd * dd;
			}
			
			return System.Math.Sqrt(ret);
		}
		
		private double SwarmRadius()
		{
			Solution best = Job.Optimizer.Best;
			double radius = 0;
			
			foreach (Solution solution in Job.Optimizer.Population)
			{
				double dist = EuclideanDistance(solution, best);
				
				if (dist > radius)
				{
					radius = dist;
				}
			}
			
			return radius;
		}
		
		private void UpdateContext()
		{
			d_context["k"] = Job.Optimizer.CurrentIteration;
		}
		
		public override void AfterUpdate()
		{
			base.AfterUpdate();
			
			UpdateContext();
			
			// Calculate swarm radius
			double radius = SwarmRadius();
			
			// Check stagnation
			if (radius / d_diagonal >= d_stagnationThreshold.Evaluate(d_context, Biorob.Math.Constants.Context))
			{
				return;
			}
			
			// Regroup
			double[] range = new double[Job.Optimizer.Parameters.Count];
			range.Initialize();
			Solution best = Job.Optimizer.Best;
			
			for (int i = 0; i < range.Length; ++i)
			{
				foreach (Solution solution in Job.Optimizer.Population)
				{
					double dist = System.Math.Abs(solution.Parameters[i].Value - best.Parameters[i].Value);
					
					if (dist > range[i])
					{
						range[i] = dist;
					}
				}
				
				Boundary boundary = Job.Optimizer.Parameters[i].Boundary;
				range[i] = System.Math.Min(boundary.Max - boundary.Min, d_regroupingFactor.Evaluate(d_context, Biorob.Math.Constants.Context) * range[i]);
			}
			
			// Reinitialize particles
			foreach (Solution solution in Job.Optimizer.Population)
			{
				PSO.Particle particle = (PSO.Particle)solution;

				for (int i = 0; i < particle.Parameters.Count; ++i)
				{
					particle.SetPosition(i, best.Parameters[i].Value + Job.Optimizer.State.Random.Range(-0.5, 0.5) * range[i]);
				}				
			}
		}
	}
}

