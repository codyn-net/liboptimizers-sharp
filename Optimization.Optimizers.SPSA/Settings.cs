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

namespace Optimization.Optimizers.SPSA
{
	public class Settings : Optimization.Optimizer.Settings
	{
		[Attributes.Setting("learning-rate", "0.01", Description="Learning rate (ak), expression")]
		public string LearningRate;
		
		[Attributes.Setting("perturbation-rate", "0.01", Description="Perturbation rate (ck), expression")]
		public string PerturbationRate;
	}
}
