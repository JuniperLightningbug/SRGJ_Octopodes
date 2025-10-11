using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu( fileName = "CompiledHexgridMesh", menuName = "Scriptable Objects/Compiled Hexgrid Mesh" )]
public class SO_CompiledHexgridMesh : ScriptableObject
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
#if UNITY_EDITOR
			EditorUtility.SetDirty( this );
#endif
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

