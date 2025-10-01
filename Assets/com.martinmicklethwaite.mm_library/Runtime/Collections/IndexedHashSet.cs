using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MM
{
	/**
	 *** Enumerable dictionary/list combination where:
	 *
	 ** [Pros:]
	 * - Each item can only be represented once (enforced)
	 * - Add and Remove are both O(1)
	 * (If we pretend that dictionaries have O(1) lookups)
	 * - Add() and Remove() during enumeration is safe (no items are skipped or parsed twice)
	 * - Indexed iteration is fast (dense array)
	 *
	 ** [Cons:]
	 * - Nested enumerations of the same IndexedHashSet object instance are not supported at all
	 * (This is the trade-off for the safe quick remove functionality: the enumeration is stored on the object itself)
	 * - Order is mutable and not guaranteed
	 *
	 ** Usage:
	 * - For any non-recursive operations that might remove items, use in-built enumeration (foreach)
	 * - For any operations that do not remove items, it's safe to access and increment indices directly (for)
	 */
	public class IndexedHashSet<T> : IReadOnlyList<T>
	{
		private readonly List<T> _list;
		private readonly Dictionary<T, int> _indices;

		/*
		 * Note: storing this here is how we can safely remove items during enumeration, but it also prohibits nested
		 * enumerations over the same IndexedHashSet<T> instance
		 */
		private int _enumeratorIdx = -1;
		public int _enumeratorCount = 0; // Tracked for debugging only - warn user about nested enumerations

		public string ContentString()
		{
			return string.Join( ", ", _list.ToArray() );
		}

		public IndexedHashSet()
		{
			_list = new List<T>();
			_indices = new Dictionary<T, int>();
		}

		public IndexedHashSet( int capacity )
		{
			_list = new List<T>( capacity );
			_indices = new Dictionary<T, int>( capacity );
		}

#region O(1) Operations

		public bool Add( T item )
		{
			if( _indices.TryAdd( item, _list.Count ) )
			{
				_list.Add( item );
				return true;
			}

			return false;
		}
		
		private T QuickRemoveInternal( int idx )
		{
			// Internal function - validation has passed already
			T removeItem = _list[idx];
			
			/*
			 * Guard for enumeration:
			 * Make sure the last item in the list is swapping to a position ahead of the current _enumeratorIdx
			 */
			if( idx <= _enumeratorIdx )
			{
				if( idx < _enumeratorIdx )
				{
					/*
					 * If the item to remove has already been passed:
					 * - Move the current item to the position of deleted item
					 * - Queue the current position for the quick-remove-swap instead of the one already enumerated
					 */
					_list[idx] = _list[_enumeratorIdx];
					_indices[_list[_enumeratorIdx]] = idx;
					idx = _enumeratorIdx;
				}

				/*
				 * The current _enumeratorIdx item will swap with the last list item.
				 * Backtrack by 1 so we don't skip it.
				 */
				--_enumeratorIdx;
			}

			// Now do the swap with the clamped removeIdx
			_list[idx] = _list[_list.Count - 1];
			_indices[_list[_enumeratorIdx]] = idx;

			// Now, remove the last list item
			_list.RemoveAt( _list.Count - 1 );
			_indices.Remove( removeItem );

			return removeItem;
		}

		public bool Remove( T item )
		{
			int removeIdx;
			if( !_indices.TryGetValue( item, out removeIdx ) )
			{
				return false;
			}

			QuickRemoveInternal( removeIdx );
			return true;
		}


		public bool RemoveAt( int index, ref T outRemovedItem )
		{
			if( index < 0 || index >= _list.Count )
			{
				return false;
			}
			
			outRemovedItem = QuickRemoveInternal( index );
			return true;
		}

		public void Clear()
		{
			_list.Clear();
			_indices.Clear();
		}

#endregion

#region IReadOnlyList

		public int Count => _list.Count;

		public T this[ int idx ] => (idx < _list.Count && idx >= 0) ? _list[idx] : default;

		public IEnumerator<T> GetEnumerator()
		{
#if DEBUG
			if( _enumeratorCount > 0 )
			{
				Debug.LogWarningFormat( "Nested enumerations for {0} are not supported", GetType() );
			}
#endif
			return new IndexedHashSetEnumerator( this );
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private struct IndexedHashSetEnumerator : IEnumerator<T>
		{
			private IndexedHashSet<T> _target;

			public IndexedHashSetEnumerator( IndexedHashSet<T> target )
			{
				_target = target;
				++_target._enumeratorCount;
				Reset();
			}

			public bool MoveNext()
			{
				_target._enumeratorIdx++;
				return _target._enumeratorIdx < _target.Count;
			}

			public void Reset()
			{
				_target._enumeratorIdx = -1;
			}

			object IEnumerator.Current => Current;
			public T Current => _target[_target._enumeratorIdx];

			public void Dispose()
			{
				if( _target != null )
				{
					--_target._enumeratorCount;
				}
			}
		}

#endregion
	}
}