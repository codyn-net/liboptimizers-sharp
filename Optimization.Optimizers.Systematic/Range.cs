/*
 *  Range.cs - This file is part of optimizers-sharp
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
using System.Collections;

namespace Optimization.Optimizers.Systematic
{
	public class Range : IEnumerable<double>
	{
		private string d_name;
		private double d_min;
		private double d_step;
		private double d_max;
		
		public Range(string name, double min, double step, double max)
		{
			d_name = name;
			d_min = min;
			d_step = step;
			d_max = max;
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public double Min
		{
			get
			{
				return d_min;
			}
		}
		
		public double Max
		{
			get
			{
				return d_max;
			}
		}
		
		public double Step
		{
			get
			{
				return d_step;
			}
		}
		
		public IEnumerator<double> GetEnumerator()
		{
			double i = d_min;
			
			while ((d_min < d_max && i <= d_max) || (d_min > d_max && i >= d_max))
			{
				yield return i;
				i += d_step;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
