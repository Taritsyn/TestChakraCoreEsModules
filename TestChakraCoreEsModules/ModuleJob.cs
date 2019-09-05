using TestChakraCoreEsModules.JsRt;

namespace TestChakraCoreEsModules
{
	/// <summary>
	/// Represents a module job
	/// </summary>
	internal struct ModuleJob
	{
		/// <summary>
		/// The reference to an ES module
		/// </summary>
		public JsModuleRecord Module;

		/// <summary>
		/// The script to parse
		/// </summary>
		public string Script;

		/// <summary>
		/// The location the script came from
		/// </summary>
		public string SourceUrl;

		/// <summary>
		/// Flag indicating whether the script has been parsed
		/// </summary>
		public bool IsParsed;
	}
}