using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class Satellite3D : MonoBehaviour
{
	private static readonly int _propertyIDAlpha = Shader.PropertyToID("_Alpha");

	public MeshRenderer _meshRenderer;
	public MeshFilter _meshFilter;
	public const int kOutlineMaterialIdx = 1;
	
	public GameObject _highlightObject;
	public float _highlightAnimationTime = 0.2f;
	private Material _highlightMaterial;
	[SerializeField, ReadOnly] private float _currentAlpha = 0.0f;
	private bool _bHighlighted = false;

	[Header( "Assigned from object builder - debugging only" ), Expandable, AllowNesting]
	public SO_Satellite _satelliteData;

	private SatelliteOrbit _orbit;
	public SatelliteOrbit Orbit => _orbit;

	public void Initialise( SatelliteOrbit inOrbit, SO_Satellite inSatellite )
	{
		_orbit = inOrbit;
		_satelliteData = inSatellite;
		RefreshVisuals();
	}

	public void RefreshVisuals()
	{
		if( _meshRenderer && _satelliteData._outlineMaterial &&
		    _meshRenderer.materials.Length > kOutlineMaterialIdx )
		{
			_meshRenderer.SetMaterials( new List<Material>()
				{ _meshRenderer.materials[0], _satelliteData._outlineMaterial } ); // todo without instancing
			//_meshRenderer.materials[kOutlineMaterialIdx] = _satelliteData._outlineMaterial;
		}

		if( _meshFilter && _satelliteData._mesh )
		{
			_meshFilter.sharedMesh = _satelliteData._mesh;
		}

		// Add a random roll - otherwise this will line up with the orbit circle always
		transform.localRotation = Quaternion.Euler( 0.0f, 0.0f, Random.Range( 0.0f, 360.0f ) );

		if( _highlightObject )
		{
			// Instance the material
			_highlightMaterial = _highlightObject.GetComponent<MeshRenderer>().material;
		}
	}

	void Update()
	{
		if( _bHighlighted && _highlightMaterial )
		{
			SetHighlightAlpha( Mathf.Clamp01( _currentAlpha + Time.deltaTime / _highlightAnimationTime ) );
		}
	}

	private void SetHighlightAlpha( float alpha )
	{
		if( Mathf.Approximately( alpha, _currentAlpha ) || !_highlightMaterial )
		{
			return;
		}
		_currentAlpha = alpha;
		_highlightMaterial.SetFloat( _propertyIDAlpha, _currentAlpha );
	}
	
	public void Highlight( bool bOn )
	{
		if( _highlightObject )
		{
			_highlightObject.SetActive( bOn );
			_bHighlighted = bOn;
		}

		if( !bOn )
		{
			SetHighlightAlpha( 0.0f );
		}
	}

	public void SafeMode( bool bOn )
	{
		//TODO safe mode
	}
}
