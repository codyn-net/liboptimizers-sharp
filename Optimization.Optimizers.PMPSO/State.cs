using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.PMPSO
{
	public class State : Optimization.State
	{
		private List<MutationSet> d_mutationSets;
		private double d_mutationProbability;
		private double d_cognitiveMutationProbability;
		private double d_socialMutationProgability;

		public State(Settings settings) : base(settings)
		{
			d_mutationSets = new List<MutationSet>();
		}
		
		public List<MutationSet> MutationSets
		{
			get
			{
				return d_mutationSets;
			}
		}
		
		public double MutationProbability
		{
			get
			{
				return d_mutationProbability;
			}
			set
			{
				d_mutationProbability = value;
			}
		}
		
		public double CognitiveMutationProbability
		{
			get
			{
				return d_cognitiveMutationProbability;
			}
			set
			{
				d_cognitiveMutationProbability = value;
			}
		}
		
		public double SocialMutationProbability
		{
			get
			{
				return d_socialMutationProgability;
			}
			set
			{
				d_socialMutationProgability = value;
			}
		}
	}
}
