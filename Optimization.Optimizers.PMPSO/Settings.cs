/*
 *  Settings.cs - This file is part of pmpso-sharp
 *
 *  Copyright (C) 2010 - Jesse van den Kieboom
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

namespace Optimization.Optimizers.PMPSO
{
	public class Settings : Optimization.Optimizers.PSO.Settings
	{
		[Setting("mutation-probability", "0", Description="Global mutation probability expression, use 'k' for iteration")]
		public string MutationProbability;
		
		[Setting("social-mutation-probability", "0", Description="Social mutation probability expression, use 'k' for iteration")]
		public string SocialMutationProbability;
		
		[Setting("cognitive-mutation-probability", "0", Description="Cognitive mutation probability expression, use 'k' for iteration")]
		public string CognitiveMutationProbability;
		
		[Setting("fused-particles", 2, Description="The number of particles that are fused together in terms of mutations")]
		public int FusedParticles;
		
		[Setting("min-neighborhoods", 1, Description="The minimum number of neighborhoods")]
		public int MinNeighborhoods;
		
		[Setting("initial-neighborhoods", 0, Description="The initial number of neighborhoods (0 for having 2 fusions per neighborhood)")]
		public int InitialNeighborhoods;
		
		[Setting("merge-interval", 0, Description="Neighborhood merge interval (0 for automatic linear merging)")]
		public double MergeInterval;
	}
}