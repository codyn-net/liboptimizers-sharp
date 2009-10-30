/*
 *  PSO.cs - This file is part of optimizers-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
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
using System.Reflection;
using Optimization;
using System.Collections.Generic;

namespace Optimization.Optimizers.PSO
{
	[Attributes.Optimizer(Description="Standard Particle Swarm Optimization")]
	public class PSO : Optimizer
	{
		// Map to contain per parameter best particles
		Dictionary<string, Particle> d_bests;
		
		// Override 'Configuration' property returning subclassed Settings
		public new PSO.Settings Configuration
		{
			get
			{
				return base.Configuration as PSO.Settings;
			}
		}
		
		public PSO()
		{
			d_bests = new Dictionary<string, Particle>();
		}
		
		protected override Settings CreateSettings ()
		{
			return new Optimization.Optimizers.PSO.Settings();
		}
		
		protected override Solution CreateSolution(uint idx)
		{
			return new Particle(idx, Fitness, State);
		}
		
		public override void Update(Solution solution)
		{
			// Update is implemented on the particle
			(solution as Particle).Update(d_bests);
		}
		
		public override void InitializePopulation()
		{
			base.InitializePopulation();
			d_bests.Clear();
		}
		
		protected override void UpdateBest()
		{
			base.UpdateBest();
			
			// Update per parameter bests
			foreach (Solution solution in Population)
			{
				foreach (Parameter parameter in solution.Parameters)
				{
					if (!d_bests.ContainsKey(parameter.Name) ||
					     solution.Fitness > d_bests[parameter.Name].Fitness)
					{
						d_bests[parameter.Name] = solution.Clone() as Particle;
					}
				}
			}
		}
	}
}
