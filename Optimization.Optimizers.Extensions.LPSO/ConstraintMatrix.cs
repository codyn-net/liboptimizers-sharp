using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.Extensions.LPSO
{
	public class ConstraintMatrix
	{
		private List<Linear.Constraint> d_constraints;
		private List<Linear.Vector> d_equations;
		
		private List<Linear.Constraint> d_nullspaceConstraints;
		private List<Linear.Vector> d_nullspaceEquations;

		private List<string> d_parameters;
		private List<Boundary> d_boundaries;

		private double[] d_r1;
		private double[] d_r2;
		private double[] d_rn;

		public ConstraintMatrix(uint populationSize)
		{
			d_parameters = new List<string>();

			d_constraints = new List<Linear.Constraint>();
			d_equations = new List<Linear.Vector>();
			
			d_nullspaceConstraints = new List<Linear.Constraint>();
			d_nullspaceEquations = new List<Linear.Vector>();

			d_boundaries = new List<Boundary>();
			
			d_r1 = new double[populationSize];
			d_r2 = new double[populationSize];
		}
		
		private void Transpose(List<Linear.Vector> vertices, List<Linear.Vector> equations)
		{
			for (int i = 0; i < vertices.Count; ++i)
			{
				Linear.Vector v = vertices[i];

				for (int j = 0; j < v.Count; ++j)
				{
					if (i == 0)
					{
						equations.Add(new Linear.Vector());
					}
					
					equations[j].Add(v[j]);
				}
			}
		}
		
		public bool Solve()
		{
			List<Linear.Constraint> consts = new List<Linear.Constraint>(d_constraints);
			List<Linear.Constraint> nullspace = new List<Linear.Constraint>();
			
			foreach (Linear.Constraint c in d_constraints)
			{
				if (c.Equality)
				{
					nullspace.Add(new Linear.Constraint(true, c.Coefficients, 0));
				}
				else
				{
					// FIXME: this is probably not really correct
					nullspace.Add(new Linear.Constraint(false, c.Coefficients, c.Value));
				}
			}
			
			d_nullspaceConstraints.Clear();
			d_nullspaceConstraints.AddRange(nullspace);

			// Add constraints from parameter boundaries
			for (int i = 0; i < d_parameters.Count; ++i)
			{
				Boundary boundary = d_boundaries[i];
				
				Linear.Vector coefficients = new Linear.Vector(d_parameters.Count, 0);
				
				coefficients[i] = 1;

				consts.Add(new Linear.Constraint(false, coefficients, boundary.MaxInitial));
				nullspace.Add(new Linear.Constraint(false, coefficients, 1));
				
				coefficients[i] = -1;
				consts.Add(new Linear.Constraint(false, coefficients, -boundary.MinInitial));
				nullspace.Add(new Linear.Constraint(false, coefficients, 1));
			}

			List<Linear.Vector> vertices = Linear.Vertices(consts);
			
			if (vertices.Count == 0)
			{
				return false;
			}
			
			d_equations.Clear();
			d_nullspaceEquations.Clear();
			
			Transpose(vertices, d_equations);
			
			vertices = Linear.Vertices(nullspace);
			Transpose(vertices, d_nullspaceEquations);
			
			d_rn = new double[d_nullspaceEquations[0].Count];
			
			return true;
		}
		
		public void Add(Parameter parameter)
		{
			d_parameters.Add(parameter.Name);
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
		
		private bool Validate(Dictionary<string, double> values, List<Linear.Constraint> constraints, out Linear.Constraint constraint)
		{
			Linear.Vector vals = new Linear.Vector(d_parameters.Count);
			constraint = null;
			
			foreach (string idx in d_parameters)
			{
				vals.Add(values[idx]);
			}

			foreach (Linear.Constraint c in constraints)
			{
				if (!c.Validate(vals))
				{
					constraint = c;
					return false;
				}
			}
			
			return true;
		}
		
		public bool Validate(Dictionary<string, double> values, out Linear.Constraint constraint)
		{
			return Validate(values, d_constraints, out constraint);
		}

		public bool ValidateNull(Dictionary<string, double> values, out Linear.Constraint constraint)
		{
			return Validate(values, d_nullspaceConstraints, out constraint);
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
		
		public List<Linear.Constraint> NullspaceConstraints
		{
			get
			{
				return d_nullspaceConstraints;
			}
		}
		
		public List<Linear.Vector> NullspaceEquations
		{
			get
			{
				return d_nullspaceEquations;
			}
		}
		
		public List<string> Parameters
		{
			get
			{
				return d_parameters;
			}
		}
		
		public int ParameterIndex(string name)
		{
			return d_parameters.IndexOf(name);
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
		
		public double[] RN
		{
			get
			{
				return d_rn;
			}
		}
	}
}

