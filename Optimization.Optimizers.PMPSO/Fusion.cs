using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.PMPSO
{
	/**
	 * Fusion:
	 *
	 * The fusion class contains a list of particles which are fused together
	 * in the sense of configuration space (i.e. they mutate together between
	 * different mutation spaces). This is essentially necessary since the PSO
	 * does not perform when there is only one particle.
	 *
	 */
	public class Fusion : List<Particle>
	{
		private class Probabilities
		{
			public double Exploration;
			public double Cognitive;
			public double Social;
			
			public Probabilities()
			{
				Exploration = 0;
				Cognitive = 0;
				Social = 0;
			}
		}

		private State d_state;
		private Particle d_best;

		public Fusion(List<Particle> particles, State state) : base(particles)
		{
			d_state = state;
		}
		
		public void UpdateBest()
		{
			// Update the configuration with the highest fitness
			foreach (Particle particle in this)
			{
				if (d_best == null || particle.ConfigurationBest.Fitness > d_best.Fitness)
				{
					d_best = (Particle)particle.ConfigurationBest.Clone();
				}
			}
		}
		
		private double[] CalculateMutationProbabilities(int mutationIndex, Probabilities probabilities, MutationSet s, Particle gbest)
		{
			double[] ret = new double[s.Count];
			
			for (int i = 0; i < ret.Length; ++i)
			{
				ret[i] = 1;
			}
			
			List<uint> activeSet = this[0].ActiveSet;
			
			for (int i = 0; i < s.Count; ++i)
			{
				// Don't care about non-mutation, since it's just the inverse
				// of the mutation probability
				if (activeSet[mutationIndex] == i)
				{
					continue;
				}

				// Exploration mutation probability
				ret[i] *= 1 - probabilities.Exploration;
				
				// Super local best mutation probability
				if (d_best != null && d_best.ActiveSet[mutationIndex] == i)
				{
					ret[i] *= 1 - probabilities.Cognitive;
				}
				
				// Super global best mutation probability
				if (gbest != null && gbest.ActiveSet[mutationIndex] == i)
				{
					ret[i] *= 1 - probabilities.Social;
				}
			}
			
			for (int i = 0; i < ret.Length; ++i)
			{
				ret[i] = 1 - ret[i];
			}
			
			return ret;
		}
		
		private bool Mutate(int mutationIndex, Probabilities probabilities, MutationSet s, Particle gbest)
		{
			// Calculate mutation probability
			double[] mutprob = CalculateMutationProbabilities(mutationIndex, probabilities, s, gbest);
			double r = d_state.Random.NextDouble();
			double accumulated = 0;
			
			for (int i = 0; i < mutprob.Length; ++i)
			{
				accumulated += mutprob[i];

				if (r < accumulated)
				{
					foreach (Particle particle in this)
					{
						particle.ActiveSet[mutationIndex] = (uint)i;
					}

					return true;
				}
			}
			
			return false;
		}
		
		private Probabilities CalculateProbabilityNormalization(Particle lbest, Particle gbest)
		{
			List<MutationSet> sets = d_state.MutationSets;
			Probabilities probabilities = new Probabilities();
			
			Particle current = this[0];
			
			// First count the number of possible mutations for each
			// mutation set, regarding Pe, Pl and Pg
			for (int i = 0; i < sets.Count; ++i)
			{
				// Counter for Pe
				probabilities.Exploration += sets[i].Count - 1;
				
				// Counter for Pl
				if (lbest != null && lbest.ActiveSet[i] != current.ActiveSet[i])
				{
					// Count only when not in the lbest configuration right now
					probabilities.Cognitive += 1;
				}
				
				// Counter for Pg
				if (gbest != null && gbest.ActiveSet[i] != current.ActiveSet[i])
				{
					// Count only when not in the gbest configuration right now
					probabilities.Social += 1;
				}
			}
			
			// And then normalize it
			// P_ret = 1 - (1 - P_val)^(1 / P_count)
			if (probabilities.Exploration != 0)
			{
				probabilities.Exploration = 1 - System.Math.Pow(1 - d_state.MutationProbability, 1 / probabilities.Exploration);
			}
			
			if (probabilities.Cognitive != 0)
			{
				probabilities.Cognitive = 1 - System.Math.Pow(1 - d_state.CognitiveMutationProbability, 1 / probabilities.Cognitive);
			}
			
			if (probabilities.Social != 0)
			{
				probabilities.Social = 1 - System.Math.Pow(1 - d_state.SocialMutationProbability, 1 / probabilities.Social);
			}

			return probabilities;
		}
		
		public void Mutate(Particle gbest)
		{
			List<MutationSet> sets = d_state.MutationSets;
			bool hasmutated = false;
			Probabilities probabilities = CalculateProbabilityNormalization(d_best, gbest);

			for (int i = 0; i < sets.Count; ++i)
			{
				hasmutated |= Mutate(i, probabilities, sets[i], gbest);
			}
			
			if (hasmutated)
			{
				foreach (Particle particle in this)
				{
					particle.UpdateParameterSet();
				}
			}
		}
		
		public Particle Best
		{
			get
			{
				return d_best;
			}
		}
		
		public void Initialize()
		{
			Optimization.Random r = d_state.Random;
			List<uint> activeSet = new List<uint>();
			List<MutationSet> sets = d_state.MutationSets;
			
			// Initialize active parameter set randomly
			for (int i = 0; i < sets.Count; ++i)
			{
				MutationSet s = sets[i];

				int num = s.Count - 1;
				int idx = (int)System.Math.Round(r.Range(0, num));
				
				activeSet.Add((uint)idx);
			}
			
			foreach (Particle particle in this)
			{
				particle.ActiveSet.Clear();
				particle.ActiveSet.AddRange(activeSet);
				particle.UpdateParameterSet();
			}
		}
	}
}
