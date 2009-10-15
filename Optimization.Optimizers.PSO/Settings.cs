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

namespace Optimization.Optimizers.PSO
{
	public class Settings : Optimizer.Settings
	{
		public enum BoundaryConditionType
		{
			None,
			Stick,
			Bounce
		}
		
		[Setting("max-velocity", -1.0, Description="Maximum particle velocity (in fraction of parameter space)")]
		public double MaxVelocity;
		
		[Setting("coginitive-factor", 1.49455, Description="Cognitive factor constant")]
		public double CognitiveFactor;
		
		[Setting("social-factor", 1.49455, Description="Social factor constant")]
		public double SocialFactor;
		
		[Setting("constriction", 0.729, Description="Velocity update constriction")]
		public double Constriction;
		
		[Setting("boundary-condition", BoundaryConditionType.Bounce, Description="Boundary condition")]
		public BoundaryConditionType BoundaryCondition;

		[Setting("boundary-damping", 0.95, Description="Boundary velocity damping when condition is Bounce")]
		public double BoundaryDamping;
	}
}
