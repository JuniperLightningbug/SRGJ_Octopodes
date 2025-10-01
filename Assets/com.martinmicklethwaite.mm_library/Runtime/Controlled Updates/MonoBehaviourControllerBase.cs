using System;
using UnityEngine;

namespace MM
{
	/**
	 *** A central location to execute game loop update functions with hard-coded execution order
	 *
	 ** Usage:
	 * 
	 * This class is abstract in order to maintain project-independence.
	 * 
	 * Project setup:
	 * 1. Extend from this class ('MonoBehaviourControllerBase') to create a project-specific controller
	 * 2. Extend from the 'MonoBehaviourControlledBase' class to create a project-specific controlled MonoBehaviour
	 * (use the new 'MonoBehaviourControllerBase' subclass as 'T')
	 * 3. Override the 'OverrideUpdateOrder' property with project-specific 'MonoBehaviourControlledBase' subclasses
	 * (or other classes implementing 'IControlled' - they don't have to be MonoBehaviours!)
	 * 4. Add an instance of this 'SingletonHubBase' subclass to the scene
	 *
	 * Adding a new 'MonoBehaviourControlledBase':
	 * 1. Extend the new class from the project's 'MonoBehaviourControlledBase' subclass
	 * 2. Add the new type to the 'MonoBehaviourControllerBase' project subclass's 'OverrideUpdateOrder' array
	 * (3.) (Optionally) Implement any combination of 'IControlledUpdate', 'IControlledLateUpdate',
	 * 'IControlledFixedUpdate' and/or 'IControlledDebugUpdate'
	 * 
	 */
	[DefaultExecutionOrder(-1)]
	public abstract class MonoBehaviourControllerBase<TSelf> : StandaloneSingletonBase<TSelf> 
		where TSelf : MonoBehaviourControllerBase<TSelf>
	{
		protected abstract Type[] OverrideUpdateOrder { get; }
		protected MonoBehaviourControllerInternal _controller;
		
#region StandaloneSingletonBase

		protected override void Initialise()
		{
			_controller = new MonoBehaviourControllerInternal( gameObject, OverrideUpdateOrder );
		}

#endregion
		
#region IControlled Injection

		public void Register( Type inType, IControlled inControlled )
		{
			if( _controller != null )
			{
				_controller.Register( inType, inControlled );
			}
		}

		public void Unregister( Type inType, IControlled inControlled )
		{
			if( _controller != null )
			{
				_controller.Unregister( inType, inControlled );
			}
		}
		
#endregion

#region Updates

		private void Update()
		{
			if( _controller != null )
			{
				_controller.ControlledUpdate( Time.deltaTime );
#if DEBUG
				_controller.ControlledDebugUpdate( Time.deltaTime );
#endif
			}
		}

		private void LateUpdate()
		{
			if( _controller != null )
			{
				_controller.ControlledLateUpdate( Time.deltaTime );
			}
		}
		
		private void FixedUpdate()
		{
			if( _controller != null )
			{
				_controller.ControlledFixedUpdate( Time.fixedDeltaTime );
			}
		}
#endregion
	}
}