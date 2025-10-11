using UnityEngine;

[CreateAssetMenu(fileName = "DEBUG_Globals", menuName = "Scriptable Objects/DEBUG_Globals")]
public class DEBUG_Globals : ScriptableObject
{
	// Global variables
	// Note: default is FALSE. Variable fetches should apply release behaviour when false.
	public bool _bShowPlanetLayerFaces = false;
	public bool _bShowPlanetLayerFaceNormals = false;
	public bool _bRemovePlanetLayerFOW = false;

	public float _debugStormDamagePerSecond = 0.1f;
	public bool _bDebugStormIsActive = false;
	
	// Config variables (these belong elsewhere)
	public float _satelliteSafeModeTime = 3.0f;
	public float _satelliteRadius = 0.2f;
	public bool _bInfiniteHealth = false;
	public float _discoveryFadeOutStartTime = 5.0f;
	public float _discoveryFadeOutEndTime = 8.0f;
	
	
	// Global access
	public void ActivateProfile()
	{
		_activeProfile = this;
	}
	private static DEBUG_Globals _activeProfile;
	public static DEBUG_Globals ActiveProfile
	{
		get
		{
			if( !_activeProfile )
			{
				_activeProfile = ScriptableObject.CreateInstance<DEBUG_Globals>();
			}

			return _activeProfile;
		}
		set
		{
#if UNITY_EDITOR
			_activeProfile = value;
#endif
		}
	}
}
