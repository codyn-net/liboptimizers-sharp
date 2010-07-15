using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.SPSA
{
	public class Algorithm
	{
		private static Parameter[] CloneParameters(IEnumerable<Parameter> source)
		{
			List<Parameter> ret = new List<Parameter>();
			
			foreach (Parameter p in source)
			{
				ret.Add((Parameter)p.Clone());
			}
			
			return ret.ToArray();
		}
		
		private static double GenerateDelta(State state)
		{
			return 2 * System.Math.Round(state.Random.NextDouble()) - 1;
		}

		public static void Generate(State state, Settings settings, double perturbationRate, IEnumerable<Parameter> parameters, out Parameter[] p0, out Parameter[] p1, out double[] deltas)
		{
			p0 = CloneParameters(parameters);
			p1 = CloneParameters(parameters);

			deltas = new double[p0.Length];
			
			int i = 0;
			
			foreach (Parameter parameter in parameters)
			{
				deltas[i] = GenerateDelta(state);
				
				double theta = deltas[i] * perturbationRate;

				p0[i].Value += theta;
				p1[i].Value -= theta;
				
				if (settings.BoundaryCondition == Optimization.Optimizers.SPSA.Settings.BoundaryConditionType.StickAll)
				{
					Boundary boundary = parameter.Boundary;

					p0[i].Value = System.Math.Max(System.Math.Min(p0[i].Value, boundary.Max), boundary.Min);
					p1[i].Value = System.Math.Max(System.Math.Min(p1[i].Value, boundary.Max), boundary.Min);
				}
				
				++i;
			}
		}
		
		public static double[] Update(IEnumerable<Parameter> parameters, double f0, double f1, double perturbationRate, double learningRate, double epsilon, double[] deltas)
		{
			// Note: we do gradient _ascend_ and not descend in this framework
			double constant = (f1 - f0) / (2 * perturbationRate);
			
			List<double> ret = new List<double>();
			int i = 0;
			
			foreach (Parameter parameter in parameters)
			{
				Boundary boundary = parameter.Boundary;
				double maxStep = epsilon * (boundary.Max - boundary.Min);

				double dtheta = learningRate * constant * deltas[i];
				ret.Add(System.Math.Sign(dtheta) * System.Math.Min(System.Math.Abs(dtheta), maxStep));				
			}
			
			return ret.ToArray();
		}
	}
}

