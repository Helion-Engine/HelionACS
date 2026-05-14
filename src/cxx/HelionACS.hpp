#pragma once

#include "ACSVM/ACSVM/Types.hpp"
#include "HelionACSExport.hpp"
#include <cstddef>

struct ModuleData {
    std::size_t length;
    ACSVM::Byte* data;
};
HELIONACS_API ModuleData MakeModuleData(std::size_t length);
using LoadModuleCallback = ModuleData(*)(void* context, const char* path, std::size_t pathLength);
using CallSpecImplCallback = ACSVM::Word(*)(void* context, ACSVM::Thread* thread, ACSVM::Word spec, const ACSVM::Word* argv, ACSVM::Word argc);
using CheckTagCallback = bool(*)(void* context, ACSVM::Word type, ACSVM::Word tag);

class Executor;

HELIONACS_API Executor* MakeExecutor(LoadModuleCallback loadModuleCallback, CallSpecImplCallback callSpecImplCallback, CheckTagCallback checkTagCallback, void* executorContext);
using FreeCSThreadInfoData = void (*)(void* data);
struct CSThreadInfo {
    void* data;
    FreeCSThreadInfoData freeCallback;
};
HELIONACS_API void LoadHubMap(
    Executor* executor,
    ACSVM::Word hubId,
    ACSVM::Word mapId,
    const char** moduleNames, std::size_t moduleNamesLength
);
HELIONACS_API ACSVM::Word ScriptStartType(Executor* executor, ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API ACSVM::Word ScriptStartTypeForced(Executor* executor, ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);

HELIONACS_API bool ScriptStartName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API bool ScriptStartNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API bool ScriptStartForcedName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API bool ScriptStartForcedNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API ACSVM::Word ScriptStartResultName(Executor* executor, const char* name, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API ACSVM::Word ScriptStartResultNum(Executor* executor, ACSVM::Word num, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
HELIONACS_API bool ScriptStopName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId);
HELIONACS_API bool ScriptStopNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId);
HELIONACS_API bool ScriptPauseName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId);
HELIONACS_API bool ScriptPauseNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId);

HELIONACS_API bool HasActiveThread(Executor* executor);
HELIONACS_API void Exec(Executor* executor);

struct CallFuncThreadData {
    const char* printBufData;
    std::size_t printBufSize;

    void* threadInfoData;

    void* context;
};

using CallFunc = bool (*)(void* funcContext, ACSVM::Thread *thread, ACSVM::Word const *argv, ACSVM::Word argc);
HELIONACS_API ACSVM::Word AddCallFunc(Executor* executor, void* funcContext, CallFunc callFunc);
HELIONACS_API void AddCodeDataACS0(Executor* executor, ACSVM::Word code, const char *args, ACSVM::Word stackArgC, ACSVM::Word callFunc);
HELIONACS_API void MakeThreadTagWait(ACSVM::Thread* thread, ACSVM::Word type, ACSVM::Word tag);
HELIONACS_API void GetThreadPrintBuffer(ACSVM::Thread* thread, const char** buf, std::size_t* length);
HELIONACS_API void* GetThreadContext(ACSVM::Thread* thread);
HELIONACS_API void* GetThreadThreadInfoData(ACSVM::Thread* thread);
HELIONACS_API void PushThreadStack(ACSVM::Thread* thread, ACSVM::Word value);