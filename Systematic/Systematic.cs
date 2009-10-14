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
				XmlAttribute max = node.Attributes["max"];
				XmlAttribute name = node.Attributes["name"];
				
				if (name == null)
				{
					Console.Error.WriteLine("Name not specified in range");
					continue;
				}
				
				if (min == null)
				{
					Console.Error.WriteLine("Minimum value not specified in range");
					continue;
				}
				
				if (max == null)
				{
					Console.Error.WriteLine("Maximum value not specified in range");
					continue;
				}
				
				double dmin = Double.Parse(min.Value);
				double dmax = Double.Parse(max.Value);
				double dstep;
				string sname = name.Value;
				
				if (step != null)
				{
					dstep = Double.Parse(step.Value);
				}
				else
				{
					dstep = (dmax - dmin) / 10;
				}
				
				if (dmax - (dmin + dstep) >= dmax - dmin)
				{
					Console.Error.WriteLine("Invalid range ({0}, {1}, {2})", dmin, dstep, dmax);
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
