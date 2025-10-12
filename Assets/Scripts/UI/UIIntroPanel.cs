using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIIntroPanel : MonoBehaviour
{
	[NonSerialized, ShowNonSerializedField]
	private int _panelIdx = 0;
	
	[SerializeField] private float _fadeTime = 0.5f;
	[SerializeField] private float _targetAlpha = 1.0f;
	private Sequence _textFadeInSequence;
	[SerializeField] private UIMainMenu _uiMainMenu;
	
	[SerializeField] private Button _nextButton;
	
	[System.Serializable] public struct IntroPanel
	{
		public GameObject _panel;
		public List<TextMeshProUGUI> _texts;
	}
	
	[SerializeField] private List<IntroPanel> _introPanels;
	
	void OnEnable()
	{
		_panelIdx = 0;

		HideAll();

		ShowPanel( _panelIdx );
	}

	void OnDisable()
	{
		HideAll();
	}

	private void HideAll()
	{
		if( _introPanels != null )
		{
			for( int i = 0; i < _introPanels.Count; ++i )
			{
				_introPanels[i]._panel?.SetActive( false );
				if( _introPanels[i]._texts != null )
				{
					for( int j = 0; j < _introPanels[i]._texts.Count; ++j )
					{
						_introPanels[i]._texts[j].alpha = 0.0f;
					}
				}
			}
		}

		if( _nextButton )
		{
			_nextButton.interactable = false;
		}
	}

	private void ShowPanel( int idx )
	{
		_textFadeInSequence?.Kill();
		HideAll();
		_nextButton.interactable = false;
		_introPanels[idx]._panel?.SetActive( true );
		
		_textFadeInSequence = DOTween.Sequence();
		for( int i = 0; i < _introPanels[idx]._texts.Count; ++i )
		{
			_textFadeInSequence.Append( _introPanels[idx]._texts[i].DOFade( _targetAlpha, _fadeTime )
				.SetEase( Ease.InOutSine ) );
		}

		_textFadeInSequence.AppendCallback( () => _nextButton.interactable = true );
	}

	public void UI_NextPanel()
	{
		++_panelIdx;
		if( _panelIdx >= _introPanels.Count )
		{
			UI_EndAndSceneTransition();
		}
		else
		{
			HideAll();
			ShowPanel( _panelIdx );
		}
	}

	public void UI_SkipAll()
	{
		_textFadeInSequence?.Kill();
		HideAll();
		UI_EndAndSceneTransition();
	}

	public void UI_EndAndSceneTransition()
	{
		if( !_uiMainMenu )
		{
			_uiMainMenu = FindAnyObjectByType<UIMainMenu>();
		}

		if( _uiMainMenu )
		{
			_uiMainMenu.UI_StartGame();
		}
	}
}
