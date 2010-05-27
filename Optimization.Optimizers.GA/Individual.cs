using System;
using Optimization;
using System.Collections.Generic;

namespace Optimization.Optimizers.GA
{
	public class Individual : Solution
	{
		int d_lastCutPoint;
		List<double> d_mutations;

		public Individual(uint id, Fitness fitness, State state) : base (id, fitness, state)
		{
		}

		public override void Reset()
		{
			base.Reset();

			ResetMutations();
		}

		public override void Copy(Optimization.Solution other)
		{
			base.Copy(other);
			
			Individual individual = other as Individual;

			d_lastCutPoint = individual.LastCutPoint;
			d_mutations = new List<double>(individual.Mutations);
		}

		public void ResetMutations()
		{
			d_mutations = new List<double>();

			for (int i = 0; i < Parameters.Count; ++i)
			{
				d_mutations.Add(0);
			}
		}
		
		public List<double> Mutations
		{
			get
			{
				return d_mutations;
			}
		}
		
		public int LastCutPoint
		{
			get
			{
				return d_lastCutPoint;
			}
			set
			{
				d_lastCutPoint = value;
			}
		}

		public override object Clone()
		{
			object ret = new Individual(Id, Fitness.Clone() as Fitness, State);
			
			(ret as Solution).Copy(this);
			return ret;
		}
	}
}
