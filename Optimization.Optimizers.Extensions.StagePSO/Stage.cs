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
		
		public Stage()
		{
			d_particles = new List<PSO.Particle>();
		}
		
		public Stage(string expression, string condition) : this()
		{
			Biorob.Math.Expression.Create(expression, out d_expression);

			if (condition != null)
			{
				Biorob.Math.Expression.Create(condition, out d_condition);
			}
		}

		public Stage(XmlNode node) : this()
		{
			XmlAttribute attr;

			Biorob.Math.Expression.Create(node.InnerText.Trim(), out d_expression);
			
			attr = node.Attributes["condition"];
			
			if (attr != null)
			{
				Biorob.Math.Expression.Create(attr.Value, out d_condition);
			}
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
		
		public void Add(PSO.Particle particle)
		{
			d_particles.Add(particle);
			
			if (d_best == null)
			{
				d_best = particle;
			}
			else
			{
				if (Fitness.CompareByMode(Fitness.CompareMode, d_expression.Evaluate(particle.Fitness.Context), d_expression.Evaluate(d_best.Fitness.Context)) > 0)
				{
					d_best = particle;
				}
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

