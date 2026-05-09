using System.Runtime.InteropServices;

namespace HelionACS.Interop
{
    public static unsafe partial class Methods
    {
        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ModuleData MakeModuleData([NativeTypeName("std::size_t")] nuint length);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern Executor* MakeExecutor([NativeTypeName("LoadModuleCallback")] delegate* unmanaged[Cdecl]<void*, sbyte*, nuint, ModuleData> loadModuleCallback, [NativeTypeName("CallSpecImplCallback")] delegate* unmanaged[Cdecl]<void*, Thread*, uint, uint*, uint, uint> callSpecImplCallback, [NativeTypeName("CheckTagCallback")] delegate* unmanaged[Cdecl]<void*, uint, uint, byte> checkTagCallback, void* executorContext);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void LoadHubMap(Executor* executor, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId, [NativeTypeName("const char **")] sbyte** moduleNames, [NativeTypeName("std::size_t")] nuint moduleNamesLength);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ACSVM::Word")]
        public static extern uint ScriptStartType(Executor* executor, [NativeTypeName("ACSVM::Word")] uint type, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ACSVM::Word")]
        public static extern uint ScriptStartTypeForced(Executor* executor, [NativeTypeName("ACSVM::Word")] uint type, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptStartName(Executor* executor, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptStartNum(Executor* executor, [NativeTypeName("ACSVM::Word")] uint num, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptStartForcedName(Executor* executor, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptStartForcedNum(Executor* executor, [NativeTypeName("ACSVM::Word")] uint num, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ACSVM::Word")]
        public static extern uint ScriptStartResultName(Executor* executor, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ACSVM::Word")]
        public static extern uint ScriptStartResultNum(Executor* executor, [NativeTypeName("ACSVM::Word")] uint num, [NativeTypeName("ACSVM::Word *")] uint* argV, [NativeTypeName("std::size_t")] nuint argC, CSThreadInfo info);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptStopName(Executor* executor, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptStopNum(Executor* executor, [NativeTypeName("ACSVM::Word")] uint num, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptPauseName(Executor* executor, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte ScriptPauseNum(Executor* executor, [NativeTypeName("ACSVM::Word")] uint num, [NativeTypeName("ACSVM::Word")] uint hubId, [NativeTypeName("ACSVM::Word")] uint mapId);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte HasActiveThread(Executor* executor);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void Exec(Executor* executor);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ACSVM::Word")]
        public static extern uint AddCallFunc(Executor* executor, void* funcContext, [NativeTypeName("CallFunc")] delegate* unmanaged[Cdecl]<void*, Thread*, uint*, uint, byte> callFunc);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void AddCodeDataACS0(Executor* executor, [NativeTypeName("ACSVM::Word")] uint code, [NativeTypeName("const char *")] sbyte* args, [NativeTypeName("ACSVM::Word")] uint stackArgC, [NativeTypeName("ACSVM::Word")] uint callFunc);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MakeThreadTagWait([NativeTypeName("ACSVM::Thread *")] Thread* thread, [NativeTypeName("ACSVM::Word")] uint type, [NativeTypeName("ACSVM::Word")] uint tag);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void GetThreadPrintBuffer([NativeTypeName("ACSVM::Thread *")] Thread* thread, [NativeTypeName("const char **")] sbyte** buf, [NativeTypeName("std::size_t *")] nuint* length);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* GetThreadContext([NativeTypeName("ACSVM::Thread *")] Thread* thread);

        [DllImport("HelionACS-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* GetThreadThreadInfoData([NativeTypeName("ACSVM::Thread *")] Thread* thread);
    }
}
