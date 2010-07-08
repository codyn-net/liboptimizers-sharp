using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.PMPSO
{
	/**
	 * Neighborhood:
	 *
	 * A neighborhood is a list of fused particles from which the global best
	 * configuration space is calculated. Neighborhoods can be merged together
	 * to increase the attraction of previously found configuration spaces. More
	 * neighborhoods encourages exploration while less neighborhoods encourage
	 * exploitation.
	 */
	public class Neighborhood : List<Fusion>
	{
		Particle d_best;

		public Neighborhood()
		{
		}
		
		public void UpdateBest()
		{
			// For each of the fused particle sets
			foreach (Fusion fusion in this)
			{
				// Update the best configuration of the fused particle set
				fusion.UpdateBest();
				Particle fbest = fusion.Best;
				
				// Update the best configuration set of the neighborhood if the one from the
				// fused particle set is better
				if (fbest != null && (d_best == null || fbest.Fitness > d_best.Fitness))
				{
					d_best = (Particle)fbest.Clone();
				}
			}
		}
		
		public Particle ConfigurationBest
		{
			get
			{
				return d_best;
			}
		}
		
		public void Merge(Neighborhood other)
		{
			// Add all the fusions from the other neighborhood into this
			// neighborhood
			foreach (Fusion fusion in other)
			{
				Add(fusion);
			}
			
			// Make sure to also update the best configuration if the best configuration
			// from the other neighborhood is better than this one
			if (other.ConfigurationBest != null &&
			    (d_best == null || other.ConfigurationBest.Fitness > d_best.Fitness))
			{
				d_best = (Particle)other.ConfigurationBest.Clone();
			}
		}
	}
}
