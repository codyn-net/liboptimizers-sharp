/*
 *  Particle.cs - This file is part of optipso
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
		Dictionary<string, Particle> d_bests;
		
		public Particle(uint id, Fitness fitness, State state) : base (id, fitness, state)
		{
			d_velocity = new List<double>();
			d_bests = new Dictionary<string, Particle>();
		}

		public override void Copy(Optimization.Solution other)
		{
			base.Copy(other);
			
			Particle particle = other as Particle;
			particle.d_velocity.AddRange(d_velocity);
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
			List<string> data = new List<string>();
			
			foreach (double vel in d_velocity)
			{
				data.Add(vel.ToString());
			}
			
			// Set velocity string in velocity column data
			Data["velocity"] = String.Join(",", data.ToArray());
		}
		
		private void UpdateBest()
		{
			// For each parameter, update the best solution
			foreach (Parameter parameter in Parameters)
			{
				if (!d_bests.ContainsKey(parameter.Name) || Fitness > d_bests[parameter.Name].Fitness)
				{
					d_bests[parameter.Name] = Clone() as Particle;
				}
			}
		}
		
		private void UpdatePosition(int idx)
		{
			// Update the particle position using euler integration with the new velocity.
			// This correctly handles the boundary conditions as set in the PSO optimizer
			// settings.

			Parameter parameter = Parameters[idx];
			Boundary boundary = parameter.Boundary;
			double newpos = parameter.Value + d_velocity[idx];
			
			// Update position to 'newpos' given a certain boundary condition
			if (newpos > boundary.Max || newpos < boundary.Min)
			{
				switch (Configuration.BoundaryCondition)
				{
					// No boundary condition
					case PSONS.Settings.BoundaryConditionType.None:
						parameter.Value = newpos;
					break;
					// Sticky boundary condition, clip to boundary
					case PSONS.Settings.BoundaryConditionType.Stick:
						parameter.Value = System.Math.Max(System.Math.Min(boundary.Max, newpos), boundary.Min);
					break;
					// Bounce boundary condition, reflect and damp
					case PSONS.Settings.BoundaryConditionType.Bounce:
						if (newpos > boundary.Max)
						{
							parameter.Value = boundary.Max - Configuration.BoundaryDamping * (newpos - boundary.Max);
						}
						else
						{
							parameter.Value = boundary.Min + Configuration.BoundaryDamping * (newpos - boundary.Min);
						}
						
						d_velocity[idx] = Configuration.BoundaryDamping * -d_velocity[idx];
					break;
				}
			}
			else
			{
				// Within boundaries simply update position
				parameter.Value = newpos;
			}
		}
		
		public void Update(Dictionary<string, Particle> bests)
		{
			// Main update function, applies the PSO update rule to update particle velocity
			// and position. This update function works with bests on a per parameter basis
			// to allow optimization with a dynamical number of parameters
			UpdateBest();
			
			PSONS.Settings settings = Configuration;

			for (int i = 0; i < Parameters.Count; ++i)
			{
				Parameter parameter = Parameters[i];

				double r1 = State.Random.Range(0, 1);
				double r2 = State.Random.Range(0, 1);
				double pg = 0;
				double pl = 0;
				
				// Calculate per parameter global best
				if (bests.ContainsKey(parameter.Name))
				{
					Parameter best = bests[parameter.Name].Parameters[i];
					pg = best.Value - parameter.Value;					
				}
				
				// Calculate per parameter local best
				if (d_bests.ContainsKey(parameter.Name))
				{
					Parameter best = d_bests[parameter.Name].Parameters[i];
					pl = best.Value - parameter.Value;
				}
				
				// PSO velocity update rule
				d_velocity[i] = settings.Constriction * (d_velocity[i] + 
				                                         r1 * settings.CognitiveFactor * pl +
				                                         r2 * settings.SocialFactor * pg);

				// Limit the maximum velocity according to the MaxVelocity setting
				double maxvel = settings.MaxVelocity * (parameter.Boundary.Max - parameter.Boundary.Min);
				
				if (maxvel > 0 && d_velocity[i] > maxvel)
				{
					d_velocity[i] = maxvel;
				}
				
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
	}
}
