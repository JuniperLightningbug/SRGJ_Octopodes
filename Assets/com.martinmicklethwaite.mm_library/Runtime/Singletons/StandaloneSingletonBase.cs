using UnityEngine;

namespace MM
{
	/**
	 * 
	 *** A simple singleton base class, with optional override for scene persistence.
	 * 
	 * This is an independent singleton instance - no external influence over initialisation, update order, mode, etc.
	 * (although it's possible to add IControlled functionality manually in child monobehaviours).
	 *
	 ** Usage:
	 * 
	 * The intended usage is to set up the two fundamental singletons in a project:
	 * - The singleton manager ('SingletonHubBase')
	 * - The MonoBehaviour controller ('MonoBehaviourControllerBase')
	 * Other singletons ideally derive from 'SingletonComponent' to benefit from the structure of 'SingletonHubBase'.
	 *
	 * That said, it's also useful in the general case for rapid testing and prototyping before race conditions
	 * become problematic.
	 */
	public abstract class StandaloneSingletonBase<T> : MonoBehaviour where T : StandaloneSingletonBase<T>
	{
		protected virtual bool BPersistent => false;
		protected bool _bInitialised = false;

		private static T _instance;

		public static T Instance
		{
			get
			{
				if( !_instance && ApplicationUtils.BIsPlaying )
				{
					GameObject obj = new GameObject( $"{nameof( T )} [runtime-generated]", typeof( T ) );
				}

				return _instance;
			}
		}

		private void Awake()
		{
			InitialiseSingleton();
		}
		
		private void InitialiseSingleton()
		{
			if( _bInitialised ||
			    !ApplicationUtils.BIsPlaying)
			{
				return;
			}

			if( _instance != null && _instance != this )
			{
				OnBeforeDestroyForExistingInstance( _instance );
				MM.ComponentUtils.DestroyPlaymodeSafe( gameObject );
			}
			else
			{
				_instance = this as T;
				if( BPersistent )
				{
					DontDestroyOnLoad( this );
				}
				
				Initialise();
				_bInitialised = true;
			}
		}

		protected virtual void Initialise()
		{
			
		}

		/**
		 * Use this if we need to pass information to an existing singleton before destroying self
		 */
		protected virtual void OnBeforeDestroyForExistingInstance( T existingInstance )
		{

		}
	}
}