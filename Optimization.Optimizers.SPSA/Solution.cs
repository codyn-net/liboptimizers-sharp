
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
		
		private double GenerateDelta()
		{
			return 2 * System.Math.Round(State.Random.NextDouble()) - 1;
		}
		
		public Optimization.Solution[] Generate(double gradientRate)
		{
			d_solutions[0].Parameters = Parameters;
			d_solutions[1].Parameters = Parameters;
			
			d_deltas = new double[Parameters.Count];
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				Parameter parameter = Parameters[i];
				d_deltas[i] = GenerateDelta();
				
				double thetaPlus = parameter.Value + d_deltas[i] * gradientRate;
				double thetaMin = parameter.Value - d_deltas[i] * gradientRate;
				
				d_solutions[0].Parameters[i].Value = thetaPlus;
				d_solutions[1].Parameters[i].Value = thetaMin;
			}
			
			return d_solutions;
		}
		
		public void Update(double gradientRate, double learningRate)
		{
			double constant = (d_solutions[0].Fitness.Value - d_solutions[1].Fitness.Value) / (2 * gradientRate);
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				Parameters[i].Value -= learningRate * constant * d_deltas[i];
			}
		}
	}
}
