using NaughtyAttributes;
using UnityEngine;


[CreateAssetMenu( fileName = "EncyclopediaEntry", menuName = "Scriptable Objects/EncyclopediaEntry" )]
public class SO_EncyclopediaEntry : ScriptableObject
{
	[SerializeField] public string _title;
	[SerializeField, ResizableTextArea] public string _content;
}