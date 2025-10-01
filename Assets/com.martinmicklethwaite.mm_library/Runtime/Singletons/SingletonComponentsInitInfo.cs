using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MM
{
	/**
	 *** Wrapper for 'SingletonComponent' initialisation data to attach a 'PropertyDrawer'
	 * 
	 * Unity doesn't support custom inspectors for generic typed classes - even for fixed type subclasses.
	 * These wrappers & 'PropertyDrawer's allows us to avoid a generic type custom inspector altogether.
	 */
	[System.Serializable]
	public class SingletonComponentsInitInfo
	{
		// Exposed to inspector (in hard-coded order)
		[SerializeField]
		public SingletonComponentInitInfo[] _configs = Array.Empty<SingletonComponentInitInfo>();

		public string DebugString()
		{
			List<string> debugStrings = new List<string>();
			for( int i = 0; i < _configs.Length; ++i )
			{
				debugStrings.Add( string.Format(
					"{0}: [{1}{2}]",
					_configs[i]._typeString,
					(_configs[i]._bActive ? "ACTIVE: " : "inactive"),
					(_configs[i]._bActive ? ": " + _configs[i]._initialisationMode.ToString() : "") ) );
			}

			return string.Join( "; ", debugStrings.ToArray() );
		}
	}
	
	[System.Serializable]
	public class SingletonComponentInitInfo
	{
		[SerializeField]
		public string _typeString;
		
		[SerializeField]
		public bool _bActive;

		[SerializeField]
		public ESingletonComponentInitialisationMode _initialisationMode;

		[SerializeField]
		[Tooltip("Optionally assign default values from a ScriptableObject")]
		public SingletonComponent _presetData;
		
		// Derived fields
		public Type _type;
		[SerializeField]
		public string _typeDisplayString;
		
		public void InitialiseReadableTypeFields()
		{
			InitialiseTypeField();
			
			const string kSingletonComponentTypePrefix = nameof( SingletonComponent );
			bool bFormatted = false;
			string readableString = _typeString;
			
			// Try to remove assembly qualifiers (after the ',')
			string[] typeNameSplitQualifiers = _typeString.Split( "," );
			if( typeNameSplitQualifiers.Length > 0 )
			{
				readableString = typeNameSplitQualifiers[0];
				bFormatted = true;
			}
			
			// Try to remove namespace (before the final '.')
			string[] typeNameSplitNamespace = typeNameSplitQualifiers[0].Split( "." );
			if( typeNameSplitNamespace.Length > 0 )
			{
				readableString = typeNameSplitNamespace[typeNameSplitNamespace.Length - 1];
				bFormatted = true;
			}
			
			// Try to remove prefix (assuming MM style guide: trim "{base_class_name}_")
			string[] typeNameSplitPrefix = readableString.Split( '_' );
			if( readableString.Length > kSingletonComponentTypePrefix.Length &&
			    typeNameSplitPrefix.Length > 1 &&
			    typeNameSplitPrefix[0].Equals( kSingletonComponentTypePrefix ) )
			{
				readableString = readableString.Substring( kSingletonComponentTypePrefix.Length + 1 );
				bFormatted = true;
			}

			if( !bFormatted )
			{
				// Nothing to remove - leave as literal
				_typeDisplayString = string.Format( "\"{0}\"", _typeString );
			}
			else
			{
				_typeDisplayString = readableString;
			}
		}

		public void InitialiseTypeField()
		{
			// Can't serialise the type directly - use the string as the persistent data instead
			_type = Type.GetType( _typeString );
		}
	}
}