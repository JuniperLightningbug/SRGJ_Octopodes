using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlanetsConfig", menuName = "Scriptable Objects/SO_PlanetsConfig")]
public class SO_PlanetsConfig : ScriptableObject
{
    public List<SO_PlanetConfig> _planetConfigs;
}
