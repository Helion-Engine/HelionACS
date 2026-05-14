#include "HelionACS.hpp"

#include "ACSVM/ACSVM/Environment.hpp"
#include "ACSVM/ACSVM/Module.hpp"
#include "ACSVM/ACSVM/Scope.hpp"
#include "ACSVM/ACSVM/Types.hpp"
#include "ACSVM/ACSVM/Thread.hpp"
#include "ACSVM/ACSVM/CodeData.hpp"
#include "ACSVM/ACSVM/Script.hpp"
#include "ACSVM/ACSVM/Action.hpp"
#include <cstddef>
#include <format>
#include <iostream>
#include <span>
#include <string_view>

class VoidPointerThreadInfo : public ACSVM::ThreadInfo {
public:
    void* data;
    FreeCSThreadInfoData freeCallback;
    VoidPointerThreadInfo(CSThreadInfo ti) : data(ti.data), freeCallback(ti.freeCallback) {}

    ~VoidPointerThreadInfo() override {
        this->freeCallback(this->data);
    }
};

class ThreadImpl : public ACSVM::Thread {
private:
    const VoidPointerThreadInfo* info = nullptr;

public:
    void* executorContext;

    explicit ThreadImpl(ACSVM::Environment* env, void* executorContext) : ACSVM::Thread(env), info(nullptr), executorContext(executorContext) {}

    void start(ACSVM::Script *script, ACSVM::MapScope *map, const ACSVM::ThreadInfo *info, const ACSVM::Word *argV, ACSVM::Word argC) override {
        ACSVM::Thread::start(script, map, info, argV, argC);
        if (info) {
            this->info = static_cast<const VoidPointerThreadInfo*>(info);
        }
    }
    void stop() override {
        ACSVM::Thread::stop();

        delete this->info; this->info = nullptr;
    }
    const ACSVM::ThreadInfo* getInfo() const override {
        return this->info;
    }
};

class Env : public ACSVM::Environment {
private:
    LoadModuleCallback loadModuleCallback;
    CallSpecImplCallback callSpecImplCallback;
    CheckTagCallback checkTagCallback;
    void* executorContext;
public:
    Env(LoadModuleCallback loadModuleCallback, CallSpecImplCallback callSpecImplCallback, CheckTagCallback checkTagCallback, void* executorContext)
        : loadModuleCallback(loadModuleCallback), callSpecImplCallback(callSpecImplCallback), checkTagCallback(checkTagCallback), executorContext(executorContext) {}

    void loadModule(ACSVM::Module *module) override {
        auto data = this->loadModuleCallback(this->executorContext, module->name.s->str, module->name.s->len);
        module->readBytecode(data.data, data.length);
    }
    ACSVM::Word callSpecImpl(ACSVM::Thread *thread, ACSVM::Word spec, const ACSVM::Word *argV, ACSVM::Word argC) override {
        return this->callSpecImplCallback(this->executorContext, thread, spec, argV, argC);
    }
    bool checkTag(ACSVM::Word type, ACSVM::Word tag) override {
        return this->checkTagCallback(this->executorContext, type, tag);
    }
    ACSVM::Thread * allocThread() override {
        return new ThreadImpl(this, this->executorContext);
    }
};

enum class ScriptType : ACSVM::Word {
    Open        = 1,
    Respawn     = 2,
    Death       = 3,
    Enter       = 4,
    Pickup      = 5,
    BlueReturn  = 6,
    RedReturn   = 7,
    WhiteReturn = 8,
    Lightning   = 12,
    Unloading   = 13,
    Disconnect  = 14,
    Return      = 15,
    Event       = 16,
    Kill        = 17,
    Reopen      = 18,
};

class Executor {
private:
    Env env;
    ACSVM::HubScope* currentHubScope = nullptr;
    ACSVM::MapScope* currentMapScope = nullptr;
public:
    Executor(LoadModuleCallback loadModuleCallback, CallSpecImplCallback callSpecImplCallback, CheckTagCallback checkTagCallback, void* executorContext) : env(loadModuleCallback, callSpecImplCallback, checkTagCallback, executorContext) {}

    void LoadHubMap(ACSVM::Word hubId, ACSVM::Word mapId, std::span<const char*> moduleNames) {
        auto global = env.getGlobalScope(0); global->active = true;

        if (
            this->currentHubScope != nullptr && this->currentMapScope != nullptr
            && this->currentHubScope->id == hubId && this->currentMapScope->id == mapId) {
            return;
        }

        if (this->currentHubScope != nullptr && this->currentHubScope->id != hubId) {
            this->currentHubScope->reset();
            this->currentHubScope = nullptr;
            currentMapScope = nullptr;
        }
        this->currentHubScope = global->getHubScope(hubId); this->currentHubScope->active = true;
        if (this->currentMapScope != nullptr && this->currentMapScope->id != mapId) {
            this->currentMapScope->active = false;
            this->currentMapScope = nullptr;
        }
        this->currentMapScope = this->currentHubScope->getMapScope(mapId); this->currentMapScope->active = true;

        if (!this->currentMapScope->hasModules()) {
            auto modules = std::vector<ACSVM::Module *> {};
            for (const auto& n : moduleNames) {
                modules.push_back(this->env.getModule(env.getModuleName(n)));
                auto module = this->env.getModule(env.getModuleName(n));
            }
            this->currentMapScope->addModules(modules.data(), modules.size());
        }
    }

    ACSVM::MapScope::ScriptStartInfo MakeInfo(CSThreadInfo info, ACSVM::Word* argV, std::size_t argC) {
        auto actualInfo = ACSVM::MapScope::ScriptStartInfo {};
        actualInfo.argV = argV;
        actualInfo.argC = argC;
        auto threadInfo = new VoidPointerThreadInfo(info);
        actualInfo.info = threadInfo;
        return actualInfo;
    }
    ACSVM::ScriptName GetScriptName(ACSVM::Word scriptId) {
        return ACSVM::ScriptName(scriptId);
    }
    ACSVM::ScriptName GetScriptName(std::string_view scriptName) {
        auto str = this->env.getString(scriptName.data(), scriptName.length());
        return ACSVM::ScriptName(str);
    }
    ACSVM::ScopeID GetScope(ACSVM::Word hubId, ACSVM::Word mapId) {
        return ACSVM::ScopeID(0, hubId, mapId);
    }

    ACSVM::Word ScriptStartType(ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
        if (this->currentMapScope == nullptr) { return 0; }
        return this->currentMapScope->scriptStartType(type, this->MakeInfo(info, argV, argC));
    }
    ACSVM::Word ScriptStartTypeForced(ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
        if (this->currentMapScope == nullptr) { return 0; }
        return this->currentMapScope->scriptStartTypeForced(type, this->MakeInfo(info, argV, argC));
    }
    template<typename T>
    bool ScriptStart(T scriptId, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
        if (this->currentMapScope == nullptr) { return false; }
        return this->currentMapScope->scriptStart(this->GetScriptName(scriptId), this->GetScope(hubId, mapId), this->MakeInfo(info, argV, argC));
    }
    template<typename T>
    bool ScriptStartForced(T scriptId, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
        if (this->currentMapScope == nullptr) { return false; }
        return this->currentMapScope->scriptStartForced(this->GetScriptName(scriptId), this->GetScope(hubId, mapId), this->MakeInfo(info, argV, argC));
    }
    template<typename T>
    ACSVM::Word ScriptStartResult(T scriptId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
        if (this->currentMapScope == nullptr) { return 0; }
        return this->currentMapScope->scriptStartResult(this->GetScriptName(scriptId), this->MakeInfo(info, argV, argC));
    }
    template<typename T>
    bool ScriptStop(T scriptId, ACSVM::Word hubId, ACSVM::Word mapId) {
        if (this->currentMapScope == nullptr) { return false; }
        return this->currentMapScope->scriptStop(this->GetScriptName(scriptId), this->GetScope(hubId, mapId));
    }
    template<typename T>
    bool ScriptPause(T scriptId, ACSVM::Word hubId, ACSVM::Word mapId) {
        if (this->currentMapScope == nullptr) { return false; }
        return this->currentMapScope->scriptStop(this->GetScriptName(scriptId), this->GetScope(hubId, mapId));
    }

    bool HasActiveThread() {
        return this->env.hasActiveThread();
    }
    void Exec() {
        this->env.exec();
    }

    ACSVM::Word AddCallFunc(void* funcContext, CallFunc callFunc) {
        return this->env.addCallFunc([funcContext, callFunc] (ACSVM::Thread* thread, const ACSVM::Word* argv, ACSVM::Word argc) { return callFunc(funcContext, thread, argv, argc); });
    }
    void AddCodeDataACS0(ACSVM::Word code, const char* args, ACSVM::Word stackArgC, ACSVM::Word callFunc) {
        this->env.addCodeDataACS0(code, { args, stackArgC, callFunc });
    }
};

ModuleData MakeModuleData(std::size_t length) {
    ModuleData ret;
    ret.data = (length != 0) ? (new ACSVM::Byte[length] { 0 }) : nullptr;
    ret.length = length;
    return ret;
}

Executor* MakeExecutor(LoadModuleCallback loadModuleCallback, CallSpecImplCallback callSpecImplCallback, CheckTagCallback checkTagCallback, void* executorContext) {
    return new Executor(loadModuleCallback, callSpecImplCallback, checkTagCallback, executorContext);
}
void LoadHubMap(
    Executor* executor,
    ACSVM::Word hubId,
    ACSVM::Word mapId,
    const char** moduleNames, std::size_t moduleNamesLength
) {
    executor->LoadHubMap(hubId, mapId, { moduleNames, moduleNamesLength });
}
ACSVM::Word ScriptStartType(Executor* executor, ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStartType(type, argV, argC, info);
}
ACSVM::Word ScriptStartTypeForced(Executor* executor, ACSVM::Word type, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStartTypeForced(type, argV, argC, info);
}

// boilerplate hell
bool ScriptStartName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStart(std::string_view(name), hubId, mapId, argV, argC, info);
}
bool ScriptStartNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStart(num, hubId, mapId, argV, argC, info);
}
bool ScriptStartForcedName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStartForced(std::string_view(name), hubId, mapId, argV, argC, info);
}
bool ScriptStartForcedNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStartForced(num, hubId, mapId, argV, argC, info);
}
ACSVM::Word ScriptStartResultName(Executor* executor, const char* name, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStartResult(std::string_view(name), argV, argC, info);
}
ACSVM::Word ScriptStartResultNum(Executor* executor, ACSVM::Word num, ACSVM::Word* argV, std::size_t argC, CSThreadInfo info) {
    return executor->ScriptStartResult(num, argV, argC, info);
}
bool ScriptStopName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId) {
    return executor->ScriptStop(std::string_view(name), hubId, mapId);
}
bool ScriptStopNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId) {
    return executor->ScriptStop(num, hubId, mapId);
}
bool ScriptPauseName(Executor* executor, const char* name, ACSVM::Word hubId, ACSVM::Word mapId) {
    return executor->ScriptPause(std::string_view(name), hubId, mapId);
}
bool ScriptPauseNum(Executor* executor, ACSVM::Word num, ACSVM::Word hubId, ACSVM::Word mapId) {
    return executor->ScriptPause(num, hubId, mapId);
}

bool HasActiveThread(Executor *executor) {
    return executor->HasActiveThread();
}
void Exec(Executor* executor) {
    executor->Exec();
}
ACSVM::Word AddCallFunc(Executor* executor, void* funcContext, CallFunc callFunc) {
    return executor->AddCallFunc(funcContext, callFunc);
}
void AddCodeDataACS0(Executor* executor, ACSVM::Word code, const char *args, ACSVM::Word stackArgC, ACSVM::Word callFunc) {
    executor->AddCodeDataACS0(code, args, stackArgC, callFunc);
}
void MakeThreadTagWait(ACSVM::Thread *thread, ACSVM::Word type, ACSVM::Word tag) {
    thread->state = { ACSVM::ThreadState::WaitTag, tag, type };
}
void GetThreadPrintBuffer(ACSVM::Thread* thread, const char** buf, std::size_t* length) {
    if (thread->printBuf.size() != 0) {
        *buf = thread->printBuf.data();
    } else {
        *buf = nullptr;
    }
    *length = thread->printBuf.size();
}
void* GetThreadContext(ACSVM::Thread* thread) {
    return static_cast<const ThreadImpl*>(thread)->executorContext;
}
void* GetThreadThreadInfoData(ACSVM::Thread* thread) {
    return static_cast<const VoidPointerThreadInfo*>(thread->getInfo())->data;
}
void PushThreadStack(ACSVM::Thread *thread, ACSVM::Word value) {
    thread->dataStk.push(value);
}

ACSVM::Word GetString(ACSVM::Thread* thread, ACSVM::Word index, const char** str) {
    auto mapString = thread->scopeMap->getString(index);
    *str = mapString->str;
    return mapString->len;
}
