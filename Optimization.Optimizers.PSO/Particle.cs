/*
 *  Particle.cs - This file is part of optimizers-sharp
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
using Optimization;
using PSONS = Optimization.Optimizers.PSO;

namespace Optimization.Optimizers.PSO
{
	public class Particle : Solution
	{
		List<double> d_velocity;
		Particle d_personalBest;
		
		public Particle(uint id, Fitness fitness, State state) : base (id, fitness, state)
		{
			d_velocity = new List<double>();
			d_personalBest = null;
		}

		public override void Copy(Optimization.Solution other)
		{
			base.Copy(other);
			
			Particle particle = other as Particle;
			particle.d_velocity.AddRange(d_velocity);
			particle.d_personalBest = d_personalBest;
		}

		public override object Clone()
		{
			object ret = new Particle(Id, Fitness.Clone() as Fitness, State);
			
			(ret as Solution).Copy(this);
			return ret;
		}

		// Convenient cast for settings to pso settings
		private PSONS.Settings Configuration
		{
			get
			{
				return State.Settings as PSONS.Settings;
			}
		}
		
		public override void Reset()
		{
			base.Reset();
			
			// In the solution reset, initialize the particles velocity vector
			// randomly in the parameter space.
			d_velocity.Clear();
			
			// Add velocity to the velocity vector for each parameter, initialized within
			foreach (Parameter parameter in Parameters)
			{
				AddVelocity(parameter);
			}
			
			// Update the velocity data string which will be saved in the database
			UpdateVelocityData();
		}
		
		private void UpdateVelocityData()
		{
			// Create string of velocity data to save in the database 'velocity' column
			for (int i = 0; i < d_velocity.Count; ++i)
			{
				Data[String.Format("v{0}", i)] = d_velocity[i];
			}
		}
		
		private void UpdateBest()
		{
			if (d_personalBest == null || Fitness > d_personalBest.Fitness)
			{
				d_personalBest = (Particle)Clone();
			}
		}
		
		public void SetPosition(int idx, double newpos)
		{
			Parameter parameter = Parameters[idx];
			Boundary boundary = parameter.Boundary;

			// Update position to 'newpos' given a certain boundary condition
			while (newpos > boundary.Max || newpos < boundary.Min)
			{
				switch (Configuration.BoundaryCondition)
				{
					// No boundary condition
					case PSONS.Settings.BoundaryConditionType.None:
						parameter.Value = newpos;
						return;
					// Sticky boundary condition, clip to boundary
					case PSONS.Settings.BoundaryConditionType.Stick:
						parameter.Value = System.Math.Max(System.Math.Min(boundary.Max, newpos), boundary.Min);
						return;
					// Bounce boundary condition, reflect and damp
					case PSONS.Settings.BoundaryConditionType.Bounce:
						if (newpos > boundary.Max)
						{
							newpos = boundary.Max - Configuration.BoundaryDamping * (newpos - boundary.Max);
						}
						else
						{
							newpos = boundary.Min + Configuration.BoundaryDamping * (boundary.Min - newpos);
						}
						
						d_velocity[idx] = Configuration.BoundaryDamping * -d_velocity[idx];
					break;
				}
			}
			
			// Update actual position
			parameter.Value = newpos;
		}
		
		private void UpdatePosition(int idx)
		{
			// Update the particle position using euler integration with the new velocity.
			// This correctly handles the boundary conditions as set in the PSO optimizer
			// settings.
			Parameter parameter = Parameters[idx];
			double newpos = parameter.Value + d_velocity[idx];

			SetPosition(idx, newpos);
		}
		
		private void LimitVelocity(int idx)
		{
			PSONS.Settings settings = Configuration;
			double maxvel = settings.MaxVelocity * (Parameters[idx].Boundary.Max - Parameters[idx].Boundary.Min);
				
			if (maxvel > 0 && System.Math.Abs(d_velocity[idx]) > maxvel)
			{
				d_velocity[idx] = d_velocity[idx] > 0 ? maxvel : -maxvel;
			}
		}
		
		public void Update(Particle gbest)
		{
			// Main update function, applies the PSO update rule to update particle velocity
			// and position.
			UpdateBest();
			
			PSONS.Settings settings = Configuration;

			for (int i = 0; i < Parameters.Count; ++i)
			{
				Parameter parameter = Parameters[i];

				double r1 = State.Random.Range(0, 1);
				double r2 = State.Random.Range(0, 1);

				double pg = 0;
				double pl = 0;
				
				// Global best difference
				pg = gbest.Parameters[i].Value - parameter.Value;
				
				// Local best difference
				pl = d_personalBest.Parameters[i].Value - parameter.Value;
				
				// PSO velocity update rule
				d_velocity[i] = settings.Constriction * (d_velocity[i] + 
				                                         r1 * settings.CognitiveFactor * pl +
				                                         r2 * settings.SocialFactor * pg);

				// Limit the maximum velocity according to the MaxVelocity setting
				LimitVelocity(i);

				// Update the particle position
				UpdatePosition(i);
			}
		}

		public override void Add(Parameter parameter)
		{
			base.Add(parameter);
			
			// When a new parameter is added, also add a velocity
			AddVelocity(parameter);
		}

		public override void Remove(string name)
		{
			// Remove velocity
			for (int i = 0; i < Parameters.Count; ++i)
			{
				if (Parameters[i].Name == name)
				{
					d_velocity.RemoveAt(i);
					break;
				}
			}
			
			base.Remove(name);
		}
		
		private void AddVelocity(Parameter parameter)
		{
			// Calculate maximum velocity factor
			double factor = Configuration.MaxVelocity;
			
			if (factor <= 0)
			{
				factor = 1;
			}

			// We assume here that we can just append the velocity
			double span = parameter.Boundary.Max - parameter.Boundary.Min;
			d_velocity.Add(State.Random.Range(-span, span) * factor);
		}
		
		public List<double> Velocity
		{
			get
			{
				return d_velocity;
			}
		}
	}
}
