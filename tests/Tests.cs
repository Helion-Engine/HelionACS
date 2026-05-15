using Xunit;
using System.IO;
using System.Collections.Generic;

namespace Tests;

class TestThreadInfo {
    public readonly int x = 512;
}

class MyExecutor : HelionACS.Executor {
    public MyExecutor() {
        AddCodeDataACS0(57, "",   2, CF_Random);
        AddCodeDataACS0(58, "WW", 0, CF_Random);
        AddCodeDataACS0(61, "",   1, CF_TagWait);
        AddCodeDataACS0(62, "W",  0, CF_TagWait);
        AddCodeDataACS0(86, "",   0, CF_EndPrint);

        AddFuncDataACS0(9, CF_GetActorVelX);
    }

    public override byte[] LoadModule(string moduleName) {
        Assert.Equal("module", moduleName);

        var data = File.ReadAllBytes("test.acs.o");
        return data;
    }
    public override uint CallSpecImpl(HelionACS.ThreadHandle thread, uint spec, uint[] args) {
        ranLineSpecials.Add(spec);
        ranLineSpecialArgs.Add(args);
        return 214; // no special meaning, randomly chosen sentinel value
    }
    public bool ShouldTagWait = false;
    public override bool CheckTag(uint type, uint tag) {
        Assert.Equal(10u, tag);
        return !ShouldTagWait;
    }

    public readonly List<string> printBufferOutput = [];
    public List<uint> ranLineSpecials = [];
    public List<uint[]> ranLineSpecialArgs = [];

    public bool CF_Random(HelionACS.ThreadHandle thread, uint[] args) {
        var min = (int)args[0];
        var max = (int)args[1];
        thread.PushStack(
            (uint)((min, max) switch {
                (0, 7) => 5,
                (4, 15) => 8,
                (-5, 12) => -2,
                (_, _) => 0,
            })
        );
        return false;
    }
    public bool CF_EndPrint(HelionACS.ThreadHandle thread, uint[] args) {
        var threadInfo = thread.GetThreadInfo() as TestThreadInfo;
        Assert.Equal(512, threadInfo.x);
        printBufferOutput.Add(thread.GetPrintBuf());
        return false;
    }
    public bool CF_TagWait(HelionACS.ThreadHandle thread, uint[] args) {
        Assert.Equal(10u, args[0]);
        thread.MakeTagWait(0, args[0]);
        return true;
    }
    public bool CF_GetActorVelX(HelionACS.ThreadHandle thread, uint[] args) {
        if (args[0] == 5) {
            thread.PushStack((24 << 16) + (1 << 15)); // 24.5 in fixed point
        } else {
            thread.PushStack(0);
        }
        return false;
    }
}

public class ExecutorTests
{
    readonly MyExecutor executor; 
    public ExecutorTests()
    {
        executor = new MyExecutor();
        executor.LoadHubMap(0, 0, ["module"]);
    }

    [Fact]
    public void TestRunOpenScripts()
    {
        executor.ScriptStartType(HelionACS.ScriptType.Open, [], new TestThreadInfo());

        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal([], executor.printBufferOutput);

        for (var i = 0; i < 5; i++) {
            Assert.True(executor.HasActiveThread()); executor.Exec();
            Assert.Equal(["Hi"], executor.printBufferOutput);
        }

        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hi", "Hi again!", "4"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunOpenScriptsNoModule() {
        var executor = new MyExecutor();
        executor.ScriptStartType(HelionACS.ScriptType.Open, [], new TestThreadInfo());
    }

    [Fact]
    public void TestRunSpecificScript()
    {
        Assert.True(executor.ScriptStart(2, 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2"], executor.printBufferOutput);

        Assert.True(executor.ScriptStart("Named", 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2", "Hello from a named script"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunScriptsForced()
    {
        Assert.True(executor.ScriptStartForced(2, 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.ScriptStartForced("Named", 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2", "Hello from a named script"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunNonExistentScriptReturnsFalse()
    {
        Assert.False(executor.ScriptStartForced(12345, 0, 0, [], new TestThreadInfo()));
    }

    [Fact]
    public void TestRunScriptWithResult()
    {
        Assert.Equal(500u, executor.ScriptStartResult("ReturnsResult", [], new TestThreadInfo()));
    }

    [Fact]
    public void TestRunScriptUsingALineSpecial()
    {
        Assert.True(executor.ScriptStart("UsesALineSpecial", 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal([11], executor.ranLineSpecials);
        Assert.Equal([[10, 32, 0, 0, 0]], executor.ranLineSpecialArgs);
        Assert.Equal(["Return value: 214"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestTagWait()
    {
        Assert.True(executor.ScriptStart("UsesTagWait", 0, 0, [], new TestThreadInfo()));
        executor.ShouldTagWait = true;
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Pre-wait"], executor.printBufferOutput);

        for (var i = 0; i < 10; i++) {
            Assert.True(executor.HasActiveThread()); executor.Exec();
            Assert.Equal(["Pre-wait"], executor.printBufferOutput);
        }
        executor.ShouldTagWait = false;

        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Pre-wait", "Post-wait"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRandom()
    {
        Assert.True(executor.ScriptStart("UsesRandom", 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["5", "8", "-2"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestAddFunc()
    {
        Assert.True(executor.ScriptStart("UsesGetActorVelX", 0, 0, [], new TestThreadInfo()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["24.5", "0"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }
}
