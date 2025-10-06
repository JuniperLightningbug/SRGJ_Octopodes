using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

/**
 * Object representing a satellite orbital
 * Orbits are all represented as circles (in this project, for the time being) with consistent periods
 * Multiple satellites can share the same orbit, with different time offsets
 */
public class SatelliteOrbit : MonoBehaviour
{
	/*
	 * A note on intended transform hierarchy:
	 *
	 * -> Root (with attached script): world space, moves to planet position but does not rotate
	 * |-> _orbitalTransformRoot: Rotates to the correct direction
	 * |--> _orbitalVisualsToScaleRoot (component or child transform if necessary for visuals scale or rotation offset)
	 * |--> _satelliteRootTransform: root of all spawned satellites (rotates in local space over time)
	 * |---> [n] spawned satellites
	 *
	 * We're using the root's Y-up for the orbitals (i.e. the visualisation and inputs are for XZ, the axis is Y)
	 */
	
	// Rotate the orbital circle to the input direction
	[SerializeField] private Transform _orbitalTransformRoot;
	
	// Transform to apply a _scale_ to in order to create the desired radius (e.g. a circle from a line renderer)
	[SerializeField] private Transform _orbitalVisualsToScaleRoot;
	
	// Transform parenting all the satellites. Since we're rotating them at the same rate, we can do it once.
	// But we don't necessarily want to rotate the whole object (e.g. the line renderer, other visuals)
	[SerializeField] private Transform _satelliteTransformRoot;

	[SerializeField] private Object _satellitePrefab;

	[ReadOnly, SerializeField] private List<Transform> _satelliteTransforms;

	[SerializeField, Tooltip("Set by instantiating manager")] private float _radius = 1.0f;
	[SerializeField, Tooltip("Set by instantiating manager (Rotations Per Second)")] private float _orbitSpeed = 1.0f;

	private Vector3 _directionReference0 = Vector3.right;
	private Vector3 _directionReference1 = Vector3.forward;
	private Vector3 _cachedTransformUp = Vector3.up;

	public void Initialise( Vector3 centre, Quaternion rotation, float radius, bool bApplyRadiusFromSurface, float orbitSpeed, Vector3 startReferencePosition )
	{
		transform.position = centre;
		transform.rotation = rotation;
		transform.localScale = Vector3.one;
		_cachedTransformUp = transform.up;

		_radius = bApplyRadiusFromSurface ? startReferencePosition.magnitude + radius : radius;
		if( _orbitalVisualsToScaleRoot )
		{
			_orbitalVisualsToScaleRoot.localScale = Vector3.one * _radius;
		}
		_orbitSpeed = orbitSpeed;

		Vector3 startDirection = transform.InverseTransformPoint( startReferencePosition ).normalized;
		_directionReference0 = startDirection;
		_directionReference1 = startDirection;
		
		UpdateOrbitRotationToReferencePositions();
	}

	void Update()
	{
		// Rotate satellites
		float frameRotationDegrees = _orbitSpeed * Time.deltaTime * 360.0f;
		_satelliteTransformRoot.Rotate( new Vector3( 0.0f, -frameRotationDegrees, 0.0f ), Space.Self );
	}

	public void UpdateSecondReferencePosition( Vector3 pos1 )
	{
		_directionReference1 = transform.InverseTransformPoint( pos1 ).normalized;
		UpdateOrbitRotationToReferencePositions();
	}

	public void UpdateOrbitRotationToReferencePositions()
	{
		// Note: we're using Y-up for the orbitals (i.e. the visualisation and inputs are for XZ, the axis is Y)
		if( ApproximatelyEqualOrOpposed( _directionReference0, _directionReference1 ) )
		{
			// Flatten on first radial position instead - this will happen on first frame & can happen again later
			_directionReference1 = Vector3.Cross( _directionReference0, _cachedTransformUp ).normalized;
			// Obviously this won't work for a polar reference 0. TODO. We probably should take the camera forward, but it's a corner case so maybe hard code instead.
		}
		
		// New up direction is the normal of the plane formed by (0,0,0), _directionReference0 and _directionReference1
		Vector3 upDirection = Vector3.Cross(_directionReference0, _directionReference1  ).normalized;
		_orbitalTransformRoot.rotation = Quaternion.FromToRotation( _cachedTransformUp, upDirection );
	}
	
	// Extend MM.MathsUtils - TODO
	// Essentially it's "are these two normalised vectors parallel to each other"
	public static bool ApproximatelyEqualOrOpposed( Vector3 a, Vector3 b, float epsilon = MM.MathsUtils.kFloatEpsilon )
	{
		return MM.MathsUtils.Approximately( Mathf.Abs( a.x ), Mathf.Abs( b.x ), epsilon ) &&
		       MM.MathsUtils.Approximately( Mathf.Abs( a.y ), Mathf.Abs( b.y ), epsilon ) &&
		       MM.MathsUtils.Approximately( Mathf.Abs( a.z ), Mathf.Abs( b.z ), epsilon );
	}

	public Transform LaunchSatellite()
	{
		if( _satellitePrefab )
		{
			// For now, just use the first direction reference point for the initial position
			// TODO: We'll need to think of something better for this - and we will need to handle rotations.
			// TODO: We should apply a local offset and then a local y rotation instead.
			Transform satelliteRoot = _satelliteTransformRoot ? _satelliteTransformRoot : transform;
			GameObject newSatelliteObj = Instantiate( _satellitePrefab, satelliteRoot ) as GameObject;
			if( newSatelliteObj )
			{
				newSatelliteObj.transform.localPosition = satelliteRoot.InverseTransformDirection( _directionReference0 ) * _radius;
				
				// TODO TEMP: COLOUR MAT
				MeshRenderer newMeshRenderer = newSatelliteObj.GetComponent<MeshRenderer>();
				if( newMeshRenderer )
				{
					newMeshRenderer.material.SetColor( "_BaseColor", Random.ColorHSV(0.0f, 1.0f, 0.3f, 0.8f, 0.5f, 0.8f, 1.0f, 1.0f ) );
				}
				
				return newSatelliteObj.transform;
			}
		}

		return null;
	}
}
