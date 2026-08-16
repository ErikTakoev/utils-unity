using UnityEngine;

namespace Expecto.Utils
{
	public class ParticleHelper : MonoBehaviour
	{
		[SerializeField] private ParticleSystem[] particles;

		public void Play(int particleIndex)
		{
			if (!ValidateIndex(particleIndex))
			{
				return;
			}

			particles[particleIndex].Play(withChildren: true);
		}

		public void StopEmission(int particleIndex)
		{
			if (!ValidateIndex(particleIndex))
			{
				return;
			}

			particles[particleIndex].Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}

		private bool ValidateIndex(int particleIndex)
		{
			if (particles == null)
			{
				Debug.LogError($"[{nameof(ParticleHelper)}] Particles array on '{gameObject.name}' is null.", this);
				return false;
			}

			if (particleIndex < 0 || particleIndex >= particles.Length)
			{
				Debug.LogError($"[{nameof(ParticleHelper)}] Index {particleIndex} is out of bounds for particles array (length: {particles.Length}) on '{gameObject.name}'.", this);
				return false;
			}

			if (particles[particleIndex] == null)
			{
				Debug.LogWarning($"[{nameof(ParticleHelper)}] ParticleSystem at index {particleIndex} on '{gameObject.name}' is null.", this);
				return false;
			}

			return true;
		}
	}
}
