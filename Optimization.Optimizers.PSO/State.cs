using System;

namespace Optimization.Optimizers.PSO
{
	public class State : Optimization.State
	{
		[Flags]
		public enum VelocityUpdateType
		{
			Default = 1 << 0,
			DisableMomentum = 1 << 1,
			DisableLocal = 1 << 2,
			DisableGlobal = 1 << 3
		}
		
		private VelocityUpdateType d_velocityUpdateComponents;

		public State(Optimization.Settings settings) : base(settings)
		{
			d_velocityUpdateComponents = State.VelocityUpdateType.Default;
		}
		
		public VelocityUpdateType VelocityUpdateComponents
		{
			get
			{
				return d_velocityUpdateComponents;
			}
			set
			{
				d_velocityUpdateComponents = value;
			}
		}
	}
}

