/*
 *  Systematic.cs - This file is part of optimizers-sharp
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
using System.Xml;

namespace Optimization.Optimizers.Systematic
{
	[Optimization.Attributes.Optimizer(Description="Systematic search")]
	public class Systematic : Optimization.Optimizer
	{		
		List<Range> d_ranges;
		Queue<Solution> d_solutionQueue;
		
		public Systematic()
		{
		}
		
		public override void InitializePopulation()
		{
			d_solutionQueue = new Queue<Solution>();
			
			GenerateSolutions();
			Update();
		}
		
		private void Add(double[] values)
		{
			Optimization.Solution solution = CreateSolution((uint)d_solutionQueue.Count);
			
			// Set solution parameter template
			solution.Parameters = Parameters;

			for (int i = 0; i < values.Length; ++i)
			{
				solution.Parameters[i].Value = values[i];
			}
			
			d_solutionQueue.Enqueue(solution);
		}
		
		private void GenerateSolutions(int idx, double[] values)
		{
			foreach (double val in d_ranges[idx])
			{
				values[idx] = val;
				
				if (idx < d_ranges.Count - 1)
				{
					GenerateSolutions(idx + 1, values);
				}
				else
				{
					Add(values);
				}
			}
		}
		
		private void GenerateSolutions()
		{
			double[] values = new double[d_ranges.Count];
			GenerateSolutions(0, values);
			
			Configuration.MaxIterations = (uint)System.Math.Ceiling(d_solutionQueue.Count / (double)Configuration.PopulationSize);
		}
		
		private double Evaluate(string s)
		{
			Optimization.Math.Expression expression = new Optimization.Math.Expression();

			if (!expression.Parse(s))
			{
				Console.Error.WriteLine("Could not parse expression: {0}", s);
				return 0;
			}
			
			return expression.Evaluate(Optimization.Math.Constants.Context);
		}
		
		public override void FromXml(System.Xml.XmlNode root)
		{
			base.FromXml(root);
			
			// Parse systematic test ranges, and create parameters
			d_ranges = new List<Range>();
			XmlNodeList nodes = root.SelectNodes("boundaries/range");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute min = node.Attributes["min"];
				XmlAttribute step = node.Attributes["step"];
				XmlAttribute steps = node.Attributes["steps"];
				XmlAttribute max = node.Attributes["max"];
				XmlAttribute name = node.Attributes["name"];
				
				if (name == null)
				{
					Console.Error.WriteLine("Name not specified in range");
					continue;
				}
				
				if (min == null)
				{
					Console.Error.WriteLine("Minimum value not specified in range: {0}", name.Value);
					continue;
				}
				
				if (max == null)
				{
					Console.Error.WriteLine("Maximum value not specified in range: {0}", name.Value);
					continue;
				}
				
				double dmin = Evaluate(min.Value);
				double dmax = Evaluate(max.Value);
				double dstep;
				string sname = name.Value;
				
				if (step != null)
				{
					dstep = Evaluate(step.Value);
				}
				else if (steps != null)
				{
					dstep = (dmax - dmin) / (Evaluate(steps.Value) - 1);
				}
				else
				{
					dstep = (dmax - dmin) / 10;
				}
				
				if (dmax - (dmin + dstep) >= dmax - dmin)
				{
					Console.Error.WriteLine("Invalid range ({1}, {2}, {3}): {0}", name.Value, dmin, dstep, dmax);
					continue;
				}
				
				d_ranges.Add(new Range(sname, dmin, dstep, dmax));
				Parameters.Add(new Parameter(sname, new Boundary(sname, dmin, dmax)));
			}
		}
		
		public override void Update()
		{
			Population.Clear();
			
			for (int i = 0; i < Configuration.PopulationSize; ++i)
			{
				if (d_solutionQueue.Count == 0)
				{
					break;
				}
				
				Add(d_solutionQueue.Dequeue());
			}
		}
		
		protected override bool Finished ()
		{
			return d_solutionQueue.Count == 0;
		}
	}
}
