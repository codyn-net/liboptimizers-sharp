using System;
using System.Collections.Generic;

namespace Optimization.Dispatcher.Internal.PMPSO
{
	[Optimization.Attributes.Dispatcher(Name="pmpso")]
	public class PMPSO : Optimization.Dispatcher.Internal.Dispatcher
	{
		Dictionary<uint, List<double>> d_optima;
		
		public PMPSO()
		{
			d_optima = new Dictionary<uint, List<double>>();
		}
		
		private List<double> GenerateOptimum(Solution solution)
		{
			System.Random r = new System.Random();
			List<double> ret = new List<double>();
			
			for (int i = 0; i < solution.Parameters.Count; ++i)
			{
				ret.Add(r.NextDouble());
			}
			
			return ret;
		}
		
		public override Dictionary<string, double> Evaluate(Solution solution)
		{
			Optimization.Optimizers.PMPSO.Particle particle;
			particle = (Optimization.Optimizers.PMPSO.Particle)solution;

			uint hash = particle.Hash;
			double s = 0;
			List<double> optimum;
			
			if (!d_optima.TryGetValue(hash, out optimum))
			{
				optimum = GenerateOptimum(solution);
				d_optima[hash] = optimum;
			}
			
			for (int i = 0; i < particle.Parameters.Count; ++i)
			{
				double dd = optimum[i] - particle.Parameters[i].Value;
				s += dd * dd;
			}
			
			s = 1 - System.Math.Sqrt(s) / System.Math.Sqrt(particle.Parameters.Count);
			
			Dictionary<string, double> fitness = new Dictionary<string, double>();
			fitness["value"] = s;
			
			return fitness;
		}
	}
}
