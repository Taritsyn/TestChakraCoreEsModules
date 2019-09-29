using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace TestChakraCoreEsModules.JsRt
{
	/// <summary>
	/// A reference to an ES module
	/// </summary>
	/// <remarks>A module record represents an ES module.</remarks>
	internal struct JsModuleRecord
	{
		/// <summary>
		/// The reference
		/// </summary>
		private readonly IntPtr _reference;

		/// <summary>
		/// Gets a invalid module record
		/// </summary>
		public static JsModuleRecord Invalid
		{
			get { return new JsModuleRecord(IntPtr.Zero); }
		}

		/// <summary>
		/// Gets or sets a exception
		/// </summary>
		public JsValue Exception
		{
			get
			{
				IntPtr exceptionPtr;

				JsErrorHelpers.ThrowIfError(NativeMethods.JsGetModuleHostInfo(this,
					JsModuleHostInfoKind.Exception, out exceptionPtr));
				var exception = (JsValue)exceptionPtr;

				return exception;
			}

			set
			{
				var exceptionPtr = (IntPtr)value;

				JsErrorHelpers.ThrowIfError(NativeMethods.JsSetModuleHostInfo(this,
					JsModuleHostInfoKind.Exception, exceptionPtr));
			}
		}

		/// <summary>
		/// Gets or sets a specifier
		/// </summary>
		public JsValue Specifier
		{
			get
			{
				IntPtr specifierPtr;

				JsErrorHelpers.ThrowIfError(NativeMethods.JsGetModuleHostInfo(this,
					JsModuleHostInfoKind.HostDefined, out specifierPtr));
				var specifier = (JsValue)specifierPtr;

				return specifier;
			}

			set
			{
				var specifierPtr = (IntPtr)value;

				JsErrorHelpers.ThrowIfError(NativeMethods.JsSetModuleHostInfo(this,
					JsModuleHostInfoKind.HostDefined, specifierPtr));
			}
		}

		/// <summary>
		/// Gets or sets a URL
		/// </summary>
		public JsValue Url
		{
			get
			{
				IntPtr urlPtr;

				JsErrorHelpers.ThrowIfError(NativeMethods.JsGetModuleHostInfo(this,
					JsModuleHostInfoKind.Url, out urlPtr));
				var url = (JsValue)urlPtr;

				return url;
			}

			set
			{
				var urlPtr = (IntPtr)value;

				JsErrorHelpers.ThrowIfError(NativeMethods.JsSetModuleHostInfo(this,
					JsModuleHostInfoKind.Url, urlPtr));
			}
		}

		/// <summary>
		/// Gets a namespace object
		/// </summary>
		public JsValue Namespace
		{
			get
			{
				JsValue moduleNamespace;
				JsErrorHelpers.ThrowIfError(NativeMethods.JsGetModuleNamespace(this, out moduleNamespace));

				return moduleNamespace;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the module record is valid
		/// </summary>
		public bool IsValid
		{
			get { return _reference != IntPtr.Zero; }
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="JsModuleRecord"/> struct
		/// </summary>
		/// <param name="reference">The reference</param>
		private JsModuleRecord(IntPtr reference)
		{
			_reference = reference;
		}


		/// <summary>
		/// Initialize a <code>JsModuleRecord</code> from host
		/// </summary>
		/// <remarks>Bootstrap the module loading process by creating a new module record</remarks>
		/// <param name="referencingModule">The parent module of the new module - invalid module record
		/// for a root module</param>
		/// <param name="specifier">The specifier coming from the module source code</param>
		/// <param name="normalizedSpecifier">The normalized specifier for the module</param>
		/// <returns>The new module record. The host should not try to call this API twice with the same
		/// <paramref name="normalizedSpecifier"/></returns>
		public static JsModuleRecord Create(JsModuleRecord referencingModule, string specifier,
			string normalizedSpecifier)
		{
			JsValue specifierValue = JsValue.FromString(specifier);
			specifierValue.AddRef();

			JsValue normalizedSpecifierValue = JsValue.FromString(normalizedSpecifier);
			normalizedSpecifierValue.AddRef();

			JsModuleRecord moduleRecord;

			try
			{
				JsErrorHelpers.ThrowIfError(NativeMethods.JsInitializeModuleRecord(referencingModule,
					normalizedSpecifierValue, out moduleRecord));
				moduleRecord.Specifier = specifierValue;
				moduleRecord.Url = normalizedSpecifierValue;
			}
			finally
			{
				specifierValue.Release();
				normalizedSpecifierValue.Release();
			}

			return moduleRecord;
		}

		/// <summary>
		/// Sets a user implemented callback to fetch additional imported modules in ES modules
		/// </summary>
		/// <param name="fetchImportedModuleCallback">The callback function being set</param>
		public void SetFetchImportedModuleCallback(JsFetchImportedModuleCallback fetchImportedModuleCallback)
		{
			JsErrorHelpers.ThrowIfError(NativeMethods.JsSetModuleHostInfo(this,
				JsModuleHostInfoKind.FetchImportedModuleCallback,
				Marshal.GetFunctionPointerForDelegate(fetchImportedModuleCallback)
			));
		}

		/// <summary>
		/// Sets a user implemented callback to fetch imported modules dynamically in scripts
		/// </summary>
		/// <param name="fetchImportedModuleFromScriptCallback">The callback function being set</param>
		public void SetFetchImportedModuleFromScriptCallback(
			JsFetchImportedModuleFromScriptCallback fetchImportedModuleFromScriptCallback)
		{
			JsErrorHelpers.ThrowIfError(NativeMethods.JsSetModuleHostInfo(this,
				JsModuleHostInfoKind.FetchImportedModuleFromScriptCallback,
				Marshal.GetFunctionPointerForDelegate(fetchImportedModuleFromScriptCallback)
			));
		}

		/// <summary>
		/// Sets a user implemented callback to get notification when the module is ready
		/// </summary>
		/// <param name="notifyModuleReadyCallback">The callback function being set</param>
		public void SetNotifyModuleReadyCallback(JsNotifyModuleReadyCallback notifyModuleReadyCallback)
		{
			JsErrorHelpers.ThrowIfError(NativeMethods.JsSetModuleHostInfo(this,
				JsModuleHostInfoKind.NotifyModuleReadyCallback,
				Marshal.GetFunctionPointerForDelegate(notifyModuleReadyCallback)
			));
		}

		/// <summary>
		/// Parse the source for an ES module
		/// </summary>
		/// <remarks>
		/// This is basically ParseModule operation in ES6 spec. It is slightly different in that:
		/// a) The <code>JsModuleRecord</code> was initialized earlier, and passed in as an argument.
		/// b) This includes a check to see if the module being parsed is the last module in the
		///    dependency tree. If it is it automatically triggers module instantiation.
		/// </remarks>
		/// <param name="script">The source script to be parsed, but not executed in this code</param>
		/// <param name="sourceContext">A cookie identifying the script that can be used by debuggable
		/// script contexts</param>
		/// <param name="exception">The error object if there is parse error</param>
		public void ParseSource(string script, JsSourceContext sourceContext, out JsValue exception)
		{
			Encoding encoding;
			JsParseModuleSourceFlags sourceFlags;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				encoding = Encoding.Unicode;
				sourceFlags = JsParseModuleSourceFlags.DataIsUTF16LE;
			}
			else
			{
				encoding = Encoding.UTF8;
				sourceFlags = JsParseModuleSourceFlags.DataIsUTF8;
			}

			var byteArrayPool = ArrayPool<byte>.Shared;
			int bufferLength = encoding.GetByteCount(script);
			byte[] buffer = byteArrayPool.Rent(bufferLength + 1);
			buffer[bufferLength] = 0;

			encoding.GetBytes(script, 0, script.Length, buffer, 0);

			try
			{
				NativeMethods.JsParseModuleSource(this, sourceContext, buffer, (uint)bufferLength,
					sourceFlags, out exception);
			}
			finally
			{
				byteArrayPool.Return(buffer);
			}
		}

		/// <summary>
		/// Execute module code
		/// </summary>
		/// <remarks>
		/// This method implements 15.2.1.1.6.5, "ModuleEvaluation" concrete method.
		/// This method should be called after the engine notifies the host that the module is ready.
		/// This method only needs to be called on root modules - it will execute all of the dependent modules.
		///
		/// One moduleRecord will be executed only once. Additional execution call on the same module record
		/// will fail.
		/// </remarks>
		/// <returns>The return value of the module</returns>
		public JsValue Evaluate()
		{
			JsValue result;
			NativeMethods.JsModuleEvaluation(this, out result);

			return result;
		}

		/// <summary>
		/// Adds a reference to a module record
		/// </summary>
		/// <remarks>
		/// Calling <code>AddRef</code> ensures that the module record will not be freed until
		/// <code>Release</code> is called.
		/// </remarks>
		/// <returns>The object's new reference count</returns>
		public uint AddRef()
		{
			uint count;
			JsErrorHelpers.ThrowIfError(NativeMethods.JsModuleRecordAddRef(this, out count));

			return count;
		}

		/// <summary>
		/// Releases a reference to a module record
		/// </summary>
		/// <remarks>
		/// Removes a reference to a module record that was created by <code>AddRef</code>.
		/// </remarks>
		/// <returns>The object's new reference count</returns>
		public uint Release()
		{
			uint count;
			JsErrorHelpers.ThrowIfError(NativeMethods.JsModuleRecordRelease(this, out count));

			return count;
		}
	}
}