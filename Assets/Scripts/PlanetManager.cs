using MM;
using NaughtyAttributes;
using UnityEngine;

public class PlanetManager : StandaloneSingletonBase<PlanetManager>
{
	[SerializeField] private Object _planetPrefab;

	[SerializeField] private SO_PlanetsConfig _planetsData;
	[SerializeField] private SO_PlanetConfig _activePlanetData;
	[SerializeField] private int _activePlanetIdx = -1;
	[SerializeField] private Planet _activePlanet;

	public Planet ActivePlanet => _activePlanet;
	
	[Button( "Create Planet" )]
	private void Editor_CreatePlanet()
	{
		TryCreatePlanet();
	}

	[Button( "Clear Active Planet" )]
	private void Editor_ClearActivePlanet()
	{
		ClearActivePlanet();
	}

	[Button( "Next Planet" )]
	private void Editor_NextPlanet()
	{
		GoToNextPlanet();
	}

	public void GoToNextPlanet()
	{
		GoToPlanetIdx( _activePlanetIdx + 1 );
	}

	public void GoToPlanetIdx( int planetIdx )
	{
		if( _planetsData )
		{
			_activePlanetData = GetPlanetConfigAtIdx( planetIdx );
			_activePlanetIdx = planetIdx;
		}
	}

	public SO_PlanetConfig GetPlanetConfigAtIdx( int planetIdx )
	{
		if( _planetsData )
		{
			if( _planetsData._bActivateTutorial && _planetsData._tutorialPlanetConfig != null )
			{
				if( planetIdx <= 0 )
				{
					return _planetsData._tutorialPlanetConfig;
				}
				else
				{
					// We used tutorial planet as 0, so idx=1 should translate to the normal list idx=0, etc.
					planetIdx--;
				}
			}
			
			planetIdx = Mathf.Min( planetIdx, _planetsData._planetConfigs.Count );
			return _planetsData._planetConfigs[ planetIdx ];
		}

		return null;
	}

	public Planet TryCreatePlanet()
	{
		ClearActivePlanet();

		if( _activePlanetData == null )
		{
			_activePlanetData = GetPlanetConfigAtIdx( _activePlanetIdx );
		}

		if( _activePlanetData != null )
		{
			GameObject newPlanetObj = Instantiate( _planetPrefab, transform ) as GameObject;
			Planet newPlanet = newPlanetObj?.GetComponent<Planet>();
			if( newPlanet )
			{
				newPlanet.InitialisePlanet( _activePlanetData );
				newPlanet.ChangeSensorType( SO_PlanetConfig.ESensorType.INVALID );
				_activePlanet = newPlanet;
			}
			return newPlanet;
		}

		return null;
	}

	public void ClearActivePlanet()
	{
		if( _activePlanet != null )
		{
			MM.ComponentUtils.DestroyPlaymodeSafe( _activePlanet.gameObject );
		}
	}
}

