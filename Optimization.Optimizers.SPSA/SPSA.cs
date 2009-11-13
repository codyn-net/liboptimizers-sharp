/*
 *  SPSA.cs - This file is part of optipso
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
using Optimization;
using System.Collections.Generic;

namespace Optimization.Optimizers.SPSA
{	
	[Attributes.Optimizer(Description="Simultaneous Perturbation Stochastic Approximation")]
	public class SPSA : Optimizer
	{
		Optimization.Math.Expression d_learningRate;
		Optimization.Math.Expression d_perturbationRate;
		List<Optimizers.SPSA.Solution> d_solutions;
		Dictionary<string, object> d_rateContext;
		
		public SPSA()
		{
			d_learningRate = new Optimization.Math.Expression();
			d_perturbationRate = new Optimization.Math.Expression();
			
			d_solutions = new List<Optimizers.SPSA.Solution>();
			d_rateContext = new Dictionary<string, object>();
			
			d_rateContext["k"] = 0;
		}
		
		public new Optimizers.SPSA.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimizers.SPSA.Settings;
			}
		}

		public override void Initialize ()
		{
			d_learningRate.Parse(Configuration.LearningRate);
			d_perturbationRate.Parse(Configuration.PerturbationRate);

			base.Initialize();
		}
		
		private double PerturbationRate
		{
			get
			{
				return d_perturbationRate.Evaluate(d_rateContext);
			}
		}
		
		private double LearningRate
		{
			get
			{
				return d_learningRate.Evaluate(d_rateContext);
			}
		}
		
		public override void InitializePopulation()
		{
			// For each of our solutions, we generate the two gradient solutions
			// and put it in the population
			double perturbationRate = PerturbationRate;

			for (uint i = 0; i < Configuration.PopulationSize; ++i)
			{
				Solution solution = CreateSolution(i) as Solution;
				solution.Parameters = Parameters;
				
				solution.Reset();
				
				d_solutions.Add(solution);
				Population.AddRange(solution.Generate(perturbationRate));
			}
		}
		
		public override void Update()
		{
			foreach (Solution solution in d_solutions)
			{
				double perturbationRate = PerturbationRate;

				solution.Update(perturbationRate, LearningRate);
				Optimization.Solution[] generated = solution.Generate(perturbationRate);
				
				Population[(int)solution.Id * 2] = generated[0];
				Population[(int)solution.Id * 2 + 1] = generated[1];
			}
		}
		
		protected override Optimization.Optimizer.Settings CreateSettings()
		{
			return new Optimizers.SPSA.Settings();
		}
		
		protected override Optimization.Solution CreateSolution(uint idx)
		{
			return new Optimizers.SPSA.Solution(idx, Fitness, State);
		}
		
		protected override void IncrementIteration()
		{
			base.IncrementIteration();
			d_rateContext["k"] = CurrentIteration;
		}
	}
}
