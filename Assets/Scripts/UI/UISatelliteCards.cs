using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UISatelliteCards : MonoBehaviour
{
	[Header( "Config" )]
	[SerializeField] private Object _satellite2DPrefab;
	[SerializeField] private int _satellite2DMaxNum = 10;
	
	[Header("Runtime")]
	[SerializeField] private List<Satellite2D> _inactiveCards = new List<Satellite2D>();
	[SerializeField] private List<Satellite2D> _activeCards = new List<Satellite2D>();
	[SerializeField] private Satellite2D _selectedCard;
	[SerializeField] private bool _bDisabledInteraction = false;
	
	private void OnGlobalEvent_DrawSatelliteCard( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_Satellite newSatelliteData )
		{
			if( newSatelliteData != null )
			{
				ActivateCard( newSatelliteData );
			}
		}
	}
	
	private void OnGlobalEvent_DisableInteraction( EventBus.EventContext context, object obj = null )
	{
		_bDisabledInteraction = true;
	}
	
	private void OnGlobalEvent_EnableInteraction( EventBus.EventContext context, object obj = null )
	{
		_bDisabledInteraction = false;
	}

	private void OnGlobalEvent_LaunchedSatellite( EventBus.EventContext context, object obj = null )
	{
		DeactivateCurrentSelection();
	}

	private void OnCardClicked( Satellite2D satelliteClicked )
	{
		if( _bDisabledInteraction )
		{
			return;
		}
		
		Satellite2D previousSelection = _selectedCard;
		
		// Remove old selection
		if( _selectedCard )
		{
			_selectedCard.SetSelected( false );
			_selectedCard = null;
		}

		// Unless this is a toggle-off event, add new selection
		if( satelliteClicked && previousSelection != satelliteClicked )
		{
			satelliteClicked.SetSelected( true );
			_selectedCard = satelliteClicked;
		}

		if( _selectedCard != null )
		{
			EventBus.Invoke( this, EventBus.EEventType.UI_QueueSatelliteCard, _selectedCard._satelliteData );
		}
		else
		{
			EventBus.Invoke( this, EventBus.EEventType.UI_DequeueSatelliteCard );
		}
	}

	private Satellite2D ActivateCard( SO_Satellite newSatelliteData )
	{
		if( !newSatelliteData )
		{
			return null;
		}
		
		if( _inactiveCards.Count == 0 )
		{
			// TODO we could instantiate more
			Debug.LogErrorFormat(
				"Tried adding a new satellite card when there are none available. Count: {0}, Max: {1}",
				_activeCards.Count, _satellite2DMaxNum );
			return null;
		}

		Satellite2D newActiveCard = _inactiveCards[_inactiveCards.Count - 1];
		
		if( newActiveCard )
		{
			newActiveCard.Initialise( newSatelliteData );
			newActiveCard.gameObject.SetActive( true );
			_inactiveCards.RemoveAt( _inactiveCards.Count - 1 );
			_activeCards.Add( newActiveCard );
			return newActiveCard;
		}
		
		return null;
	}

	private void DeactivateCurrentSelection()
	{
		if( _selectedCard )
		{
			DeactivateCard( _selectedCard );
		}
	}
	
	private bool DeactivateCard( Satellite2D toDeactivate )
	{
		if( toDeactivate )
		{
			for( int i = 0; i < _activeCards.Count; ++i )
			{
				if( _activeCards[i] == toDeactivate )
				{
					DeactivateCardAt( i );
					return true;
				}
			}
		}

		return false;
	}

	private bool DeactivateCardAt( int index )
	{
		if( index >= 0 && index < _activeCards.Count )
		{
			Satellite2D toDeactivate = _activeCards[index];
			_inactiveCards.Add( toDeactivate );
			_activeCards.RemoveAt( index );
			
			if( _selectedCard && _selectedCard == toDeactivate )
			{
				_selectedCard.SetSelected( false );
				_selectedCard.gameObject.SetActive( false );
				_selectedCard = null;
			}
			return true;
		}

		return false;
	}

	private void Start()
	{
		// Create cards
		if( _satellite2DPrefab != null )
		{
			for( int i = 0; i < _satellite2DMaxNum; ++i )
			{
				GameObject newCardObj = Instantiate( _satellite2DPrefab, transform ) as GameObject;
				if( newCardObj != null )
				{
					Satellite2D satellite = newCardObj.GetComponent<Satellite2D>();
					if( satellite != null )
					{
						_inactiveCards.Add( satellite );
						if( satellite._button )
						{
							satellite._button.onClick.AddListener( () => OnCardClicked( satellite ) );
						}
						newCardObj.SetActive( false );
					}
					else
					{
						Destroy( newCardObj );
					}
				}
			}
		}
	}

	private void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.DrawSatelliteCard, OnGlobalEvent_DrawSatelliteCard );
		EventBus.StartListening( EventBus.EEventType.LaunchedSatellite, OnGlobalEvent_LaunchedSatellite );
		EventBus.StartListening( EventBus.EEventType.TUT_DisableDeckInteraction, OnGlobalEvent_DisableInteraction );
		EventBus.StartListening( EventBus.EEventType.TUT_EnableDeckInteraction, OnGlobalEvent_EnableInteraction );
	}

	private void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.DrawSatelliteCard, OnGlobalEvent_DrawSatelliteCard );
		EventBus.StopListening( EventBus.EEventType.LaunchedSatellite, OnGlobalEvent_LaunchedSatellite );
		EventBus.StopListening( EventBus.EEventType.TUT_DisableDeckInteraction, OnGlobalEvent_DisableInteraction );
		EventBus.StopListening( EventBus.EEventType.TUT_EnableDeckInteraction, OnGlobalEvent_EnableInteraction );
	}
}
