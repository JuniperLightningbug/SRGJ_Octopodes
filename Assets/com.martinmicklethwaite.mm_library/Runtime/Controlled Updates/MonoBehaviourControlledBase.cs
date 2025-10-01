using System;
using UnityEngine;

namespace MM
{
	/**
	 *** Variant of MonoBehaviour with a configurable and dependable update order.
	 * See: 'MonoBehaviourControllerBase'
	 */
	public abstract class MonoBehaviourControlledBase<T> : MonoBehaviour, IControlled where T : MonoBehaviourControllerBase<T>
	{
		protected virtual void OnEnableInternal()
		{

		}

		protected virtual void OnDisableInternal()
		{

		}

		private void OnEnable()
		{
			MonoBehaviourControllerBase<T> controller = MonoBehaviourControllerBase<T>.Instance;
			if( controller )
			{
				controller.Register( GetType(), this );
			}

			OnEnableInternal();
		}

		private void OnDisable()
		{
			MonoBehaviourControllerBase<T> controller = MonoBehaviourControllerBase<T>.Instance;
			if( controller )
			{
				controller.Unregister( GetType(), this );
			}

			OnDisableInternal();
		}
	}
}