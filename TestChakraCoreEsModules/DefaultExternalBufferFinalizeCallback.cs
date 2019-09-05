using System.Runtime.InteropServices;

using TestChakraCoreEsModules.JsRt;

namespace TestChakraCoreEsModules
{
	/// <summary>
	/// Default callback for finalization of external buffer
	/// </summary>
	internal static class DefaultExternalBufferFinalizeCallback
	{
		/// <summary>
		/// Gets a instance of default callback for finalization of external buffer
		/// </summary>
		public static readonly JsFinalizeCallback Instance = Marshal.FreeHGlobal;
	}
}