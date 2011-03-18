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
		private bool d_loadingFromStorage;
		private List<IPSOExtension> d_extensions;

		// Override 'Configuration' property returning subclassed Settings
		public new PSO.Settings Configuration
		{
			get
			{
				return (PSO.Settings)base.Configuration;
			}
		}
		
		protected override Optimization.State CreateState()
		{
			return new State(Configuration);
		}
		
		public PSO()
		{
			d_loadingFromStorage = false;
			d_extensions = new List<IPSOExtension>();
		}
		
		protected override void Setup()
		{
			base.Setup();
			
			foreach (Extension ext in Extensions)
			{
				if (ext is IPSOExtension)
				{
					d_extensions.Add((IPSOExtension)ext);
				}
			}
		}
		
		protected override Settings CreateSettings()
		{
			// Create pso settings
			return new Optimization.Optimizers.PSO.Settings();
		}
		
		public override Solution CreateSolution(uint idx)
		{
			// Create new particle
			return new Particle(idx, Fitness, State);
		}
		
		protected override void UpdateBest()
		{
			foreach (Solution sol in Population)
			{
				((Particle)sol).UpdateBest();
			}

			base.UpdateBest();
		}
		
		protected virtual Particle GetUpdateBest(Particle particle)
		{
			return (Particle)Best;
		}
		
		public virtual double CalculateVelocityUpdate(Particle particle, Particle best, int i)
		{
			double ret = particle.CalculateVelocityUpdate(best, i);
			
			foreach (IPSOExtension ext in d_extensions)
			{
				ret += ext.CalculateVelocityUpdate(particle, best, i);
			}
			
			return ret;
		}
		
		public new State State
		{
			get
			{
				return (State)base.State;
			}
		}
		
		private void CalculateVelocityUpdateComponents(Particle particle)
		{
			State.VelocityUpdateComponents = State.VelocityUpdateType.Default;
			
			foreach (IPSOExtension ext in d_extensions)
			{
				State.VelocityUpdateComponents |= ext.VelocityUpdateComponents(particle);
			}
		}
		
		private void ValidateVelocityUpdate(Particle particle, double[] velocityUpdate)
		{
			foreach (IPSOExtension ext in d_extensions)
			{
				ext.ValidateVelocityUpdate(particle, velocityUpdate);
			}
		}
		
		public override void Update(Solution solution)
		{
			Particle particle = (Particle)solution;
			double[] velocityUpdate = new double[solution.Parameters.Count];
			CalculateVelocityUpdateComponents(particle);

			Particle best = GetUpdateBest(particle);
			
			for (int i = 0; i < velocityUpdate.Length; ++i)
			{
				velocityUpdate[i] = CalculateVelocityUpdate(particle, best, i);
			}
			
			ValidateVelocityUpdate(particle, velocityUpdate);

			// Update is implemented on the particle
			particle.Update(velocityUpdate);
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer, Storage.Records.Solution solution, Optimization.Solution sol)
		{
			base.FromStorage(storage, optimizer, solution, sol);
			
			if (d_loadingFromStorage)
			{
				return;
			}
			
			// Add some protection because we don't want to recursively load the best particle
			d_loadingFromStorage = true;

			Storage.Records.Solution best = storage.ReadSolution(-1, (int)sol.Id);
			Particle particle = (Particle)sol;
			
			if (best != null)
			{
				Solution b = CreateSolution((uint)best.Index);
				FromStorage(storage, optimizer, best, b);
				
				particle.PersonalBest = (Particle)b;
			}
			else
			{
				particle.PersonalBest = null;
			}
			
			d_loadingFromStorage = false;
		}
	}
}
