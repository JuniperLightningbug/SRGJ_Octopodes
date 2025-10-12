using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

public class Satellite3D : MonoBehaviour
{
	private static readonly int _propertyIDBaseColour = Shader.PropertyToID( "_BaseColor" );

	[Header( "Prefab Components" )]
	public MeshRenderer _meshRenderer;

	public MeshFilter _meshFilter;
	public Collider _collider;
	public Transform _satelliteZRotationRoot;
	public Transform _satelliteMeshRoot;
	private Vector3 _originalMeshRootPosition;
	private Quaternion _originalMeshRootRotation;
	private Vector3 _originalMeshRootScale;
	public ParticleSystem _damageParticles;
	public ParticleSystem _deathParticles;

	public GameObject _highlightObject;

	private Material _meshMaterial;
	private Color _originalMeshColour;

	[Header( "Prefab Config" )]
	public float _outlineAlpha = 0.4f;

	public float _deathScaleDownMult = 0.3f;
	public float _deathScaleDownAnimationTime = 0.2f;
	public Color _safeModeMeshColour = Color.black;
	public Color _deadMeshColour = Color.black;

	public float _damageShakePeriod = 5.0f;

	public float _damageShakeStrength_Rotation = 16.0f;
	public int _damageShakeVibrato = 10;
	public float _damageShakeRandomness = 50.0f;
	public float _damageShakeFadeOutTime = 0.5f;

	[Header( "Assigned from object builder - debugging only" ), Expandable, AllowNesting]
	public SO_Satellite _satelliteData;

	private SatelliteOrbit _orbit;
	public SatelliteOrbit Orbit => _orbit;

	// While safe mode is active, this satellite is not selectable
	private Sequence _safeModeSequence;

	private Sequence _deathSequence;

#region Outline

	[Header("Outline Config")]
	[SerializeField] private float _outlineTweenTime = 0.2f;

	[SerializeField] private Color _outlineColorDark = Color.black;
	[SerializeField] private Color _outlineColorNone = new Color( 0, 0, 0, 0 );
	[SerializeField, Tooltip( "Assigned from object builder - debugging only" )]
	private Color _outlineColor = Color.white;
	private Material _outlineMaterial;

	private enum EOutlineState
	{
		None,
		Dark,
		Colour,
	}
	private EOutlineState _currentOutlineState;
	private Tween _outlineTween;

	public void UpdateOutline(
		bool bOrbitIsActive,
		bool bIsAlive,
		bool bIsSensorTypeSelected,
		bool bIsSensorTypeHovered,
		bool bIsSafeModeOn )
	{
		EOutlineState newOutlineState = EOutlineState.None;
		if( !bIsAlive || !bOrbitIsActive )
		{
			newOutlineState = EOutlineState.None;
		}
		else if( bIsSafeModeOn )
		{
			newOutlineState = EOutlineState.Dark;
		}
		else if( bIsSensorTypeSelected || bIsSensorTypeHovered )
		{
			newOutlineState = EOutlineState.Colour;
		}
		// Else EOutlineState.None
		
		if( _currentOutlineState == newOutlineState )
		{
			return;
		}
		_currentOutlineState = newOutlineState;

		Color targetColour = Color.white;
		_outlineTween?.Kill();
		switch( _currentOutlineState )
		{
			case EOutlineState.Dark:
				targetColour = _outlineColorDark;
				break;
			case EOutlineState.Colour:
				targetColour = _outlineColor;
				break;
			case EOutlineState.None:
				targetColour = _outlineColorNone;
				break;
		}
		
		_outlineTween = _outlineMaterial.DOColor( targetColour, _outlineTweenTime ).SetEase( Ease.OutSine );
	}

#endregion


	private Tween _takeDamageTweenRotation;
	private Sequence _takeDamageFadeSequence;

	public void Initialise( SatelliteOrbit inOrbit, SO_Satellite inSatellite )
	{
		_orbit = inOrbit;
		_satelliteData = inSatellite;
		RefreshVisuals();
	}

	void Awake()
	{
		_originalMeshRootPosition = _satelliteMeshRoot.localPosition;
		_originalMeshRootRotation = _satelliteMeshRoot.localRotation;
		_originalMeshRootScale = _satelliteMeshRoot.localScale;
	}

	[Button( "Refresh Visuals" )]
	public void RefreshVisuals()
	{
		if( _meshRenderer )
		{
			_meshMaterial = _meshRenderer.materials[0];
			_outlineMaterial = _meshRenderer.materials[1];
			_originalMeshColour = _meshMaterial.GetColor( _propertyIDBaseColour );
			_meshRenderer.SetMaterials( new List<Material>() { _meshMaterial, _outlineMaterial } );
		}

		if( _meshFilter && _satelliteData?._mesh )
		{
			_meshFilter.sharedMesh = _satelliteData._mesh;
		}

		// Add a random roll - otherwise this will line up with the orbit circle always
		if( _satelliteZRotationRoot )
		{
			_satelliteZRotationRoot.localRotation = Quaternion.Euler( 0.0f, 0.0f, Random.Range( 0.0f, 360.0f ) );
		}

		_originalMeshRootPosition = _satelliteMeshRoot.localPosition;
		_originalMeshRootRotation = _satelliteMeshRoot.localRotation;
		_originalMeshRootScale = _satelliteMeshRoot.localScale;

		if( _satelliteData )
		{
			_outlineColor = _satelliteData._outlineColour;
		}
	}

	public void Highlight( bool bOn )
	{
		if( _highlightObject )
		{
			_highlightObject.SetActive( bOn );
		}
	}

	public void ToggleSafeMode( float safeModeTime, bool bOn )
	{
		if( bOn )
		{
			DeactivateStormDamage( false );
		}

		if( _collider )
		{
			// Disable mouse click while active
			_collider.enabled = !bOn;
		}

		if( _meshMaterial )
		{
			_meshMaterial.SetColor( _propertyIDBaseColour, bOn ? _safeModeMeshColour : _originalMeshColour );
		}
	}

	[Button( "Debug: Activate Damage Effects" )]
	public void ActivateStormDamage()
	{
		if( !_satelliteMeshRoot )
		{
			return;
		}

		_takeDamageTweenRotation?.Kill( complete: false );
		_takeDamageFadeSequence?.Kill( complete: false );

		_satelliteMeshRoot.localRotation = _originalMeshRootRotation;

		_takeDamageTweenRotation = _satelliteMeshRoot.DOShakeRotation(
			duration: _damageShakePeriod,
			strength: _damageShakeStrength_Rotation,
			vibrato: _damageShakeVibrato,
			randomness: _damageShakeRandomness,
			fadeOut: false, // Continuous shaking
			randomnessMode: ShakeRandomnessMode.Harmonic
		).SetLoops( -1, LoopType.Restart );

		if( _damageParticles )
		{
			_damageParticles.Play();
		}
	}

	[Button( "Debug: Deactivate Damage Effects" )]
	public void DeactivateStormDamage()
	{
		DeactivateStormDamage( true );
	}

	public void DeactivateStormDamage( bool bWithFadeOut )
	{
		if( !_satelliteMeshRoot )
		{
			return;
		}

		_takeDamageFadeSequence?.Kill( complete: false );
		if( _takeDamageTweenRotation.IsActive() )
		{
			_takeDamageTweenRotation?.Kill( complete: false );

			if( bWithFadeOut )
			{

				_takeDamageFadeSequence = DOTween.Sequence();

				_takeDamageFadeSequence.Join( _satelliteMeshRoot.DOShakeRotation(
					duration: _damageShakeFadeOutTime,
					strength: _damageShakeStrength_Rotation,
					vibrato: _damageShakeVibrato,
					randomness: _damageShakeRandomness,
					fadeOut: true // Fade out shaking
				) );

				_takeDamageFadeSequence.Append(
					_satelliteMeshRoot.DOLocalRotate( _originalMeshRootRotation.eulerAngles, 0.1f ) );
			}
			else if( _satelliteMeshRoot )
			{
				_satelliteMeshRoot.localRotation = _originalMeshRootRotation;
			}
		}

		if( _damageParticles )
		{
			_damageParticles.Stop();
		}
	}

	[Button( "Debug: Activate Kill Effects" )]
	public void Kill()
	{
		if( _deathSequence != null )
		{
			return;
		}

		_safeModeSequence?.Kill( complete: true );

		DeactivateStormDamage(); // Stop wobbling (with fade)

		if( _collider )
		{
			// Disable clickable
			_collider.enabled = false;
		}

		_deathSequence = DOTween.Sequence();
		_deathSequence.Join( transform
			.DOScale( transform.localScale.x * _deathScaleDownMult, _deathScaleDownAnimationTime )
			.SetEase( Ease.OutQuad ) );

		_meshMaterial.SetColor( _propertyIDBaseColour, _deadMeshColour );

		if( _deathParticles )
		{
			_deathParticles.Play();
		}
	}

	void OnDestroy()
	{
		_safeModeSequence?.Kill();
		_deathSequence?.Kill();
		_outlineTween?.Kill();
		_takeDamageTweenRotation?.Kill();
		_takeDamageFadeSequence?.Kill();
	}
}
