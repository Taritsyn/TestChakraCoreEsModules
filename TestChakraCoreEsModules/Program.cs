using System;
using static System.Console;

using TestChakraCoreEsModules.JsRt;

namespace TestChakraCoreEsModules
{
	class Program
	{
		static void Main(string[] args)
		{
			using (JsRuntime runtime = JsRuntime.Create())
			{
				JsContext context = runtime.CreateContext();
				context.AddRef();

				JsSourceContext sourceContext = JsSourceContext.FromIntPtr(IntPtr.Zero);
				var moduleManager = new EsModuleManager(() => sourceContext++);
				var scope = new JsScope(context);

				try
				{
					JsValue moduleNamespace;

					// It's not working. Always returns a result equal to `undefined`.
					JsValue resultValue = moduleManager.EvaluateModuleCode(
						@"import * as geometry from './geometry/geometry.js';
new geometry.Square(15).area;",
						"Files/main-with-return-value.js",
						out moduleNamespace
					);
					WriteLine("Return value: {0}", resultValue.ConvertToString().ToString());

					// It's works. We can return the result value by using the default export.
					moduleManager.EvaluateModuleCode(
						@"import * as geometry from './geometry/geometry.js';
export default new geometry.Square(20).area;",
						"Files/main-with-default-export.js",
						out moduleNamespace
					);
					JsPropertyId defaultPropertyId = JsPropertyId.FromString("default");
					JsValue defaultPropertyValue = moduleNamespace.GetProperty(defaultPropertyId);
					WriteLine("Default export: {0}", defaultPropertyValue.ConvertToString().ToString());

					// It's works. We can return the result value by using the named export.
					moduleManager.EvaluateModuleCode(
						@"import * as geometry from './geometry/geometry.js';
export let squareArea = new geometry.Square(25).area;",
						"Files/main-with-named-export.js",
						out moduleNamespace
					);
					JsPropertyId squareAreaPropertyId = JsPropertyId.FromString("squareArea");
					JsValue squareAreaPropertyValue = moduleNamespace.GetProperty(squareAreaPropertyId);
					WriteLine("Named export: {0}", squareAreaPropertyValue.ConvertToString().ToString());
				}
				catch (JsException e)
				{
					WriteLine("During working of JavaScript engine an error occurred.");
					WriteLine();
					Write(e.Message);

					var scriptException = e as JsScriptException;
					if (scriptException != null)
					{
						WriteLine();

						JsValue errorValue = scriptException.Metadata.GetProperty("exception");
						JsValueType errorValueType = errorValue.ValueType;

						if (errorValueType == JsValueType.Error || errorValueType == JsValueType.Object)
						{
							JsValue messageValue;
							JsPropertyId stackPropertyId = JsPropertyId.FromString("stack");

							if (errorValue.HasProperty(stackPropertyId))
							{
								messageValue = errorValue.GetProperty(stackPropertyId);
							}
							else
							{
								messageValue = errorValue.GetProperty("message");
							}

							WriteLine(messageValue.ToString());
						}
						else if (errorValueType == JsValueType.String)
						{
							WriteLine(errorValue.ToString());
						}
						else
						{
							WriteLine(errorValue.ConvertToString().ToString());
						}
					}
				}
				finally
				{
					scope.Dispose();
					moduleManager?.Dispose();
					context.Release();
				}
			}
		}
	}
}