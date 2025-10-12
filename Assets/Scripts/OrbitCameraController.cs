using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCameraController : MonoBehaviour
{
	[SerializeField] private Vector2 _orbitSpeed = Vector2.one;
	[SerializeField] private float _maxOrbitSpeed = 10.0f;

	[SerializeField, MinMaxSlider( -90.0f, 90.0f )]
	private Vector2 _orbitXRange = new Vector2( -90.0f, 90.0f );

	[SerializeField] private float _FOVZoomSpeed = 1.0f;

	[SerializeField, MinMaxSlider( 15.0f, 80.0f )]
	private Vector2 _FOVZoomRange = new Vector2( 15.0f, 0.0f );

	[SerializeField] private float _distanceZoomSpeed = 1.0f;

	[SerializeField, MinMaxSlider( -10.0f, 0.0f )]
	private Vector2 _distanceZoomRange = new Vector2( -5.0f, -1.5f );

	public enum EZoomType
	{
		FOV,
		Distance,
	}

	[SerializeField] private EZoomType _zoomType = EZoomType.Distance;
	[SerializeField] private Camera _orbitCamera;

	[NonSerialized, ShowNonSerializedField] private Vector3 _eulerRotationDefault = Vector3.zero;
	[NonSerialized, ShowNonSerializedField] private float _zoomFOVDefault = 60.0f;
	[NonSerialized, ShowNonSerializedField] private float _zoomDistanceDefault = 2.0f;

	private bool _bHasInitialised = false;

	private Vector3 _currentEulerRotation;
	private float _currentFOV;
	private float _currentDistance;

	void Start()
	{
		Initialise();
	}

	private void Initialise()
	{
		if( !_bHasInitialised )
		{
			_bHasInitialised = true;
			_eulerRotationDefault = transform.rotation.eulerAngles;

			if( !_orbitCamera )
			{
				_orbitCamera = GetComponent<Camera>();
			}

			if( !_orbitCamera )
			{
				_orbitCamera = Camera.main;
			}

			_zoomFOVDefault = _orbitCamera.fieldOfView;
			_zoomDistanceDefault = _orbitCamera.transform.localPosition.z;

			ResetToDefault();
		}
	}

	public void ResetToDefault()
	{
		_currentDistance = _zoomDistanceDefault;
		_currentEulerRotation = _eulerRotationDefault;
		
		_currentEulerRotation.x = Mathf.Clamp( _currentEulerRotation.x,
			_orbitXRange.x, _orbitXRange.y );
		
		// _zoomFOVDefault = _orbitCamera.fieldOfView = _zoomFOVDefault;
		_orbitCamera.transform.localPosition = new Vector3(
			_orbitCamera.transform.localPosition.x,
			_orbitCamera.transform.localPosition.y,
			_currentDistance );
		transform.rotation = Quaternion.Euler( _currentEulerRotation );

	}

	void Update()
	{
		Initialise();
		OrbitCamera();
		switch( _zoomType )
		{
			case EZoomType.FOV:
				ZoomCameraFOV();
				break;
			case EZoomType.Distance:
				ZoomCameraDistance();
				break;
		}
	}

	private void OrbitCamera()
	{
		if( Mouse.current.rightButton.isPressed && !Mouse.current.rightButton.wasPressedThisFrame )
		{
			Vector2 mouseDelta = Mouse.current.delta.ReadValue();
			Vector2 rotationDelta = new Vector2( -mouseDelta.y * _orbitSpeed.x, mouseDelta.x * _orbitSpeed.y );
			rotationDelta = Vector2.ClampMagnitude( rotationDelta, _maxOrbitSpeed );

			Vector2 previousEulerRotation = _currentEulerRotation;

			_currentEulerRotation.x = Mathf.Clamp( _currentEulerRotation.x + rotationDelta.x,
				_orbitXRange.x, _orbitXRange.y );
			_currentEulerRotation.y += rotationDelta.y;

			transform.rotation = Quaternion.Euler( _currentEulerRotation );
		}
		else if( Mouse.current.rightButton.wasReleasedThisFrame )
		{
			EventBus.Invoke( EventBus.EEventType.TUT_Callback_CameraControlsComplete );
		}
	}

	private void ZoomCameraFOV()
	{
		float scrollInput = Mouse.current.scroll.ReadValue().y;
		if( Mathf.Abs( scrollInput ) > 0.0f && !Mathf.Approximately( scrollInput, 0.0f ) )
		{
			_currentFOV = Mathf.Clamp(
				_currentFOV - scrollInput * _FOVZoomSpeed,
				_FOVZoomRange.x,
				_FOVZoomRange.y );
			_orbitCamera.fieldOfView = _currentFOV;
		}
	}

	private void ZoomCameraDistance()
	{
		float scrollInput = Mouse.current.scroll.ReadValue().y;
		if( Mathf.Abs( scrollInput ) > 0.0f && !Mathf.Approximately( scrollInput, 0.0f ) )
		{
			_currentDistance = Mathf.Clamp(
				_currentDistance + scrollInput * _distanceZoomSpeed,
				_distanceZoomRange.x,
				_distanceZoomRange.y );
			_orbitCamera.transform.localPosition = new Vector3(
				_orbitCamera.transform.localPosition.x,
				_orbitCamera.transform.localPosition.y,
				_currentDistance );
		}
	}
}
