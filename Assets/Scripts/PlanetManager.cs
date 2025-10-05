using NaughtyAttributes;
using UnityEngine;

public class PlanetManager : MonoBehaviour
{
	[SerializeField] private SO_PlanetsConfig _planetsData;
	[SerializeField] private Object _planetPrefab;

	[SerializeField] private int _activePlanetIdx;
	[SerializeField] private Planet _activePlanet;

	[Button( "Create Planet" )]
	private void Editor_CreatePlanet()
	{
		CreatePlanet( _activePlanetIdx );
	}

	[Button( "Clear Planet" )]
	private void Editor_ClearPlanet()
	{
		ClearPlanet();
	}

	[Button( "Next Planet" )]
	private void Editor_NextPlanet()
	{
		if( _planetsData )
		{
			_activePlanetIdx = (_activePlanetIdx + 1) % _planetsData._planetConfigs.Count;
		}
	}

	public void CreatePlanet()
	{
		CreatePlanet( _activePlanetIdx );
	}
	
	public void CreatePlanet( int planetIdx )
	{
		_activePlanetIdx = planetIdx;
		ClearPlanet();

		if( _planetsData && planetIdx > 0 && planetIdx < _planetsData._planetConfigs.Count )
		{
			GameObject newPlanetObj = Instantiate( _planetPrefab, transform ) as GameObject;
			Planet newPlanet = newPlanetObj?.GetComponent<Planet>();
			if( newPlanet )
			{
				newPlanet.InitialisePlanet( _planetsData._planetConfigs[planetIdx] );
				_activePlanet = newPlanet;
			}
		}
	}

	public void ClearPlanet()
	{
		if( _activePlanet != null )
		{
			MM.ComponentUtils.DestroyPlaymodeSafe( _activePlanet.gameObject );
		}
	}
}

