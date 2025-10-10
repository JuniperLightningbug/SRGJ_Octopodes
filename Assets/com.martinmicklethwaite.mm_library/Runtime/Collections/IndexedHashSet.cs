using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MM
{
	/**
	 *** Enumerable dictionary/list combination where:
	 *
	 * - Each item can only be represented once (enforced)
	 * - Add and Remove are both O(1)
	 * - Indexed iteration is fast (dense array)
	 * - Order is mutable and not guaranteed
	 * 
	 */
	public class IndexedHashSet<T> 
	{
		private readonly List<T> _list;
		private readonly Dictionary<T, int> _indices;

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
			
			_list[idx] = _list[_list.Count - 1];
			_indices[_list[_list.Count - 1]] = idx;
			
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

		public void RemoveAt( int index )
		{
			if( index >= 0 && index < _list.Count )
			{
				QuickRemoveInternal( index );
			}
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

		public bool Contains( T item )
		{
			return _indices.ContainsKey( item );
		}

#endregion
		
		public int Count => _list.Count;

		public T this[ int idx ] => (idx < _list.Count && idx >= 0) ? _list[idx] : default;
	}
}