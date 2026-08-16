using UnityEngine;

namespace Expecto.Utils
{
	public class AnimatorHelper : MonoBehaviour
	{
		[SerializeField] private Animator[] animators;

		public void SetTriggerFormatted(string formattedValue)
		{
			if (string.IsNullOrEmpty(formattedValue))
			{
				return;
			}

			string[] parts = formattedValue.Split(':');
			if (parts.Length == 2 && int.TryParse(parts[0], out int index))
			{
				if (!ValidateIndex(index))
				{
					return;
				}

				animators[index].SetTrigger(parts[1]);
			}
			else
			{
				Debug.LogError($"[{nameof(AnimatorHelper)}] Invalid string format '{formattedValue}' on '{gameObject.name}'. Expected format: 'animatorIndex:triggerName' (e.g., '0:TriggerName').", this);
			}
		}

		public void ActivateTrigger(int animatorIndex)
		{
			if (!ValidateIndex(animatorIndex))
			{
				return;
			}

			animators[animatorIndex].SetTrigger("Activate");
		}

		public void DeactivateTrigger(int animatorIndex)
		{
			if (!ValidateIndex(animatorIndex))
			{
				return;
			}

			animators[animatorIndex].SetTrigger("Deactivate");
		}

		private bool ValidateIndex(int animatorIndex)
		{
			if (animators == null)
			{
				Debug.LogError($"[{nameof(AnimatorHelper)}] Animators array on '{gameObject.name}' is null.", this);
				return false;
			}

			if (animatorIndex < 0 || animatorIndex >= animators.Length)
			{
				Debug.LogError($"[{nameof(AnimatorHelper)}] Index {animatorIndex} is out of bounds for animators array (length: {animators.Length}) on '{gameObject.name}'.", this);
				return false;
			}

			if (animators[animatorIndex] == null)
			{
				Debug.LogWarning($"[{nameof(AnimatorHelper)}] Animator at index {animatorIndex} on '{gameObject.name}' is null.", this);
				return false;
			}

			return true;
		}
	}
}
