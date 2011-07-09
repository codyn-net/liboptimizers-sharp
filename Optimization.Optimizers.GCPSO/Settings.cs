/*
 *  Settings.cs - This file is part of optimizers-sharp
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
using Optimization;
using Optimization.Attributes;

namespace Optimization.Optimizers.GCPSO
{
	public class Settings : Optimization.Settings
	{
		[Setting("sample-size", 1, Description="Random sample size for best particle (fraction of parameter space)")]
		public double SampleSize;
		
		[Setting("success-threshold", 15, Description="Number of successes before increasing sample size")]
		public int SuccessThreshold;
		
		[Setting("failure-threshold", 5, Description="Number of failures before decreasing sample size")]
		public int FailureThreshold;
		
		[Setting("minimum-sample-size", 0, Description="Minimum sample size")]
		public double MinimumSampleSize;
	}
}