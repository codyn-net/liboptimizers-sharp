/*
 *  ADPSO.cs - This file is part of optimizers-sharp
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
using System.Collections.Generic;

namespace Optimization.Optimizers.ADPSO
{
	[Optimization.Attributes.Optimizer(Description="Adaptive Diversity Particle Swarm Optimization")]
	public class ADPSO : Optimization.Optimizers.PSO.PSO
	{
		List<double> d_factors;
		double d_normalization;
		
		// Override 'Configuration' property returning subclassed Settings
		public new Optimization.Optimizers.ADPSO.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimization.Optimizers.ADPSO.Settings;
			}
		}
		
		protected override Settings CreateSettings()
		{
			return new Optimization.Optimizers.ADPSO.Settings();
		}
		
		protected override Solution CreateSolution(uint idx)
		{
			return new Particle(idx, Fitness, State);
		}
		
		private void InitializeFactors()
		{
			if (d_factors != null)
			{
				return;
			}
			
			// Factors is a list of the boundary dimensions for each parameter
			d_factors = new List<double>();
			d_normalization = 0;
			
			foreach (Optimization.Parameter parameter in Parameters)
			{
				double dd = parameter.Boundary.Max - parameter.Boundary.Min;
				d_normalization += dd;

				d_factors.Add(dd);
			}
		}
		
		protected double Distance(Particle a, Particle b)
		{
			double sum = 0;
			
			// Calculate the euclidian distance between two particles as fraction
			// of the parameter space (i.e. for each parameter 0 -> 1)
			for (int i = 0; i < a.Parameters.Count; ++i)
			{
				double dd = a.Parameters[i].Value - b.Parameters[i].Value;
				sum += (dd * dd) / d_factors[i];
			}
			
			return System.Math.Sqrt(sum * d_normalization);
		}
		
		protected bool Collides(Particle a, Particle b)
		{
			double distance = Distance(a, b);
			double match = 2;
			
			if (Configuration.AdaptationConstant > 0)
			{
				match = System.Math.Pow(Configuration.AdaptationConstant, a.Bounced) + 
				        System.Math.Pow(Configuration.AdaptationConstant, b.Bounced);
			}
			
			return distance < match * Configuration.CollisionRadius;
		}
		
		protected void Bounce(Particle p1, Particle p2)
		{
			for (int i = 0; i < p1.Velocity.Count; ++i)
			{
				// Bounce position
				double ov1 = p1.Velocity[i];
				double ov2 = p2.Velocity[i];

				// Reflect velocity
				p1.Velocity[i] = -p1.Velocity[i];
				p2.Velocity[i] = -p2.Velocity[i];

				// And position
				if (Configuration.AdaptationConstant > 0)
				{
					p1.SetPosition(i, p1.Parameters[i].Value - (1 + System.Math.Pow(Configuration.AdaptationConstant, -(int)p1.Bounced)) * ov1);
					p2.SetPosition(i, p2.Parameters[i].Value - (1 + System.Math.Pow(Configuration.AdaptationConstant, -(int)p2.Bounced)) * ov2);
				}
				else
				{
					p1.SetPosition(i, p1.Parameters[i].Value - 2 * ov1);
					p2.SetPosition(i, p2.Parameters[i].Value - 2 * ov2);
				}
			}
			
			p1.IncreaseBounced();
			p2.IncreaseBounced();
		}
		
		public override void Update()
		{
			base.Update();
			
			InitializeFactors();
			
			// Do collision detection
			for (int i = 0; i < Population.Count; ++i)
			{
				Particle p1 = (Particle)Population[i];

				for (int j = i + 1; j < Population.Count; ++j)
				{
					Particle p2 = (Particle)Population[j];

					if (Collides(p1, p2))
					{
						Bounce(p1, p2);
					}
				}
			}
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			base.FromStorage(storage, optimizer);
			
			// This causes the factors to be recalculated next update
			d_factors = null;
		}
	}
}
