using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MM
{
	public enum ESingletonComponentInitialisationMode
	{
		OnAwake,
		OnPoll,
			
		// e.g.: Testing scene - we might want to avoid clutter, but also to be notified of
		// missed dependencies without breaking
		OnPollWithWarning,
	}
	
	/**
	 *** An instantiable, central manager for presently active singleton components.
	 *
	 ** Function:
	 * 
	 * - Single source of truth for singleton initialisation and update order
	 * - Handles singleton updates efficiently from a central location (each opt-in by the SingletonComponent)
	 * - Provides singleton component options to the inspector to be easily toggled on/off per-scene
	 * - Provides a single access point for active singletons with safe, O(1) lookups
	 * - Optionally handles singleton lazy updates on accessors (opt-in by the SingletonComponent)
	 *
	 * 
	 ** Usage:
	 *
	 * This class is abstract in order to maintain project-independence.
	 * 
	 * Project setup:
	 * 1. Extend from this class for a new project
	 * 2. Assign the project-specific 'MonoBehaviourControllerBase' implementation as 'TController'
	 * 3. Override the 'OverrideInitialisationOrder' property with the order of 'SingletonComponent' types to include
	 * 4. Add an instance of this 'SingletonHubBase' subclass to the scene
	 * 5. In the 'SingletonHubBase' inspector: select relevant 'SingletonComponents' and assign data (see below)
	 *
	 * Adding a new 'SingletonComponent':
	 * 1. Add the new type to the 'OverrideInitialisationOrder' array (decides the order of init & update)
	 * (2.) (Optionally) Implement 'IControlledUpdate', 'IControlledFixedUpdate', 'IControlledLateUpdate' and/or
	 * 'IControlledDebugUpdate' for required update hooks
	 * (3.) (Optionally) Override 'LazyUpdateMode' to set up lazy update hooks
	 * (4.) (Optionally) Override 'BStartsActive' to defer active state at runtime
	 * 5. In the 'SingletonHubBase' inspector: Click the auto-populate button to add the new entry
	 * 6. In the 'SingletonHubBase' inspector: Choose initialisation protocol and assign SO data if relevant
	 *
	 * 
	 ** Future scope:
	 *
	 * It would be useful to combine separate instances of 'SingletonHubBase' at runtime.
	 * e.g.:
	 * Scene A has a 'SingletonHubBase' instance SH(A) that initialises a subset of 'SingletonComponents' SC(A)
	 * Scene B has a 'SingletonHubBase' instance SH(B) that initialises a subset of 'SingletonComponents' SC(B)
	 * At runtime, Scene A is loaded first: SH(A) is initialised with SC(A).
	 * Scene B is loaded second: SH(B) is discarded.
	 *
	 * Proposal: When Scene B is loaded, SH(B) would pass data to SH(A) before being discarded, and SH(A) would handle
	 * new initialisations for a new SC(A) = SC(A) U SC(B)
	 * Pro: Each scene can keep track of its own 'SingletonComponent' dependencies independently
	 * Con: Obfuscates the initialisation order, because the runtime scene load order would take precedent
	 *
	 */
	public abstract class SingletonHubBase<TSelf, TController> : StandaloneSingletonBase<TSelf>,
		IControlledUpdate, IControlledDebugUpdate, IControlledLateUpdate, IControlledFixedUpdate,
		ISerializationCallbackReceiver,
		ISingletonHubBaseEditorUtility
		where TSelf : SingletonHubBase<TSelf, TController>
		where TController : MonoBehaviourControllerBase<TController>
	{
		// This is the source of truth for SingletonComponent types allowed and their init/update order
		protected abstract Type[] OverrideInitialisationOrder { get; }

		/*
		 * This class is a sub-controller, i.e. it implements IControlled and relays the received update calls
		 * from the project's MonoBehaviourControlledUpdateBase's implementation.
		 */
		private readonly Dictionary<Type, IControlledUpdate> _controlledUpdates = new Dictionary<Type, IControlledUpdate>();
		private readonly Dictionary<Type, IControlledLateUpdate> _controlledLateUpdates = new Dictionary<Type, IControlledLateUpdate>();
		private readonly Dictionary<Type, IControlledFixedUpdate> _controlledFixedUpdates = new Dictionary<Type, IControlledFixedUpdate>();
		private readonly Dictionary<Type, IControlledDebugUpdate> _controlledDebugUpdates = new Dictionary<Type, IControlledDebugUpdate>();
		private Dictionary<Type, float> _lastLazyUpdateTimes;
		
#region StandaloneSingletonBase

		protected override bool BPersistent => true;

		protected override void Initialise()
		{
			CacheInitialisationInfos();
			CreateSingletonComponentsOnAwake();
		}

#endregion

#region SingletonComponent Inspector Data

		[SerializeField]
		public SingletonComponentsInitInfo _inspectorComponentsInitInfo;

		public void TryRefreshInspectorData()
		{
#if UNITY_EDITOR
			if( !InspectorDataNeedsRefresh() )
			{
				return;
			}

			Debug.Log( "Updating SingletonComponent Info list..." );
			Debug.Log( _inspectorComponentsInitInfo.DebugString() );

			Type[] singletonComponentUpdateOrder = OverrideInitialisationOrder;
			List<SingletonComponentInitInfo> previousList = _inspectorComponentsInitInfo._configs.ToList();
			int previousCount = previousList.Count;

			// Make a new list - this way, we can easily ensure that the update order is visible in the inspector
			List<SingletonComponentInitInfo> newSingletonComponentList =
				new List<SingletonComponentInitInfo>();

			List<string> addedList = new List<string>();

			for( int updateOrderIdx = 0; updateOrderIdx < singletonComponentUpdateOrder.Length; ++updateOrderIdx )
			{
				bool bExists = false;

				for( int inspectorListIdx = previousList.Count - 1; inspectorListIdx >= 0; --inspectorListIdx )
				{
					if( previousList[inspectorListIdx]._typeString ==
					    singletonComponentUpdateOrder[updateOrderIdx].AssemblyQualifiedName )
					{
						// Copy an existing item into the new list
						bExists = true;
						
						// In case type names were changed - don't skip this update
						previousList[inspectorListIdx].InitialiseReadableTypeFields();
						
						newSingletonComponentList.Add( previousList[inspectorListIdx] );
						previousList.RemoveAt( inspectorListIdx );
						break;
					}
				}

				if( !bExists )
				{
					// Add a missing entry to the new list
					SingletonComponentInitInfo newInitInfo = new SingletonComponentInitInfo()
					{
						_typeString = singletonComponentUpdateOrder[updateOrderIdx].AssemblyQualifiedName,
						_bActive = false,
						_initialisationMode = ESingletonComponentInitialisationMode.OnPollWithWarning,
					};
					newInitInfo.InitialiseReadableTypeFields();
					newSingletonComponentList.Add( newInitInfo );
					addedList.Add( newInitInfo._typeString );
				}
			}

			Debug.LogFormat( "\tSingletonComponent Info list updated: " +
			                 "\n\tPreviously: {0} entries. Now: {1} entries." +
			                 " {2} Added: [{3}];" +
			                 " {4} Removed: [{5}]",
				previousCount, newSingletonComponentList.Count,
				addedList.Count, string.Join( ", ", addedList.ToArray() ),
				previousList.Count, string.Join( ", ", previousList.Select( x => x._typeString ).ToArray() ) );
			Debug.Log( _inspectorComponentsInitInfo.DebugString() );

			_inspectorComponentsInitInfo._configs = newSingletonComponentList.ToArray();
#endif
		}

		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR

#endif
		}

		public void OnAfterDeserialize()
		{
#if UNITY_EDITOR
			TryRefreshInspectorData();
#endif
		}

		/**
		 * Returns true if more singleton types have been added in code since the config data
		 * was assigned in the inspector
		 */
		public bool InspectorDataNeedsRefresh()
		{
			if( OverrideInitialisationOrder.Length != _inspectorComponentsInitInfo._configs.Length )
			{
				return true;
			}

			for( int i = 0; i < _inspectorComponentsInitInfo._configs.Length; ++i )
			{
				if( _inspectorComponentsInitInfo._configs[i]._type != OverrideInitialisationOrder[i] )
				{
					return true;
				}
			}

			return false;
		}

#endregion

#region SingletonComponent Runtime Data

		// Lookup table for runtime initialisations (created and validated on init)
		private Dictionary<Type, SingletonComponentInitInfo> _initialisationInfoDictionary;

		// Lookup table for all currently active and initialised singleton components
		private Dictionary<Type, SingletonComponent> _activeSingletonComponents;
		
		private void CacheInitialisationInfos()
		{
			_initialisationInfoDictionary = new Dictionary<Type, SingletonComponentInitInfo>();
			for( int i = 0; i < _inspectorComponentsInitInfo._configs.Length; ++i )
			{
				if( _inspectorComponentsInitInfo._configs[i]._bActive )
				{
					// Unity serialisation doesn't work for Type
					_inspectorComponentsInitInfo._configs[i].InitialiseTypeField();
					
					_initialisationInfoDictionary.Add(
						_inspectorComponentsInitInfo._configs[i]._type,
						_inspectorComponentsInitInfo._configs[i] );
				}
			}
		}

		private void CreateSingletonComponentsOnAwake()
		{
			if( _activeSingletonComponents == null )
			{
				_activeSingletonComponents = new Dictionary<Type, SingletonComponent>();
			}

			Debug.Log( "Creating SingletonComponent instances..." );
			Type[] cachedInitOrder = OverrideInitialisationOrder;
			for( int i = 0; i < cachedInitOrder.Length; ++i )
			{
				if( _initialisationInfoDictionary.TryGetValue( cachedInitOrder[i],
					   out SingletonComponentInitInfo initInfo ) )
				{
					if( initInfo._bActive && initInfo._initialisationMode ==
					   ESingletonComponentInitialisationMode.OnAwake )
					{
						SingletonComponent newComponent = InitialiseSingletonComponent( initInfo );
						_activeSingletonComponents.Add( initInfo._type, newComponent );
					}
				}
			}

			Debug.LogFormat(
				"Finished creating SingletonComponent instances. SingletonComponents active: {0}",
				_activeSingletonComponents.Count );
		}

		private SingletonComponent InitialiseSingletonComponent( Type singletonComponentType )
		{
			if( _initialisationInfoDictionary.TryGetValue( singletonComponentType,
				   out SingletonComponentInitInfo initInfo ) )
			{
				return InitialiseSingletonComponent( initInfo );
			}

			return null;
		}

		private SingletonComponent InitialiseSingletonComponent( in SingletonComponentInitInfo initInfo )
		{
			if( _activeSingletonComponents.ContainsKey( initInfo._type ) )
			{
				Debug.LogWarningFormat( "\tUnable to create SingletonComponent instance: [{0}] - type already exists.",
					initInfo._typeString );
				return null;
			}

			Debug.LogFormat( "\tCreating SingletonComponent instance: [{0}]",
				initInfo._typeString );

			SingletonComponent newSingletonComponent;
			SingletonComponent preset = initInfo._presetData;
			if( preset )
			{
				newSingletonComponent = ScriptableObject.Instantiate( preset );
			}
			else
			{
				newSingletonComponent =
					ScriptableObject.CreateInstance( initInfo._type ) as
						SingletonComponent;
			}

			if( newSingletonComponent )
			{
				newSingletonComponent.Initialise();

				if( newSingletonComponent is IControlled newSingletonControl )
				{
					StartSingletonComponentListeners( initInfo._type, newSingletonControl );
				}

				if( newSingletonComponent.LazyUpdateMode ==
				    SingletonComponent.ESingletonComponentLazyUpdateMode.OnPollOncePerFrame )
				{
					_lastLazyUpdateTimes.Add( initInfo._type, Time.time );
				}
			}

			return newSingletonComponent;
		}

		private void StartSingletonComponentListeners( SingletonComponent singletonComponent )
		{
			if( singletonComponent )
			{
				StartSingletonComponentListeners( singletonComponent.GetType(), singletonComponent );
			}
		}
		
		private void StopSingletonComponentListeners( SingletonComponent singletonComponent )
		{
			if( singletonComponent )
			{
				StopSingletonComponentListeners( singletonComponent.GetType(), singletonComponent );
			}
		}
		
		private void StartSingletonComponentListeners( Type type, in IControlled singletonControlled )
		{
			if( singletonControlled is IControlledUpdate controlledUpdate )
			{
				_controlledUpdates.Add( type, controlledUpdate );
			}

			if( singletonControlled is IControlledLateUpdate controlledLateUpdate )
			{
				_controlledLateUpdates.Add( type, controlledLateUpdate );
			}

			if( singletonControlled is IControlledFixedUpdate controlledFixedUpdate )
			{
				_controlledFixedUpdates.Add( type, controlledFixedUpdate );
			}

			if( singletonControlled is IControlledDebugUpdate controlledDebugUpdate )
			{
				_controlledDebugUpdates.Add( type, controlledDebugUpdate );
			}
		}

		private void StopSingletonComponentListeners( Type type, in IControlled singletonControlled )
		{
			if( singletonControlled is IControlledUpdate )
			{
				_controlledUpdates.Remove( type );
			}

			if( singletonControlled is IControlledLateUpdate )
			{
				_controlledLateUpdates.Remove( type );
			}

			if( singletonControlled is IControlledFixedUpdate )
			{
				_controlledFixedUpdates.Remove( type );
			}

			if( singletonControlled is IControlledDebugUpdate )
			{
				_controlledDebugUpdates.Remove( type );
			}
		}
		
#endregion

#region Interface

		/**
		 * Use this to avoid all initialisations on poll; output might be null
		 */
		public T TryGet<T>() where T : SingletonComponent
		{
			Type tType = typeof(T);
			if( _activeSingletonComponents.TryGetValue( tType, out SingletonComponent outComponent ) )
			{
				if( outComponent._bInitialised )
				{
					TryLazyUpdate( tType, outComponent );
					return (T)outComponent;
				}
			}

			return null;
		}

		/**
		 * Use this to try to initialise an inactive singleton component if it's inactive
		 */
		public T Get<T>() where T : SingletonComponent
		{
			Type tType = typeof(T);
			SingletonComponent outComponent;
			if( _activeSingletonComponents.TryGetValue( typeof( T ), out outComponent ) )
			{
				// Singleton component exists
				if( !outComponent._bInitialised )
				{
					outComponent.Initialise();
				}
			}
			else if( _initialisationInfoDictionary.TryGetValue( typeof( T ), out SingletonComponentInitInfo initInfo ) &&
			         initInfo._bActive)
			{
				// Singleton component doesn't exist - create one
				if( initInfo._initialisationMode == ESingletonComponentInitialisationMode.OnPoll ||
				    initInfo._initialisationMode == ESingletonComponentInitialisationMode.OnPollWithWarning )
				{
					if( initInfo._initialisationMode == ESingletonComponentInitialisationMode.OnPollWithWarning )
					{
						Debug.LogWarningFormat( "Initialising SingletonComponent of type: [{0}]", initInfo._typeString );
					}
					outComponent = InitialiseSingletonComponent( initInfo );
				}
			}

			if( outComponent )
			{
				TryLazyUpdate( tType, outComponent );
				return (T)outComponent;
			}
			return null;
		}

		private void TryLazyUpdate( Type type, SingletonComponent singletonComponent )
		{
			switch( singletonComponent.LazyUpdateMode )
			{
				case SingletonComponent.ESingletonComponentLazyUpdateMode.OnPoll:
				{
					singletonComponent.LazyUpdate();
					break;
				}
				case SingletonComponent.ESingletonComponentLazyUpdateMode.OnPollOncePerFrame:
				{
					if( _lastLazyUpdateTimes.TryGetValue( type, out float lastUpdateTime ) )
					{
						float time = Time.time;
						if( !MathsUtils.Approximately( lastUpdateTime, time ) )
						{
							_lastLazyUpdateTimes[type] = time;
							singletonComponent.LazyUpdate();
						}
					}
					break;
				}
			}
		}

#endregion

#region IControlled

		private void OnEnable()
		{
			MonoBehaviourControllerBase<TController> controller = MonoBehaviourControllerBase<TController>.Instance;
			if( controller )
			{
				controller.Register( GetType(), this );
			}
		}

		private void OnDisable()
		{
			MonoBehaviourControllerBase<TController> controller = MonoBehaviourControllerBase<TController>.Instance;
			if( controller )
			{
				controller.Unregister( GetType(), this );
			}
		}

		public void ControlledUpdate( float deltaTime )
		{
			if( _controlledUpdates == null ||
			    OverrideInitialisationOrder == null ||
			    _controlledUpdates.Count == 0 )
			{
				return;
			}
			
			for( int i = 0; i < OverrideInitialisationOrder.Length; ++i )
			{
				if( _controlledUpdates.TryGetValue( OverrideInitialisationOrder[i],
					   out IControlledUpdate controlledUpdate ) )
				{
					controlledUpdate.ControlledUpdate( deltaTime );
				}
			}
		}

		public void ControlledLateUpdate( float deltaTime )
		{
			if( _controlledLateUpdates == null ||
			    OverrideInitialisationOrder == null ||
			    _controlledLateUpdates.Count == 0 )
			{
				return;
			}
			
			for( int i = 0; i < OverrideInitialisationOrder.Length; ++i )
			{
				if( _controlledLateUpdates.TryGetValue( OverrideInitialisationOrder[i],
					   out IControlledLateUpdate controlledLateUpdate ) )
				{
					controlledLateUpdate.ControlledLateUpdate( deltaTime );
				}
			}
		}

		public void ControlledFixedUpdate( float fixedDeltaTime )
		{
			if( _controlledFixedUpdates == null ||
			    OverrideInitialisationOrder == null ||
			    _controlledFixedUpdates.Count == 0 )
			{
				return;
			}
			
			for( int i = 0; i < OverrideInitialisationOrder.Length; ++i )
			{
				if( _controlledFixedUpdates.TryGetValue( OverrideInitialisationOrder[i],
					   out IControlledFixedUpdate controlledFixedUpdate ) )
				{
					controlledFixedUpdate.ControlledFixedUpdate( fixedDeltaTime );
				}
			}
		}

#endregion
		
#region Debug

		public void ControlledDebugUpdate( float deltaTime )
		{
			if( _controlledDebugUpdates == null ||
			    OverrideInitialisationOrder == null ||
			    _controlledDebugUpdates.Count == 0 )
			{
				return;
			}
			
			for( int i = 0; i < OverrideInitialisationOrder.Length; ++i )
			{
				if( _controlledDebugUpdates.TryGetValue( OverrideInitialisationOrder[i],
					   out IControlledDebugUpdate controlledDebugUpdate ) )
				{
					controlledDebugUpdate.ControlledDebugUpdate( deltaTime );
				}
			}
		}

#endregion
	}
}