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
		List<double[]> d_ranges;
		uint d_currentId;
		uint d_numberOfSolutions;
		
		public Systematic()
		{
			d_currentId = 0;
			d_ranges = new List<double[]>();
		}
		
		public new Optimization.Optimizers.Systematic.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimization.Optimizers.Systematic.Settings;
			}
		}
		
		protected override Settings CreateSettings ()
		{
			return new Optimization.Optimizers.Systematic.Settings();
		}
		
		public override void InitializePopulation()
		{
			d_currentId = Configuration.StartIndex;
			Update();
		}
		
		private Solution GenerateSolution(uint idx)
		{
			Optimization.Solution solution = CreateSolution(idx);
			
			// Set solution parameter template
			solution.Parameters = Parameters;
			
			uint ptr = d_numberOfSolutions;

			// Fill parameters
			for (int i = 0; i < d_ranges.Count; ++i)
			{
				double[] values = d_ranges[i];
				uint ptrRest = ptr / (uint)values.Length;

				uint pidx = idx / ptrRest;
				idx = idx % ptrRest;

				solution.Parameters[i].Value = values[pidx];				
				ptr = ptrRest;
			}

			return solution;
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
			d_ranges.Clear();
			d_currentId = 0;
			d_numberOfSolutions = 0;

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
				
				if (dmax > dmin != dstep > 0)
				{
					Console.Error.WriteLine("Invalid range ({1}, {2}, {3}): {0} ({4})", name.Value, dmin, dstep, dmax, dmax - dmin);
					continue;
				}
				
				Range range = new Range(sname, dmin, dstep, dmax);
				double[] all = range.ToArray();
				d_ranges.Add(all);
				
				if (d_numberOfSolutions == 0)
				{
					d_numberOfSolutions = (uint)all.Length;
				}
				else
				{
					d_numberOfSolutions *= (uint)all.Length;
				}

				Parameters.Add(new Parameter(sname, new Boundary(sname, dmin, dmax)));
			}
			
			Configuration.MaxIterations = (uint)System.Math.Ceiling(d_numberOfSolutions / (double)Configuration.PopulationSize);
		}
		
		public override void Update()
		{
			Population.Clear();
			
			for (int i = 0; i < Configuration.PopulationSize; ++i)
			{
				if (d_currentId >= d_numberOfSolutions)
				{
					break;
				}
				
				Add(GenerateSolution(d_currentId));
				d_currentId++;
			}
		}
		
		protected override bool Finished ()
		{
			return d_currentId >= d_numberOfSolutions;
		}
	}
}
