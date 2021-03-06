﻿using ReactNative.Chakra;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace JSRTManaged
{
    public class JSRTManagedExecutor : IDisposable
    {
        private JavaScriptSourceContext _currentSourceContext;
        private JavaScriptRuntime _runtime;
        private JavaScriptContext _context;
        private JavaScriptValue _global;

        public JSRTManagedExecutor()
        {
            _currentSourceContext = JavaScriptSourceContext.None;
            Initialize();
            InitializeJSON();
            InitializeConsole();
        }

        private void Initialize()
        {
            Native.ThrowIfError(Native.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out _runtime));
            Native.ThrowIfError(Native.JsCreateContext(_runtime, out _context));
            Native.ThrowIfError(Native.JsSetCurrentContext(_context));
            Native.ThrowIfError(Native.JsGetGlobalObject(out _global));
        }

        private JavaScriptValue _jsonParse;
        private JavaScriptValue _jsonStringify;

        private void InitializeJSON()
        {
            JavaScriptPropertyId jsonId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName("JSON", out jsonId));

            JavaScriptValue jsonObj;
            Native.ThrowIfError(Native.JsGetProperty(_global, jsonId, out jsonObj));

            JavaScriptPropertyId jsonParseId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName("parse", out jsonParseId));
            Native.ThrowIfError(Native.JsGetProperty(jsonObj, jsonParseId, out _jsonParse));

            JavaScriptPropertyId jsonStringifyId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName("stringify", out jsonStringifyId));
            Native.ThrowIfError(Native.JsGetProperty(jsonObj, jsonStringifyId, out _jsonStringify));
        }

        public JavaScriptValue JsonParse(JavaScriptValue value)
        {
            JavaScriptValue result;
            JavaScriptValue[] args = new[] { _global, value };
            Native.ThrowIfError(Native.JsCallFunction(_jsonParse, args, (ushort)args.Length, out result));

            return result;
        }

        public JavaScriptValue JsonStringify(JavaScriptValue value)
        {
            JavaScriptValue result;
            JavaScriptValue[] args = new[] { _global, value };
            Native.ThrowIfError(Native.JsCallFunction(_jsonStringify, args, (ushort)args.Length, out result));

            return result;
        }

        private static JavaScriptNativeFunction _consoleLog;
        private static JavaScriptNativeFunction _consoleError;
        private static JavaScriptNativeFunction _consoleWarn;
        private static JavaScriptNativeFunction _consoleInfo;

        private static JavaScriptValue InvokeConsole(string type, JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] args, ushort argumentCount, IntPtr callbackData)
        {
            GCHandle handleObj = (GCHandle)callbackData;
            JSRTManagedExecutor self = (JSRTManagedExecutor)handleObj.Target;

            Debug.Write($"[JS] {type} ");

            for (ushort i = 1; i < argumentCount; i++)
            {
                JavaScriptValue resultJsString = self.JsonStringify(args[i]);

                IntPtr str;
                UIntPtr strLen;
                Native.ThrowIfError(Native.JsStringToPointer(resultJsString, out str, out strLen));
                var stringifiedResult = Marshal.PtrToStringUni(str, (int)strLen);
                Debug.Write($"{stringifiedResult} ");
            }

            Debug.WriteLine("");

            return JavaScriptValue.Invalid;
        }

        private static JavaScriptValue ConsoleLog(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] args, ushort argumentCount, IntPtr callbackData)
        {
            return InvokeConsole("log", callee, isConstructCall, args, argumentCount, callbackData);
        }

        private static JavaScriptValue ConsoleError(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] args, ushort argumentCount, IntPtr callbackData)
        {
            return InvokeConsole("error", callee, isConstructCall, args, argumentCount, callbackData);
        }

        private static JavaScriptValue ConsoleWarn(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] args, ushort argumentCount, IntPtr callbackData)
        {
            return InvokeConsole("warn", callee, isConstructCall, args, argumentCount, callbackData);
        }

        private static JavaScriptValue ConsoleInfo(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] args, ushort argumentCount, IntPtr callbackData)
        {
            return InvokeConsole("info", callee, isConstructCall, args, argumentCount, callbackData);
        }

        private void InitializeConsole()
        {
            JavaScriptPropertyId consolePropertyId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName("console", out consolePropertyId));

            JavaScriptValue consoleObj;
            Native.ThrowIfError(Native.JsCreateObject(out consoleObj));
            Native.ThrowIfError(Native.JsSetProperty(_global, consolePropertyId, consoleObj, true));

            _consoleLog = ConsoleLog;
            _consoleError = ConsoleError;
            _consoleWarn = ConsoleWarn;
            _consoleInfo = ConsoleInfo;

            GCHandle self = GCHandle.Alloc(this);
            DefineHostCallback(consoleObj, "info", _consoleInfo, (IntPtr)self);
            DefineHostCallback(consoleObj, "log", _consoleInfo, (IntPtr)self);
            DefineHostCallback(consoleObj, "warn", _consoleInfo, (IntPtr)self);
            DefineHostCallback(consoleObj, "error", _consoleInfo, (IntPtr)self);
        }

        private void DefineHostCallback(JavaScriptValue globalObject, string callbackName, JavaScriptNativeFunction callback, IntPtr callbackState)
        {
            JavaScriptPropertyId propertyId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName(callbackName, out propertyId));

            JavaScriptValue function;
            Native.ThrowIfError(Native.JsCreateFunction(callback, callbackState, out function));
            Native.ThrowIfError(Native.JsSetProperty(globalObject, propertyId, function, true));
        }

        public void Dispose()
        {
            Native.ThrowIfError(Native.JsSetCurrentContext(JavaScriptContext.Invalid));
            Native.ThrowIfError(Native.JsDisposeRuntime(_runtime));
        }

        public string GetGlobalVariable(string variable)
        {
            JavaScriptPropertyId variableId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName(variable, out variableId));

            JavaScriptValue variableValue;
            Native.ThrowIfError(Native.JsGetProperty(_global, variableId, out variableValue));

            JavaScriptValue stringifiedValue = JsonStringify(variableValue);

            IntPtr str;
            UIntPtr strLen;
            Native.ThrowIfError(Native.JsStringToPointer(stringifiedValue, out str, out strLen));
            return Marshal.PtrToStringUni(str, (int)strLen);
        }

        public void SetGlobalVariable(string variable, string value)
        {
            JavaScriptPropertyId variableId;
            Native.ThrowIfError(Native.JsGetPropertyIdFromName(variable, out variableId));

            JavaScriptValue stringValue;
            Native.ThrowIfError(Native.JsPointerToString(value, (UIntPtr)value.Length, out stringValue));

            JavaScriptValue parsedValue = JsonParse(stringValue);

            Native.ThrowIfError(Native.JsSetProperty(_global, variableId, parsedValue, true));
        }

        public JavaScriptValue RunScript(string script, string sourceUri)
        {
            string source = LoadScriptAsync(script).Result;

            _currentSourceContext = JavaScriptSourceContext.Add(_currentSourceContext, 1);

            JavaScriptValue result;
            Native.ThrowIfError(Native.JsRunScript(source, _currentSourceContext, sourceUri, out result));

            return result;
        }

        private static async Task<string> LoadScriptAsync(string file)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file).AsTask().ConfigureAwait(false);
                using (var stream = await storageFile.OpenStreamForReadAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = $"File read exception for asset '{file}'.";
                throw new InvalidOperationException(exceptionMessage, ex);
            }
        }

        #region Test Functions

        public int AddNumbers(int first, int second)
        {
            string source = "(() => { return function(x, y) { console.log(x, y); return x + y; }; })()";
            JavaScriptValue function = RunScript(source, string.Empty);

            JavaScriptValue intResult;
            JavaScriptValue arg1, arg2;
            Native.ThrowIfError(Native.JsIntToNumber(first, out arg1));
            Native.ThrowIfError(Native.JsIntToNumber(second, out arg2));
            JavaScriptValue[] args = new[] { _global, arg1, arg2 };
            Native.ThrowIfError(Native.JsCallFunction(function, args, 3, out intResult));

            int intValue;
            Native.ThrowIfError(Native.JsNumberToInt(intResult, out intValue));

            return intValue;
        }

        #endregion
    }
}
