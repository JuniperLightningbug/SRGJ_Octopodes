using NaughtyAttributes;
using UnityEngine;

public class DEBUG_GlobalsSceneInstancer : MonoBehaviour
{
	[SerializeField, Expandable] private DEBUG_Globals _profile;

	void Awake()
	{
		_profile.ActivateProfile();
	}
}
