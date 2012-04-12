using System;
using System.Collections.Generic;
using System.Xml;

namespace Optimization.Optimizers.Extensions.StagePSO
{
	public class Stage
	{
		private Biorob.Math.Expression d_condition;
		private Biorob.Math.Expression d_expression;
		private List<PSO.Particle> d_particles;
		private PSO.Particle d_best;
		private uint d_priority;
		
		public Stage(uint priority)
		{
			d_particles = new List<PSO.Particle>();
			d_priority = priority;
		}
		
		public Stage(string expression, string condition, uint priority) : this(priority)
		{
			Biorob.Math.Expression.Create(expression, out d_expression);

			if (condition != null)
			{
				Biorob.Math.Expression.Create(condition, out d_condition);
			}
		}

		public Stage(XmlNode node, uint priority) : this(priority)
		{
			XmlAttribute attr;

			Biorob.Math.Expression.Create(node.InnerText.Trim(), out d_expression);
			
			attr = node.Attributes["condition"];
			
			if (attr != null)
			{
				Biorob.Math.Expression.Create(attr.Value, out d_condition);
			}
		}
		
		public uint Priority
		{
			get
			{
				return d_priority;
			}
		}
		
		public bool Validate(PSO.Particle particle)
		{
			return Validate(particle.Fitness.Context, Biorob.Math.Constants.Context);
		}
		
		public bool Validate(params Dictionary<string, object>[] context)
		{
			if (d_condition == null)
			{
				return true;
			}
			else
			{
				return d_condition.Evaluate(context) > 0.5;
			}
		}
		
		public double Value(PSO.Particle particle)
		{
			return Value(particle.Fitness);
		}
		
		public double Value(Fitness fitness)
		{
			return Value(fitness.Context, Biorob.Math.Constants.Context);
		}
		
		public double Value(params Dictionary<string, object>[] context)
		{
			return d_expression.Evaluate(context);
		}
		
		public Biorob.Math.Expression Condition
		{
			get
			{
				return d_condition;
			}
		}
		
		public Biorob.Math.Expression Expression
		{
			get
			{
				return d_expression;
			}
		}
		
		public void Clear()
		{
			d_particles.Clear();
			d_best = null;
		}
		
		public int Compare(Fitness f1, Fitness f2)
		{
			if (f1 == null && f2 == null)
			{
				return 0;
			}
			else if (f1 == null)
			{
				return -1;
			}
			else if (f2 == null)
			{
				return 1;
			}
			
			return Fitness.CompareByMode(Fitness.CompareMode, Value(f1), Value(f2));
		}
		
		public int Compare(PSO.Particle p1, PSO.Particle p2)
		{
			if (p1 == null && p2 == null)
			{
				return 0;
			}
			else if (p1 == null)
			{
				return -1;
			}
			else if (p2 == null)
			{
				return 1;
			}
			
			return Compare(p1.Fitness, p2.Fitness);
		}
		
		public int CompareToBest(PSO.Particle particle)
		{
			if (!Validate(particle))
			{
				return -1;
			}

			return Compare(particle, d_best);
		}
		
		public void Add(PSO.Particle particle)
		{
			d_particles.Add(particle);
			
			if (CompareToBest(particle) > 0)
			{
				d_best = particle;
			}
		}
		
		public PSO.Particle Best
		{
			get
			{
				return d_best;
			}
		}
	}
}

