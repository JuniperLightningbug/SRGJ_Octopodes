using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


/**
 * While SatelliteManager handles the instancing of Satellite3Ds and their orbits, this class gives the player new
 * satellites to use. Information is passed to UISatelliteCards to display, and draw events should be received from
 * a central orchestration event e.g. "after storm", "on progression reached", "on planet started", etc.
 *
 * The current plan is to make this a component of GameManager, rather than a separate singleton or MB
 */
[System.Serializable]
public class SatelliteDeck
{
	// Note: not using a queue because it's useful to serialise the list for debugging
	[SerializeField] private List<SO_Satellite> _queuedSatellites = new List<SO_Satellite>();
	[SerializeField] private int _queuePosition = 0;

	void Initialise( List<SO_Satellite> inSatellitesData )
	{
		_queuedSatellites = inSatellitesData;
		_queuePosition = 0;
	}

	public bool DrawSatellites( int num )
	{
		if( !Application.isPlaying )
		{
			return false;
		}

		if( _queuePosition >= _queuedSatellites.Count )
		{
			Debug.LogWarningFormat( "Satellite queue is empty!" );
			return false;
		}
		
		int numClamped = Mathf.Min( _queuedSatellites.Count - _queuePosition, num );
		if( num != numClamped )
		{
			Debug.LogWarningFormat( "Trying to dequeue {0} satellite datas but there's only {1} left", num,
				_queuedSatellites.Count );
		}

		for( int i = 0; i < numClamped; ++i )
		{
			EventBus.Invoke( EventBus.EEventType.DrawSatelliteCard, _queuedSatellites[_queuePosition] );
			++_queuePosition;
		}
		return numClamped > 0;
	}

}
