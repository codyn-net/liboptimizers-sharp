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
			for (double i = d_min; i <= d_max; i += d_step)
			{
				yield return i;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
