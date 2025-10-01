using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Collections;
using UnityEngine;

public class PlasmaParticles_Old : MM.StandaloneSingletonBase<PlasmaParticles_Old>
{
	private static readonly int _propertyIDCameraUp = Shader.PropertyToID("_CameraUp");
	private static readonly int _propertyIDCameraPos = Shader.PropertyToID("_CameraPos");
	private static readonly int _propertyIDColour = Shader.PropertyToID("_Colour");
	private static readonly int _propertyIDPosition = Shader.PropertyToID( "_Position" );
	
	// Unity constraint - max batch size
	private const int _kBatchSize = 1023;
	
	[Header( "Visuals" )]
	[SerializeField] private Mesh _particleMesh;
	[SerializeField] private Material _particleMaterial;
	[SerializeField] private Color _baseColour0 = Color.white;
	[SerializeField] private Color _baseColour1 = Color.white;
	[SerializeField] private Vector2 _colourByVelocity = Vector2.zero;

	[Header( "Behaviour" )]
	[SerializeField] private float _particleLifetime = 5.0f;
	[SerializeField] private int _maxCount = _kBatchSize;
	[ShowNonSerializedField] private int _currentCount = 0;
	
	// Runtime
	private Vector3[] _positions;
	private Vector3[] _velocities;
	private float[] _lifetimes;
	private Vector4[] _colours;
	
	private MaterialPropertyBlock _cachedMatPropBlockObj;
	private Vector4[] _positionsRenderQueue;
	private Vector4[] _coloursRenderQueue;
	private Matrix4x4[] _matricesRenderQueue; // Always identity - but required
	
	private Transform _cameraTransform;
	
	// TODO: Hard-coded for now
	private Vector3 _mCentreVec = Vector3.zero;
	private Vector3 _mDir = Vector3.up;
	private float _mMag = 1.0f;
	
	[SerializeField, MinMaxSlider(0.0f, 1.0f)]
	private Vector2 _atmosphereRadius = Vector2.zero;
	
	// Multiplier per second: min at earth radius, max at atmosphere radius
	[SerializeField, MinMaxSlider( 0.0f, 10.0f )]
	private Vector2 _atmosphereDragPerSec = Vector2.one;
	
	[SerializeField] private float _k = 1.0f;

	protected override void Initialise()
	{
		_positions = new Vector3[_maxCount];
		_velocities = new Vector3[_maxCount];
		_lifetimes = new float[_maxCount];
		_colours = new Vector4[_maxCount];

		for( int i = 0; i < _maxCount; ++i )
		{
			_colours[i] = _baseColour0;
		}
		
		_positionsRenderQueue = new Vector4[_kBatchSize];
		_coloursRenderQueue = new Vector4[_kBatchSize];
		
		// We're not using this (billboard calculation in shader instead) - don't bother updating it each frame
		_matricesRenderQueue = new Matrix4x4[_kBatchSize];

		for( int i = 0; i < _kBatchSize; ++i )
		{
			_matricesRenderQueue[i] = Matrix4x4.identity;
		}
		
		_currentCount = 0;
		
		_cachedMatPropBlockObj = new MaterialPropertyBlock();
		
		_cameraTransform = Camera.main?.transform;
	}

	void FixedUpdate()
	{
		// Apply physics movement in fixed update
		Step( Time.fixedDeltaTime );
	}

	void Update()
	{
		// Apply visuals in update
		Draw();
	}
	
	private void Step( float deltaTime )
	{
		Stack<int> destroyStack = new Stack<int>();
		
		for( int i = 0; i < _currentCount; ++i )
		{
			_lifetimes[i] -= deltaTime;
			if( _lifetimes[i] < 0.0f )
			{
				destroyStack.Push( i );
				continue;
			}
			
			Vector3 pos = _positions[i];
			Vector3 vel = _velocities[i];
			Vector3 rVec = pos - _mCentreVec;
			
			// Check collision with earth (kill) or atmosphere (slow)
			float distToMagnetCentre = rVec.magnitude;
			if( distToMagnetCentre <= _atmosphereRadius.x )
			{
				destroyStack.Push( i );
				continue;
			}
			if( distToMagnetCentre <= _atmosphereRadius.y )
			{
				float dragT = Mathf.InverseLerp( _atmosphereRadius.x, _atmosphereRadius.y, distToMagnetCentre );
				vel *= (1.0f - deltaTime * Mathf.Lerp( _atmosphereDragPerSec.y, _atmosphereDragPerSec.x, dragT ));
			}
			
			// Apply force from magnetic field
			Vector3 fVec = GetMagneticForce( rVec, vel );
			vel += fVec * deltaTime;
			pos += vel * deltaTime;
			
			_positions[i] = pos;
			_velocities[i] = vel;
			_colours[i] = Color.Lerp( _baseColour0, _baseColour1, Mathf.InverseLerp( _colourByVelocity.x, _colourByVelocity.y, vel.magnitude ) );
			//_colours[i] = Color.Lerp( _baseColour0, _baseColour1, _lifetimes[i] / _particleLifetime );
		}
		
		// We iterated through the list in order & reversed the destroy order with a stack
		// Otherwise, the quick-swap-remove wouldn't be safe while enumerating
		while( destroyStack.Count > 0 )
		{
			int destroyIndex = destroyStack.Pop();
			RemoveAt( destroyIndex );
		}
	}
	
	// Render in Update
	private void Draw()
	{
		_particleMaterial.SetVector( _propertyIDCameraUp, _cameraTransform.up );
		_particleMaterial.SetVector(_propertyIDCameraPos, _cameraTransform.position);
		
		for( int i = 0; i < _currentCount; i += _kBatchSize )
		{
			int thisBatchSize = Mathf.Min( _kBatchSize, _currentCount - i );
			for( int j = 0; j < thisBatchSize; ++j )
			{
				_positionsRenderQueue[j] = _positions[i + j];
				_coloursRenderQueue[j] = _colours[i + j];
			}
			
			_cachedMatPropBlockObj.SetVectorArray(_propertyIDPosition, _positionsRenderQueue);
			_cachedMatPropBlockObj.SetVectorArray(_propertyIDColour, _coloursRenderQueue);
			
			Graphics.DrawMeshInstanced(
				_particleMesh,
				0,
				_particleMaterial,
				_matricesRenderQueue,
				thisBatchSize,
				_cachedMatPropBlockObj
			);
		}
	}

	private Vector3 GetMagneticForce(
		Vector3 rVec,
		Vector3 vVec
	)
	{
		Vector3 bVec = MagneticFieldAtPoint( rVec, _mDir * _mMag );
		return _k * Vector3.Cross( vVec, bVec );
	}

	public static Vector3 MagneticFieldAtPoint( Vector3 rVec, Vector3 mVec )
	{
		float rMag = rVec.magnitude;
		float rMag2 = rMag * rMag;
		float rMag5 = Mathf.Max( rMag2 * rMag2 * rMag, MM.MathsUtils.kFloatEpsilon );
		return (3.0f * Vector3.Dot( mVec, rVec ) * rVec - rMag2 * mVec) / rMag5;
	}

	public bool Create( Vector3 position, Vector3 velocity )
	{
		if( _currentCount >= _maxCount )
		{
			return false;
		}
		
		_positions[_currentCount] = position;
		_velocities[_currentCount] = velocity;
		_lifetimes[_currentCount] = _particleLifetime;
		_colours[_currentCount] = _baseColour0;
		++_currentCount;

		return true;
	}

	public void Reset()
	{
		_currentCount = 0;
	}

	private void RemoveAt( int idx )
	{
		if( idx < 0 || idx >= _currentCount )
		{
			return;
		}
		
		int lastIdx = _currentCount - 1;
		
		// Swap all associated data
		_positions[idx] = _positions[lastIdx];
		_velocities[idx] = _velocities[lastIdx];
		_lifetimes[idx] = _lifetimes[lastIdx];
		_colours[idx] = _colours[lastIdx];
		
		--_currentCount;
	}

}
