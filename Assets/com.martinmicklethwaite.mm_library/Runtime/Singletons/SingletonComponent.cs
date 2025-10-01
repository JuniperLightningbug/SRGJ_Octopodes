using UnityEngine;

namespace MM
{
	/**
	 *** ScriptableObject that behaves as a singleton at runtime with dependable initialisation behaviour and order
	 *
	 * See: 'SingletonHubBase'
	 */
	public abstract class SingletonComponent : ScriptableObject, IControlled
	{
		public enum ESingletonComponentLazyUpdateMode
		{
			None,
			OnPoll,
			OnPollOncePerFrame,
		}
		
#region SingletonHub accessor interface
		
		protected bool _bIsActive = false;
		public bool BIsActive => _bIsActive;
		public bool _bInitialised = false;

		public void Initialise( bool bForceReInitialise = false )
		{
			if( bForceReInitialise || !_bInitialised )
			{
				InitialiseInternal();
				_bIsActive = BStartsActive;
				_bInitialised = true;
			}
		}

#endregion

#region Virtual/Abstract
		protected virtual bool BStartsActive => true;
		public virtual ESingletonComponentLazyUpdateMode LazyUpdateMode => ESingletonComponentLazyUpdateMode.None;
		
		protected abstract void InitialiseInternal();

		public virtual void LazyUpdate()
		{
		}

#endregion

		// TODO: Enable, disable - hook up to stonhub to turn off listeners
		// TODO: Dispose stuff. StopListeners on stonhub and also remove as instanced ston from stonhub dict?

	}
}