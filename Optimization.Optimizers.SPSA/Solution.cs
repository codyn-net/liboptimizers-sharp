
/*
 *  Solution.cs - This file is part of optimizers-sharp
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
	public class Solution : Optimization.Solution
	{
		private Optimization.Solution[] d_solutions;
		private double[] d_deltas;
		
		public Solution(uint id, Fitness fitness, State state) : base (id, fitness, state)
		{
			d_solutions = new Optimization.Solution[2] {
				new Optimization.Solution(Id * 2, Fitness, State),
				new Optimization.Solution(Id * 2 + 1, Fitness, State)
			};
		}

		
		public Optimization.Solution[] Solutions
		{
			get
			{
				return d_solutions;
			}
			set
			{
				d_solutions = value;
			}
		}

		public void Generate(double perturbationRate)
		{
			Parameter[] p0;
			Parameter[] p1;
			
			Algorithm.Generate(State, (Optimization.Optimizers.SPSA.Settings)State.Settings, perturbationRate, Parameters, out p0, out p1, out d_deltas);

			d_solutions[0].Parameters = new List<Parameter>(p0);
			d_solutions[1].Parameters = new List<Parameter>(p1);
		}
		
		public void Update(double perturbationRate, double learningRate, double epsilon)
		{
			Settings.BoundaryConditionType boundaryCondition = ((Settings)State.Settings).BoundaryCondition;
			double[] update = Algorithm.Update(Parameters, d_solutions[0].Fitness.Value, d_solutions[1].Fitness.Value, perturbationRate, learningRate, epsilon, d_deltas);

			for (int i = 0; i < update.Length; ++i)
			{
				Parameter parameter = Parameters[i];
				Boundary boundary = parameter.Boundary;
				
				double newValue = parameter.Value - update[i];
				
				if (boundaryCondition == Optimization.Optimizers.SPSA.Settings.BoundaryConditionType.StickAll ||
				    boundaryCondition == Optimization.Optimizers.SPSA.Settings.BoundaryConditionType.StickResult)
				{
					newValue = System.Math.Max(System.Math.Min(newValue, boundary.Max), boundary.Min);
				}

				Parameters[i].Value = newValue;
			}
			
			Generate(perturbationRate);
		}
	}
}
