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

public record struct ThreadInfoData(int Activator);

public class StackUnderflowException : Exception {}

public enum CallFuncResult
{
    NextOp,
    ReevaluateState,
}

public readonly ref struct ThreadHandle
{
    private readonly unsafe Interop.Thread* m_ptr;

    internal unsafe ThreadHandle(Interop.Thread* ptr)
    {
        m_ptr = ptr;
    }

    public unsafe void MakeTagWait(uint type, uint tag)
    {
        Interop.Methods.MakeThreadTagWait(m_ptr, type, tag);
    }

    public unsafe string GetPrintBuf()
    {
        var buf = (sbyte*)null;
        var length = (nuint)0;
        Interop.Methods.GetThreadPrintBuffer(m_ptr, &buf, &length);
        if (buf == null)
            return "";
        
        return Marshal.PtrToStringUTF8((nint)buf, (int)length);
    }

    public unsafe void AppendToPrintBuf(string str)
    {
        var byteString = Buffers.GetUtf8BufferStatic(str);
        fixed (byte* buf = &byteString[0])
        {
            Interop.Methods.AppendThreadPrintBuffer(m_ptr, (sbyte*)buf, (uint)str.Length);
        }
    }

    public unsafe ThreadInfoData GetThreadInfo()
    {
        var activator = Interop.Methods.GetThreadActivator(m_ptr);
        return new ThreadInfoData(activator);
    }

    public unsafe void PushStack(uint value)
    {
        Interop.Methods.PushThreadStack(m_ptr, value);
    }

    public unsafe uint GetStack(uint index)
    {
        return Interop.Methods.GetThreadStack(m_ptr, index);
    }

    public unsafe string GetString(uint index)
    {
        sbyte* str;
        uint length = Interop.Methods.GetString(m_ptr, index, &str);
        return Marshal.PtrToStringUTF8((nint)str, (int)length);
    }
}

public abstract class Executor
{
    const int FixedBits = 16;
    const int FixedOne = 1 << FixedBits;
    static int ToFixedPoint(double f) => (int)(f * FixedOne);
    static double FromFixedPoint(int f) => ((double)f) / FixedOne;

    public delegate CallFuncResult CallFunc(ThreadHandle threadHandle, uint[] args);
    public delegate void CallFuncV(ThreadHandle thread, uint[] args);
    public delegate int CallFuncI(ThreadHandle thread, uint[] args);
    public delegate bool CallFuncB(ThreadHandle thread, uint[] args);
    public delegate string CallFuncS(ThreadHandle thread, uint[] args);

    unsafe readonly protected Interop.Executor* m_executor;
    private readonly List<GCHandle> m_handles;

    public unsafe Executor()
    {
        m_handles = [];
        var selfHandle = AddHandle(this);
        var callbacks = new Interop.Callbacks {
            loadModuleCallback = &LoadModuleCDecl,
            callSpecImplCallback = &CallSpecImplCDecl,
            checkTagCallback = &CheckTagCDecl,
        };
        m_executor = Interop.Methods.MakeExecutor(callbacks, (void*)GCHandle.ToIntPtr(selfHandle));
    }

    ~Executor()
    {
        foreach (var handle in m_handles)
        {
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
    unsafe static uint CallSpecImplCDecl(void* context, Interop.Thread* thread, uint spec, uint* argv, uint argc)
    {
        var self = GCHandle<Executor>.FromIntPtr((nint)context).Target;

        var argsSpan = new ReadOnlySpan<uint>(argv, (int)argc);
        var args = argsSpan.ToArray();

        return self.CallSpecImpl(new ThreadHandle(thread), spec, args);
    }
    public abstract uint CallSpecImpl(ThreadHandle threadHandle, uint spec, uint[] args);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    unsafe static byte GenericCallFunc(void* funcContext, Interop.Thread* thread, uint* argv, uint argc)
    {
        var delegateCallFunc = GCHandle<CallFunc>.FromIntPtr((nint)funcContext).Target;
        var argsSpan = new ReadOnlySpan<uint>(argv, (int)argc);
        var args = argsSpan.ToArray();

        var result = delegateCallFunc.Invoke(new ThreadHandle(thread), args) switch
        {
            CallFuncResult.NextOp => (byte)0,
            CallFuncResult.ReevaluateState => (byte)1,
            _ => (byte)0,
        };
        return result;
    }

    public unsafe void AddCodeDataACS0(uint code, string args, uint stackArgC, CallFunc callFunc)
    {
        var argsBytes = Buffers.GetUtf8Buffer(args);
        var callFuncHandle = AddHandle(callFunc);

        var func = Interop.Methods.AddCallFunc(m_executor, (void*)GCHandle.ToIntPtr(callFuncHandle), &GenericCallFunc);
        fixed (byte* argsBytesPtr = argsBytes)
        {
            Interop.Methods.AddCodeDataACS0(m_executor, code, (sbyte*)argsBytesPtr, stackArgC, func);
        }
    }

    public unsafe void AddFuncDataACS0(uint code, CallFunc callFunc)
    {
        var callFuncHandle = AddHandle(callFunc);
        var func = Interop.Methods.AddCallFunc(m_executor, (void*)GCHandle.ToIntPtr(callFuncHandle), &GenericCallFunc);
        Interop.Methods.AddFuncDataACS0(m_executor, code, func);
    }

    public void AddCodeDataACS0V(uint code, string args, uint stackArgC, CallFuncV callFunc)
    {
        AddCodeDataACS0(code, args, stackArgC, (thread, args) => {
            callFunc(thread, args);
            return CallFuncResult.NextOp;
        });
    }

    public void AddFuncDataACS0V(uint code, CallFuncV callFunc)
    {
        AddFuncDataACS0(code, (thread, args) => {
            callFunc(thread, args);
            // funcs can actually never really be void, they just return 0 in ZDoom if they should be void
            thread.PushStack(0u);
            return CallFuncResult.NextOp;
        });
    }

    public void AddCodeDataACS0I(uint code, string args, uint stackArgC, CallFuncI callFunc)
    {
        AddCodeDataACS0(code, args, stackArgC, (thread, args) => {
            var ret = callFunc(thread, args);
            thread.PushStack((uint)ret);
            return CallFuncResult.NextOp;
        });
    }
    public void AddFuncDataACS0I(uint code, CallFuncI callFunc)
    {
        AddFuncDataACS0(code, (thread, args) => {
            var ret = callFunc(thread, args);
            thread.PushStack((uint)ret);
            return CallFuncResult.NextOp;
        });
    }

    public void AddCodeDataACS0B(uint code, string args, uint stackArgC, CallFuncB callFunc)
    {
        AddCodeDataACS0(code, args, stackArgC, (thread, args) => {
            var ret = callFunc(thread, args);
            thread.PushStack(ret ? 1u : 0u);
            return CallFuncResult.NextOp;
        });
    }
    public void AddFuncDataACS0B(uint code, CallFuncB callFunc)
    {
        AddFuncDataACS0(code, (thread, args) => {
            var ret = callFunc(thread, args);
            thread.PushStack(ret ? 1u : 0u);
            return CallFuncResult.NextOp;
        });
    }

    public delegate double CallFuncF(ThreadHandle thread, uint[] args);

    public void AddCodeDataACS0F(uint code, string args, uint stackArgC, CallFuncF callFunc)
    {
        AddCodeDataACS0(code, args, stackArgC, (thread, args) => {
            var ret = callFunc(thread, args);
            thread.PushStack((uint)ToFixedPoint(ret));
            return CallFuncResult.NextOp;
        });
    }

    public void AddFuncDataACS0F(uint code, CallFuncF callFunc)
    {
        AddFuncDataACS0(code, (thread, args) => {
            var ret = callFunc(thread, args);
            thread.PushStack((uint)ToFixedPoint(ret));
            return CallFuncResult.NextOp;
        });
    }

    public void AddCodeDataACS0S(uint code, string args, uint stackArgC, CallFuncS callFunc)
    {
        throw new NotImplementedException();
    }

    public void AddFuncDataACS0S(uint code, CallFuncS callFunc)
    {
        throw new NotImplementedException();
    }

    public void LoadHubMap(uint hubId, uint mapId, string[] moduleNames)
    {
        var moduleNamesC = Array.ConvertAll(
            moduleNames,
            s => {
                var bytes = Buffers.GetUtf8Buffer(s);
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

    private Interop.CSThreadInfo MakeCSThreadInfo(ThreadInfoData threadInfo)
    {
        return new Interop.CSThreadInfo
        {
            activator = threadInfo.Activator,
        };
    }

    public unsafe uint ScriptStartType(ScriptType type, uint[] args, ThreadInfoData threadInfo)
    {
        fixed (uint* argV = args)
            return Interop.Methods.ScriptStartType(m_executor, (uint)type, argV, (nuint)args.Length, MakeCSThreadInfo(threadInfo));
    }

    public unsafe uint ScriptStartTypeForced(ScriptType type, uint[] args, ThreadInfoData threadInfo)
    {
        fixed (uint* argV = args)
            return Interop.Methods.ScriptStartTypeForced(m_executor, (uint)type, argV, (nuint)args.Length, MakeCSThreadInfo(threadInfo));
    }

    unsafe private static T AdaptScriptName<T>(string name, Func<nuint, T> toCall)
    {
        var nameBytes = Buffers.GetUtf8Buffer(name);
        fixed (byte* namePtr = nameBytes)
            return toCall((nuint)namePtr);
    }

    unsafe private static T AdaptScriptArgs<T>(uint[] args, Func<nuint, T> toCall)
    {
        fixed (uint* argV = args)
            return toCall((nuint)argV);
    }

    unsafe private static T AdaptScriptNameAndArgs<T>(string name, uint[] args, Func<nuint, nuint, T> toCall)
    {
        var nameBytes = Buffers.GetUtf8Buffer(name);
        fixed (byte* namePtr = nameBytes)
            fixed (uint* argV = args)
                return toCall((nuint)namePtr, (nuint)argV);
    }

    public unsafe bool ScriptStart(string name, uint hubId, uint mapId, uint[] args, ThreadInfoData threadInfo)
    {
        return AdaptScriptNameAndArgs(name, args, (s, a) => Interop.Methods.ScriptStartName(m_executor, (sbyte*)s, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo))) != 0;
    }

    public unsafe bool ScriptStart(uint num, uint hubId, uint mapId, uint[] args, ThreadInfoData threadInfo)
    {
        return AdaptScriptArgs(args, a => Interop.Methods.ScriptStartNum(m_executor, num, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo)) != 0);
    }

    public unsafe bool ScriptStartForced(string name, uint hubId, uint mapId, uint[] args, ThreadInfoData threadInfo)
    {
        return AdaptScriptNameAndArgs(name, args, (s, a) => Interop.Methods.ScriptStartForcedName(m_executor, (sbyte*)s, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo))) != 0;
    }

    public unsafe bool ScriptStartForced(uint num, uint hubId, uint mapId, uint[] args, ThreadInfoData threadInfo)
    {
        return AdaptScriptArgs(args, a => Interop.Methods.ScriptStartForcedNum(m_executor, num, hubId, mapId, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo)) != 0);
    }

    public unsafe uint ScriptStartResult(string name, uint[] args, ThreadInfoData threadInfo)
    {
        return AdaptScriptNameAndArgs(name, args, (s, a) => Interop.Methods.ScriptStartResultName(m_executor, (sbyte*)s, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo)));
    }

    public unsafe uint ScriptStartResult(uint num, uint[] args, ThreadInfoData threadInfo)
    {
        return AdaptScriptArgs(args, a => Interop.Methods.ScriptStartResultNum(m_executor, num, (uint*)a, (nuint)args.Length, MakeCSThreadInfo(threadInfo)));
    }

    public unsafe bool ScriptStop(string name, uint hubId, uint mapId)
    {
        return AdaptScriptName(name, s => Interop.Methods.ScriptStopName(m_executor, (sbyte*)s, hubId, mapId)) != 0;
    }

    public unsafe bool ScriptStop(uint num, uint hubId, uint mapId)
    {
        return Interop.Methods.ScriptStopNum(m_executor, num, hubId, mapId) != 0;
    }

    public unsafe bool ScriptPause(string name, uint hubId, uint mapId)
    {
        return AdaptScriptName(name, s => Interop.Methods.ScriptPauseName(m_executor, (sbyte*)s, hubId, mapId)) != 0;
    }

    public unsafe bool ScriptPause(uint num, uint hubId, uint mapId)
    {
        return Interop.Methods.ScriptPauseNum(m_executor, num, hubId, mapId) != 0;
    }

    public unsafe bool HasActiveThread()
    {
        return Interop.Methods.HasActiveThread(m_executor) != 0;
    }

    public unsafe void Exec()
    {
        var error = Interop.Methods.Exec(m_executor);
        if (error == Interop.ExecError.StackUnderflow)
            throw new StackUnderflowException();
    }

    public unsafe bool SaveState(string file)
    {
        var fileBytes = Buffers.GetUtf8BufferStatic(file);
        fixed (byte* filename = &fileBytes[0])
        {
            return Interop.Methods.SaveState(m_executor, (sbyte*)filename) != 0;
        }
    }

    public unsafe bool LoadState(uint hubId, uint mapId, string file)
    {
        var fileBytes = Buffers.GetUtf8BufferStatic(file);
        fixed (byte* filename = &fileBytes[0])
        {
            return Interop.Methods.LoadState(m_executor, hubId, mapId, (sbyte*)filename) != 0;
        }
    }
}
