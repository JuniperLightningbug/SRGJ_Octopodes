using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SatelliteDeck", menuName = "Scriptable Objects/SatelliteDeck")]
public class SO_SatelliteDeck : ScriptableObject
{
	// Note: not using a queue because it's useful to serialise the list for debugging
	[SerializeField] private List<SO_Satellite> _startingSet = new List<SO_Satellite>();
	[SerializeField] private List<SO_Satellite> _loopingSet = new List<SO_Satellite>();

	private int _startingSetPosition = 0;
	private int _loopingSetPosition = 0;
	
	public void Reset()
	{
		_startingSetPosition = 0;
		_loopingSetPosition = 0;
	}

	public void Shuffle()
	{
		for( int i = 0; i < _loopingSet.Count - 1; ++i )
		{
			int swapIdx = Random.Range( i, _loopingSet.Count );
			(_loopingSet[swapIdx], _loopingSet[_loopingSetPosition]) =
				(_loopingSet[_loopingSetPosition], _loopingSet[swapIdx]);
		}
	}

	public bool DrawSatellites( int num, bool bLoop = true )
	{
		if( !Application.isPlaying )
		{
			return false;
		}

		bool bDrewSatellites = false;
		for( int i = 0; i < num; ++i )
		{
			SO_Satellite drewSatellite = DrawSatellite();
			if( drewSatellite )
			{
				EventBus.Invoke( EventBus.EEventType.DrawSatelliteCard, drewSatellite );
				bDrewSatellites = true;
			}
		}

		return bDrewSatellites;
	}

	private SO_Satellite DrawSatellite()
	{
		SO_Satellite drewSatellite = null;
		if( _startingSetPosition < _startingSet.Count )
		{
			drewSatellite = _startingSet[_startingSetPosition];
			_startingSetPosition++;
		}
		else if( _loopingSetPosition < _loopingSet.Count )
		{
			drewSatellite = _loopingSet[_loopingSetPosition];
			_loopingSetPosition = (_loopingSetPosition + 1) % _loopingSet.Count;
		}
		return drewSatellite;
	}

}
