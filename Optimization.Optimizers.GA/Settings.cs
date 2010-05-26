/*
 *  Settings.cs - This file is part of optimizers-sharp
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
using Optimization.Attributes;

namespace Optimization.Optimizers.GA
{
	public class Settings : Optimizer.Settings
	{
		public enum SelectionType
		{
			Tournament,
			RouletteWheel
		}
		
		[Optimization.Attributes.Setting("selection", SelectionType.Tournament, Description="Type of selection of individuals to make a new population.")]
		public SelectionType Selection;
		
		[Optimization.Attributes.Setting("tournament-size", "3", Description="Tournament size when selection is tournament. You can use the variable 'k' indicating the current iteration.")]
		public string TournamentSize;
		
		[Optimization.Attributes.Setting("tournament-probability", "1", Description="Probability with which an individual is selected from a tournament (p * (1 - p)^i). You can use the variable 'k' indicating the current iteration.")]
		public string TournamentProbability;
		
		[Optimization.Attributes.Setting("mutation-probability", "0.1", Description="Probability of mutation. You can use the variable 'k' indicating the current iteration.")]
		public string MutationProbability;
		
		[Optimization.Attributes.Setting("mutation-rate", "0.1", Description="Mutation rate as factor of the parameter space. You can use the variable 'k' indicating the current iteration.")]
		public string MutationRate;
		
		[Optimization.Attributes.Setting("crossover-probability", "0.1", Description="Probability of cross-over. You can use the variable 'k' indicating the current iteration.")]
		public string CrossoverProbability;

		[Optimization.Attributes.Setting("discrete", "false", Description="Whether all parameters should be considered discrete by default.")]
		public bool Discrete;
	}
}
