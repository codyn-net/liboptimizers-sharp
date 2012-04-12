using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.Extensions.LPSO
{
	public class Linear
	{
		public static double Tolerance = 10e-7;

		public class Exception : System.Exception
		{
			public Exception(string format, params object[] args) : base(String.Format(format, args))
			{
			}
		}

		public class BadDimension : Exception
		{
			public BadDimension(string format, params object[] args) : base(format, args)
			{
			}
		}

		public class Vector : List<double>
		{
			public Vector()
			{
			}
			
			public Vector(int n) : base(n)
			{
			}
			
			public Vector(int n, double val) : base(n)
			{
				for (int i = 0; i < n; ++i)
				{
					Add(val);
				}
			}

			public Vector(Vector other) : base(other)
			{
			}
			
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
			
			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}
				
				Vector o = obj as Vector;
				
				if (o == null)
				{
					return false;
				}
				
				if (o.Count != Count)
				{
					return false;
				}
				
				for (int i = 0; i < Count; ++i)
				{
					if (Math.Abs(this[i] - o[i]) > Tolerance)
					{
						return false;
					}
				}
				
				return true;
			}
			
			public double Dot(Vector other)
			{
				if (other.Count != Count)
				{
					throw new BadDimension("Number of dimensions have to be equal");
				}
				
				double s = 0;
				
				for (int i = 0; i < Count; ++i)
				{
					s += this[i] * other[i];
				}
				
				return s;
			}
		}

		public class Constraint
		{
			public Vector Coefficients;
			public double Value;
			public bool Equality;
			
			public Constraint(bool equality)
			{
				Coefficients = new Vector();
				Value = 0;
				Equality = equality;
			}
			
			public Constraint(bool equality, Vector coefficients, double val) : this(equality)
			{
				Coefficients = new Vector(coefficients);
				
				Value = val;
			}
			
			public bool Validate(Vector r)
			{
				if (r.Count != Coefficients.Count)
				{
					return false;
				}
				
				double s = Coefficients.Dot(r);
				
				if (Equality)
				{
					return Math.Abs(s - Value) <= Tolerance;
				}
				else
				{
					return s <= Value + Tolerance;
				}
			}
		}

		public static Vector Solve(Constraint[] constraints)
		{
			// Convert to upper triangular form
			int n = constraints.Length;

			List<Vector> A = new List<Vector>();
			Vector b = new Vector();

			for (int i = 0; i < n; ++i)
			{
				Constraint constraint = constraints[i];

				if (constraint.Coefficients.Count != n)
				{
					throw new BadDimension("Constraint coefficients must be square");
				}
				
				A.Add(new Vector(constraint.Coefficients));
				b.Add(constraint.Value);
			}
			
			/* Triangular form */
			for (int k = 0; k < n - 1; ++k)
			{
				for (int i = k + 1; i < n; ++i)
				{
					if (A[k][k] == 0)
					{
						continue;
					}

					double x = A[i][k] / A[k][k];
					
					for (int j = k + 1; j < n; ++j)
					{
						A[i][j] = A[i][j] - A[k][j] * x;
					}
					
					b[i] = b[i] - b[k] * x;
				}
			}
			
			Vector ret = new Vector(n, 0);
			
			/* Back substitution */
			for (int i = n - 1; i >= 0; --i)
			{
				ret[i] = b[i];

				for (int j = i + 1; j < n; ++j)
				{
					ret[i] -= A[i][j] * ret[j];
				}
				
				ret[i] /= A[i][i];
			}
			
			return ret;
		}
		
		private static IEnumerable<T[]> Combinations<T>(int element, int startat, IEnumerable<T> list, T[] ret)
		{
			if (element == ret.Length)
			{
				yield return ret;
				yield break;
			}
			
			IEnumerator<T> enumerator = list.GetEnumerator();
			int i = startat;
			
			while (enumerator.MoveNext())
			{
  				ret[element] = enumerator.Current;

  				foreach (T[] c in Combinations(element + 1, i + 1, list, ret))
  				{
  					yield return c;
  				}
  				
  				++i;
  			}
		}
		
		private static IEnumerable<T[]> Combinations<T>(IEnumerable<T> list, int k)
		{
			T[] ret = new T[k];
			
			return Combinations(0, 0, list, ret);
		}
		
		private static bool Contains(List<Vector> list, Vector r)
		{
			foreach (Vector rr in list)
			{
				if (rr.Equals(r))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public static List<Vector> Vertices(List<Constraint> system)
		{
			// Find all vertices that are feasible by having active constraints
			int n = system.Count;
			
			if (n == 0)
			{
				return new List<Vector>();
			}

			int num = system[0].Coefficients.Count;
			
			for (int i = 1; i < n; ++i)
			{
				if (system[i].Coefficients.Count != num)
				{
					throw new BadDimension("Number of dimensions of constraints is not the same");
				}
			}
			
			// Need at least num equations
			if (system.Count < num)
			{
				throw new BadDimension("Number of variables is larger than the number of equations");
			}
			
			List<Vector> solutions = new List<Vector>();
			
			// Take all combinations of num equations out of system.Length equations and solve them
			foreach (Constraint[] comb in Combinations(system, num))
			{
				Vector ret;

				try
				{
					ret = Solve(comb);
				}
				catch (System.DivideByZeroException)
				{
					continue;
				}
				
				bool feasible = true;

				// Verify feasibility according to constraints
				foreach (Constraint cons in system)
				{
					if (!cons.Validate(ret))
					{
						feasible = false;
						break;
					}
				}
				
				if (feasible && !Contains(solutions, ret))
				{
					solutions.Add(ret);
				}
			}
			
			return solutions;
		}
	}
}

