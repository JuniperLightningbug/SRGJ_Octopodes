using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIOutro : MonoBehaviour
{
	[NonSerialized, ShowNonSerializedField]
	private int _panelIdx = 0;

	[SerializeField] private float _startDelay = 1.0f;
	[SerializeField] private float _startFadeTime = 2.0f;
	[SerializeField] private float _fadeTime = 0.5f;
	[SerializeField] private float _targetAlpha = 1.0f;
	private Sequence _textFadeInSequence;
	[SerializeField] private Image _outroCoverImage;
	
	[SerializeField] private Button _nextButton;
	[SerializeField] private Button _skipButton;
	[SerializeField] private string _nextSceneName = "MainMenu";
	
		
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
		
		_textFadeInSequence = DOTween.Sequence();
		_textFadeInSequence.AppendInterval( _startDelay );

		if( _outroCoverImage )
		{
			_textFadeInSequence.Append( _outroCoverImage.DOFade( 0.0f, _startFadeTime ) );
			_textFadeInSequence.AppendCallback( () => _outroCoverImage.raycastTarget = false );
		}

		_textFadeInSequence.AppendCallback( () => ShowPanel( 0 ) );
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
		
		if( idx >= _introPanels.Count - 1 )
		{
			_nextButton.gameObject.SetActive( false );
		}
		else
		{
			_textFadeInSequence.AppendCallback( () =>
			{
				_nextButton.interactable = true;
			} );
		}
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
		if( _outroCoverImage )
		{
			_textFadeInSequence?.Kill();
			_outroCoverImage.raycastTarget = true;
			_textFadeInSequence = DOTween.Sequence();
			_textFadeInSequence.Append( _outroCoverImage.DOFade( 1.0f, _startFadeTime ) );
			_textFadeInSequence.AppendCallback( () => SceneManager.LoadScene( _nextSceneName ) );
		}
		else
		{
			SceneManager.LoadScene( _nextSceneName );
		}
	}

}
