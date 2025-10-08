using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetConfig", menuName = "Scriptable Objects/Planet Config")]
public class SO_PlanetConfig : ScriptableObject
{
	public enum ESensorType
	{
		INVALID = -1,
		
		Aurora,
		Radar,
		Magneto,
		Plasma,
		MassSpec,
		// Add others here
		
		COUNT
	}
	
	[System.Serializable]
	public struct PlanetLayerTuple
	{
		public ESensorType _sensorType;
		public Mesh _mesh;
		public Material _material;
		public bool _bActive;
		public bool _bHasTexture;
	}
	
	public string _name;
	public List<PlanetLayerTuple> _planetLayers = new List<PlanetLayerTuple>();
	public Mesh _planetMesh;
	public Material _planetMaterial;

	[Tooltip( "RotationTime" )]
	public float _rotationTime = 0.0f;

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
				if( _planetLayers[j]._sensorType == (ESensorType)i )
				{
					foundLayerCount++;
					if( foundLayerCount > maxLayerCount || _planetLayers[j]._mesh == null )
					{
						duplicateEntryIdxs.Add( j );
					}
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
