#pragma once

#include "ACSVM/ACSVM/Types.hpp"
#include <cstddef>

extern "C" {
    struct ModuleData {
        std::size_t length;
        ACSVM::Byte* data;
    };
    ModuleData MakeModuleData(std::size_t length);
    using LoadModuleCallback = ModuleData(*)(void* context, const char* path, std::size_t pathLength);
    using CallSpecImplCallback = ACSVM::Word(*)(void* context, ACSVM::Thread* thread, ACSVM::Word spec, const ACSVM::Word* argv, ACSVM::Word argc);
    using CheckTagCallback = bool(*)(void* context, ACSVM::Word type, ACSVM::Word tag);

    class Executor;

    Executor* MakeExecutor(LoadModuleCallback loadModuleCallback, CallSpecImplCallback callSpecImplCallback, CheckTagCallback checkTagCallback, void* executorContext);
    using FreeCSThreadInfoData = void (*)(void* data);
    struct CSThreadInfo {
        void* data;
        FreeCSThreadInfoData freeCallback;
    };
    void LoadHubMap(
        Executor* executor,
        ACSVM::Word hubId,
        ACSVM::Word mapId,
        const char** moduleNames, std::size_t moduleNamesLength
    );
    ACSVM::Word ScriptStartType(Executor* executor, ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    ACSVM::Word ScriptStartTypeForced(Executor* executor, ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);

    bool ScriptStartName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    bool ScriptStartNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    bool ScriptStartForcedName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    bool ScriptStartForcedNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    ACSVM::Word ScriptStartResultName(Executor* executor, const char* name, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    ACSVM::Word ScriptStartResultNum(Executor* executor, ACSVM::Word num, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info);
    bool ScriptStopName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId);
    bool ScriptStopNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId);
    bool ScriptPauseName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId);
    bool ScriptPauseNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId);

    bool HasActiveThread(Executor* executor);
    void Exec(Executor* executor);

    struct CallFuncThreadData {
        const char* printBufData;
        std::size_t printBufSize;

        void* threadInfoData;

        void* context;
    };

    using CallFunc = bool (*)(void* funcContext, ACSVM::Thread *thread, ACSVM::Word const *argv, ACSVM::Word argc);
    ACSVM::Word AddCallFunc(Executor* executor, void* funcContext, CallFunc callFunc);
    void AddCodeDataACS0(Executor* executor, ACSVM::Word code, const char *args, ACSVM::Word stackArgC, ACSVM::Word callFunc);
    void MakeThreadTagWait(ACSVM::Thread* thread, ACSVM::Word type, ACSVM::Word tag);
    void GetThreadPrintBuffer(ACSVM::Thread* thread, const char** buf, std::size_t* length);
    void* GetThreadContext(ACSVM::Thread* thread);
    void* GetThreadThreadInfoData(ACSVM::Thread* thread);
}
