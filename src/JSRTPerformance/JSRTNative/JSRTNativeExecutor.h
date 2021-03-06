#pragma once

#include "pch.h"
#include <jsrt.h>
#include <ppltasks.h>
#include "ChakraHost.h"
#include "ChakraStringResult.h"

using namespace concurrency;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

namespace JSRTNative {

public ref class JSRTNativeExecutor sealed
{
public:
	int InitializeHost();
	int DisposeHost();

	ChakraStringResult GetGlobalVariable(String^ variableName);
	int SetGlobalVariable(String^ variableName, String^ value);

	int AddNumbers(int first, int second);

	ChakraStringResult RunScript(String^ source, String^ sourceUri);
	ChakraStringResult RunScriptFromFile(String^ source, String^ sourceUri);

	ChakraStringResult CallFunctionAndReturnFlushedQueue(String^ moduleName, String^ methodName, String^ args); // TODO: Parse the arg
	ChakraStringResult InvokeCallbackAndReturnFlushedQueue(int callbackId, String^ args); // global object "__fbBatchedBridge", method "invokeCallbackAndReturnFlushedQueue" parse the arg
	ChakraStringResult FlushedQueue(); // global object "__fbBatchedBridge", "flushedQueue", invoke with global obj
private:
	ChakraHost host;
};

};
