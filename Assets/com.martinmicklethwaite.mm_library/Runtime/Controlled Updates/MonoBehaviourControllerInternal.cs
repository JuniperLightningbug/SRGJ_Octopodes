using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace MM
{
	/**
	 *** Bookkeeping for 'MonoBehaviourControllerBase'
	 *
	 * Keeping this separate means that we can create sub-controllers through composition rather than inheritance
	 */
	public class MonoBehaviourControllerInternal
	{
		public struct ControlledGroup<T> where T : IControlled
		{
			public Dictionary<Type, IndexedHashSet<T>> _ordered;
			public IndexedHashSet<T> _unordered;

			public bool BHasOrderedContent => _ordered != null && _ordered.Count > 0;
			public bool BHasUnorderedContent => _unordered != null && _unordered.Count > 0;

			public void Add( Type inType, T inControlled, bool bOrdered )
			{
				if( bOrdered )
				{
					AddOrdered( inType, inControlled );
				}
				else
				{
					AddUnordered( inControlled );
				}
			}

			public void Remove( Type inType, T inControlled, bool bOrdered )
			{
				if( bOrdered )
				{
					RemoveUnordered( inControlled );
				}
				else
				{
					RemoveOrdered( inType, inControlled );
				}
			}

			public void AddUnordered( T inControlled )
			{
				if( _unordered == null )
				{
					_unordered = new IndexedHashSet<T>();
				}

				_unordered.Add( inControlled );
			}

			public void AddOrdered( Type inType, T inControlled )
			{
				if( _ordered == null )
				{
					_ordered = new Dictionary<Type, IndexedHashSet<T>>();
				}

				if( !_ordered.TryGetValue( inType, out IndexedHashSet<T> outSet ) )
				{
					outSet = new IndexedHashSet<T>();
					_ordered.Add( inType, outSet );
				}

				outSet.Add( inControlled );
			}

			public void RemoveUnordered( T inControlled )
			{
				if( _unordered != null )
				{
					_unordered.Remove( inControlled );
				}
			}

			public void RemoveOrdered( Type inType, T inControlled )
			{
				if( _ordered != null && _ordered.TryGetValue( inType, out IndexedHashSet<T> outSet ) )
				{
					outSet.Remove( inControlled );
					if( outSet.Count == 0 )
					{
						_ordered.Remove( inType );
					}
				}
			}

			public void Clear()
			{
				_ordered.Clear();
				_unordered.Clear();
			}
		}
		
		private readonly GameObject _owner;
		private readonly Type[] _updateOrder;

		private readonly HashSet<Type> _updateTypeOrderHashSet = new HashSet<Type>();

		private ControlledGroup<IControlledUpdate> _updateGroup;
		private ControlledGroup<IControlledLateUpdate> _lateUpdateGroup;
		private ControlledGroup<IControlledFixedUpdate> _fixedUpdateGroup;
		private ControlledGroup<IControlledDebugUpdate> _debugUpdateGroup;

		public MonoBehaviourControllerInternal( GameObject owner, Type[] inUpdateOrder )
		{
			_owner = owner;
			_updateOrder = inUpdateOrder;

			Initialise();
		}

		public void Initialise()
		{
			for( int i = 0; i < _updateOrder.Length; ++i )
			{
				if( !_updateTypeOrderHashSet.Add( _updateOrder[i] ) )
				{
					UnityEngine.Debug.LogWarningFormat( "Duplicate type {0} in {1} ordered update list; skipping",
						_updateOrder[i].ToString(), DebugUtils.GetNameSafe( _owner ) );
				}
			}
		}

#region IControlled Injection

		public void Register( Type inType, IControlled inControlled )
		{
			bool bOrdered = _updateTypeOrderHashSet.Contains( inType );
			bool bAddedUnorderedControlledDebugCheck = false;

			if( inControlled is IControlledUpdate inControlledUpdate )
			{
				_updateGroup.Add( inType, inControlledUpdate, bOrdered );
				bAddedUnorderedControlledDebugCheck |= !bOrdered;
			}

			if( inControlled is IControlledLateUpdate inControlledLateUpdate )
			{
				_lateUpdateGroup.Add( inType, inControlledLateUpdate, bOrdered );
				bAddedUnorderedControlledDebugCheck |= !bOrdered;
			}

			if( inControlled is IControlledFixedUpdate inControlledFixedUpdate )
			{
				_fixedUpdateGroup.Add( inType, inControlledFixedUpdate, bOrdered );
				bAddedUnorderedControlledDebugCheck |= !bOrdered;
			}

#if DEBUG
			if( inControlled is IControlledDebugUpdate inControlledDebugUpdate )
			{
				_debugUpdateGroup.Add( inType, inControlledDebugUpdate, bOrdered );
				bAddedUnorderedControlledDebugCheck |= !bOrdered;
			}

			if( bAddedUnorderedControlledDebugCheck )
			{
				UnityEngine.Debug.LogWarningFormat(
					"{0}: Not assigned update order position in {1}. Updating in debug mode.",
					inType.Name, DebugUtils.GetNameSafe( _owner ) );
			}
#endif
		}

		public void Unregister( Type inType, IControlled inControlled )
		{
			bool bOrdered = _updateTypeOrderHashSet.Contains( inType );

			if( inControlled is IControlledUpdate inControlledUpdate )
			{
				_updateGroup.Remove( inType, inControlledUpdate, bOrdered );
			}

			if( inControlled is IControlledLateUpdate inControlledLateUpdate )
			{
				_lateUpdateGroup.Remove( inType, inControlledLateUpdate, bOrdered );
			}

			if( inControlled is IControlledFixedUpdate inControlledFixedUpdate )
			{
				_fixedUpdateGroup.Remove( inType, inControlledFixedUpdate, bOrdered );
			}

#if DEBUG
			if( inControlled is IControlledDebugUpdate inControlledDebugUpdate )
			{
				_debugUpdateGroup.Remove( inType, inControlledDebugUpdate, bOrdered );
			}
#endif
		}

#endregion

#region Debug

		[Conditional( "DEBUG" )]
		public void ControlledDebugUpdate( float deltaTime )
		{
			if( _debugUpdateGroup.BHasOrderedContent )
			{
				for( int i = 0; i < _updateOrder.Length; ++i )
				{
					if( _debugUpdateGroup._ordered.TryGetValue( _updateOrder[i], 
						out IndexedHashSet<IControlledDebugUpdate> controlledDebugUpdates ) )
					{
						foreach( IControlledDebugUpdate controlledDebugUpdate in controlledDebugUpdates )
						{
							controlledDebugUpdate.ControlledDebugUpdate( deltaTime );
						}
					}
				}
			}

			if( _debugUpdateGroup.BHasUnorderedContent )
			{
				foreach( IControlledDebugUpdate controlledDebugUpdate in _debugUpdateGroup._unordered )
				{
					controlledDebugUpdate.ControlledDebugUpdate( deltaTime );
				}
			}
		}

#endregion

#region Updates

		public void ControlledUpdate( float deltaTime )
		{
			if( _updateGroup.BHasOrderedContent )
			{
				for( int i = 0; i < _updateOrder.Length; ++i )
				{
					if( _updateGroup._ordered.TryGetValue( _updateOrder[i],
						out IndexedHashSet<IControlledUpdate> controlledUpdates ) )
					{
						foreach( IControlledUpdate controlledUpdate in controlledUpdates ) // TODO THIS ISN'T WORKING
						{
							controlledUpdate.ControlledUpdate( deltaTime );
						}
					}
				}
			}

			if( _updateGroup.BHasUnorderedContent )
			{
				foreach( IControlledUpdate controlledUpdate in _updateGroup._unordered )
				{
					controlledUpdate.ControlledUpdate( Time.deltaTime );
				}
			}
		}

		public void ControlledLateUpdate( float deltaTime )
		{
			if( _lateUpdateGroup.BHasOrderedContent )
			{
				for( int i = 0; i < _updateOrder.Length; ++i )
				{
					if( _lateUpdateGroup._ordered.TryGetValue( _updateOrder[i], 
						out IndexedHashSet<IControlledLateUpdate> controlledLateUpdates ) )
					{
						foreach( IControlledLateUpdate controlledLateUpdate in controlledLateUpdates )
						{
							controlledLateUpdate.ControlledLateUpdate( deltaTime );
						}
					}
				}
			}

			if( _lateUpdateGroup.BHasUnorderedContent )
			{
				foreach( IControlledLateUpdate controlledLateUpdate in _lateUpdateGroup._unordered )
				{
					controlledLateUpdate.ControlledLateUpdate( Time.deltaTime );
				}
			}
		}

		public void ControlledFixedUpdate( float fixedDeltaTime )
		{
			if( _fixedUpdateGroup.BHasOrderedContent )
			{
				for( int i = 0; i < _updateOrder.Length; ++i )
				{
					if( _fixedUpdateGroup._ordered.TryGetValue( _updateOrder[i],
						out IndexedHashSet<IControlledFixedUpdate> controlledFixedUpdates ) )
					{
						foreach( IControlledFixedUpdate controlledFixedUpdate in controlledFixedUpdates )
						{
							controlledFixedUpdate.ControlledFixedUpdate( fixedDeltaTime );
						}
					}
				}
			}

			if( _fixedUpdateGroup.BHasUnorderedContent )
			{
				foreach( IControlledFixedUpdate controlledFixedUpdate in _fixedUpdateGroup._unordered )
				{
					controlledFixedUpdate.ControlledFixedUpdate( Time.fixedDeltaTime );
				}
			}
		}

#endregion
	}
}