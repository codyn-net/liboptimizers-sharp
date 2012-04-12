using System;

namespace Optimization.Optimizers.PSO
{
	public interface IPSOExtension
	{
		double CalculateVelocityUpdate(Particle particle, Particle best, int i);
		State.VelocityUpdateType VelocityUpdateComponents(Particle particle);
		void ValidateVelocityUpdate(Particle particle, double[] velocityUpdate);
		
		Particle GetUpdateBest(Particle particle);
		bool UpdateParticleBest(Particle particle);
	}
}
