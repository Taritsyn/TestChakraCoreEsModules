using System;

namespace TestChakraCoreEsModules.JsRt
{
	/// <summary>
	/// Flags for parsing a module
	/// </summary>
	[Flags]
	internal enum JsParseModuleSourceFlags
	{
		/// <summary>
		/// Module source is UTF16
		/// </summary>
		DataIsUTF16LE = 0x00000000,

		/// <summary>
		/// Module source is UTF8
		/// </summary>
		DataIsUTF8 = 0x00000001
	}
}