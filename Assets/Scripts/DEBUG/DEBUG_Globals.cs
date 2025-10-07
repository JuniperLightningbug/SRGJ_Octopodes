using UnityEngine;

[CreateAssetMenu(fileName = "DEBUG_Globals", menuName = "Scriptable Objects/DEBUG_Globals")]
public class DEBUG_Globals : ScriptableObject
{
	// Global variables
	// Note: default is FALSE. Variable fetches should apply release behaviour when false.
	public bool _bShowPlanetLayerFaces = false;
	public bool _bShowPlanetLayerFaceNormals = false;
	public bool _bRemovePlanetLayerFOW = false;
	
	
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
