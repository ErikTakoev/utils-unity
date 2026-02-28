using System;
using UnityEngine;

namespace Expecto.Utils
{
	[Serializable]
	public class InterfaceRef<T> where T : class
	{
		[SerializeField]
		protected MonoBehaviour _target;

		private T _value;

		public T Value
		{
			get
			{
				if (_target == null) return null;
				_value ??= _target as T;
				return _value;
			}
		}

		public MonoBehaviour Target => _target;
	}
}
