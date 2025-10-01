using UnityEngine;

namespace MM
{
	public static class ApplicationUtils
	{
		public static bool BIsQuitting { get; private set; }
		public static bool BIsPlaying => Application.isPlaying && !BIsQuitting;
		
		static void OnQuit()
		{
			BIsQuitting = true;
		}

		[RuntimeInitializeOnLoadMethod] 
		static void RunOnStart()
		{
			BIsQuitting = false;
			Application.quitting += OnQuit; 
		} 
	}
}