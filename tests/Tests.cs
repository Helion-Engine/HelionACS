using Xunit;
using System.IO;
using System.Collections.Generic;

namespace Tests;

class MyExecutor : HelionACS.Executor {
    public MyExecutor() {
        AddCodeDataACS0(61, "",  1, CF_TagWait);
        AddCodeDataACS0(62, "W", 0, CF_TagWait);
        AddCodeDataACS0(86, "",  0, CF_EndPrint);
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

    public bool CF_EndPrint(HelionACS.ThreadHandle thread, uint[] args) {
        var _ = thread.GetThreadInfo();
        printBufferOutput.Add(thread.GetPrintBuf());
        return false;
    }
    public bool CF_TagWait(HelionACS.ThreadHandle thread, uint[] args) {
        Assert.Equal(10u, args[0]);
        thread.MakeTagWait(0, args[0]);
        return true;
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
        executor.ScriptStartType(HelionACS.ScriptType.Open, [], new object());

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
        executor.ScriptStartType(HelionACS.ScriptType.Open, [], new object());
    }

    [Fact]
    public void TestRunSpecificScript()
    {
        Assert.True(executor.ScriptStart(2, 0, 0, [], new object()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2"], executor.printBufferOutput);

        Assert.True(executor.ScriptStart("Named", 0, 0, [], new object()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2", "Hello from a named script"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunScriptsForced()
    {
        Assert.True(executor.ScriptStartForced(2, 0, 0, [], new object()));
        Assert.True(executor.ScriptStartForced("Named", 0, 0, [], new object()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2", "Hello from a named script"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunNonExistentScriptReturnsFalse()
    {
        Assert.False(executor.ScriptStartForced(12345, 0, 0, [], new object()));
    }

    [Fact]
    public void TestRunScriptWithResult()
    {
        Assert.Equal(500u, executor.ScriptStartResult("ReturnsResult", [], new object()));
    }

    [Fact]
    public void TestRunScriptUsingALineSpecial()
    {
        Assert.True(executor.ScriptStart("UsesALineSpecial", 0, 0, [], new object()));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal([11], executor.ranLineSpecials);
        Assert.Equal([[10, 32, 0, 0, 0]], executor.ranLineSpecialArgs);
        Assert.Equal(["Return value: 214"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestTagWait()
    {
        Assert.True(executor.ScriptStart("UsesTagWait", 0, 0, [], new object()));
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
}
