using UnityEngine;

[CreateAssetMenu(fileName = "Satellite", menuName = "Scriptable Objects/Satellite")]
public class SO_Satellite : ScriptableObject
{
	[SerializeField] public string _name = "";
	[SerializeField] public string _description = "";
	[SerializeField] public Mesh _mesh;
	[SerializeField] public Color _outlineColour = Color.black;
	[SerializeField] public Sprite _icon;
	[SerializeField] public SO_PlanetConfig.ESensorType _sensorType;

	public string ReadableString()
	{
		return _name.Length > 0 ? _name : _sensorType.ToString();
	}
	

	// TODO other things here! orbitspeed or orbitheight, radius, extra sensors?
}
