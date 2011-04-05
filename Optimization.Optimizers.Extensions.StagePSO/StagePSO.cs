using System;
using System.Collections.Generic;
using Optimization;
using System.Xml;
using System.Data;

/**
 *
 * StagePSO (Staged Fitness PSO)
 *
 * This PSO extension implements a neighborhood for particles by a cascading
 * set of conditions on fitness criteria. You can use this to create staged
 * fitness optimization where criteria have to be satisfied sequentially instead
 * of in parallel.
 *
 * Example XML specification
 *
 * <extensions>
 *   <extension name="stagepso">
 *     <stage>speed</stage>
 *     <stage condition="speed >= 1.1 && speed <= 1.3">1 / torque</stage>
 *   </extension>
 * </extensions>
 *
 **/
namespace Optimization.Optimizers.Extensions.StagePSO
{
	[Optimization.Attributes.Extension(Description = "Staged Fitness PSO", AppliesTo = new Type[] {typeof(PSO.PSO)})]
	public class StagePSO : Extension, PSO.IPSOExtension
	{
		private List<Stage> d_stages;
		private Dictionary<Solution, Stage> d_neighborhoods;

		public StagePSO(Job job) : base(job)
		{
			d_stages = new List<Stage>();
			d_neighborhoods = new Dictionary<Solution, Stage>();
		}
		
		public override void FromXml(XmlNode root)
		{
			base.FromXml(root);
			
			foreach (XmlNode node in root.SelectNodes("stage"))
			{
				d_stages.Add(new Stage(node));
			}
		}
		
		private void UpdateNeighborhoods()
		{
			// Collect solutions in stages
			List<Solution> population = new List<Solution>(Job.Optimizer.Population);
			
			d_neighborhoods.Clear();
			
			for (int i = 0; i < d_stages.Count; ++i)
			{
				Stage stage = d_stages[i];
				Stage next = i != d_stages.Count - 1 ? d_stages[i + 1] : null;
				
				stage.Clear();
				
				population.RemoveAll(delegate (Solution sol) {
					PSO.Particle particle = (PSO.Particle)sol;

					if (next == null || !next.Validate(particle.Fitness.Context))
					{
						stage.Add(particle);
						d_neighborhoods[sol] = stage;

						sol.Fitness.Value = stage.Value(particle.Fitness.Context);
						sol.Data["stage"] = i;

						return true;
					}
					else
					{
						return false;
					}
				});
			}
		}
		
		public override void Initialize()
		{
			base.Initialize();
			
			StoreStages();
		}
		
		private void StoreStages()
		{
			Storage.Storage storage = Job.Optimizer.Storage;

			storage.Query("DROP TABLE IF EXISTS `stages`");
			storage.Query("CREATE TABLE `stages` (`id` INTEGER PRIMARY KEY, `condition` TEXT, `expression` TEXT)");
			
			for (int i = 0; i < d_stages.Count; ++i)
			{
				Stage stage = d_stages[i];

				storage.Query("INSERT INTO `stages` (`condition`, `expression`) VALUES (@0, @1)",
				              stage.Condition != null ? stage.Condition.Text : null,
				              stage.Expression.Text);
			}
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			base.FromStorage(storage, optimizer);
			
			storage.Query("SELECT `expression`, `condition` FROM `stages` ORDER BY `id`", delegate (IDataReader reader)
			{
				string expression = (string)reader[0];
				string condition = (string)reader[1];

				d_stages.Add(new Stage(expression, condition));
				return true;
			});
		}
	
		public double CalculateVelocityUpdate(PSO.Particle particle, PSO.Particle best, int i)
		{
			return 0;
		}

		public PSO.State.VelocityUpdateType VelocityUpdateComponents(PSO.Particle particle)
		{
			return PSO.State.VelocityUpdateType.Default;
		}

		public void ValidateVelocityUpdate(PSO.Particle particle, double[] velocityUpdate)
		{
		}

		public PSO.Particle GetUpdateBest(PSO.Particle particle)
		{
			if (d_neighborhoods.ContainsKey(particle))
			{
				return d_neighborhoods[particle].Best;
			}
			else
			{
				return null;
			}
		}
		
		public override Solution UpdateBest()
		{
			UpdateNeighborhoods();

			for (int i = d_stages.Count - 1; i >= 0; --i)
			{
				if (d_stages[i].Best != null)
				{
					return d_stages[i].Best;
				}
			}
			
			return null;
		}
	}
}

