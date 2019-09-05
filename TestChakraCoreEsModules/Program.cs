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
					JsValue resultValue = moduleManager.EvaluateModuleCode(
						@"import * as geometry from './geometry/geometry.js';
new geometry.Square(15).area;",
						"Files/main.js"
					);
					WriteLine("Result of evaluation: {0}", resultValue.ConvertToString().ToString());
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