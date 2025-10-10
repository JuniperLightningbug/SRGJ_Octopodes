using System;
using TMPro;
using UnityEngine;

public class UIEncyclopedia : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _titleTMP;
	[SerializeField] private TextMeshProUGUI _descriptionTMP;
	
	private void OnGlobalEvent_UIInfoButtonClicked( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_EncyclopediaEntry entry )
		{
			if( _titleTMP )
			{
				_titleTMP.SetText( entry._title );
			}

			if( _descriptionTMP )
			{
				_descriptionTMP.SetText( entry._description );
			}
			
			gameObject.SetActive( true );
		}
	}

	private void OnDestroy()
	{
		EventBus.StopListening( EventBus.EEventType.UI_ShowEncyclopediaEntry, OnGlobalEvent_UIInfoButtonClicked );
	}

	private void OnEnable()
	{
		InputModeManager.Instance?.AddUIBlockingObject( gameObject );
	}

	private void OnDisable()
	{
		InputModeManager.Instance?.RemoveUIBlockingObject( gameObject );
	}

	public void Initialise()
	{
		EventBus.StartListening( EventBus.EEventType.UI_ShowEncyclopediaEntry, OnGlobalEvent_UIInfoButtonClicked );
	}
	
}
