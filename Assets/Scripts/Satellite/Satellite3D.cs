using NaughtyAttributes;
using UnityEngine;

public class Satellite3D : MonoBehaviour
{
	public MeshRenderer _meshRenderer;
	public MeshFilter _meshFilter;
	public const int kOutlineMaterialIdx = 1;
	
	[Header("Assigned from object builder - debugging only"), Expandable, AllowNesting]
	public SO_Satellite _satelliteData;
	
	public void Initialise( SO_Satellite inSatellite )
	{
		_satelliteData = inSatellite;
		RefreshVisuals();
	}

	public void RefreshVisuals()
	{
		if( _meshRenderer && _satelliteData._outlineMaterial && _meshRenderer.sharedMaterials.Length > kOutlineMaterialIdx )
		{
			_meshRenderer.sharedMaterials[kOutlineMaterialIdx] = _satelliteData._outlineMaterial;
		}
		if( _meshFilter && _satelliteData._mesh )
		{
			_meshFilter.sharedMesh = _satelliteData._mesh;
		}

		// Add a random roll - otherwise this will line up with the orbit circle always
		transform.localRotation = Quaternion.Euler( 0.0f, 0.0f, Random.Range( 0.0f, 360.0f ) );
	}
}
