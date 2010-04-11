using System;
using System.Collections.Generic;

namespace Optimization.Optimizers.GA
{
	[Optimization.Attributes.OptimizerAttribute(Description="Genetic Algorithm")]
	public class GA : Optimization.Optimizer
	{
		private Optimization.Math.Expression d_tournamentSize;
		private Optimization.Math.Expression d_tournamentProbability;
		private Optimization.Math.Expression d_mutationProbability;
		private Optimization.Math.Expression d_mutationRate;
		private Optimization.Math.Expression d_crossoverProbability;
		
		private Dictionary<string, object> d_context;

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
			
			d_tournamentSize = new Optimization.Math.Expression();
			d_tournamentSize.Parse(Configuration.TournamentSize);
			
			d_tournamentProbability = new Optimization.Math.Expression();
			d_tournamentProbability.Parse(Configuration.TournamentProbability);
			
			d_mutationProbability = new Optimization.Math.Expression();
			d_mutationProbability.Parse(Configuration.MutationProbability);
			
			d_crossoverProbability = new Optimization.Math.Expression();
			d_crossoverProbability.Parse(Configuration.CrossoverProbability);
			
			d_mutationRate = new Optimization.Math.Expression();
			d_mutationRate.Parse(Configuration.MutationRate);
		}
		
		public override void Initialize ()
		{
			base.Initialize();
			
			SetupExpressions();
		}
		
		protected override Settings CreateSettings()
		{
			return new Optimization.Optimizers.GA.Settings();
		}
		
		protected override void IncrementIteration ()
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
		
		private void Mutate(Parameter parameter, double mutationRate)
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
		}
		
		private List<Solution> Reproduce(List<Solution> parents)
		{
			double mutationProbability = d_mutationProbability.Evaluate(d_context);
			double crossoverProbability = d_crossoverProbability.Evaluate(d_context);
			List<Solution> population = new List<Solution>();
			
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
				double mutationRate = d_mutationRate.Evaluate(d_context);
				
				// Apply mutation if necessary
				foreach (Parameter parameter in child.Parameters)
				{
					if (State.Random.NextDouble() <= mutationProbability)
					{
						Mutate(parameter, mutationRate);
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
			
			// Reproduce using the selection
			List<Solution> population = Reproduce(selection);
			
			Population.Clear();
			Population.AddRange(population);
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			base.FromStorage(storage, optimizer);
			
			SetupExpressions();
		}
	}
}
