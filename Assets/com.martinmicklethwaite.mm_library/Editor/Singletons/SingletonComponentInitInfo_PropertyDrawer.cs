using UnityEditor;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.Search.ObjectField;

namespace MM
{
	[CustomPropertyDrawer( typeof( SingletonComponentInitInfo ), true )]
	public class SingletonComponentInitInfo_PropertyDrawer : PropertyDrawer
	{
		const string kActiveToggleBindingPath = "_bActive";
		const string kTypeBindingPath = "_typeDisplayString";
		const string kInitialisationModeBindingPath = "_initialisationMode";
		const string kPresetContentBindingPath = "_bPresetContent";

		private const string kContentElementName = "Content";

		public override VisualElement CreatePropertyGUI( SerializedProperty property )
		{
			VisualElement root = new VisualElement();

			VisualElement header = new VisualElement()
			{
				style = { flexDirection = FlexDirection.Row },
			};
			VisualElement content = new VisualElement()
			{
				name = kContentElementName
			};

			Label label = new Label()
			{
				bindingPath = kTypeBindingPath,
				
			};

			Toggle toggle = new Toggle()
			{
				label = "",
				bindingPath = kActiveToggleBindingPath,
				toggleOnLabelClick = true,
			};
			toggle.RegisterValueChangedCallback( TryHandleToggleState );

			header.Add( toggle );
			header.Add( label );

			content.Add( new EnumField()
			{
				bindingPath = kInitialisationModeBindingPath,
				label = "Initialisation",
			} );

			content.Add( new ObjectField()
			{
				objectType = typeof( MM.SingletonComponent ),
				bindingPath = kPresetContentBindingPath,
				label = "Preset Content",
			} );

			root.Add( header );
			root.Add( content );

			HandleToggle( content, property.FindPropertyRelative( kActiveToggleBindingPath ).boolValue );

			return root;
		}

		private void TryHandleToggleState( ChangeEvent<bool> changedEvent )
		{
			VisualElement visual = changedEvent.target as VisualElement;
			if( visual == null )
			{
				return;
			}

			VisualElement expandableVisual = visual.hierarchy.parent.parent.Q( kContentElementName );
			if( expandableVisual != null )
			{
				HandleToggle( expandableVisual, changedEvent.newValue );
			}
		}

		private void HandleToggle( VisualElement content, bool bOn )
		{
			if( content != null )
			{
				content.style.display = bOn ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
	}
}