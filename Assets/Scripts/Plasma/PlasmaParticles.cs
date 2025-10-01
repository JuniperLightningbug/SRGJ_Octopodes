using System.Collections.Generic;
using System.Linq;
using MM;
using NaughtyAttributes;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

public class PlasmaParticles : StandaloneSingletonBase<PlasmaParticles>
{
	private static readonly int _propertyIDCameraUp = Shader.PropertyToID("_CameraUp");
	private static readonly int _propertyIDCameraPos = Shader.PropertyToID("_CameraPos");
	private static readonly int _propertyIDColours = Shader.PropertyToID("_Colour");
	private static readonly int _propertyIDPositions = Shader.PropertyToID( "_Position" );
	private static readonly int _propertyIDSizeMultiplier = Shader.PropertyToID( "_SizeMultiplier" );
	
	// Unity constraint - max batch size
	private const int _kBatchSize = 1023;

	[Header( "Visuals" )]
	[SerializeField] private float _particleSize = 0.3f;
	[SerializeField] private Material _particleMaterial;
	[SerializeField] private Color _baseColour0 = Color.white;
	[SerializeField] private Color _baseColour1 = Color.white;
	[SerializeField] private Vector2 _colourByVelocity = Vector2.zero;
	[SerializeField] private float _fadeOutTime = 0.1f;
	[SerializeField] [MinMaxSlider( 0.1f, 20.0f )]
	private Vector2 _sizeMultiplier = Vector2.one;

	[Header( "Behaviour" )]
	[SerializeField] [MinMaxSlider( 0.1f, 20.0f )]
	private Vector2 _particleLifetime = new Vector2( 5.0f, 5.0f );
	[SerializeField] private int _maxCount = _kBatchSize;
	[ShowNonSerializedField] private int _currentCount = 0;
	
	// Runtime
	private Mesh _particleMesh;
	private MaterialPropertyBlock _cachedMatPropBlockObj;
	private Vector4[] _positionsRenderQueue;
	private Vector4[] _coloursRenderQueue;
	private float[] _sizeMultipliersRenderQueue;
	private Matrix4x4[] _matricesRenderQueue; // Always identity - but required
	
	// Burst
	private NativeArray<Vector3> _positions;
	private NativeArray<Vector3> _velocities;
	private NativeArray<float> _lifetimes;
	private NativeArray<float> _sizeMultipliers;
	private NativeArray<Vector4> _colours;
	
	private Transform _cameraTransform;
	
	// TODO: Hard-coded for now
	private Vector3 _mCentreVec = Vector3.zero;
	private Vector3 _mDir = Vector3.up;
	private float _mMag = 1.0f;
	private float kBoundingBoxExtentsMultiplier = 99999.0f;
	
	[SerializeField, MinMaxSlider(0.0f, 1.0f)]
	private Vector2 _atmosphereRadius = Vector2.zero;
	
	// Multiplier per second: min at earth radius, max at atmosphere radius
	[SerializeField, MinMaxSlider( 0.0f, 10.0f )]
	private Vector2 _atmosphereDragPerSec = Vector2.one;
	
	[SerializeField] private float _k = 1.0f;

	protected override void Initialise()
	{
		// Make a quad mesh that doesn't cull
		_particleMesh = Instantiate(PrimitiveQuadBuilder.BuildQuad( _particleSize, kBoundingBoxExtentsMultiplier ));
		
		_positions = new NativeArray<Vector3>(_maxCount, Allocator.Persistent);
		_velocities = new NativeArray<Vector3>(_maxCount, Allocator.Persistent);
		_lifetimes = new NativeArray<float>(_maxCount, Allocator.Persistent);
		_sizeMultipliers = new NativeArray<float>(_maxCount, Allocator.Persistent);
		_colours = new NativeArray<Vector4>(_maxCount, Allocator.Persistent);

		for( int i = 0; i < _maxCount; ++i )
		{
			_colours[i] = _baseColour0;
		}
		
		_positionsRenderQueue = new Vector4[_kBatchSize];
		_coloursRenderQueue = new Vector4[_kBatchSize];
		_sizeMultipliersRenderQueue = new float[_kBatchSize];
		
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
		ParticleStepJob stepJob = new ParticleStepJob()
		{
			_deltaTime = deltaTime,
			_mCentreVec = _mCentreVec,
			_mDir = _mDir,
			_mMag = _mMag,
			_k = _k,
			_atmosphereRadius = _atmosphereRadius,
			_atmosphereDragPerSec = _atmosphereDragPerSec,
			_baseColour0 = _baseColour0,
			_baseColour1 = _baseColour1,
			_colourByVelocity = _colourByVelocity,
			_fadeOutTime = _fadeOutTime,
			_positions = _positions,
			_velocities = _velocities,
			_lifetimes = _lifetimes,
			_sizeMultipliers = _sizeMultipliers,
			_colours = _colours,
		};

		JobHandle stepJobHandle = stepJob.Schedule( _currentCount, 64 );
		stepJobHandle.Complete();

		for( int i = _currentCount - 1; i >= 0; --i )
		{
			if( _lifetimes[i] <= 0.0f )
			{
				RemoveAt( i );
			}
		}
	}
	
	void OnDestroy()
	{
		if (_positions.IsCreated) _positions.Dispose();
		if (_velocities.IsCreated) _velocities.Dispose();
		if (_lifetimes.IsCreated) _lifetimes.Dispose();
		if (_sizeMultipliers.IsCreated) _sizeMultipliers.Dispose();
		if (_colours.IsCreated) _colours.Dispose();
	}
	
	// Render in Update
	private void Draw()
	{
		_particleMaterial.SetVector( _propertyIDCameraUp, _cameraTransform.up );
		_particleMaterial.SetVector( _propertyIDCameraPos, _cameraTransform.position );
		
		for( int i = 0; i < _currentCount; i += _kBatchSize )
		{
			int thisBatchSize = Mathf.Min( _kBatchSize, _currentCount - i );
			for( int j = 0; j < thisBatchSize; ++j )
			{
				_positionsRenderQueue[j] = _positions[i + j];
				_coloursRenderQueue[j] = _colours[i + j];
				_sizeMultipliersRenderQueue[j] = _sizeMultipliers[i + j];
			}
			
			_cachedMatPropBlockObj.SetVectorArray(_propertyIDPositions, _positionsRenderQueue);
			_cachedMatPropBlockObj.SetVectorArray(_propertyIDColours, _coloursRenderQueue);
			_cachedMatPropBlockObj.SetFloatArray(_propertyIDSizeMultiplier, _sizeMultipliersRenderQueue);
			
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
	
	
	// TODO We could burst the creation as well
	
	public bool CreateMultiple( Vector3 position, Vector3[] velocities, int num )
	{
		bool bSuccess = true;
		for( int i = 0; i < num && bSuccess; ++i )
		{
			bSuccess &= Create(position, velocities[i]  );
		}
		return bSuccess;
	}
	
	public bool CreateMultiple( Vector3[] positions, Vector3 velocity, int num )
	{
		bool bSuccess = true;
		for( int i = 0; i < num && bSuccess; ++i )
		{
			bSuccess &= Create(positions[i], velocity  );
		}
		return bSuccess;
	}

	public bool CreateMultiple( Vector3[] positions, Vector3[] velocities, int num )
	{
		bool bSuccess = true;
		for( int i = 0; i < num && bSuccess; ++i )
		{
			bSuccess &= Create(positions[i], velocities[i]  );
		}
		return bSuccess;
	}

	public bool Create( Vector3 position, Vector3 velocity )
	{
		if( _currentCount >= _maxCount )
		{
			return false;
		}
		
		_positions[_currentCount] = position;
		_velocities[_currentCount] = velocity;
		_lifetimes[_currentCount] = UnityEngine.Random.Range( _particleLifetime.x, _particleLifetime.y );
		_sizeMultipliers[_currentCount] = UnityEngine.Random.Range( _sizeMultiplier.x, _sizeMultiplier.y );
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
		_sizeMultipliers[idx] = _sizeMultipliers[lastIdx];
		_colours[idx] = _colours[lastIdx];
		
		--_currentCount;
	}

#region Burst Physics Step

	[BurstCompile]
	struct ParticleStepJob : IJobParallelFor
	{
		public float _deltaTime;
		public Vector3 _mCentreVec;
		public Vector3 _mDir;
		public float _mMag;
		public float _k;
		public Vector2 _atmosphereRadius;
		public Vector2 _atmosphereDragPerSec;
		public Vector4 _baseColour0;
		public Vector4 _baseColour1;
		public Vector2 _colourByVelocity;
		public float _fadeOutTime;
		public NativeArray<Vector3> _positions;
		public NativeArray<Vector3> _velocities;
		public NativeArray<float> _lifetimes;
		public NativeArray<float> _sizeMultipliers;
		public NativeArray<Vector4> _colours;

		public void Execute( int i )
		{
			_lifetimes[i] -= _deltaTime;
			if( _lifetimes[i] < 0.0f )
			{
				return;
			}

			Vector3 pos = _positions[i];
			Vector3 vel = _velocities[i];
			Vector3 rVec = pos - _mCentreVec;

			// Check collision with earth (kill) or atmosphere (slow)
			float distToMagnetCentre = rVec.magnitude;
			if( distToMagnetCentre <= _atmosphereRadius.x )
			{
				_lifetimes[i] = -1.0f;
				return;
			}

			if( distToMagnetCentre <= _atmosphereRadius.y )
			{
				float dragT = InverseLerp( _atmosphereRadius.x, _atmosphereRadius.y, distToMagnetCentre );
				vel *= (1.0f - _deltaTime * math.lerp( _atmosphereDragPerSec.y, _atmosphereDragPerSec.x, dragT ));
			}

			// Apply force from magnetic field
			Vector3 bVec = MagneticFieldAtPoint2( rVec, _mDir * _mMag );
			Vector3 fVec = _k * math.cross( vel, bVec );
			vel += fVec * _deltaTime;
			pos += vel * _deltaTime;

			_positions[i] = pos;
			_velocities[i] = vel;
			Vector4 colour = math.lerp( _baseColour0, _baseColour1,
                           				InverseLerp( _colourByVelocity.x, _colourByVelocity.y, vel.magnitude ) );  // TODO IS THIS BACKWARDS??
			colour *= math.smoothstep( 0.0f, 1.0f, InverseLerp( 0.0f, _fadeOutTime, _lifetimes[i] ) ); // TODO I THINK i fixed the alpha? use that instead?
			_colours[i] = colour;
			//_colours[i] = Color.Lerp( _baseColour0, _baseColour1, _lifetimes[i] / _particleLifetime );
		}
		
		// TODO: NOTE: THIS IS DUPLICATE CODE
		private Vector3 MagneticFieldAtPoint2( Vector3 rVec, Vector3 mVec )
		{
			float rMag = rVec.magnitude;
			float rMag2 = rMag * rMag;
			float rMag5 = math.max( rMag2 * rMag2 * rMag, MM.MathsUtils.kFloatEpsilon );
			return (3.0f * math.dot( mVec, rVec ) * rVec - rMag2 * mVec) / rMag5;
		}

		private static float InverseLerp( float a, float b, float value )
		{
			return a != b ? math.clamp( (value - a) / (b - a), 0.0f, 1.0f ) : 0.0f;
		}
	}

#endregion
	
	
}
