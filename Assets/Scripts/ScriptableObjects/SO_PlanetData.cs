using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlanetData", menuName = "Scriptable Objects/SO_PlanetData")]
public class SO_PlanetData : ScriptableObject
{
	public enum ESensorType
	{
		INVALID = -1,
		
		Aurora,
		Magneto,
		MassSpec,
		Plasma,
		// Add others here
		
		COUNT
	}
	
	public string _key;
	public string _displayName;

	public struct PlanetLayerTuple
	{
		public ESensorType _sensorType;
		public SO_PlanetLayerMeshes.EHexgridMeshKey _mesh;
	}
	
	public List<PlanetLayerTuple> _planetLayers = new List<PlanetLayerTuple>();
	// Other info here! rotation speed, ....etc. TODO

	[Button]
	public void Validate()
	{
		Undo.RecordObject( this, "Validate" );
		List<int> duplicateEntryIdxs = new List<int>();
		for( int i = (int)ESensorType.INVALID; i <= (int)ESensorType.COUNT; ++i )
		{
			int foundLayerCount = 0;
			int maxLayerCount = (i == (int)ESensorType.INVALID || i == (int)ESensorType.COUNT) ? 0 : 1;
			for( int j = 0; j < _planetLayers.Count; ++j )
			{
				foundLayerCount++;
				if( foundLayerCount > maxLayerCount )
				{
					duplicateEntryIdxs.Add( j );
				}
			}
		}

		if( duplicateEntryIdxs.Count > 0 )
		{
			Debug.LogWarningFormat( "Found {0} duplicate/invalid entries. Removing.", duplicateEntryIdxs.Count );
			for( int i = duplicateEntryIdxs.Count - 1; i >= 0; --i )
			{
				Debug.LogWarningFormat( "Removing: [{0}:{1}] at {2}",
					_planetLayers[duplicateEntryIdxs[i]]._sensorType.ToString(),
					_planetLayers[duplicateEntryIdxs[i]]._mesh.ToString(),
					duplicateEntryIdxs[i] );
				_planetLayers.RemoveAt( duplicateEntryIdxs[i] );
			}
		}
		else
		{
			Debug.Log( "Validation complete. No issues found." );
		}
	}

}
