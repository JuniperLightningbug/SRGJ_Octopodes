using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetsConfig", menuName = "Scriptable Objects/Planets Config")]
public class SO_PlanetsConfig : ScriptableObject
{
	[Expandable]
    public List<SO_PlanetConfig> _planetConfigs;

	public SO_PlanetConfig _tutorialPlanetConfig;
	
	public bool _bActivateTutorial = true;
}
