using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.Extensions.LPSO
{
	public class ConstraintMatrix
	{
		private List<Linear.Constraint> d_constraints;
		private List<Linear.Vector> d_equations;
		private List<int> d_parameters;
		private List<Boundary> d_boundaries;

		private double[] d_r1;
		private double[] d_r2;

		public ConstraintMatrix(uint populationSize)
		{
			d_parameters = new List<int>();
			d_constraints = new List<Linear.Constraint>();
			d_equations = new List<Linear.Vector>();
			d_boundaries = new List<Boundary>();
			
			d_r1 = new double[populationSize];
			d_r2 = new double[populationSize];
		}
		
		public bool Solve()
		{
			List<Linear.Constraint> consts = new List<Linear.Constraint>(d_constraints);

			// Add constraints from parameter boundaries
			for (int i = 0; i < d_parameters.Count; ++i)
			{
				Boundary boundary = d_boundaries[i];
				
				Linear.Vector coefficients = new Linear.Vector(d_parameters.Count, 0);
				
				coefficients[i] = 1;
				consts.Add(new Linear.Constraint(false, coefficients, boundary.MaxInitial));
				
				coefficients[i] = -1;
				consts.Add(new Linear.Constraint(false, coefficients, -boundary.MinInitial));
			}

			List<Linear.Vector> vertices = Linear.Vertices(consts);
			
			if (vertices.Count == 0)
			{
				return false;
			}
			
			d_equations.Clear();
			
			// Transpose			
			for (int i = 0; i < vertices.Count; ++i)
			{
				Linear.Vector v = vertices[i];

				for (int j = 0; j < v.Count; ++j)
				{
					if (i == 0)
					{
						d_equations.Add(new Linear.Vector());
					}
					
					d_equations[j].Add(v[j]);
				}
			}
			
			return true;
		}
		
		public void Add(int idx, Parameter parameter)
		{
			d_parameters.Add(idx);
			d_boundaries.Add(parameter.Boundary);
		}
		
		public void Add(Linear.Constraint constraint)
		{
			if (constraint.Coefficients.Count != d_parameters.Count)
			{
				throw new Linear.BadDimension("Number of coefficients ({0}) does not match number of parameters ({1})", constraint.Coefficients.Count, d_parameters.Count);
			}
			
			d_constraints.Add(constraint);
		}
		
		public bool Validate(Solution solution, out Linear.Constraint constraint)
		{
			Linear.Vector vals = new Linear.Vector(d_parameters.Count);
			constraint = null;
			
			foreach (int idx in d_parameters)
			{
				vals.Add(solution.Parameters[idx].Value);
			}

			foreach (Linear.Constraint c in d_constraints)
			{
				if (!c.Validate(vals))
				{
					constraint = c;
					return false;
				}
			}
			
			return true;
		}
		
		public List<Linear.Constraint> Constraints
		{
			get
			{
				return d_constraints;
			}
		}
		
		public List<Linear.Vector> Equations
		{
			get
			{
				return d_equations;
			}
		}
		
		public List<int> Parameters
		{
			get
			{
				return d_parameters;
			}
		}
		
		public double[] R1
		{
			get
			{
				return d_r1;
			}
		}
		
		public double[] R2
		{
			get
			{
				return d_r2;
			}
		}
	}
}

