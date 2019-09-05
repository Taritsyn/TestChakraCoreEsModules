namespace TestChakraCoreEsModules.JsRt
{
	/// <summary>
	/// User implemented callback to get notification when the module is ready
	/// </summary>
	/// <remarks>
	/// The callback is invoked on the current runtime execution thread, therefore execution is blocked until the
	/// callback completes. This callback should schedule a call to <code>JsEvaluateModule</code> to run the module
	/// that has been loaded.
	/// </remarks>
	/// <param name="referencingModule">The referencing module that has finished running
	/// ModuleDeclarationInstantiation step</param>
	/// <param name="exception">If invalid value, the module is successfully initialized and host should queue
	/// the execution job otherwise it's the exception object.</param>
	/// <returns>Returns a <code>JsErrorCode.NoError</code> - note, the return value is ignored</returns>
	internal delegate JsErrorCode JsNotifyModuleReadyCallback(JsModuleRecord referencingModule, JsValue exception);
}