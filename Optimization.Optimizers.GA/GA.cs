using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

namespace Optimization.Optimizers.GA
{
	[Optimization.Attributes.OptimizerAttribute(Description="Genetic Algorithm")]
	public class GA : Optimization.Optimizer
	{
		private Optimization.Math.Expression d_tournamentSize;
		private Optimization.Math.Expression d_tournamentProbability;
		private Optimization.Math.Expression d_crossoverProbability;
		private Optimization.Math.Expression d_globalMutationProbability;
		private Optimization.Math.Expression d_globalMutationRate;

		private List<Optimization.Math.Expression> d_mutationProbability;
		private List<Optimization.Math.Expression> d_mutationRate;

		private Dictionary<string, object> d_context;

		private List<bool> d_parameterDiscrete;
		 
		public GA()
		{
			d_parameterDiscrete = new List<bool>();

			d_mutationRate = new List<Optimization.Math.Expression>();
			d_mutationProbability = new List<Optimization.Math.Expression>();

			d_globalMutationProbability = new Optimization.Math.Expression();
			d_globalMutationRate = new Optimization.Math.Expression();
		}

		// Override 'Configuration' property returning subclassed Settings
		public new Optimization.Optimizers.GA.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimization.Optimizers.GA.Settings;
			}
		}
		
		protected override Solution CreateSolution (uint idx)
		{
			return new Individual(idx, Fitness, State);
		}
		
		private void SetupExpressions()
		{
			d_context = new Dictionary<string, object>();
			d_context["k"] = 0;
			
			d_tournamentSize = new Optimization.Math.Expression();
			d_tournamentSize.Parse(Configuration.TournamentSize);
			
			d_tournamentProbability = new Optimization.Math.Expression();
			d_tournamentProbability.Parse(Configuration.TournamentProbability);
			
			d_crossoverProbability = new Optimization.Math.Expression();
			d_crossoverProbability.Parse(Configuration.CrossoverProbability);
			
			d_globalMutationProbability.Parse(Configuration.MutationProbability);
			d_globalMutationRate.Parse(Configuration.MutationRate);
		}
		
		private string NormalizeName(string name)
		{
			return name.Replace("`", "").Replace("\"", "").Replace("'", "");
		}
		
		public override void Initialize()
		{
			base.Initialize();
			
			SetupExpressions();

			// Store some additional info in the database
			Storage.Query("ALTER TABLE parameters ADD COLUMN mutation_probability TEXT");
			Storage.Query("ALTER TABLE parameters ADD COLUMN mutation_rate TEXT");
			Storage.Query("ALTER TABLE parameters ADD COLUMN discrete INTEGER");

			for (int i = 0; i < Parameters.Count; ++i)
			{
				Storage.Query("UPDATE parameters SET mutation_probability = @0, mutation_rate = @1, discrete = @2 WHERE name = @3",
				              d_mutationProbability[i].Text,
				              d_mutationRate[i].Text,
				              d_parameterDiscrete[i],
				              Parameters[i].Name);
			}

			StringBuilder q = new StringBuilder();
			q.Append("CREATE TABLE `reproduction_state` (`index` INTEGER, `iteration` INTEGER, `crossover` INTEGER");
			
			foreach (Parameter parameter in Parameters)
			{
				q.AppendFormat(", `{0}` INTEGER", NormalizeName(parameter.Name));
			}
			
			q.Append(")");
			Storage.Query(q.ToString());
		}
		
		protected override Settings CreateSettings()
		{
			return new Optimization.Optimizers.GA.Settings();
		}
		
		private void SaveReproductionState()
		{
			object[] values = new object[Parameters.Count + 3];

			values[0] = CurrentIteration;

			foreach (Solution solution in Population)
			{
				Individual individual = (Individual)solution;

				// Save current population mutation and crossover state
				StringBuilder q = new StringBuilder();
				StringBuilder vals = new StringBuilder();
				
				values[1] = individual.Id;
				values[2] = individual.LastCutPoint;

				q.Append("INSERT INTO `reproduction_state` (`iteration`, `index`, `crossover`");
				vals.Append("VALUES(@0, @1, @2");
				
				for (int i = 0; i < individual.Parameters.Count; ++i)
				{
					q.AppendFormat(", `{0}`", NormalizeName(individual.Parameters[i].Name));
					vals.AppendFormat(", @{0}", i + 3);

					values[i] = individual.Mutations[i];
				}
				
				string query = String.Format("{0}) {1})", q, vals);
				Storage.Query(query, values);
			}
		}
		
		protected override void IncrementIteration()
		{
			SaveReproductionState();

			base.IncrementIteration();
			
			d_context["k"] = CurrentIteration;
		}
		
		private List<Solution> SelectRouletteWheel()
		{
			List<Solution> ret = new List<Solution>();
			
			// Sum fitnesses for all solutions in the population
			double sum = 0;
			foreach (Solution solution in Population)
			{
				sum += solution.Fitness.Value;
			}
			
			// Select each individual based on the part of the fitness space
			// it makes up
			foreach (Solution solution in Population)
			{
				double probability = solution.Fitness.Value / sum;
				
				if (State.Random.NextDouble() <= probability)
				{
					ret.Add(solution);
				}
			}
			
			return ret;
		}
		
		private List<Solution> SelectTournament()
		{
			List<Solution> ret = new List<Solution>();
			List<Solution> population = new List<Solution>(Population);
			
			uint tournamentSize = (uint)d_tournamentSize.Evaluate(d_context);
			double tournamentProbability = d_tournamentProbability.Evaluate(d_context);
			
			Solution[] tournament = new Solution[tournamentSize];
			
			while (population.Count >= tournamentSize)
			{
				// Select TournamentSize solutions at random from the population
				for (uint i = 0; i < tournamentSize; ++i)
				{
					int idx = (int)System.Math.Round(State.Random.Range(0, population.Count - 1));
					tournament[i] = population[idx];
					population.RemoveAt(idx);
				}
				
				// Sort fitnesses
				Array.Sort(tournament, delegate (Solution a, Solution b) {
					return b.Fitness.Value.CompareTo(a.Fitness.Value);
				});
				
				for (uint i = 0; i < tournamentSize; ++i)
				{
					double probability = tournamentProbability * System.Math.Pow(1 - tournamentProbability, i);
	
					if (State.Random.NextDouble() <= probability)
					{
						ret.Add(tournament[i]);
					}
				}
			}
			
			return ret;
		}
		
		private List<Solution> Select()
		{
			switch (Configuration.Selection)
			{
				case Optimization.Optimizers.GA.Settings.SelectionType.Tournament:
					return SelectTournament();
				case Optimization.Optimizers.GA.Settings.SelectionType.RouletteWheel:
					return SelectRouletteWheel();
				default:
					return new List<Solution>(Population);
			}
		}
		
		private Individual Crossover(Solution p1, Solution p2)
		{
			// Define crossover point in parameters
			int num = System.Math.Min(p1.Parameters.Count, p2.Parameters.Count);
			int cutPoint = (int)System.Math.Round(State.Random.Range(0, num - 2));
			
			Individual ret = (Individual)CreateSolution(0);
			ret.LastCutPoint = cutPoint;

			for (int i = 0; i < cutPoint + (p2.Parameters.Count - cutPoint); ++i)
			{
				Solution take = i <= cutPoint ? p1 : p2;
				ret.Add(take.Parameters[i].Clone() as Parameter);
			}
			
			ret.ResetMutations();
			return ret;
		}
		
		private void Mutate(Individual individual, int parameterIndex, double mutationRate, bool isDiscrete)
		{
			Parameter parameter = individual.Parameters[parameterIndex];

			if (isDiscrete)
			{
				double num = parameter.Boundary.Max - parameter.Boundary.Min;

				if (num >= 1)
				{
					double val = System.Math.Round(State.Random.Range(0, num - 1)) + parameter.Boundary.Min;
					double oldvalue = parameter.Value;

					parameter.Value = val + (val >= parameter.Value ? 1 : 0);
					individual.Mutations[parameterIndex] = parameter.Value - oldvalue;
				}
			}
			else
			{
				double part = mutationRate * (parameter.Boundary.Max - parameter.Boundary.Min);
				double oldvalue = parameter.Value;

				// Mutate parameter		
				parameter.Value += State.Random.Range(-part, part);
			
				// Keep within boundaries
				if (parameter.Value > parameter.Boundary.Max)
				{
					parameter.Value = parameter.Boundary.Max;
				}
				else if (parameter.Value < parameter.Boundary.Min)
				{
					parameter.Value = parameter.Boundary.Min;
				}
				
				individual.Mutations[parameterIndex] = parameter.Value - oldvalue;
			}
		}
		
		private List<Solution> Reproduce(List<Solution> parents)
		{
			double crossoverProbability = d_crossoverProbability.Evaluate(d_context);

			List<Solution> population = new List<Solution>();
			
			for (int i = 0; i < Configuration.PopulationSize; ++i)
			{
				// Select two parent individuals from the parents
				Solution p1 = parents[(int)System.Math.Round(State.Random.Range(0, parents.Count - 1))];
				Solution p2 = parents[(int)System.Math.Round(State.Random.Range(0, parents.Count - 1))];
				
				Individual child;
				
				// Make child from crossover or from a single parent
				if (State.Random.NextDouble() <= crossoverProbability)
				{
					child = Crossover(p1, p2);
				}
				else
				{
					child = (State.Random.NextDouble() < 0.5 ? p1.Clone() : p2.Clone()) as Individual;
				}
				
				child.Id = (uint)i;
				child.ResetMutations();
				
				// Apply mutation if necessary
				for (int p = 0; p < child.Parameters.Count; ++p)
				{
					double mutationProbability = d_mutationProbability[p].Evaluate(d_context);

					if (State.Random.NextDouble() <= mutationProbability)
					{
						bool isDiscrete = d_parameterDiscrete[p];
						double mutationRate = d_mutationRate[p].Evaluate(d_context);

						Mutate(child, p, mutationRate, isDiscrete);
					}
				}
				
				population.Add(child);
			}
			
			return population;
		}
		
		public override void Update()
		{
			// Select individuals from which to make a new population
			List<Solution> selection = Select();

			if (selection.Count == 0)
			{
				selection = new List<Solution>(Population);
			}
			
			// Reproduce using the selection
			List<Solution> population = Reproduce(selection);
			
			Population.Clear();
			Population.AddRange(population);
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			base.FromStorage(storage, optimizer);
			
			SetupExpressions();

			d_mutationRate.Clear();
			d_mutationProbability.Clear();
			d_parameterDiscrete.Clear();

			for (int i = 0; i < Parameters.Count; ++i)
			{
				object[] ret = Storage.QueryFirst("SELECT mutation_probability, mutation_rate, discrete FROM parameters WHERE name = @0", Parameters[i].Name);

				Optimization.Math.Expression mutationProbability;
				Optimization.Math.Expression mutationRate;
				bool discrete;

				if (ret == null)
				{
					mutationProbability = d_globalMutationProbability;
					mutationRate = d_globalMutationRate;
					discrete = Configuration.Discrete;
				}
				else
				{
					mutationProbability = new Optimization.Math.Expression();
					mutationProbability.Parse(ret[0].ToString());

					mutationRate = new Optimization.Math.Expression();
					mutationRate.Parse(ret[1].ToString());
					
					discrete = bool.Parse(ret[2].ToString());
				}

				d_mutationProbability.Add(mutationProbability);
				d_mutationRate.Add(mutationRate);
				d_parameterDiscrete.Add(discrete);
			}
		}
		
		public override void FromXml(System.Xml.XmlNode root)
		{
			base.FromXml(root);

			d_mutationProbability.Clear();
			d_mutationRate.Clear();
			
			XmlNodeList nodes = root.SelectNodes("parameters/parameter");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute mutationProbability = node.Attributes["mutation-probability"];
				XmlAttribute mutationRate = node.Attributes["mutation-rate"];
				XmlAttribute isDiscrete = node.Attributes["discrete"];
				
				XmlAttribute name = node.Attributes["name"];

				if (name == null)
				{
					continue;
				}
				
				Optimization.Parameter parameter = Parameter(name.Value);
				
				if (parameter == null)
				{
					continue;
				}

				Optimization.Math.Expression expression;

				// Parse per parameter mutation probability
				if (mutationProbability == null)
				{
					expression = d_globalMutationProbability;
				}
				else
				{
					expression = new Optimization.Math.Expression();

					if (!expression.Parse(mutationProbability.Value))
					{
						Console.Error.WriteLine("[Error] Could not parse mutation probability for the parameter ({0}): {1}", name.Value, expression.ErrorMessage);
						expression = d_globalMutationProbability;
					}
				}

				d_mutationProbability.Add(expression);	
				
				// Parse per parameter mutation rate
				if (mutationRate == null)
				{
					expression = d_globalMutationRate;
				}
				else
				{
					expression = new Optimization.Math.Expression();

					if (!expression.Parse(mutationRate.Value))
					{
						Console.Error.WriteLine("[Error] Could not parse mutation rate for the parameter ({0}): {1}", name.Value, expression.ErrorMessage);
						expression = d_globalMutationRate;
					}
				}

				d_mutationRate.Add(expression);

				bool discrete = (isDiscrete == null ? Configuration.Discrete : bool.Parse(isDiscrete.Value));
				
				if (!discrete)
				{
					d_parameterDiscrete.Add(false);
				}
				else
				{
					if (parameter.Boundary.Min % 1 != 0)
					{
						Console.Error.WriteLine("[Error] Minimum boundary value for discrete parameter ({0}) is not discrete: {1}", name.Value, parameter.Boundary.Min);
						parameter.Boundary.Min = System.Math.Floor(parameter.Boundary.Min);
					}

					if (parameter.Boundary.Max % 1 != 0)
					{
						Console.Error.WriteLine("[Error] Maximum boundary value for discrete parameter ({0}) is not discrete: {1}", name.Value, parameter.Boundary.Max);
						parameter.Boundary.Max = System.Math.Floor(parameter.Boundary.Max);
					}
					
					d_parameterDiscrete.Add(true);
				}
			}
		}
	}
}
