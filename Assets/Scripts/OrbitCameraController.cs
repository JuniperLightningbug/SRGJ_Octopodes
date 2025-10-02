using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCameraController : MonoBehaviour
{
	[SerializeField] private Vector2 _orbitSpeed = Vector2.one;
	[SerializeField] private Vector2 _orbitXRange = new Vector2( -90.0f, 90.0f );

	private Vector3 _currentEulerRotation;

	void Awake()
	{
		_currentEulerRotation = transform.rotation.eulerAngles;
	}
	
	void Update()
	{
		if( Mouse.current.rightButton.isPressed )
		{
			Vector2 mouseDelta = Mouse.current.delta.ReadValue();

			_currentEulerRotation.x = Mathf.Clamp( _currentEulerRotation.x - mouseDelta.y * _orbitSpeed.x,
				_orbitXRange.x, _orbitXRange.y );
			_currentEulerRotation.y += mouseDelta.x * _orbitSpeed.y;

			transform.rotation = Quaternion.Euler( _currentEulerRotation );
		}
	}
}
