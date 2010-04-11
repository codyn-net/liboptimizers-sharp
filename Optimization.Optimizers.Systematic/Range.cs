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

		private Boundary d_boundary;

		private NumericSetting d_step;
		private NumericSetting d_steps;
		
		public Range(string name, Boundary boundary)
		{
			d_name = name;
			d_boundary = boundary;
			
			d_step = new NumericSetting();
			d_step.Value = (boundary.Max - boundary.Min) / 10;
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public Boundary Boundary
		{
			get
			{
				return d_boundary;
			}
		}
		
		public NumericSetting Step
		{
			get
			{
				return d_step;
			}
			set
			{
				d_step = value;
			}
		}
		
		public NumericSetting Steps
		{
			get
			{
				return d_steps;
			}
			set
			{
				d_steps = value;
			}
		}
		
		public IEnumerator<double> GetEnumerator()
		{
			double i = Boundary.Min;
			
			while ((Boundary.Min < Boundary.Max && i - 0.5 * d_step.Value <= Boundary.Max) || (Boundary.Min > Boundary.Max && i - 0.5 * d_step.Value >= Boundary.Max))
			{
				yield return i;
				i += d_step.Value;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public double[] ToArray()
		{
			List<double> ret = new List<double>();
			
			foreach (double d in this)
			{
				ret.Add(d);
			}
			
			return ret.ToArray();
		}
	}
}
