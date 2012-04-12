/*
 *  DPSO.cs - This file is part of optimizers-sharp
 *
 *  Copyright (C) 2011 - Jesse van den Kieboom
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

namespace Optimization.Optimizers.DPSO
{
	[Optimization.Attributes.Extension(Description="Dispersion Particle Swarm Optimization", AppliesTo = new Type[] {typeof(PSO.PSO)})]
	public class DPSO : Optimization.Extension, PSO.IPSOExtension
	{
		private double d_sampleSize;
		private int d_failures;
		private Solution d_lastBest;
		
		public DPSO(Job job) : base(job)
		{
		}
			
		public override void Initialize()
		{
			base.Initialize();
			
			CreateSampleSizeTable();
		}
		
		private void CreateSampleSizeTable()
		{
			Storage.Storage storage = Job.Optimizer.Storage;

			storage.Query("DROP TABLE IF EXISTS `dpso_samplesize`");
			storage.Query("CREATE TABLE `dpso_samplesize` (`id` INTEGER PRIMARY KEY, `iteration` INT, `failures` INT, `sample_size` DOUBLE)");
		}

		// Override 'Configuration' property returning subclassed Settings
		public new Optimization.Optimizers.DPSO.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimization.Optimizers.DPSO.Settings;
			}
		}
		
		protected override Optimization.Settings CreateSettings()
		{
			return new Optimization.Optimizers.DPSO.Settings();
		}
		
		public PSO.State.VelocityUpdateType VelocityUpdateComponents(PSO.Particle particle)
		{
			if (particle.Id != Job.Optimizer.Best.Id)
			{
				// Use default update when not the best
				return PSO.State.VelocityUpdateType.Default;
			}
			else
			{
				// Keep the momentum
				return PSO.State.VelocityUpdateType.DisableGlobal | PSO.State.VelocityUpdateType.DisableLocal;
			}
		}
		
		public void ValidateVelocityUpdate(PSO.Particle particle, double[] velocityUpdate)
		{
			// Don't need to do anything special here
		}
		
		public bool UpdateParticleBest(PSO.Particle particle)
		{
			particle.Data["dpso::multiplier"] = 1.0;

			if (particle.PersonalBest == null || particle.Fitness > particle.PersonalBest.Fitness)
			{
				particle.Data["dpso::failures"] = 0;
				particle.Data["dpso::multiplier"] = 1 / Configuration.EnergyInjectionFactor;
			}
			else
			{
				int failures = (int)(particle.Data["dpso::failures"]) + 1;
				particle.Data["dpso::failures"] = failures;
				
				if (failures > Configuration.FailureThreshold)
				{
					particle.Data["dpso::multiplier"] = Configuration.EnergyInjectionFactor;
					particle.Data["dpso::failures"] = 0;
				}
			}			

			return false;
		}
		
		public double CalculateVelocityUpdate(PSO.Particle particle, PSO.Particle best, int i)
		{
			if (particle.Id != best.Id)
			{
				double multiplier = (double)particle.Data["dpso::multiplier"];
				double up = particle.CalculateVelocityUpdate(best, i);
				
				return (particle.Velocity[i] + up) * multiplier - up - particle.Velocity[i];
			}
			
			double ret = 0;
			
			// Special velocity update, first reset the position
			ret -= particle.Parameters[i].Value;
			
			// Then move the particle to its best position
			ret += particle.PersonalBest.Parameters[i].Value;
			
			// Finally, add the random sample search
			Boundary boundary = particle.Parameters[i].Boundary;
			ret += d_sampleSize * (boundary.Max - boundary.Min) * (1 - 2 * particle.State.Random.NextDouble());
			
			return ret;
		}
		
		public override void Next()
		{
			if (Job.Optimizer.Best == null || d_lastBest == null || Job.Optimizer.Best.Fitness > d_lastBest.Fitness)
			{
				d_failures = 0;
				d_sampleSize = Configuration.MinimumSampleSize;
			}
			else
			{
				++d_failures;
			}
			
			if (d_failures > Configuration.FailureThreshold)
			{
				d_sampleSize *= Configuration.SampleSizeIncreaseFactor;
				d_failures = 0;
			}
			
			d_sampleSize = System.Math.Max(Configuration.MinimumSampleSize, System.Math.Min(Configuration.MaximumSampleSize, d_sampleSize));
			
			Job.Optimizer.Storage.Query("INSERT INTO `dpso_samplesize` (`iteration`, `failures`, `sample_size`) VALUES (@0, @1, @2)",
			                            Job.Optimizer.CurrentIteration,
			                            d_failures,
			                            d_sampleSize);
			
			d_lastBest = Job.Optimizer.Best;
		}
		
		public PSO.Particle GetUpdateBest(PSO.Particle particle)
		{
			return null;
		}
	}
}
