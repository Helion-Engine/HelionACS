using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace HelionACS;

public enum ScriptType : uint {
    None        = 0,
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

public readonly ref struct ThreadHandle
{
    private readonly unsafe Interop.Thread* m_ptr;
    internal unsafe ThreadHandle(Interop.Thread* ptr) {
        m_ptr = ptr;
    }

    public void MakeTagWait(uint type, uint tag) {
        unsafe {
            Interop.Methods.MakeThreadTagWait(m_ptr, type, tag);
        }
    }
    public string? GetPrintBuf() {
        unsafe {
            var buf = (sbyte*)null;
            var length = (nuint)0;
            Interop.Methods.GetThreadPrintBuffer(m_ptr, &buf, &length);
            if (buf == null) {
                return "";
            }
            return Marshal.PtrToStringUTF8((nint)buf, (int)length);
        }
    }
    public object GetThreadInfo() {
        unsafe {
            var threadInfoData = Interop.Methods.GetThreadThreadInfoData(m_ptr);
            var threadInfo = GCHandle.FromIntPtr((nint)threadInfoData).Target!;
            return threadInfo;
        }
    }
}

public abstract class Executor {
    unsafe readonly protected Interop.Executor* m_executor;
    private readonly List<GCHandle> m_handles;

    public Executor() {
        unsafe {
            m_handles = [];
            var selfHandle = AddHandle(this);
            m_executor = Interop.Methods.MakeExecutor(&LoadModuleCDecl, &CallSpecImplCDecl, &CheckTagCDecl, (void*)GCHandle.ToIntPtr(selfHandle));
        }
    }
    ~Executor() {
        foreach (var handle in m_handles) {
            handle.Free();
        }
    }

    protected GCHandle AddHandle(object obj) {
        var handle = GCHandle.Alloc(obj);
        m_handles.Add(handle);
        return handle;
    }
    protected GCHandle AddPinnedHandle(object obj) {
        var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
        m_handles.Add(handle);
        return handle;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    unsafe static Interop.ModuleData LoadModuleCDecl(void* context, sbyte* name, nuint length) {
        var self = GCHandle<Executor>.FromIntPtr((nint)context).Target;
        var moduleName = Marshal.PtrToStringUTF8((nint)name, (int)length);
        var data = self.LoadModule(moduleName);
        var ret = Interop.Methods.MakeModuleData((nuint)data.Length);
        fixed (byte* src = data) {
            Buffer.MemoryCopy(
                src,
                ret.data,
                ret.length,
                ret.length
            );
        }
        return ret;
    }
    public abstract byte[] LoadModule(string moduleName);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    unsafe static byte CheckTagCDecl(void* context, uint type, uint tag) {
        var self = GCHandle<Executor>.FromIntPtr((nint)context).Target;
        return (byte)(self.CheckTag(type, tag) ? 1 : 0);
    }
    public abstract bool CheckTag(uint type, uint tag);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    unsafe static uint CallSpecImplCDecl(void* context, Interop.Thread* thread, uint spec, uint* argv, uint argc) {
        var self = GCHandle<Executor>.FromIntPtr((nint)context).Target;

        var argsSpan = new ReadOnlySpan<uint>(argv, (int)argc);
        var args = argsSpan.ToArray();

        return self.CallSpecImpl(new ThreadHandle(thread), spec, args);
    }
    public abstract uint CallSpecImpl(ThreadHandle threadHandle, uint spec, uint[] args);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    unsafe protected static void FreeDataCallback(void* data) {
        var dataHandle = GCHandle.FromIntPtr((nint)data);
        dataHandle.Free();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    unsafe static byte GenericCallFunc(void* funcContext, Interop.Thread* thread, uint* argv, uint argc) {
        var delegateCallFunc = GCHandle<CallFunc>.FromIntPtr((nint)funcContext).Target;

        var argsSpan = new ReadOnlySpan<uint>(argv, (int)argc);
        var args = argsSpan.ToArray();

        var result = (byte)(delegateCallFunc.Invoke(new ThreadHandle(thread), args) ? 1 : 0);
        return result;
    }

    public delegate bool CallFunc(ThreadHandle threadHandle, uint[] args);
    public void AddCodeDataACS0(uint code, string args, uint stackArgC, CallFunc callFunc) {
        var argsBytes = (sbyte[]) (Array) Encoding.UTF8.GetBytes(args + "\0");
        var argsHandle = AddPinnedHandle(argsBytes);

        var callFuncHandle = AddHandle(callFunc);

        unsafe {
            var func = Interop.Methods.AddCallFunc(m_executor, (void*)GCHandle.ToIntPtr(callFuncHandle), &GenericCallFunc);
            Interop.Methods.AddCodeDataACS0(m_executor, code, (sbyte*)argsHandle.AddrOfPinnedObject(), stackArgC, func);
        }
    }

    public void LoadHubMap(uint hubId, uint mapId, string[] moduleNames) {
        var moduleNamesC = Array.ConvertAll(
            moduleNames,
            s => {
                var bytes = Encoding.UTF8.GetBytes(s + "\0");
                var mem = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, mem, bytes.Length);
                return mem;
            }
        ).ToArray();
        try {
            unsafe {
                fixed (nint* moduleNamesPtr = moduleNamesC) {
                    Interop.Methods.LoadHubMap(
                        m_executor,
                        hubId,
                        mapId,
                        (sbyte**)moduleNamesPtr, (nuint)moduleNamesC.Length
                    );
                }
            }
        } finally {
            foreach (var ptr in moduleNamesC) {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
    private static Interop.CSThreadInfo MakeCSThreadInfo(object threadInfo) {
        unsafe {
            var threadInfoHandle = GCHandle.Alloc(threadInfo);
            return new Interop.CSThreadInfo {
                data = (void*)GCHandle.ToIntPtr(threadInfoHandle),
                freeCallback = &FreeDataCallback
            };
        }
    }
    public uint ScriptStartType(ScriptType type, uint[] args, object threadInfo) {
        unsafe { fixed (uint* argV = args) {
            return Interop.Methods.ScriptStartType(m_executor, (uint)type, argV, (nuint)args.Length, MakeCSThreadInfo(threadInfo));
        } }
    }
    public uint ScriptStartTypeForced(ScriptType type, uint[] args, object threadInfo) {
        unsafe { fixed (uint* argV = args) {
            return Interop.Methods.ScriptStartTypeForced(m_executor, (uint)type, argV, (nuint)args.Length, MakeCSThreadInfo(threadInfo));
        } }
    }
    unsafe private static T AdaptScriptName<T>(string name, Func<nuint, T> toCall) {
        var nameBytes = (sbyte[]) (Array) Encoding.UTF8.GetBytes(name + "\0");
        fixed (sbyte* namePtr = nameBytes) {
            return toCall((nuint)namePtr);
        }
    }
    unsafe private static T AdaptScriptArgs<T>(uint[] args, Func<nuint, T> toCall) {
        fixed (uint* argV = args) {
            return toCall((nuint)argV);
        }
    }
    unsafe private static T AdaptScriptNameAndArgs<T>(string name, uint[] args, Func<nuint, nuint, T> toCall) {
        var nameBytes = (sbyte[]) (Array) Encoding.UTF8.GetBytes(name + "\0");
        fixed (sbyte* namePtr = nameBytes) {
            fixed (uint* argV = args) {
                return toCall((nuint)namePtr, (nuint)argV);
            }
        }
    }
    public bool ScriptStart(string name, uint hubId, uint mapId, uint[] args, object threadInfo) {
        unsafe { return AdaptScriptNameAndArgs(name, args, (s, a) => Interop.Methods.ScriptStartName(m_executor, (sbyte*)s, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo))) != 0; }
    }
    public bool ScriptStart(uint num, uint hubId, uint mapId, uint[] args, object threadInfo) {
        unsafe { return AdaptScriptArgs(args, a => Interop.Methods.ScriptStartNum(m_executor, num, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo)) != 0); }
    }
    public bool ScriptStartForced(string name, uint hubId, uint mapId, uint[] args, object threadInfo) {
        unsafe { return AdaptScriptNameAndArgs(name, args, (s, a) => Interop.Methods.ScriptStartForcedName(m_executor, (sbyte*)s, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo))) != 0; }
    }
    public bool ScriptStartForced(uint num, uint hubId, uint mapId, uint[] args, object threadInfo) {
        unsafe { return AdaptScriptArgs(args, a => Interop.Methods.ScriptStartForcedNum(m_executor, num, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo)) != 0); }
    }
    public uint ScriptStartResult(string name, uint[] args, object threadInfo) {
        unsafe { return AdaptScriptNameAndArgs(name, args, (s, a) => Interop.Methods.ScriptStartResultName(m_executor, (sbyte*)s, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo))); }
    }
    public uint ScriptStartResult(uint num, uint[] args, object threadInfo) {
        unsafe { return AdaptScriptArgs(args, a => Interop.Methods.ScriptStartResultNum(m_executor, num, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo))); }
    }
    public bool ScriptStop(string name, uint hubId, uint mapId) {
        unsafe { return AdaptScriptName(name, s => Interop.Methods.ScriptStopName(m_executor, (sbyte*)s, hubId, mapId)) != 0; }
    }
    public bool ScriptStop(uint num, uint hubId, uint mapId) {
        unsafe { return Interop.Methods.ScriptStopNum(m_executor, num, hubId, mapId) != 0; }
    }
    public bool ScriptPause(string name, uint hubId, uint mapId) {
        unsafe { return AdaptScriptName(name, s => Interop.Methods.ScriptPauseName(m_executor, (sbyte*)s, hubId, mapId)) != 0; }
    }
    public bool ScriptPause(uint num, uint hubId, uint mapId) {
        unsafe { return Interop.Methods.ScriptPauseNum(m_executor, num, hubId, mapId) != 0; }
    }

    public bool HasActiveThread() {
        unsafe {
            return Interop.Methods.HasActiveThread(m_executor) != 0;
        }
    }
    public void Exec() {
        unsafe {
            Interop.Methods.Exec(m_executor);
        }
    }
}
