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
	public class StagePSO : Extension
	{
		private List<Stage> d_stages;
		private Dictionary<Solution, Stage> d_neighborhoods;

		public StagePSO(Job job) : base(job)
		{
			d_stages = new List<Stage>();
			d_neighborhoods = new Dictionary<Solution, Stage>();
		}
		
		private void Setup()
		{
			Fitness.OverrideCompare(CompareSolutions);
		}
		
		private int CompareSolutions(Fitness f1, Fitness f2)
		{
			Stage s1 = f1.GetUserData<Stage>("StagePSOStage");
			Stage s2 = f2.GetUserData<Stage>("StagePSOStage");
			
			// Just because in the first iteration, the progress is sent
			// before calculating the stages
			if (s1 == null && s2 == null)
			{
				UpdateNeighborhoods();

				s1 = f1.GetUserData<Stage>("StagePSOStage");
				s2 = f2.GetUserData<Stage>("StagePSOStage");
			}
			
			if (s1.Priority > s2.Priority)
			{
				return 1;
			}
			
			if (s2.Priority > s1.Priority)
			{
				return -1;
			}
			
			return s1.Compare(f1, f2);
		}
		
		public override void FromXml(XmlNode root)
		{
			uint priority = 0;
			base.FromXml(root);
			
			foreach (XmlNode node in root.SelectNodes("stage"))
			{
				d_stages.Add(new Stage(node, priority++));
			}
			
			Setup();
		}
		
		public override bool Finished()
		{
			return base.Finished();
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
						
						particle.Fitness.SetUserData("StagePSOStage", stage);
						particle.Fitness.Value = stage.Value(particle.Fitness.Context);
						
						particle.Data["stage"] = i;
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
			uint priority = 0;
			
			storage.Query("SELECT `expression`, `condition` FROM `stages` ORDER BY `id`", delegate (IDataReader reader)
			{
				string expression = (string)reader[0];
				string condition = (string)reader[1];

				d_stages.Add(new Stage(expression, condition, priority++));
				return true;
			});
			
			Setup();
		}
		
		public override void Next()
		{
			base.Next();
			UpdateNeighborhoods();
		}
	}
}

