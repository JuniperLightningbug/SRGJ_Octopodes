using System;
using System.Collections.Generic;
using MM;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu( fileName = "HexgridMeshData", menuName = "Scriptable Objects/HexgridMeshData" )]
public class SO_HexgridMeshData : ScriptableObject
{
	// Input data
	[SerializeField] private Mesh _mesh;

	[SerializeField, ReadOnly] public HexgridMeshData _meshData = new HexgridMeshData();

	public void InitialiseFromMesh( Mesh inMesh )
	{
		_mesh = inMesh;
		Initialise();
	}

	[Button( "Bake mesh face data" )]
	public void Initialise()
	{
		if( _meshData == null )
		{
			_meshData = new HexgridMeshData();
		}

		if( _mesh == null )
		{
			_meshData.Clear();
			EditorUtility.SetDirty( this );
			return;
		}
		
		if( _mesh == _meshData._mesh )
		{
			return;
		}

		_meshData.InitialiseFromMesh( _mesh );

#if UNITY_EDITOR
		EditorUtility.SetDirty( this );
#endif
	}
}

