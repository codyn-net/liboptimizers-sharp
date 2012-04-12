using System;
using System.Collections.Generic;
using System.Xml;
using System.Data;

namespace Optimization.Optimizers.GA
{
	[Optimization.Attributes.OptimizerAttribute(Description="Genetic Algorithm")]
	public class GA : Optimization.Optimizer
	{
		private class ParameterAugmentation
		{
			public Biorob.Math.Expression MutationProbability;
			public Biorob.Math.Expression MutationRate;
			public bool IsDiscrete;
			
			public ParameterAugmentation()
			{
			}
		}

		private Biorob.Math.Expression d_tournamentSize;
		private Biorob.Math.Expression d_tournamentProbability;
		private Biorob.Math.Expression d_mutationProbability;
		private Biorob.Math.Expression d_mutationRate;
		private Biorob.Math.Expression d_crossoverProbability;
		
		private Dictionary<string, object> d_context;
		private List<ParameterAugmentation> d_parameterAugmentation;

		// Override 'Configuration' property returning subclassed Settings
		public new Optimization.Optimizers.GA.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimization.Optimizers.GA.Settings;
			}
		}
		
		private void SetupExpressions()
		{
			d_context = new Dictionary<string, object>();
			d_context["k"] = 0;
			
			d_tournamentSize = new Biorob.Math.Expression();
			d_tournamentSize.Parse(Configuration.TournamentSize);
			
			d_tournamentProbability = new Biorob.Math.Expression();
			d_tournamentProbability.Parse(Configuration.TournamentProbability);
			
			d_mutationProbability = new Biorob.Math.Expression();
			d_mutationProbability.Parse(Configuration.MutationProbability);
			
			d_crossoverProbability = new Biorob.Math.Expression();
			d_crossoverProbability.Parse(Configuration.CrossoverProbability);
			
			d_mutationRate = new Biorob.Math.Expression();
			d_mutationRate.Parse(Configuration.MutationRate);
		}
		
		public override void Initialize()
		{
			base.Initialize();
			
			SetupExpressions();
			StoreAugmentation();
		}
		
		private void StoreAugmentation()
		{
			Storage.Query("ALTER TABLE `parameters` ADD COLUMN `mutation_rate` TEXT");
			Storage.Query("ALTER TABLE `parameters` ADD COLUMN `mutation_probability` TEXT");
			Storage.Query("ALTER TABLE `parameters` ADD COLUMN `discrete` INT");
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				ParameterAugmentation aug = d_parameterAugmentation[i];
				
				Storage.Query("UPDATE `parameters` SET `mutation_rate` = @0, `mutation_probability` = @1, `discrete` = @2 WHERE `name` = @3",
				              aug.MutationRate != null ? aug.MutationRate.Text : null,
				              aug.MutationProbability != null ? aug.MutationProbability.Text : null,
				              aug.IsDiscrete ? 1 : 0,
				              Parameters[i].Name);
			}
		}
		
		protected override Settings CreateSettings()
		{
			return new Optimization.Optimizers.GA.Settings();
		}
		
		protected override void IncrementIteration()
		{
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
		
		private Solution Crossover(Solution p1, Solution p2)
		{
			// Define crossover point in parameters
			int num = System.Math.Min(p1.Parameters.Count, p2.Parameters.Count);
			int cutPoint = (int)System.Math.Round(State.Random.Range(0, num - 2));
			
			Solution ret = new Solution(0, Fitness, State);
			
			for (int i = 0; i < cutPoint + (p2.Parameters.Count - cutPoint); ++i)
			{
				Solution take = i <= cutPoint ? p1 : p2;
				ret.Add(take.Parameters[i].Clone() as Parameter);
			}
			
			return ret;
		}
		
		public override void InitializePopulation()
		{
			base.InitializePopulation();
			
			foreach (Solution solution in Population)
			{
				for (int i = 0; i < solution.Parameters.Count; ++i)
				{
					EnsureDiscrete(solution.Parameters[i], i);
				}
			}
		}
		
		private void EnsureDiscrete(Parameter parameter)
		{
			EnsureDiscrete(parameter, -1);
		}
		
		private void EnsureDiscrete(Parameter parameter, int idx)
		{
			if (idx < 0)
			{
				idx = Parameters.IndexOf(parameter);
			}
			
			if (d_parameterAugmentation[idx].IsDiscrete)
			{
				double v = Math.Round(parameter.Value);
				
				parameter.Value = Math.Min(Math.Max(v, Math.Ceiling(parameter.Boundary.Min)), Math.Floor(parameter.Boundary.Max));
			}
		}
		
		private void Mutate(Parameter parameter, double mutationRate, int idx)
		{
			double part = mutationRate * (parameter.Boundary.Max - parameter.Boundary.Min);

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
			
			EnsureDiscrete(parameter, idx);
		}
		
		private List<Solution> Reproduce(List<Solution> parents)
		{
			double globalMutationProbability;
			double crossoverProbability;
			List<Solution> population = new List<Solution>();
			
			globalMutationProbability = d_mutationProbability.Evaluate(d_context);
			crossoverProbability = d_crossoverProbability.Evaluate(d_context);
			
			for (int i = 0; i < Configuration.PopulationSize; ++i)
			{
				// Select two parent individuals from the parents
				Solution p1 = parents[(int)System.Math.Round(State.Random.Range(0, parents.Count - 1))];
				Solution p2 = parents[(int)System.Math.Round(State.Random.Range(0, parents.Count - 1))];
				
				Solution child;
				
				// Make child from crossover or from a single parent
				if (State.Random.NextDouble() <= crossoverProbability)
				{
					child = Crossover(p1, p2);
				}
				else
				{
					child = (State.Random.NextDouble() < 0.5 ? p1.Clone() : p2.Clone()) as Solution;
				}
				
				child.Id = (uint)i;
				
				// Apply mutation if necessary
				for (int j = 0; j < child.Parameters.Count; ++j)
				{
					ParameterAugmentation aug = d_parameterAugmentation[j];
					double mutprob = globalMutationProbability;
					
					if (aug.MutationProbability != null)
					{
						mutprob = aug.MutationProbability.Evaluate(d_context);
					}

					if (State.Random.NextDouble() <= mutprob)
					{
						double mutrate;
						
						if (aug.MutationRate != null)
						{
							mutrate = aug.MutationRate.Evaluate(d_context);
						}
						else
						{
							mutrate = d_mutationRate.Evaluate(d_context);
						}

						Mutate(child.Parameters[j], mutrate, j);
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
			
			d_parameterAugmentation = new List<ParameterAugmentation>(Parameters.Count);
			for (int i = 0; i < Parameters.Count; ++i)
			{
				d_parameterAugmentation.Add(null);
			}
			
			Storage.Query("SELECT `name`, `mutation_rate`, `mutation_probability`, `discrete` FROM `parameters` ORDER BY `id`", delegate (IDataReader reader) {
				string name = reader.GetString(0);
				object mutrate = reader.GetValue(1);
				object mutprob = reader.GetValue(2);
				bool discrete = reader.GetInt32(3) == 1;
				
				ParameterAugmentation aug = new ParameterAugmentation();
				
				aug.IsDiscrete = discrete;
				
				int idx = Parameters.IndexOf(Parameter(name));
				
				if (mutrate != null)
				{
					Biorob.Math.Expression.Create(reader.GetString(1), out aug.MutationRate);
				}
				
				if (mutprob != null)
				{
					Biorob.Math.Expression.Create(reader.GetString(2), out aug.MutationProbability);
				}
				
				d_parameterAugmentation[idx] = aug;
				return true;
			});
			
			SetupExpressions();
		}
		
		public override void FromXml(XmlNode root)
		{
			base.FromXml(root);
			
			d_parameterAugmentation = new List<ParameterAugmentation>(Parameters.Count);
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				d_parameterAugmentation.Add(null);
			}
			
			foreach (XmlNode node in root.SelectNodes("parameters/parameter"))
			{
				XmlAttribute mutprob = node.Attributes["mutation-probability"];
				XmlAttribute mutrate = node.Attributes["mutation-rate"];
				XmlAttribute isdisc = node.Attributes["discrete"];
				XmlAttribute name = node.Attributes["name"];
				
				Parameter param = Parameter(name.Value);
				int idx = Parameters.IndexOf(param);
				
				ParameterAugmentation aug = new ParameterAugmentation();
				
				if (mutprob != null)
				{
					Biorob.Math.Expression.Create(mutprob.Value, out aug.MutationProbability);
				}
				
				if (mutrate != null)
				{
					Biorob.Math.Expression.Create(mutrate.Value, out aug.MutationRate);
				}
				
				if (isdisc != null)
				{
					aug.IsDiscrete = bool.Parse(isdisc.Value);
				}
				else
				{
					aug.IsDiscrete = false;
				}
				
				d_parameterAugmentation[idx] = aug;
			}
		}
	}
}
