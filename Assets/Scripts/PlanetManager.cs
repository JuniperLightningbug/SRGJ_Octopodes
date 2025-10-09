using MM;
using NaughtyAttributes;
using UnityEngine;

public class PlanetManager : StandaloneSingletonBase<PlanetManager>
{
	[SerializeField] private SO_PlanetsConfig _planetsData;
	[SerializeField] private Object _planetPrefab;

	[SerializeField] private int _activePlanetIdx = -1;
	[SerializeField] private Planet _activePlanet;

	public Planet ActivePlanet => _activePlanet;
	
	[Button( "Create Planet" )]
	private void Editor_CreatePlanet()
	{
		TryCreatePlanet( _activePlanetIdx );
	}

	[Button( "Clear Active Planet" )]
	private void Editor_ClearActivePlanet()
	{
		ClearActivePlanet();
	}

	[Button( "Next Planet" )]
	private void Editor_NextPlanet()
	{
		GoToNextPlanet( true );
	}

	public void GoToNextPlanet( bool bCreateImmediately, bool bWrap = false )
	{
		GoToPlanetIdx( _activePlanetIdx + 1, bCreateImmediately, bWrap );
	}

	public void GoToPlanetIdx( int planetIdx, bool bCreateImmediately, bool bWrap = false )
	{
		if( _planetsData )
		{
			if( bWrap )
			{
				planetIdx %= _planetsData._planetConfigs.Count;
			}

			_activePlanetIdx = planetIdx;
		}

		if( bCreateImmediately )
		{
			TryCreatePlanet( _activePlanetIdx );
		}
	}

	public Planet TryCreatePlanet()
	{
		return TryCreatePlanet( _activePlanetIdx );
	}
	
	public Planet TryCreatePlanet( int planetIdx )
	{
		_activePlanetIdx = planetIdx;
		ClearActivePlanet();

		if( _planetsData && planetIdx >= 0 && planetIdx < _planetsData._planetConfigs.Count )
		{
			GameObject newPlanetObj = Instantiate( _planetPrefab, transform ) as GameObject;
			Planet newPlanet = newPlanetObj?.GetComponent<Planet>();
			if( newPlanet )
			{
				newPlanet.InitialisePlanet( _planetsData._planetConfigs[planetIdx] );
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

