using System;
using System.Collections.Generic;
using System.IO;

using TestChakraCoreEsModules.Helpers;
using TestChakraCoreEsModules.JsRt;
using TestChakraCoreEsModules.Resources;
using TestChakraCoreEsModules.Utilities;

namespace TestChakraCoreEsModules
{
	/// <summary>
	/// ES module manager
	/// </summary>
	internal sealed class EsModuleManager : IDisposable
	{
		/// <summary>
		/// Delegate that returns a new source context
		/// </summary>
		private Func<JsSourceContext> _getNextSourceContext;

		/// <summary>
		/// Callback for receiving notification to fetch a dependent module
		/// </summary>
		private JsFetchImportedModuleCallback _fetchImportedModuleCallback;

		/// <summary>
		/// Callback for receiving notification when module is ready
		/// </summary>
		private JsNotifyModuleReadyCallback _notifyModuleReadyCallback;

		/// <summary>
		/// Cache of modules
		/// </summary>
		private Dictionary<string, JsModuleRecord> _moduleCache = new Dictionary<string, JsModuleRecord>();

		/// <summary>
		/// Queue of module jobs
		/// </summary>
		private Queue<ModuleJob> _moduleJobQueue = new Queue<ModuleJob>();

		/// <summary>
		/// Synchronizer of module evaluation
		/// </summary>
		private readonly object _evaluationSynchronizer = new object();

		/// <summary>
		/// Flag indicating whether this object is disposed
		/// </summary>
		private readonly InterlockedStatedFlag _disposedFlag = new InterlockedStatedFlag();


		/// <summary>
		/// Constructs an instance of ES module manager
		/// </summary>
		/// <param name="getNextSourceContext">Delegate that returns a new source context</param>
		public EsModuleManager(Func<JsSourceContext> getNextSourceContext)
		{
			_getNextSourceContext = getNextSourceContext;
			_fetchImportedModuleCallback = FetchImportedModule;
			_notifyModuleReadyCallback = NotifyModuleReady;
		}

		private static string GetModuleFullPath(JsModuleRecord referencingModule, string modulePath)
		{
			string parentModulePath = "/";
			if (referencingModule.IsValid)
			{
				JsValue parentModuleUrl = referencingModule.Url;
				parentModulePath = parentModuleUrl.IsValid && parentModuleUrl.ValueType == JsValueType.String ?
					parentModuleUrl.ToString() : string.Empty;
			}
			string moduleFullPath = PathHelpers.MakeAbsolutePath(parentModulePath, modulePath);

			return moduleFullPath;
		}

		private JsErrorCode FetchImportedModule(JsModuleRecord referencingModule, JsValue specifier,
			out JsModuleRecord dependentModuleRecord)
		{
			string modulePath = specifier.ValueType == JsValueType.String ?
				specifier.ToString() : string.Empty;
			string moduleFullPath = GetModuleFullPath(referencingModule, modulePath);

			if (_moduleCache.ContainsKey(moduleFullPath))
			{
				dependentModuleRecord = _moduleCache[moduleFullPath];
			}
			else
			{
				dependentModuleRecord = JsModuleRecord.Create(referencingModule, modulePath, moduleFullPath);
				dependentModuleRecord.AddRef();

				_moduleCache[moduleFullPath] = dependentModuleRecord;

				ModuleJob job = new ModuleJob
				{
					Module = dependentModuleRecord,
					Script = string.Empty,
					SourceUrl = moduleFullPath,
					IsParsed = false
				};
				_moduleJobQueue.Enqueue(job);
			}

			return JsErrorCode.NoError;
		}

		private JsErrorCode NotifyModuleReady(JsModuleRecord referencingModule, JsValue exception)
		{
			if (!exception.IsValid)
			{
				ModuleJob job = new ModuleJob
				{
					Module = referencingModule,
					Script = string.Empty,
					SourceUrl = string.Empty,
					IsParsed = true
				};
				_moduleJobQueue.Enqueue(job);
			}

			return JsErrorCode.NoError;
		}

		private JsValue EvaluateModulesTree()
		{
			JsValue result = JsValue.Invalid;

			while (_moduleJobQueue.Count > 0)
			{
				ModuleJob job = _moduleJobQueue.Dequeue();
				JsModuleRecord module = job.Module;

				if (job.IsParsed)
				{
					result = module.Evaluate();
				}
				else
				{
					string code = job.Script;
					if (code.Length == 0)
					{
						string path = job.SourceUrl;

						try
						{
							code = File.ReadAllText(path.TrimStart('/'));
						}
						catch (IOException e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
						{
							string errorMessage = string.Format(Strings.Runtime_ModuleNotFound, path);
							JsValue errorValue = JsErrorHelpers.CreateError(errorMessage);
							module.Exception = errorValue;

							break;
						}
						catch (Exception e)
						{
							string errorMessage = string.Format(Strings.Runtime_ModuleNotLoaded, path, e.Message);
							JsValue errorValue = JsErrorHelpers.CreateError(e.Message);
							module.Exception = errorValue;

							break;
						}
					}

					JsValue exception;
					module.ParseSource(code, _getNextSourceContext(), out exception);

					if (exception.IsValid)
					{
						break;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Evaluates an module code
		/// </summary>
		/// <param name="code">Module code</param>
		/// <param name="path">Path to the module</param>
		/// <returns>Result of the module evaluation</returns>
		public JsValue EvaluateModuleCode(string code, string path)
		{
			if (code == null)
			{
				throw new ArgumentNullException(
					nameof(code),
					string.Format(Strings.Common_ArgumentIsNull, nameof(code))
				);
			}

			if (path == null)
			{
				throw new ArgumentNullException(
					nameof(path),
					string.Format(Strings.Common_ArgumentIsNull, nameof(path))
				);
			}

			if (string.IsNullOrWhiteSpace(code))
			{
				throw new ArgumentException(
					string.Format(Strings.Common_ArgumentIsEmpty, nameof(code)),
					nameof(code)
				);
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException(
					string.Format(Strings.Common_ArgumentIsEmpty, nameof(path)),
					nameof(path)
				);
			}

			return EvaluateModuleInternal(code, path);
		}

		/// <summary>
		/// Evaluates an module file
		/// </summary>
		/// <param name="path">Path to the module file</param>
		/// <returns>Result of the module evaluation</returns>
		public JsValue EvaluateModuleFile(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(
					nameof(path),
					string.Format(Strings.Common_ArgumentIsNull, nameof(path))
				);
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException(
					string.Format(Strings.Common_ArgumentIsEmpty, nameof(path)),
					nameof(path)
				);
			}

			if (!File.Exists(path))
			{
				throw new FileNotFoundException(
					string.Format(Strings.Common_FileNotExist, path),
					path
				);
			}

			return EvaluateModuleInternal(string.Empty, path);
		}

		private JsValue EvaluateModuleInternal(string code, string path)
		{
			JsModuleRecord invalidModule = JsModuleRecord.Invalid;
			string modulePath = path;
			string moduleFullPath = GetModuleFullPath(invalidModule, modulePath);

			JsValue result;

			lock (_evaluationSynchronizer)
			{
				JsModuleRecord module = JsModuleRecord.Create(invalidModule, modulePath, moduleFullPath);
				module.SetFetchImportedModuleCallback(_fetchImportedModuleCallback);
				module.SetNotifyModuleReadyCallback(_notifyModuleReadyCallback);

				ModuleJob job = new ModuleJob
				{
					Module = module,
					Script = code,
					SourceUrl = moduleFullPath,
					IsParsed = false
				};
				_moduleJobQueue.Enqueue(job);

				try
				{
					result = EvaluateModulesTree();
					JsValue exception = module.Exception;

					if (exception.IsValid)
					{
						JsValue metadata = JsValue.Invalid;
						if (!JsContext.HasException)
						{
							JsErrorHelpers.SetException(exception);
						}
						metadata = JsContext.GetAndClearExceptionWithMetadata();

						throw JsErrorHelpers.CreateScriptExceptionFromMetadata(metadata);
					}
				}
				finally
				{
					_moduleJobQueue.Clear();
					module.Release();
				}
			}

			return result;
		}

		#region IDisposable implementation

		/// <summary>
		/// Disposes a ES module manager
		/// </summary>
		public void Dispose()
		{
			if (_disposedFlag.Set())
			{
				if (_moduleJobQueue != null)
				{
					_moduleJobQueue.Clear();
					_moduleJobQueue = null;
				}

				if (_moduleCache != null)
				{
					_moduleCache.Clear();
					_moduleCache = null;
				}

				_fetchImportedModuleCallback = null;
				_notifyModuleReadyCallback = null;
				_getNextSourceContext = null;
			}
		}

		#endregion
	}
}