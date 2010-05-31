/*
 *  Systematic.cs - This file is part of optimizers-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
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
using System.Collections.Generic;
using System.Xml;
using System.Data;

namespace Optimization.Optimizers.Systematic
{
	[Optimization.Attributes.Optimizer(Description="Systematic search")]
	public class Systematic : Optimization.Optimizer
	{		
		List<Range> d_ranges;
		uint d_currentId;
		uint d_numberOfSolutions;
		
		public Systematic()
		{
			d_currentId = 0;
			d_ranges = new List<Range>();
		}
		
		public override void Initialize()
		{
			base.Initialize();
			
			// Save ranges
			StoreRanges();
		}
		
		private void StoreRanges()
		{
			Storage.Query("DROP TABLE IF EXISTS `ranges`");
			Storage.Query("CREATE TABLE `ranges` (`id` INTEGER PRIMARY KEY, `step` DOUBLE, `step_repr` TEXT, `steps` INT, `steps_repr` TEXT)");
			
			for (int i = 0; i < d_ranges.Count; ++i)
			{
				Range range = d_ranges[i];

				object ret = Storage.QueryValue("SELECT `id` FROM `parameters` WHERE `name` = @0", Parameters[i].Name);

				Storage.Query(@"INSERT INTO `ranges` (`id`, `step`, `step_repr`, `steps`, `steps_repr`)
				                VALUES (@0, @1, @2, @3, @4)",
				              ret,
				              range.Step.Value,
				              range.Step.Representation,
				              range.Steps != null ? range.Steps.Value : -1,
				              range.Steps != null ? range.Steps.Representation : "");
			}
		}
		
		public new Optimization.Optimizers.Systematic.Settings Configuration
		{
			get
			{
				return base.Configuration as Optimization.Optimizers.Systematic.Settings;
			}
		}
		
		protected override Settings CreateSettings()
		{
			return new Optimization.Optimizers.Systematic.Settings();
		}
		
		public override void InitializePopulation()
		{
			d_currentId = Configuration.StartIndex;
			Update();
		}
		
		private Solution GenerateSolution(uint idx)
		{
			Optimization.Solution solution = CreateSolution(idx);
			
			// Set solution parameter template
			solution.Parameters = Parameters;
			
			uint ptr = d_numberOfSolutions;

			// Fill parameters
			for (int i = 0; i < d_ranges.Count; ++i)
			{
				Range range = d_ranges[i];
				double[] values = range.ToArray();

				uint ptrRest = ptr / (uint)values.Length;

				uint pidx = idx / ptrRest;
				idx = idx % ptrRest;

				solution.Parameters[i].Value = values[pidx];				
				ptr = ptrRest;
			}

			return solution;
		}
		
		public override void FromXml(System.Xml.XmlNode root)
		{
			base.FromXml(root);
			
			// Parse systematic test ranges, and create parameters
			d_ranges.Clear();
			d_currentId = 0;
			d_numberOfSolutions = 0;

			XmlNodeList nodes = root.SelectNodes("parameters/parameter");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute name = node.Attributes["name"];

				if (name == null)
				{
					continue;
				}
				
				// Get previously parsed parameter, min and max values are already taken from
				// the boundary here. We just need to determine the range, using step or steps
				Parameter parameter = Parameter(name.Value);

				XmlAttribute step = node.Attributes["step"];
				XmlAttribute steps = node.Attributes["steps"];
				
				Range range = new Range(name.Value, parameter.Boundary);
				
				if (step != null)
				{
					range.Step.Representation = step.Value;
				}
				else if (steps != null)
				{
					range.Steps = new NumericSetting();
					range.Steps.Representation = steps.Value;
					
					if (range.Steps.Value == 1)
					{
						range.Step.Value = 0;
					}
					else
					{
						range.Step.Value = (range.Boundary.Max - range.Boundary.Min) / (range.Steps.Value - 1);
					}
				}
				
				if (range.Boundary.Max > range.Boundary.Min != range.Step.Value > 0)
				{
					throw new Exception(String.Format("XML: Invalid range specified {0}. Boundaries and steps result in 0 values.", name.Value));
				}
				
				d_ranges.Add(range);
				double[] all = range.ToArray();
				
				if (d_numberOfSolutions == 0)
				{
					d_numberOfSolutions = (uint)all.Length;
				}
				else
				{
					d_numberOfSolutions *= (uint)all.Length;
				}
			}
			
			Configuration.MaxIterations = (uint)System.Math.Ceiling(d_numberOfSolutions / (double)Configuration.PopulationSize);
		}
		
		public override void Update()
		{
			Population.Clear();
			
			for (int i = 0; i < Configuration.PopulationSize; ++i)
			{
				if (d_currentId >= d_numberOfSolutions)
				{
					break;
				}
				
				Add(GenerateSolution(d_currentId));
				d_currentId++;
			}
		}
		
		protected override bool Finished ()
		{
			return d_currentId >= d_numberOfSolutions;
		}
		
		public override void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			base.FromStorage(storage, optimizer);
			
			d_ranges.Clear();
			
			d_numberOfSolutions = 0;
			
			// Read ranges from the storage
			storage.Query("SELECT `parameters`.`name`, `ranges`.`step_repr`, `ranges`.`steps_repr` FROM `ranges` LEFT JOIN `parameters` ON (`parameters`.`id` = `ranges`.`id`) ORDER BY `parameters`.`id`", delegate (IDataReader reader) {
				string name = (string)reader["name"];
				string step = (string)reader["step_repr"];
				string steps = (string)reader["steps_repr"];
				
				Parameter parameter = Parameter(name);
				Range range = new Range(parameter.Name, parameter.Boundary);
				
				range.Step.Representation = step;

				if (!String.IsNullOrEmpty(steps))
				{
					range.Steps = new NumericSetting();
					range.Steps.Representation = steps;
				}
				
				d_ranges.Add(range);
				double[] all = range.ToArray();
				
				if (d_numberOfSolutions == 0)
				{
					d_numberOfSolutions = (uint)all.Length;
				}
				else
				{
					d_numberOfSolutions *= (uint)all.Length;
				}
				
				return true;
			});
			
			object val = Storage.QueryValue("SELECT MAX(`index`) FROM `solution`");
			
			if (val != null)
			{
				d_currentId = (uint)((Int64)val + 1);
			}
			else
			{
				d_currentId = 0;
			}
		}
	}
}
