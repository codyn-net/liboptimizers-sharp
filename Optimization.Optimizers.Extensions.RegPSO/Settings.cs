using System;

namespace Optimization.Optimizers.Extensions.RegPSO
{
	public class Settings : Optimization.Settings
	{
		[Attributes.Setting("stagnation-threshold", "0.00011", Description="The stagnation threshold")]
		public string StagnationThreshold;
		
		[Attributes.Setting("regrouping-factor", "1.2 / stagnation_threshold", Description="The regrouping factor")]
		public string RegroupingFactor;
	}
}

