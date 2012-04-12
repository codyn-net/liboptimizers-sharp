/*
 *  PMPSO.cs - This file is part of pmpso-sharp
 *
 *  Copyright (C) 2010 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License as published by the 
 * Free Software Foundation; either version 2.1 of the License, or (at your 
 * option) any later version.
 * 
 * This library is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License 
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using PSONS = Optimization.Optimizers.PSO;
using System.Xml;
using System.Text.RegularExpressions;

namespace Optimization.Optimizers.PMPSO
{
	[Attributes.Optimizer(Description="Partial Mutation Particle Swarm Optimization")]
	public class PMPSO : PSONS.PSO
	{
		private Dictionary<uint, Particle> d_subswarmBests;
		private Biorob.Math.Expression d_mutationProbability;
		private Biorob.Math.Expression d_socialMutationProbability;
		private Biorob.Math.Expression d_cognitiveMutationProbability;
		private List<Neighborhood> d_neighborhoods;
		private double d_nextMerge;
		
		public PMPSO()
		{
			d_mutationProbability = new Biorob.Math.Expression();
			d_socialMutationProbability = new Biorob.Math.Expression();
			d_cognitiveMutationProbability = new Biorob.Math.Expression();
			d_neighborhoods = new List<Neighborhood>();
		}
		
		public override void InitializePopulation()
		{
			base.InitializePopulation();
			
			d_mutationProbability.Parse(Configuration.MutationProbability);
			d_socialMutationProbability.Parse(Configuration.SocialMutationProbability);
			d_cognitiveMutationProbability.Parse(Configuration.CognitiveMutationProbability);
			
			if (Configuration.MinNeighborhoods <= 0)
			{
				Configuration.MinNeighborhoods = 1;
			}
			
			d_subswarmBests = new Dictionary<uint, Particle>();
			int idx = 0;
			
			if (Configuration.FusedParticles <= 0)
			{
				throw new Exception("Number of fused particles must be larger than 0");
			}
			
			// Do the particle fusion
			Neighborhood neighborhood = new Neighborhood();
			
			/* Calculate the number of fusion sets in each neighborhood to fullfil
			 * the initial number of neighborhoods constraint:
			 * 
			 * n = Ceil(PopulationSize / FusionSize) / InitialNeighborhoods
			 *  */
			double fusionPerNeighborhood;
			
			if (Configuration.InitialNeighborhoods <= 0)
			{
				// Maximum number of neighborhoods
				fusionPerNeighborhood = 2;
			}
			else
			{
				fusionPerNeighborhood = System.Math.Ceiling(Population.Count / (double)Configuration.FusedParticles) / Configuration.InitialNeighborhoods;
			}
			
			if (fusionPerNeighborhood < 1)
			{
				fusionPerNeighborhood = 1;
			}
			
			double nextFusion = fusionPerNeighborhood;

			while (idx < Population.Count)
			{
				List<Solution> solutions = Population.GetRange(idx, System.Math.Min(Configuration.FusedParticles, Population.Count - idx));
				List<Particle> particles = new List<Particle>();

				idx += Configuration.FusedParticles;
				
				foreach (Solution solution in solutions)
				{
					particles.Add((Particle)solution);
				}
				
				// Create new fusion set for the particles				
				Fusion fus = new Fusion(particles, State);
				
				// Initialize will intialize the subswarm configuration of the fusion set
				fus.Initialize();
				
				// Add the fusion set to the neighborhood
				neighborhood.Add(fus);
				
				if (neighborhood.Count >= nextFusion)
				{
					nextFusion += fusionPerNeighborhood - neighborhood.Count;

					d_neighborhoods.Add(neighborhood);
					neighborhood = new Neighborhood();
				}
			}
			
			if (neighborhood.Count != 0)
			{
				d_neighborhoods.Add(neighborhood);
			}
			
			if (Configuration.MergeInterval == 0)
			{
				/* Calculate the merge interval based on the number of neighborhoods,
				 * number of particles and number of iterations.
				 * 
				 * Number of merges 'nm':
				 * ------------------------------------------------------ 
				 * 2^n = numNeighborhoods
				 * n = log(numNeighborhoods - minNeighborhoods) / log(2)
				 * interval = maxIterations / (n + 1)
				 */
				double num = System.Math.Ceiling(System.Math.Log(d_neighborhoods.Count / Configuration.MinNeighborhoods) / System.Math.Log(2));
				Configuration.MergeInterval = Configuration.MaxIterations / (double)(num + 1);
			}
			
			d_nextMerge = Configuration.MergeInterval;
		}

		// Override 'Configuration' property returning subclassed Settings
		public new Optimization.Optimizers.PMPSO.Settings Configuration
		{
			get
			{
				return (Optimization.Optimizers.PMPSO.Settings)base.Configuration;
			}
		}
		
		protected override Optimization.State CreateState()
		{
			return new State(Configuration);
		}
		
		protected override Settings CreateSettings()
		{
			// Create pso settings
			return new Optimization.Optimizers.PMPSO.Settings();
		}
		
		public override Solution CreateSolution(uint idx)
		{
			return new Particle(idx, Fitness, (Optimization.State)State);
		}
		
		protected override void SaveIteration()
		{
			base.SaveIteration();
			
			Storage.SaveActiveParameters();
		}
		
		public override void Update()
		{
			Dictionary<string, object> vars = new Dictionary<string, object>();
			vars["k"] = CurrentIteration;
			vars["P"] = Population.Count;
			vars["N"] = Configuration.MaxIterations;
			
			// Evaluate the expressions for the different probabilities
			State.MutationProbability = d_mutationProbability.Evaluate(vars, Biorob.Math.Constants.Context);
			State.SocialMutationProbability = d_socialMutationProbability.Evaluate(vars, Biorob.Math.Constants.Context);
			State.CognitiveMutationProbability = d_cognitiveMutationProbability.Evaluate(vars, Biorob.Math.Constants.Context);
			
			// Update the population
			base.Update();
			
			// Mutate all the fused particles
			foreach (Neighborhood neighborhood in d_neighborhoods)
			{
				// The global best for mutation is taken from the neighborhood
				Particle gbest = neighborhood.ConfigurationBest;

				foreach (Fusion fusion in neighborhood)
				{
					/* Mutate the fused particle set according to the global
					 * best configuration of the neighborhood
					 */
					fusion.Mutate(gbest);
				}
			}
			
			// Merge neighborhoods if needed
			if (CurrentIteration >= d_nextMerge && d_neighborhoods.Count / 2 >= Configuration.MinNeighborhoods)
			{
				MergeNeighborhoods();
				d_nextMerge += Configuration.MergeInterval;
			}
		}
		
		private void MergeNeighborhoods()
		{
			// Sort all the neighborhoods based on their best configuration fitness
			List<Neighborhood> sorted = new List<Neighborhood>(d_neighborhoods);
			sorted.Sort(delegate (Neighborhood a, Neighborhood b) {
				return a.ConfigurationBest.Fitness > b.ConfigurationBest.Fitness ? -1 : 1;	
			});

			int i = 0;
			d_neighborhoods.Clear();
			
			// Top/bottom merge the neighborhoods
			while (true)
			{
				int left = i;
				int right = sorted.Count - i - 1;
				
				if (left > right)
				{
					break;
				}
				else if (left == right)
				{
					// Only one left
					d_neighborhoods.Add(sorted[left]);
					break;
				}
				else
				{
					sorted[left].Merge(sorted[right]);
					d_neighborhoods.Add(sorted[left]);
				}
				
				++i;
			}
		}
		
		private void UpdateSubswarmBests()
		{
			// Store new subswarm bests
			foreach (Solution solution in Population)
			{
				Particle particle = (Particle)solution;
				Particle sbest;

				/* Update the subswarm best is there is no best yet for the
				 * subswarm in which this particle resides, or if the fitness
				 * of the particle is bigger than the fitness of the best until
				 * now */
				if (!d_subswarmBests.TryGetValue(particle.Hash, out sbest) ||
				    particle.Fitness > sbest.Fitness)
				{
					d_subswarmBests[particle.Hash] = (Particle)particle.Clone();
				}
			}
		}

		protected override void UpdateBest()
		{
			base.UpdateBest();

			// Update swarm bests
			UpdateSubswarmBests();
			
			// Update the bests for all the neighborhoods
			foreach (Neighborhood neighborhood in d_neighborhoods)
			{
				neighborhood.UpdateBest();
			}
		}
		
		protected override PSONS.Particle GetUpdateBest(PSONS.Particle particle)
		{
			// Get the subswarm best for this particle
			Particle sbest = null;
			d_subswarmBests.TryGetValue(((Particle)particle).Hash, out sbest);
			
			return sbest;
		}
		
		public new State State
		{
			get
			{
				return (Optimization.Optimizers.PMPSO.State)base.State;
			}
		}
		
		public override void FromXml(XmlNode root)
		{
			base.FromXml(root);
			
			State.MutationSets.Clear();
			
			// Parse custom xml nodes for defining mutation sets
			XmlNodeList nodes = root.SelectNodes("mutation-set");
			
			foreach (XmlNode node in nodes)
			{
				ParseMutationSet(node);
			}
		}
		
		private int ParameterFromName(string name)
		{
			if (String.IsNullOrEmpty(name))
			{
				return -1;
			}
			
			for (int i = 0; i < Parameters.Count; ++i)
			{
				if (Parameters[i].Name == name)
				{
					return i;
				}
			}

			return -1;
		}
		
		private bool AddParameterSet(MutationSet s, string[] parameters)
		{
			List<uint> indices = new List<uint>();
			
			foreach (string name in parameters)
			{
				string nm = name.Trim();
				int idx = ParameterFromName(nm);
				
				if (idx < 0)
				{
					Console.Error.WriteLine("Could not find parameter `{0}' for mutation set", nm);
					return false;
				}
				
				indices.Add((uint)idx);
			}
			
			if (indices.Count != 0)
			{
				s.Add(indices.ToArray());
			}
			
			return true;
		}
		
		private List<string> MatchParameterNames(string pattern)
		{
			Regex r;

			r = new Regex(pattern);
			
			List<string> ret = new List<string>();
			
			foreach (Parameter parameter in Parameters)
			{
				if (r.IsMatch(parameter.Name))
				{
					ret.Add(parameter.Name);
				}
			}
			
			return ret;
		}
		
		private string[] ParameterSetNames(XmlNode node)
		{
			XmlAttribute attr = node.Attributes["match"];
			
			if (attr != null && attr.Value == "regex")
			{
				return MatchParameterNames(node.InnerText).ToArray();
			}
			else
			{
				return node.InnerText.Split(',');
			}
		}
		
		private void ParseMutationSet(XmlNode node)
		{
			MutationSet s = new MutationSet();
			
			XmlNodeList parameters = node.SelectNodes("parameters");
			XmlNodeList parameter = node.SelectNodes("parameter");
			
			if (parameters.Count == 0 && parameter.Count == 0)
			{
				if (!AddParameterSet(s, ParameterSetNames(node)))
				{
					return;
				}
			}
			
			foreach (XmlNode p in parameter)
			{
				if (!AddParameterSet(s, ParameterSetNames(p)))
				{
					return;
				}
			}
			
			foreach (XmlNode p in parameters)
			{
				List<string> names = new List<string>();

				foreach (XmlNode c in p.SelectNodes("parameter"))
				{
					names.AddRange(ParameterSetNames(c));
				}
				
				if (!AddParameterSet(s, names.ToArray()))
				{
					return;
				}
			}
			
			if (s.Count == 1)
			{
				// Add empty set
				s.Insert(0, new uint[] {});
			}
			
			if (s.Count != 0)
			{			
				State.MutationSets.Add(s);
			}
			else
			{
				Console.Error.WriteLine("Ignoring empty mutation set");
			}
		}
	}
}
