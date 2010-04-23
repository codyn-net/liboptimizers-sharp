
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
		
		public Optimization.Optimizers.SPSA.Settings.BoundaryConditionType BoundaryCondition
		{
			get
			{
				return ((Optimization.Optimizers.SPSA.Settings)State.Settings).BoundaryCondition;
			}
		}
		
		public void Generate(double perturbationRate)
		{
			d_solutions[0].Parameters = Parameters;
			d_solutions[1].Parameters = Parameters;
			
			d_deltas = new double[Parameters.Count];
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				d_deltas[i] = GenerateDelta();
				
				double theta = d_deltas[i] * perturbationRate;

				d_solutions[0].Parameters[i].Value += theta;
				d_solutions[1].Parameters[i].Value -= theta;
				
				if (BoundaryCondition == Optimization.Optimizers.SPSA.Settings.BoundaryConditionType.StickAll)
				{
					Boundary boundary = Parameters[i].Boundary;

					d_solutions[0].Parameters[i].Value = System.Math.Max(System.Math.Min(d_solutions[0].Parameters[i].Value, boundary.Max), boundary.Min);
					d_solutions[1].Parameters[i].Value = System.Math.Max(System.Math.Min(d_solutions[1].Parameters[i].Value, boundary.Max), boundary.Min);
				}
			}
		}
		
		public void Update(double perturbationRate, double learningRate, double epsilon)
		{
			// Note: we do gradient _ascend_ and not descend in this framework
			double constant = (d_solutions[1].Fitness.Value - d_solutions[0].Fitness.Value) / (2 * perturbationRate);
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				Boundary boundary = Parameters[i].Boundary;
				double maxStep = epsilon * (boundary.Max - boundary.Min);

				double dtheta = learningRate * constant * d_deltas[i];
				double newValue = Parameters[i].Value - System.Math.Sign(dtheta) * System.Math.Min(System.Math.Abs(dtheta), maxStep);
				
				if (BoundaryCondition == Optimization.Optimizers.SPSA.Settings.BoundaryConditionType.StickAll ||
				    BoundaryCondition == Optimization.Optimizers.SPSA.Settings.BoundaryConditionType.StickResult)
				{
					Parameters[i].Value = System.Math.Max(System.Math.Min(newValue, boundary.Max), boundary.Min);
				}
			}
			
			Generate(perturbationRate);
		}
	}
}
