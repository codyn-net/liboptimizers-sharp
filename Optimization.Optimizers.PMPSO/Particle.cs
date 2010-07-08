/*
 *  Particle.cs - This file is part of pmpso
 *
 *  Copyright (C) 2010 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License as published by the 
 * Free Software Foundation; either version 2.1 of the License, or (at your 
 * option) any later version.
 * 
 * This library is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License 
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
 */

using System;
using System.Collections.Generic;
using Optimization;
using PSONS = Optimization.Optimizers.PSO;

namespace Optimization.Optimizers.PMPSO
{
	public class Particle : PSONS.Particle
	{
		private Dictionary<uint, Particle> d_personalBests;
		private List<Parameter> d_allParameters;
		private List<uint> d_activeSet;
		private uint d_hash;

		public Particle(uint id, Fitness fitness, Optimization.State state) : base(id, fitness, state)
		{
			d_personalBests = new Dictionary<uint, Particle>();
			d_allParameters = new List<Parameter>();
			d_activeSet = new List<uint>();
			d_hash = 0;
		}
		
		public override void Copy(Solution other)
		{
			base.Copy(other);
			
			Particle particle = (Particle)other;

			d_activeSet = new List<uint>(particle.d_activeSet);
			d_personalBests = new Dictionary<uint, Particle>(particle.d_personalBests);
			
			d_allParameters = new List<Parameter>(particle.d_allParameters);
			d_hash = particle.d_hash;
		}
		
		public override object Clone()
		{
			object ret = new Particle(Id, Fitness.Clone() as Fitness, State);
			
			(ret as Solution).Copy(this);
			return ret;
		}
		
		public List<uint> ActiveSet
		{
			get
			{
				return d_activeSet;
			}
		}

		public override void Reset()
		{
			base.Reset();
			
			// Store all the original parameters
			d_allParameters.Clear();
			d_allParameters.AddRange(Parameters);			
		}
		
		private uint[] Difference(uint[] s1, uint[] s2)
		{
			List<uint> diff = new List<uint>();
			
			// Calculate all the indices in s1 that are not in s2
			for (int i = 0; i < s1.Length; ++i)
			{
				if (Array.IndexOf(s2, s1[i]) == -1)
				{
					diff.Add(s1[i]);
				}
			}
			
			return diff.ToArray();
		}
		
		public void UpdateParameterSet()
		{
			List<Parameter> all = new List<Parameter>(d_allParameters);
			List<MutationSet> sets = MutationSets;

			for (int i = 0; i < d_activeSet.Count; ++i)
			{
				MutationSet s = sets[i];
				uint activeIndex = d_activeSet[i];
				uint[] activeIndices = s[(int)activeIndex];
				
				for (int j = 0; j < s.Count; ++j)
				{
					if (j == activeIndex)
					{
						continue;
					}

					uint[] indices = Difference(s[j], activeIndices);
					for (int k = 0; k < indices.Length; ++k)
					{
						all.Remove(d_allParameters[(int)indices[k]]);
					}
				}
			}
			
			Parameters.Clear();
			Parameters.AddRange(all);
			
			RecalculateHash();
			
			// Update the standard PSO personal best from our cache of personal
			// best for each configuration
			Particle best;
			if (d_personalBests.TryGetValue(d_hash, out best))
			{
				base.PersonalBest = (Particle)best.Clone();
			}
			else
			{
				base.PersonalBest = null;
			}
		}
		
		public void RecalculateHash()
		{
			List<MutationSet> sets = MutationSets;
			uint multiplier = 1;
			
			d_hash = 0;
			
			for (int i = 0; i < sets.Count; ++i)
			{
				uint activeIndex = d_activeSet[i];
				d_hash += multiplier * activeIndex;
				
				multiplier *= (uint)sets[i].Count;
			}
			
			Data["subswarm"] = d_hash;
		}
		
		public uint Hash
		{
			get
			{
				return d_hash;
			}
		}

		private List<MutationSet> MutationSets
		{
			get
			{
				return ((Optimization.Optimizers.PMPSO.State)base.State).MutationSets;
			}
		}
		
		public override void UpdateBest()
		{
			base.UpdateBest();

			if (!d_personalBests.ContainsKey(d_hash) ||
			    Fitness > d_personalBests[d_hash].Fitness)
			{
				d_personalBests[d_hash] = (Particle)Clone();
			}
		}
		
		public Particle ConfigurationBest
		{
			get
			{
				return (Particle)base.PersonalBest;
			}
		}
		
		public override Optimization.Optimizers.PSO.Particle PersonalBest
		{
			get
			{
				Particle ret = null;
				d_personalBests.TryGetValue(d_hash, out ret);
				return ret;
			}
		}
	}
}
