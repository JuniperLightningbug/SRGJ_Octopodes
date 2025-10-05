using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public class Planet : MonoBehaviour
{
	[Header("Planet-specific data")]
	[SerializeField, Expandable] private SO_PlanetData _planetData;

	[Header( "Prefab hierarchy caches" )]
	//[SerializeField] private MeshFilter _planetMeshFilter; // TODO - maybe we just do this in the prefab?
	[SerializeField] private MeshFilter _auroraMeshFilter;
	[SerializeField] private MeshFilter _magnetoMeshFilter;
	[SerializeField] private MeshFilter _massSpecMeshFilter;
	[SerializeField] private MeshFilter _plasmaMeshFilter;
	// TODO textures?
	
	//[SerializeField] private SO_HexgridLayers
	private Mesh _debugtestmesh = null;
	
	public void InitialisePlanet( SO_PlanetData planetData )
	{
		_planetData = planetData;
		InitialisePlanet();
	}

	[Button("Initialise")]
	public void EditorInitialisePlanet()
	{
#if UNITY_EDITOR
		Undo.RecordObject( this, "Initialise planet" );
#endif
		InitialisePlanet();
	}

	public void InitialisePlanet()
	{
		if( !_planetData )
		{
			return;
		}
	}
	
#region MonoBehaviour

	void OnDestroy()
	{
		if( _debugtestmesh )
		{
			Destroy( _debugtestmesh );
		}
	}

#endregion
}
