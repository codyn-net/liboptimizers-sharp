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
 *     <stage condition="speed &gt;= 1.1 &amp;&amp; speed &lt;= 1.3">1 / torque</stage>
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
		private List<object> d_lastBest;

		public StagePSO(Job job) : base(job)
		{
			d_stages = new List<Stage>();
			d_lastBest = new List<object>();
		}
		
		private void Setup()
		{
			Fitness.OverrideCompare(CompareSolutions);
		}
		
		private int CompareSolutions(Fitness f1, Fitness f2)
		{
			Stage s1 = f1.GetUserData<Stage>("StagePSOStage");
			Stage s2 = f2.GetUserData<Stage>("StagePSOStage");
			
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
		
		public override bool SuppressConvergenceCalculation()
		{
			return true;
		}
		
		private bool BestDifference(int window, out double val)
		{
			// Check for the last 'window' obtained fitnesses in the last stage
			int ptr = d_lastBest.Count - 1;
			double minval = double.MaxValue;
			double maxval = double.MinValue;
			
			val = 0;

			while (window >= 0 && ptr >= 0)
			{
				if (d_lastBest[ptr] != null)
				{
					double fitval = (double)d_lastBest[ptr];
					maxval = Math.Max(maxval, fitval);
					minval = Math.Min(minval, fitval);

					--window;
				}

				--ptr;
			}
			
			if (window < 0)
			{
				val = maxval - minval;
				return true;
			}
			else
			{
				return false;
			}
		}
		
		public override bool Finished()
		{
			Optimizer opti = Job.Optimizer;
			
			uint miniter = (uint)opti.MinIterations.Evaluate(Biorob.Math.Constants.Context);
			
			Solution best = opti.Best;
			
			if (best != null)
			{
				Stage stage = best.Fitness.GetUserData<Stage>("StagePSOStage");
				
				if (stage == d_stages[d_stages.Count - 1])
				{
					d_lastBest.Add(best.Fitness.Value);
				}
				else
				{
					d_lastBest.Add(null);
				}
			}
			else
			{
				d_lastBest.Add(null);
			}
			
			if (opti.CurrentIteration < miniter)
			{
				return false;
			}
			
			double threshold = opti.ConvergenceThreshold.Evaluate(Biorob.Math.Constants.Context);
			uint window = (uint)opti.ConvergenceWindow.Evaluate(Biorob.Math.Constants.Context);
			
			double diff;
			
			if (threshold > 0 && opti.CurrentIteration >= window && BestDifference((int)window, out diff))
			{
				return diff <= threshold;
			}
			
			return false;
		}

		public override void Initialize()
		{
			base.Initialize();
			
			StoreStages();
			
			foreach (Solution sol in Job.Optimizer.Population)
			{
				sol.Data["StagePSO::waslast"] = "0";
				sol.Data["StagePSO::islast"] = "0";
				sol.Data["StagePSO::stage"] = "0";
			}
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

		public override void UpdateFitness(Solution solution)
		{
			base.UpdateFitness(solution);

			PSO.Particle particle = (PSO.Particle)solution;

			for (int i = 0; i < d_stages.Count; ++i)
			{
				Stage stage = d_stages[i];
				Stage next = i != d_stages.Count - 1 ? d_stages[i + 1] : null;

				if (next == null || !next.Validate(particle.Fitness.Context))
				{
					particle.Fitness.SetUserData("StagePSOStage", stage);
					particle.Fitness.Value = stage.Value(particle.Fitness.Context);

					particle.Data["StagePSO::stage"] = i;
					particle.Data["StagePSO::waslast"] = particle.Data["StagePSO::islast"];
					particle.Data["StagePSO::islast"] = (i == d_stages.Count - 1 ? "1" : "0");

					break;
				}
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
	}
}

