using UnityEngine;

namespace MM
{
	/**
	 *** Exposes functions from 'SingletonHubBase' to inspector scripts
	 * 
	 * Unity doesn't support custom inspectors for generic typed classes - even for fixed type subclasses.
	 * This interface is a workaround that helps us access/modify the relevant data from a PropertyDrawer instead.
	 */
	public interface ISingletonHubBaseEditorUtility
	{
		public void TryRefreshInspectorData();
	}
}