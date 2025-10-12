using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITutorialPopup : MonoBehaviour
{
	[SerializeField] public EventBus.EEventType _displayEvent;
	[SerializeField] private float _panelFadeInTime = 0.5f;
	[SerializeField] private float _textFadeInTime = 1.0f;
	[SerializeField] private Image _panelImage;
	[SerializeField] private List<TextMeshProUGUI> _textList = new List<TextMeshProUGUI>();
	[SerializeField] private Button _closeButton;
	[SerializeField] private float _targetPanelAlpha = 0.9f;
	private Sequence _fadeInSequence;


	public void Initialise()
	{
		EventBus.StartListening( _displayEvent, OnGlobalEvent_PopupEvent );
		EventBus.StartListening( EventBus.EEventType.TUT_Popup_HideAll, OnGlobalEvent_PopupHideAll );
	}

	public void CleanUp()
	{
		EventBus.StopListening( _displayEvent, OnGlobalEvent_PopupEvent );
		EventBus.StopListening( EventBus.EEventType.TUT_Popup_HideAll, OnGlobalEvent_PopupHideAll );
	}

	public void OnDestroy()
	{
		CleanUp();
	}

	private void OnGlobalEvent_PopupEvent( EventBus.EventContext context, object obj = null )
	{
		if( obj is bool bPauseTimeWhenActive )
		{
			if( bPauseTimeWhenActive )
			{
				InputModeManager.Instance?.AddUIBlockingObject( gameObject );
			}
		}
		gameObject.SetActive( true );
	}
	private void OnGlobalEvent_PopupHideAll( EventBus.EventContext context, object obj = null )
	{
		gameObject.SetActive( false );
	}

	private void RestartFadeInSequence()
	{
		StopFadeInSequence();
		_fadeInSequence = DOTween.Sequence();
		
		if( _panelImage )
		{
			_fadeInSequence.Append( _panelImage.DOFade( _targetPanelAlpha, _panelFadeInTime ) );
		}
		
		if( _textList != null )
		{
			for( int i = 0; i < _textList.Count; ++i )
			{
				_fadeInSequence.Append( _textList[i].DOFade( 1.0f, _textFadeInTime ) );
			}
		}

		_fadeInSequence.AppendCallback( () => _closeButton?.gameObject.SetActive( true ) );

		_fadeInSequence.SetUpdate( true ); // Ignore timescale - some tutorial popups pause time!
	}

	private void StopFadeInSequence()
	{
		_fadeInSequence?.Kill();

		if( _panelImage )
		{
			Color panelColor = _panelImage.color;
			panelColor.a = 0.0f;
			_panelImage.color = panelColor;
		}
		
		if( _textList != null )
		{
			for( int i = 0; i < _textList.Count; ++i )
			{
				_textList[i].alpha = 0.0f;
			}
		}

		if( _closeButton )
		{
			_closeButton.gameObject.SetActive( false );
		}
	}

	private void OnEnable()
	{
		RestartFadeInSequence();
	}

	private void OnDisable()
	{
		StopFadeInSequence();
		InputModeManager.Instance?.RemoveUIBlockingObject( gameObject );
	}
}
