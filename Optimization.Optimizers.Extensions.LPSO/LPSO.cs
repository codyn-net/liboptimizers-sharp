using System;
using System.Collections.Generic;
using Optimization;
using System.Xml;
using System.Data;

/**
 *
 * LPSO (Linear Constraint PSO)
 *
 * LPSO [1] is an extension for Particle Swarm Optimization implementing linear equality and inequality
 * constraints on subspaces of the parameter space. Multiple sets linear constraints can be specified
 * for a particular parameter subset.
 *
 * Example XML specification of a constraint 'x1 + x2 + x3 = 1'.
 *
 * <extensions>
 *   <extension name="lpso">
 *     <constraints>
 *       <constraint>
 *         <!-- Comma separated list of parameter names -->
 *         <parameters>x1, x2, x3</parameters>
 *
 *         <!-- Coefficients for linear equation, i.e. x1 + x2 + x3 = 1 -->
 *         <equation value="1" equality="yes">1, 1, 1</equation>
 *       </constraint>  
 *     </constraints>
 *   </extension>
 * </extensions>
 *
 * [1] Paquet, U. & Engelbrecht, A.P. (2003). A new particle swarm optimiser for linearly constrained
 *     optimisation. Evolutionary Computation, 2003. CEC'03. The 2003 Congress on,1, 227—233.
 *
 **/
namespace Optimization.Optimizers.Extensions.LPSO
{
	[Optimization.Attributes.Extension(Description = "Linear Constraint PSO", AppliesTo = new Type[] {typeof(PSO.PSO)})]
	public class LPSO : Extension, PSO.IPSOExtension
	{
		private List<ConstraintMatrix> d_constraints;
		private Dictionary<int, ConstraintMatrix> d_constraintsFor;

		public LPSO(Job job) : base(job)
		{
			d_constraints = new List<ConstraintMatrix>();
			d_constraintsFor = new Dictionary<int, ConstraintMatrix>();
		}

		protected override Optimization.Settings CreateSettings()
		{
			return new Settings();
		}
		
		public new Settings Configuration
		{
			get
			{
				return (Settings)base.Configuration;
			}
		}
		
		public override void Initialize()
		{
			base.Initialize();
			
			StoreConstraints();
		}
		
		private void StoreConstraints()
		{
			Storage.Storage storage = Job.Optimizer.Storage;

			storage.Query("DROP TABLE IF EXISTS `constraints`");
			storage.Query("DROP TABLE IF EXISTS `constraint_parameters`");
			storage.Query("DROP TABLE IF EXISTS `constraint_equations`");
			storage.Query("DROP TABLE IF EXISTS `constraint_coefficients`");

			storage.Query("CREATE TABLE `constraints` (`id` INTEGER PRIMARY KEY)");
			storage.Query("CREATE TABLE `constraint_parameters` (`id` INTEGER PRIMARY KEY, `constraint` INT, `parameter` INT)");
			storage.Query("CREATE TABLE `constraint_equations` (`id` INTEGER PRIMARY KEY, `constraint` INT, `equality` INT, `value` DOUBLE)");
			storage.Query("CREATE TABLE `constraint_coefficients` (`id` INTEGER PRIMARY KEY, `equation` INT, `parameter` INT, `value` DOUBLE)");
			
			for (int i = 0; i < d_constraints.Count; ++i)
			{
				ConstraintMatrix cons = d_constraints[i];
				
				storage.Query(@"INSERT INTO `constraints` DEFAULT VALUES");

				int cid = storage.LastInsertId;

				foreach (int p in cons.Parameters)
				{
					storage.Query(@"INSERT INTO `constraint_parameters` (`constraint`, `parameter`) VALUES (@0, @1)",
					              cid,
					              p);
				}
				
				foreach (Linear.Constraint eq in cons.Constraints)
				{
					storage.Query(@"INSERT INTO `constraint_equations` (`constraint`, `equality`, `value`) VALUES (@0, @1, @2)",
					              cid,
					              eq.Equality ? 1 : 0,
					              eq.Value);
					
					int eqid = storage.LastInsertId;
					
					foreach (double coefficient in eq.Coefficients)
					{
						storage.Query(@"INSERT INTO `constraint_coefficients` (`equation`, `value`) VALUES (@0, @1)",
						              eqid,
						              coefficient);
					}
				}
			}
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			base.FromStorage(storage, optimizer);

			storage.Query("SELECT `id` FROM `constraints` ORDER BY `id`", delegate (IDataReader reader) {
				ConstraintMatrix cons = new ConstraintMatrix(Job.Optimizer.Configuration.PopulationSize);
				
				int consid = reader.GetInt32(0);
				
				storage.Query("SELECT `parameter` FROM `constraint_parameters` WHERE `constraint` = @0", delegate (IDataReader rd) {
					int idx = reader.GetInt32(0);
					
					cons.Add(idx, Job.Optimizer.Parameters[idx]);
					d_constraintsFor[idx] = cons;
					
					return true;
				}, consid);
					
				storage.Query("SELECT `id`, `equality`, `value` FROM `constraint_equations` WHERE `constraint` = @0 ORDER BY `id`", delegate (IDataReader eqreader) {
					int eqid = eqreader.GetInt32(0);
					bool equality = eqreader.GetInt32(1) == 1;
					double val = eqreader.GetDouble(2);
					
					Linear.Vector coefficients = new Linear.Vector();
					
					storage.Query("SELECT `value` FROM `constraint_coefficients` WHERE `equation` = @0 ORDER BY `id`", delegate (IDataReader cfreader) {
						coefficients.Add(cfreader.GetDouble(0));
						return true;
					}, eqid);

					cons.Add(new Linear.Constraint(equality, coefficients, val));
					return true;
				}, consid);

				d_constraints.Add(cons);
				
				return true;
			});
		}
		
		private void Initialize(PSO.Particle particle, ConstraintMatrix constraint)
		{
			List<Linear.Vector> eqs = constraint.Equations;
			double[] rr = new double[eqs[0].Count];
			double s = 0;
			
			// Generate random variables
			for (int i = 0; i < rr.Length; ++i)
			{
				rr[i] = particle.State.Random.NextDouble();
				s += rr[i];
			}
			
			// Normalize such that sum == 1
			for (int i = 0; i < rr.Length; ++i)
			{
				rr[i] /= s;
			}
			
			// Initialize parameters of particle according to linear equations
			for (int i = 0; i < constraint.Parameters.Count; ++i)
			{
				int idx = constraint.Parameters[i];

				Parameter param = particle.Parameters[idx];
				double v = 0;
				
				for (int j = 0; j < rr.Length; ++j)
				{
					v += rr[j] * constraint.Equations[i][j];
				}
				
				param.Value = v;
				
				// Also set velocity to 0
				particle.Velocity[idx] = 0;
			}
		}

		public override void Initialize(Solution solution)
		{
			base.Initialize(solution);
			
			PSO.Particle p = (PSO.Particle)solution;
			
			// Generate initial conditions to adhere to the constraints...
			foreach (ConstraintMatrix cons in d_constraints)
			{
				Initialize(p, cons);
			}
		}
		
		public override void InitializePopulation()
		{
			base.InitializePopulation();
			
			if (!ValidateConstraints())
			{
				throw new Exception("Constraints are violated and I die");
			}
		}
		
		private void AddConstraint(XmlNode node)
		{
			XmlNodeList lst = node.SelectNodes("parameters/parameter");
			
			List<string> pars = new List<string>();
			
			if (lst.Count == 0)
			{
				XmlNode parameters = node.SelectSingleNode("parameters");
				
				if (parameters != null && !String.IsNullOrEmpty(parameters.InnerText.Trim()))
				{
					pars.AddRange(Array.ConvertAll(parameters.InnerText.Trim().Split(','), item => item.Trim()));
				}
			}
			else
			{
				foreach (XmlNode p in lst)
				{
					pars.Add(p.InnerText.Trim());
				}
			}
			
			if (pars.Count == 0)
			{
				throw new Exception("No parameters were specified");
			}
			
			ConstraintMatrix constr = new ConstraintMatrix(Job.Optimizer.Configuration.PopulationSize);
			
			foreach (string pname in pars)
			{
				Parameter p = Job.Optimizer.Parameter(pname);
				
				if (p == null)
				{
					throw new Exception(String.Format("The parameter `{0}' could not be found", pname));
				}
				
				constr.Add(Job.Optimizer.Parameters.IndexOf(p), p);
			}
			
			foreach (XmlNode eq in node.SelectNodes("equation"))
			{
				XmlAttribute val = eq.Attributes["value"];
				double v = 0;
				bool isequality = true;
				
				if (val != null)
				{
					Biorob.Math.Expression expr;
					Biorob.Math.Expression.Create(val.Value.Trim(), out expr);
					
					v = expr.Evaluate(Biorob.Math.Constants.Context);
				}
				
				XmlAttribute equality = eq.Attributes["equality"];
				
				if (equality != null)
				{
					isequality = (equality.Value.Trim() == "yes");
				}
				
				string[] coefs = Array.ConvertAll(eq.InnerText.Split(','), item => item.Trim());
				
				if (coefs.Length != pars.Count)
				{
					throw new Exception(String.Format("The number of coefficients is not equal to the number of parameters (expected {0}, but got {1})", pars.Count, coefs.Length));
				}
				
				Linear.Vector coefficients = new Linear.Vector(coefs.Length);
				
				for (int i = 0; i < coefs.Length; ++i)
				{
					Biorob.Math.Expression expr;
					Biorob.Math.Expression.Create(coefs[i].Trim(), out expr);
					
					coefficients.Add(expr.Evaluate(Biorob.Math.Constants.Context));
				}
				
				constr.Add(new Linear.Constraint(isequality, coefficients, v));
			}

			if (!constr.Solve())
			{
				throw new Exception("Could not solve system of linear constraints!");
			}
			
			foreach (int param in constr.Parameters)
			{
				if (d_constraintsFor.ContainsKey(param))
				{
					throw new Exception(String.Format("The parameter `{0}' is already part of another constraint...", Job.Optimizer.Parameters[param].Name));
				}
			}

			d_constraints.Add(constr);
			
			foreach (int param in constr.Parameters)
			{
				d_constraintsFor[param] = constr;
			}
		}
		
		public override void FromXml(XmlNode root)
		{
			base.FromXml(root);
			
			// Get linear constraints
			foreach (XmlNode node in root.SelectNodes("constraints/constraint"))
			{
				AddConstraint(node);
			}
		}
		
		public override void BeforeUpdate()
		{
			base.BeforeUpdate();
			
			foreach (ConstraintMatrix cons in d_constraints)
			{
				foreach (Solution solution in Job.Optimizer.Population)
				{
					cons.R1[solution.Id] = Job.Optimizer.State.Random.NextDouble();
					cons.R2[solution.Id] = Job.Optimizer.State.Random.NextDouble();
				}
			}
		}
		
		private bool ValidateConstraints()
		{
			double maxvel = ((PSO.Settings)Job.Optimizer.Configuration).MaxVelocity;
			bool isok = true;
			
			foreach (Solution solution in Job.Optimizer.Population)
			{
				for (int i = 0; i < solution.Parameters.Count; ++i)
				{
					Parameter param = solution.Parameters[i];
					double mv = (param.Boundary.Max - param.Boundary.Min) * maxvel;

					if (maxvel > 0 && ((PSO.Particle)solution).Velocity[i] > mv)
					{
						Console.WriteLine("Velocity boundary violated: {0} = {1}", param.Name, ((PSO.Particle)solution).Velocity[i]);
						isok = false;
					}
				}
				
				foreach (ConstraintMatrix cons in d_constraints)
				{
					Linear.Constraint constraint;

					if (cons.Validate(solution, out constraint))
					{
						continue;
					}
					
					List<string> s = new List<string>();
					
					for (int i = 0; i < cons.Parameters.Count; ++i)
					{
						int idx = cons.Parameters[i];
						s.Add(String.Format("{0:0.000} * {1} ({2:0.000})", constraint.Coefficients[i], solution.Parameters[idx].Name, solution.Parameters[idx].Value));
					}
					
					string ss = String.Join(" + ", s.ToArray());
					
					Console.WriteLine("Constraint violated: {0} {1} {2:0.000}", ss, constraint.Equality ? "=" : "<=", constraint.Value);
					isok = false;
				}
			}
			
			return isok;
		}
		
		public override void AfterUpdate()
		{
			base.AfterUpdate();
			
			ValidateConstraints();
		}
		
		private void ValidateVelocityUpdate(PSO.Particle particle, double[] velocityUpdate, ConstraintMatrix cons)
		{
			double maxsc = -1;
			
			double maxvel = ((PSO.Settings)Job.Optimizer.Configuration).MaxVelocity;
			
			foreach (int idx in cons.Parameters)
			{
				Parameter param = particle.Parameters[idx];
			
				double newvel = velocityUpdate[idx];
				double newpos = param.Value + newvel;
				double sc = -1;
			
				double maxpv = maxvel * (param.Boundary.Max - param.Boundary.Min);
			
				if (maxvel > 0 && System.Math.Abs(newvel) > maxpv)
				{
					sc = maxpv / velocityUpdate[idx];
				
					if (maxsc == -1 || sc < maxsc)
					{
						maxsc = sc;
					}
				}
				
				if (newpos < param.Boundary.Min)
				{
					sc = (param.Boundary.Min - param.Value) / velocityUpdate[idx];
				}
				else if (newpos > param.Boundary.Max)
				{
					sc = (param.Boundary.Max - param.Value) / velocityUpdate[idx];
				}
				else if (sc == -1)
				{
					continue;
				}
				
				if (maxsc == -1 || (sc != -1 && sc < maxsc))
				{
					maxsc = sc;
				}
			}
			
			if (maxsc != -1)
			{
				foreach (int idx in cons.Parameters)
				{
					Parameter param = particle.Parameters[idx];

					velocityUpdate[idx] = velocityUpdate[idx] * maxsc;
					
					/* This is a bit strange, but it is needed to solve numerical
					   inaccuracies when multipying/dividing and we MUST ensure the
					   boundaries (in particular if a sign would change) */
					if (param.Value + velocityUpdate[idx] > param.Boundary.Max)
					{
						velocityUpdate[idx] = param.Boundary.Max - param.Value;
					}
					else if (param.Value + velocityUpdate[idx] < param.Boundary.Min)
					{
						velocityUpdate[idx] = param.Boundary.Min - param.Value;
					}
				}
			}
		}
		
		public void ValidateVelocityUpdate(PSO.Particle particle, double[] velocityUpdate)
		{
			// Make sure that we are within the parameter boundaries by linearly scaling the velocity vector (if needed)
			foreach (ConstraintMatrix cons in d_constraints)
			{
				ValidateVelocityUpdate(particle, velocityUpdate, cons);
			}
		}
		
		public double CalculateVelocityUpdate(PSO.Particle particle, PSO.Particle gbest, int i)
		{
			// If there are no constraints, then just use the default
			double r1;
			double r2;

			if (!d_constraintsFor.ContainsKey(i))
			{
				r1 = particle.State.Random.NextDouble();
				r2 = particle.State.Random.NextDouble();
			}
			else
			{		
				ConstraintMatrix cons = d_constraintsFor[i];
				
				r1 = cons.R1[particle.Id];
				r2 = cons.R2[particle.Id];
			}

			PSO.Settings settings = (PSO.Settings)Job.Optimizer.Configuration;
			Parameter parameter = particle.Parameters[i];

			double pg = 0;
			double pl = 0;
			
			// Global best difference
			if (gbest != null)
			{
				pg = gbest.Parameters[i].Value - parameter.Value;
			}
			
			// Local best difference
			if (particle.PersonalBest != null)
			{
				pl = particle.PersonalBest.Parameters[i].Value - parameter.Value;
			}
			
			// PSO velocity update rule
			return settings.Constriction * (r1 * settings.CognitiveFactor * pl +
			                                r2 * settings.SocialFactor * pg);
		}

		public PSO.State.VelocityUpdateType VelocityUpdateComponents(PSO.Particle particle)
		{
			return PSO.State.VelocityUpdateType.Default | PSO.State.VelocityUpdateType.DisableGlobal | PSO.State.VelocityUpdateType.DisableLocal;
		}
		
		public PSO.Particle GetUpdateBest(PSO.Particle particle)
		{
			return null;
		}
		
		public bool UpdateParticleBest(PSO.Particle particle)
		{
			return false;
		}
	}
}

