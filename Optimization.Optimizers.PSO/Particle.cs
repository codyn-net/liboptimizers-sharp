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
		private List<double> d_velocity;
		private Particle d_personalBest;
		
		public Particle(uint id, Fitness fitness, Optimization.State state) : base (id, fitness, state)
		{
			d_velocity = new List<double>();
			d_personalBest = null;
		}

		public override void Copy(Optimization.Solution other)
		{
			base.Copy(other);
			
			Particle particle = other as Particle;
			d_velocity = new List<double>(particle.d_velocity);
			d_personalBest = particle.d_personalBest;
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
		
		public new State State
		{
			get
			{
				return (State)base.State;
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
			for (int i = 0; i < d_velocity.Count; ++i)
			{
				Data[String.Format("velocity_{0}", i)] = d_velocity[i].ToString();
			}
		}
		
		private void VelocityFromData()
		{
			d_velocity.Clear();

			for (int i = 0; i < Parameters.Count; ++i)
			{
				 d_velocity.Add(Double.Parse((string)Data[String.Format("velocity_{0}", i)]));
			}
		}
		
		public virtual void UpdateBest()
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
			for (int i = 0; i < 10; ++i)
			{
				if (!(newpos > boundary.Max || newpos < boundary.Min))
				{
					break;
				}

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
			
			if (newpos > boundary.Max)
			{
				newpos = boundary.Max;
			}
			else if (newpos < boundary.Min)
			{
				newpos = boundary.Min;
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
		
		public virtual double CalculateVelocityUpdate(Particle gbest, int i)
		{
			PSONS.Settings settings = Configuration;
			Parameter parameter = Parameters[i];
			
			double r1 = State.Random.Range(0, 1);
			double r2 = State.Random.Range(0, 1);

			double pg = 0;
			double pl = 0;
			double momentum = 0;
			
			// Global best difference
			if (gbest != null && (State.VelocityUpdateComponents & State.VelocityUpdateType.DisableGlobal) == 0)
			{
				pg = gbest.Parameters[i].Value - parameter.Value;
			}
			
			// Local best difference
			if (d_personalBest != null && (int)(State.VelocityUpdateComponents & State.VelocityUpdateType.DisableLocal) == 0)
			{
				pl = d_personalBest.Parameters[i].Value - parameter.Value;
			}
			
			if ((int)(State.VelocityUpdateComponents & State.VelocityUpdateType.DisableMomentum) == 0)
			{
				momentum = d_velocity[i];
			}
			
			// PSO velocity update rule
			return settings.Constriction * (momentum +
			                                r1 * settings.CognitiveFactor * pl +
			                                r2 * settings.SocialFactor * pg);
		}
		
		public virtual void Update(double[] velocityUpdate)
		{
			// Main update function, applies the PSO update rule to update particle velocity
			// and position.
			for (int i = 0; i < Parameters.Count; ++i)
			{
				d_velocity[i] = velocityUpdate[i];

				// Limit the maximum velocity according to the MaxVelocity setting
				LimitVelocity(i);

				// Update the particle position
				UpdatePosition(i);
			}
			
			UpdateVelocityData();
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
		
		public double[] Velocity
		{
			get
			{
				return d_velocity.ToArray();
			}
			set
			{
				d_velocity.Clear();
				d_velocity.AddRange(value);
				
				UpdateVelocityData();
			}
		}
		
		public void SetVelocity(int idx, double vel)
		{
			d_velocity[idx] = vel;
			UpdateVelocityData();
		}
		
		public virtual Particle PersonalBest
		{
			get
			{
				return d_personalBest;
			}
			set
			{
				d_personalBest = value;
			}
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer, Storage.Records.Solution solution)
		{
			base.FromStorage(storage, optimizer, solution);

			VelocityFromData();
		}
	}
}
