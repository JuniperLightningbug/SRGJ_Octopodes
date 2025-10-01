using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class PlasmaLauncher : MonoBehaviour
{
	public enum ELaunchDistributionType
	{
		UniformCircleEdge,
		UniformInQuad,
		RandomCircleEdge,
		RandomInCircle,
		RandomInCircleCentreBias,
		RandomInQuad,
		Centre,
	}
	
	[Header("Launch Properties")]
	[SerializeField] private float _speed = 1.0f;
	[SerializeField] private float _positionOffsetMultiplier = 1.0f;
	[SerializeField] private ELaunchDistributionType _launchDistributionType = ELaunchDistributionType.RandomInCircle;
	[SerializeField][MinValue(1)] private int _uniformDistributionSize = 10;
	
	[Header("Perlin noise position")]
	[SerializeField] private bool _bApplyPositionPerlinNoise = false;
	[SerializeField] private bool _bWrapPositionPerlinNoise = true;
	[SerializeField] private float _positionPerlinNoiseAmplitude = 1.0f;
	[SerializeField] private float _positionPerlinNoiseFrequency = 1.0f;
	[SerializeField] private float _positionPerlinNoiseAnimationSpeed = 1.0f;
	
	[Header("Perlin noise velocity")]
	[SerializeField] private bool _bApplyVelocityPerlinNoise = false;
	[SerializeField] private float _velocityPerlinNoiseAmplitude = 1.0f;
	[SerializeField] private float _velocityPerlinNoiseFrequency = 1.0f;
	[SerializeField] private float _velocityPerlinNoiseAnimationSpeed = 1.0f;
	
	private const float _kPerlinNoiseSeedYOffset = 100.0f;
	private const float _kZSpreadEpsilon = 0.05f; // If we spawn a lot of particles at the same plane & the same normal velocity component, it'll create artifacts

	private int _uniformDistributionTracker = 0;

#region Inspector Launch Properties
	[Header("Inspector Launch Properties")]
	[SerializeField] private int _manualLaunchCount = 10;
	[SerializeField] private float _rate = 0.0f;
	
	[Button]
	public void LaunchFromInspector()
	{
		if( CachedPlasmaParticles != null )
		{
			LaunchMultipleParticles( _manualLaunchCount );
		}
	}
	
	[Button]
	public void ClearFromInspector()
	{
		if( CachedPlasmaParticles )
		{
			CachedPlasmaParticles.Reset();
			_uniformDistributionTracker = 0;
		}
	}
#endregion
	
	private float _launchInterval;
	private float _lastLaunchTime;

	private PlasmaParticles _plasmaParticles;
	private PlasmaParticles CachedPlasmaParticles
	{
		get
		{
			if( !_plasmaParticles )
			{
				_plasmaParticles = PlasmaParticles.Instance;
			}

			return _plasmaParticles;
		}
	}

	void OnValidate()
	{
		RecalculateLaunchInterval();
	}

	void Awake()
	{
		RecalculateLaunchInterval();
		_lastLaunchTime = Time.time;
		_uniformDistributionTracker = 0;
	}

	private void RecalculateLaunchInterval()
	{
		_launchInterval = Mathf.Approximately( _rate, 0.0f ) ? 0.0f : 1.0f / _rate;
	}

	void Update()
	{
		float currentTime = Time.time;
		int frameEmissions = Mathf.FloorToInt( (currentTime - _lastLaunchTime) / _launchInterval );

		if( frameEmissions > 0 && CachedPlasmaParticles )
		{
			LaunchMultipleParticles( frameEmissions );
			_lastLaunchTime = Mathf.Min( _lastLaunchTime + _launchInterval * frameEmissions, currentTime );
		}
	}

	private void LaunchMultipleParticles( int numParticles, bool bManual = false )
	{
		if( !CachedPlasmaParticles || numParticles <= 0 )
		{
			return;
		}

		// Start in local space
		Vector3[] launchPositions = DistributeLaunchPositionsLocalSpace(
			_launchDistributionType,
			numParticles,
			_uniformDistributionTracker,
			_uniformDistributionSize );
		
		// Spawning a lot of particles in the same frame, with likely the same starting plane and normal velocity component,
		// will cause artifacts (small "gaps" between particle layers) - spread them out here instead
		for( int i = 0; i < numParticles; ++i )
		{
			launchPositions[i].z += i / (float)numParticles * _kZSpreadEpsilon;
		}

		_uniformDistributionTracker += numParticles;
		_uniformDistributionTracker %= (_launchDistributionType == ELaunchDistributionType.UniformInQuad) ?
			_uniformDistributionSize * _uniformDistributionSize : // Special case: it's a row number for a square layout
			_uniformDistributionSize;

		if( _bApplyPositionPerlinNoise )
		{
			float t = _positionPerlinNoiseAnimationSpeed * Time.time;
			for( int i = 0; i < numParticles; ++i )
			{
				// Perlin noise
				ApplyPerlinNoiseLocalPosition(
					ref launchPositions[i],
					_positionPerlinNoiseAmplitude,
					_positionPerlinNoiseFrequency,
					t,
					_bWrapPositionPerlinNoise );

				// Linear scale
				launchPositions[i] *= _positionOffsetMultiplier;
			}
		}
		
		if( !_bApplyVelocityPerlinNoise )
		{
			transform.TransformPoints( launchPositions );
			Vector3 launchVelocity = transform.forward * _speed;
			CachedPlasmaParticles.CreateMultiple( launchPositions, launchVelocity, numParticles );
		}
		else
		{
			float t = _velocityPerlinNoiseAnimationSpeed * Time.time;
			Vector3[] launchVelocities = new Vector3[numParticles];
			for( int i = 0; i < numParticles; ++i )
			{
				launchVelocities[i].z = 1.0f;
				ApplyPerlinNoiseLocalVelocity( in launchPositions[i], ref launchVelocities[i], _velocityPerlinNoiseAmplitude,
					_velocityPerlinNoiseFrequency, t );
				launchVelocities[i] = launchVelocities[i].normalized * _speed;
			}
			
			transform.TransformPoints( launchPositions );
			transform.TransformVectors( launchVelocities );
			CachedPlasmaParticles.CreateMultiple( launchPositions, launchVelocities, numParticles );
		}
	}

	private static Vector3[] DistributeLaunchPositionsLocalSpace(
		ELaunchDistributionType distributionType,
		int numPositions = 1,
		int uniformDistributionStart = 0,
		int uniformDistributionMax = 1 )
	{
		Vector3[] outPositions = new Vector3[numPositions];
		for( int i = 0; i < numPositions; ++i )
		{
			outPositions[i] = DistributeLaunchPositionLocalSpace( distributionType, uniformDistributionStart + i, uniformDistributionMax );
		}
		return outPositions;
	}

	private static Vector3 DistributeLaunchPositionLocalSpace(
		ELaunchDistributionType distributionType,
		int uniformDistributionIdx = 0,
		int uniformDistributionMax = 1 ) // 0..1 for variations across multiple launches
	{
		Vector3 outPosition;
		switch( distributionType )
		{
			case ELaunchDistributionType.UniformCircleEdge:
			{
				float angle = (uniformDistributionIdx / (float)uniformDistributionMax) * 2.0f * Mathf.PI;
				outPosition = new Vector3( Mathf.Cos( angle ), Mathf.Sin( angle ), 0.0f ) * 0.5f;
				break;
			}
			case ELaunchDistributionType.UniformInQuad:
			{
				int row = uniformDistributionIdx % uniformDistributionMax;
				int col = uniformDistributionIdx / uniformDistributionMax;
				float safeDenominator = (float)Mathf.Max( uniformDistributionMax - 1, 1.0f );
				outPosition = new Vector3(
					row / safeDenominator - 0.5f,
					col / safeDenominator - 0.5f,	
					0.0f
				);
				break;
			}
			case ELaunchDistributionType.RandomCircleEdge:
			{
				float angle = Random.Range( 0.0f, 1.0f ) * 2.0f * Mathf.PI;
				outPosition = new Vector3( Mathf.Cos( angle ), Mathf.Sin( angle ), 0.0f ) * 0.5f;
				break;
			}
			case ELaunchDistributionType.RandomInCircle:
			{
				outPosition = Random.insideUnitCircle * 0.5f;
				break;
			}
			case ELaunchDistributionType.RandomInCircleCentreBias:
			{
				float angle = Random.Range( 0.0f, 1.0f ) * 2.0f * Mathf.PI;
				outPosition = new Vector3( Mathf.Cos( angle ), Mathf.Sin( angle ), 0.0f ) * Random.Range( 0.0f, 0.5f );
				break;
			}
			case ELaunchDistributionType.RandomInQuad:
			{
				outPosition = new Vector3( Random.Range( -0.5f, 0.5f ), Random.Range( -0.5f, 0.5f ), 0.0f );
				break;
			}
			case ELaunchDistributionType.Centre:
			{
				outPosition = Vector3.zero;
				break;
			}
			default:
			{
				outPosition = Vector3.zero;
				break;
			}
		}
		return outPosition;
	}

	// Input position: x in (-0.5, 0.5), y in (-0.5, 0.5), z unaccounted for
	private static void ApplyPerlinNoiseLocalPosition( ref Vector3 position, float amplitude, float frequency, float t,
		bool bWrapQuadSpace = true )
	{
		float timeInput0 = t;
		float timeInput1 = timeInput0 / 2.0f;
		float positionXScaled = position.x * frequency;
		float positionYScaled = position.y * frequency;
		float xOffset = (
			(Mathf.PerlinNoise( positionXScaled + timeInput0, positionYScaled + timeInput1 ) + 
			 Mathf.PerlinNoise( positionXScaled - timeInput1, positionYScaled - timeInput0 )) / 2.0f
			- 0.5f) * amplitude;
		float yOffset = (
			(Mathf.PerlinNoise( positionXScaled + timeInput1 + _kPerlinNoiseSeedYOffset, positionYScaled + timeInput0 + _kPerlinNoiseSeedYOffset ) + 
			 Mathf.PerlinNoise( positionXScaled - timeInput0 + _kPerlinNoiseSeedYOffset, positionYScaled - timeInput1 + _kPerlinNoiseSeedYOffset )) / 2.0f
			- 0.5f) * amplitude;
		
		position.x += xOffset;
		position.y += yOffset;
		if( bWrapQuadSpace )
		{
			position.x = Mathf.Repeat( position.x + 0.5f, 1.0f ) - 0.5f;
			position.y = Mathf.Repeat( position.y + 0.5f, 1.0f ) - 0.5f;
		}
	}
	
	private static void ApplyPerlinNoiseLocalVelocity( in Vector3 position, ref Vector3 velocity, float amplitude, float frequency, float t )
	{
		velocity.x += (Mathf.PerlinNoise( position.x * frequency + t, position.y + t ) - 0.5f) * amplitude;
		velocity.y += (Mathf.PerlinNoise( position.x + t, position.y + t + _kPerlinNoiseSeedYOffset ) - 0.5f) * amplitude;
		// Will be stronger on diagonals of course - TODO?
	}

}
