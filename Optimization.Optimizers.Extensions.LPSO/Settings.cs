using System;

namespace Optimization.Optimizers.Extensions.LPSO
{
	public class Settings : Optimization.Settings
	{
		[Optimization.Attributes.Setting("guaranteed-convergence", 0, Description="Specify guaranteed convergence factor (enabled if factor > 0)")]
		public double GuaranteedConvergence;
		
		[Optimization.Attributes.Setting("has-initial-velocity", false, Description="Whether particles have an initial velocity")]
		public bool HasInitialVelocity;
	}
}

