/*
 *  Particle.cs - This file is part of optimizers-sharp
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
using System.Collections.Generic;

namespace Optimization.Optimizers.ADPSO
{
	public class Particle : Optimization.Optimizers.PSO.Particle
	{
		private uint d_bounced;

		public Particle(uint id, Fitness fitness, State state) : base (id, fitness, state)
		{
			d_bounced = 0;
			Data["bounced"] = 0;
		}

		public override void Copy(Optimization.Solution other)
		{
			base.Copy(other);
			
			Particle particle = other as Particle;
			particle.d_bounced = d_bounced;
		}

		public override object Clone()
		{
			object ret = new Particle(Id, Fitness.Clone() as Fitness, State);
			
			(ret as Solution).Copy(this);
			return ret;
		}
		
		public uint Bounced
		{
			get
			{
				return d_bounced;
			}
			set
			{
				d_bounced = value;
			}
		}
		
		public void IncreaseBounced()
		{
			++d_bounced;
			Data["bounced"] = d_bounced;
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer, Storage.Records.Solution solution)
		{
			base.FromStorage(storage, optimizer, solution);
			
			d_bounced = uint.Parse((string)Data["bounced"]);
		}
	}
}
