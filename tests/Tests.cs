using Xunit;
using System.IO;
using System.Collections.Generic;

namespace Tests;

class MyExecutor : HelionACS.Executor {
    public MyExecutor() {
        AddCodeDataACS0I(57, "",   2, CF_Random);
        AddCodeDataACS0I(58, "WW", 0, CF_Random);
        AddCodeDataACS0(61, "",   1, CF_TagWait);
        AddCodeDataACS0(62, "W",  0, CF_TagWait);
        AddCodeDataACS0(86, "",   0, CF_EndPrint);

        AddCodeDataACS0(149, "",        6, CF_Spawn);
        AddCodeDataACS0(150, "WSWWWWW", 0, CF_Spawn);

        AddFuncDataACS0F(9, CF_GetActorVelX);
    }
    public void UseBrokenSpawn() {
        AddCodeDataACS0(149, "",        6, CF_SpawnBroken);
        AddCodeDataACS0(150, "WSWWWWW", 0, CF_SpawnBroken);
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

    public int CF_Random(HelionACS.ThreadHandle thread, uint[] args) {
        var min = (int)args[0];
        var max = (int)args[1];
        return (min, max) switch {
            (0, 7) => 5,
            (4, 15) => 8,
            (-5, 12) => -2,
            (_, _) => 0,
        };
    }
    public HelionACS.CallFuncResult CF_EndPrint(HelionACS.ThreadHandle thread, uint[] args) {
        var threadInfo = thread.GetThreadInfo();
        Assert.Equal(512, threadInfo.Activator);
        printBufferOutput.Add(thread.GetPrintBuf());
        return HelionACS.CallFuncResult.NextOp;
    }
    public HelionACS.CallFuncResult CF_TagWait(HelionACS.ThreadHandle thread, uint[] args) {
        Assert.Equal(10u, args[0]);
        thread.MakeTagWait(0, args[0]);
        return HelionACS.CallFuncResult.ReevaluateState;
    }
    public double CF_GetActorVelX(HelionACS.ThreadHandle thread, uint[] args) {
        if (args[0] == 5) {
            return 24.5;
        } else {
            return 0.0;
        }
    }
    public HelionACS.CallFuncResult CF_Spawn(HelionACS.ThreadHandle thread, uint[] args) {
        Assert.Equal("something", thread.GetString(args[0]));
        Assert.Equal(1u, args[1]);
        Assert.Equal(2u, args[2]);
        Assert.Equal(3u, args[3]);
        Assert.Equal(0u, args[4]);
        Assert.Equal(0u, args[5]);
        thread.PushStack(1);
        return HelionACS.CallFuncResult.NextOp;
    }
    public HelionACS.CallFuncResult CF_SpawnBroken(HelionACS.ThreadHandle thread, uint[] args) {
        Assert.Equal("something", thread.GetString(args[0]));
        Assert.Equal(1u, args[1]);
        Assert.Equal(2u, args[2]);
        Assert.Equal(3u, args[3]);
        Assert.Equal(0u, args[4]);
        Assert.Equal(0u, args[5]);
        // lack of stack push
        return HelionACS.CallFuncResult.ReevaluateState;
    }
}

public class ExecutorTests
{
    private static readonly HelionACS.ThreadInfoData DefaultThreadInfo = new(512);
    readonly MyExecutor executor; 
    public ExecutorTests()
    {
        executor = new MyExecutor();
        executor.LoadHubMap(0, 0, ["module"]);
    }

    [Fact]
    public void TestRunOpenScripts()
    {
        executor.ScriptStartType(HelionACS.ScriptType.Open, [], DefaultThreadInfo);

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
        executor.ScriptStartType(HelionACS.ScriptType.Open, [], DefaultThreadInfo);
    }

    [Fact]
    public void TestRunSpecificScript()
    {
        Assert.True(executor.ScriptStart(2, 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2"], executor.printBufferOutput);

        Assert.True(executor.ScriptStart("Named", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2", "Hello from a named script"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunScriptsForced()
    {
        Assert.True(executor.ScriptStartForced(2, 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.ScriptStartForced("Named", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["Hello from script 2", "Hello from a named script"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestRunNonExistentScriptReturnsFalse()
    {
        Assert.False(executor.ScriptStartForced(12345, 0, 0, [], DefaultThreadInfo));
    }

    [Fact]
    public void TestRunScriptWithResult()
    {
        Assert.Equal(500u, executor.ScriptStartResult("ReturnsResult", [], DefaultThreadInfo));
    }

    [Fact]
    public void TestRunScriptUsingALineSpecial()
    {
        Assert.True(executor.ScriptStart("UsesALineSpecial", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal([11], executor.ranLineSpecials);
        Assert.Equal([[10, 32, 0, 0, 0]], executor.ranLineSpecialArgs);
        Assert.Equal(["Return value: 214"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestTagWait()
    {
        Assert.True(executor.ScriptStart("UsesTagWait", 0, 0, [], DefaultThreadInfo));
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
        Assert.True(executor.ScriptStart("UsesRandom", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["5", "8", "-2"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestAddFunc()
    {
        Assert.True(executor.ScriptStart("UsesGetActorVelX", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["24.5", "0"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestSpawn()
    {
        Assert.True(executor.ScriptStart("UsesSpawn", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["1"], executor.printBufferOutput);

        Assert.False(executor.HasActiveThread());
    }

    [Fact]
    public void TestStackUnderflow()
    {
        var executor = new MyExecutor();
        executor.UseBrokenSpawn();
        executor.LoadHubMap(0, 0, ["module"]);

        Assert.True(executor.ScriptStart("UsesSpawn", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread());
        Assert.Throws<HelionACS.StackUnderflowException>(() => executor.Exec());
    }

    [Fact]
    public void TestHandlingOfScriptVars()
    {
        var executor = this.executor;

        executor.LoadHubMap(0, 0, ["module"]);

        Assert.True(executor.ScriptStart("ModifiesState", 0, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["0 0 0"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        executor.LoadHubMap(0, 1, ["module"]);

        Assert.True(executor.ScriptStart("ModifiesState", 0, 1, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["0 0 1"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        executor.LoadHubMap(1, 0, ["module"]);

        Assert.True(executor.ScriptStart("ModifiesState", 1, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["0 0 2"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        Assert.True(executor.ScriptStart("ModifiesState", 1, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["1 1 3"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        executor.LoadHubMap(1, 1, ["module"]);

        Assert.True(executor.ScriptStart("ModifiesState", 1, 1, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["0 2 4"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        Assert.True(executor.ScriptStart("ModifiesState", 1, 1, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["1 3 5"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        executor.LoadHubMap(2, 0, ["module"]);

        Assert.True(executor.ScriptStart("ModifiesState", 2, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["0 0 6"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();

        Assert.True(executor.ScriptStart("ModifiesState", 2, 0, [], DefaultThreadInfo));
        Assert.True(executor.HasActiveThread()); executor.Exec();
        Assert.Equal(["1 1 7"], executor.printBufferOutput);
        executor.printBufferOutput.Clear();
    }
}
