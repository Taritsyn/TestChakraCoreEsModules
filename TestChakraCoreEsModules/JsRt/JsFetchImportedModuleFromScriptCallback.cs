namespace TestChakraCoreEsModules.JsRt
{
	/// <summary>
	/// User implemented callback to fetch imported modules dynamically in scripts
	/// </summary>
	/// <remarks>
	/// The callback is invoked on the current runtime execution thread, therefore execution is blocked untill
	/// the callback completes. Notify the host to fetch the dependent module. This is used for the dynamic
	/// import() syntax.
	///
	/// Callback should:
	/// 1. Check if the requested module has been requested before - if yes return the existing module record
	/// 2. If no create and initialize a new module record with <code>JsInitializeModuleRecord</code> to
	///    return and schedule a call to <code>JsParseModuleSource</code> for the new record.
	/// </remarks>
	/// <param name="sourceContext">The referencing script context that calls import()</param>
	/// <param name="specifier">The specifier provided to the import() call</param>
	/// <param name="dependentModuleRecord">The <code>JsModuleRecord</code> of the dependent module. If the
	/// module was requested before from other source, return the existing <code>JsModuleRecord</code>,
	/// otherwise return a newly created <code>JsModuleRecord</code>.</param>
	/// <returns>Returns <code>JsErrorCode.NoError</code> if the operation succeeded or an error code otherwise</returns>
	internal delegate JsErrorCode JsFetchImportedModuleFromScriptCallback(JsSourceContext sourceContext,
		JsValue specifier, out JsModuleRecord dependentModuleRecord);
}