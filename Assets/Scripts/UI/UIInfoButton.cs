using NaughtyAttributes;
using UnityEngine;

public class UIInfoButton : MonoBehaviour
{
    [SerializeField, Expandable] private SO_EncyclopediaEntry _encyclopediaEntry;

    public void DisplayInfo()
    {
	    EventBus.Invoke( this, EventBus.EEventType.UI_ShowEncyclopediaEntry, _encyclopediaEntry );
    }
}
