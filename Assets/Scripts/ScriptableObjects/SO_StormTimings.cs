using UnityEngine;

[CreateAssetMenu(fileName = "StormTimings", menuName = "Scriptable Objects/StormTimings")]
public class SO_StormTimings : ScriptableObject
{
	[SerializeField] public float _calmTime = 20.0f;
	[SerializeField] public float _warningTime = 10.0f;
	[SerializeField] public float _stormTime = 10.0f;
}
