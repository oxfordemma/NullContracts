using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class MethodFlowTests : TestBase
    {
        public MethodFlowTests() : base()
        {
        }

        // TODO: tests for mixed binary expressions (both || and &&)

        [Test]
        public void OnlyExecuteIfNotNull()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        if (id != null)
        {
            M(id);
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Setter_OnlyExecuteIfNotNull()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public string Something
    {
        set
        {
            M(value);
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void Getter_Error()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public string Something
    {
        get
        {
            M(Id);
            return """";
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void ReturnEarlyIfNull()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        if (id == null)
        {
            return;
        }
        M(id);
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ThrowAwayCondition_NullAndSomethingElse()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        if (id == null && false)
        {
            return;
        }
        M(id);
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void ThrowAwayCondition_NotNullOrSomethingElse()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        if (id != null || true)
        {
            M(id);
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InTryCatch()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        try 
        {
            if (id != null)
            {
                M(id);
            }
        }
        catch
        {
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InCatchBlock()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        try 
        {
        }
        catch
        {
            if (id != null)
            {
                M(id);
            }
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InFinallyBlock()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        try 
        {
        }
        finally
        {
            if (id != null)
            {
                M(id);
            }
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ReturnEarlyIfNull_ConditionalAccess()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public C(Item item)
    {
        if (item?.Id == null)
        {
            return;
        }
        M(item);
    }

    private void M([NotNull]Item item)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ReturnEarlyIfNull_DoubleConditionalAccess()
        {
            var code =
@"
public class Item
{
    public Info Info { get; set; }
}

public class Info
{
    public string Something { get; set; }
}

public class C
{
    public C(Item item)
    {
        if (item?.Info?.Something == null)
        {
            return;
        }
        M(item.Info);
    }

    private void M([NotNull]Info info)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void PreconditionIsActuallyPostCondition_Fail()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        M(id);
        if (id == null)
        {
            return;
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void OneWithPrecondition_OneWithout()
        {
            var code =
@"
public class Folder
{
    public string Id { get; set; }
}

public class C
{
    private void FixPaths(Folder originalFolder, Folder newFolder)
    {
        if (originalFolder.Id == null)
        {
            return;
        }
        if (IsSomething(newFolder.Id))
        {
        }
        else if (IsSomething(originalFolder.Id))
        {
        }
    }

    private bool IsSomething([NotNull]string id)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void DoublePrecondition()
        {
            var code =
@"
public class Folder
{
    public string Id { get; set; }
}

public class C
{
    private void FixPaths(Folder originalFolder, Folder newFolder)
    {
        if (originalFolder.Id == null || newFolder.Id == null)
        {
            return;
        }
        if (IsSomething(newFolder.Id))
        {
        }
        else if (!IsSomething(originalFolder.Id))
        {
        }
    }

    private bool IsSomething([NotNull]string id)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void IfElsePrecondition()
        {
            var code =
@"
public class C
{
    public string Item { get; set; }

    public void Load()
    {
        if (Item == null)
        {
            DoSomething("""");
        }
        else
        {
            SomeUnrelatedMethod();
            DoSomething(Item);
        }
    }

    private void SomeUnrelatedMethod()
    {
        Item = null; // trickster code - unrealistic for the analyzer to catch this
    }

    public void DoSomething([NotNull]string item)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NestedMethodCall()
        {
            var code =
@"
public class Download
{
    public Download([NotNull] string id)
    {
    }
}

public class Item
{
    public static void Method(string id)
    {
        if (id != null)
        {
            IsSomething(new Download(id));
        }
    }

    public static void IsSomething(Download download)
    {
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void ShortCircuitInLambda()
        {
            var code =
@"
public class Folder
{
    public Info Info { get; set; }
}

public class Info { }

public class Item
{
    public static void Method()
    {
        var folders = new Folder[0];
        var folder = folders.FirstOrDefault(i => i.Info != null && IsSomething(i.Info));
    }

    public static bool IsSomething([NotNull] Info info)
    {
        return true;
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void ShortCircuitInNestedLambda()
        {
            var code =
@"
public class Folder
{
    public Info Info { get; set; }
}

public class Info { }

public class Item
{
    public static void Method()
    {
        Func<Folder> func = new Func<Folder>(() =>
        {
            var folders = new Folder[0];
            return folders.FirstOrDefault(i => i.Info != null && IsSomething(i.Info) == true);
        });
    }

    public static bool IsSomething([NotNull] Info info)
    {
        return true;
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void ShortCircuitIsCondition_Not_WithSameAs()
        {
            var code =
@"
public class Folder
{
}

public class Item
{
    public static bool Method(object folder)
    {
        return !(folder is Folder) || IsSomething(folder as Folder);
    }

    public static bool IsSomething([NotNull] Folder folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void Foeach_ContinueIfNotNull()
        {
            var code =
@"
public class Folder
{
    public string Url { get; set; }
}

public class Item
{
    public static bool Method(Folder[] folders)
    {
        foreach (var folder in folders)
        {
            var uri = folder.Url;
            if (uri == null)
            {
                continue;
            }
            IsSomething(uri);
        }
        return false;
    }

    public static bool IsSomething([NotNull] string uri)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void ConditionalReturn()
        {
            var code =
@"
public class Folder
{
    public string Url { get; set; }
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder)
    {
        return folder.Url == null ? false : IsSomething(folder.Url);
    }

    public static bool IsSomething([NotNull] string uri)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void ConditionalInLocal()
        {
            var code =
@"
public class Folder
{
    public string Url { get; set; }
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder)
    {
        bool x;
        x = folder.Url == null ? false : IsSomething(folder.Url);

        return x;
    }

    public static bool IsSomething([NotNull] string uri)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void While_AssignmentInCondition()
        {
            var code =
@"
public class Item
{
    public static void Method()
    {
        string folder;
        while ((folder = Next()) != null)
        {
            IsSomething(folder);
        }
    }

    public static bool IsSomething([NotNull] string folder)
    {
        return true;
    }

    public static string Next()
    {
        return null;
    }
}
";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void DoWhile()
        {
            var code =
@"
public class Item
{
    public static void Method(string folder)
    {
        do
        {
            if (folder != null)
            {
                IsSomething(folder);
            }
        } while (true);
    }

    public static bool IsSomething([NotNull] string folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void While_AssignmentInCondition_Fails()
        {
            var code =
@"
public class Item
{
    public static void Method()
    {
        string folder;
        while ((folder = Next()) == null)
        {
            IsSomething(folder);
        }
    }

    public static bool IsSomething([NotNull] string folder)
    {
        return true;
    }

    public static string Next()
    {
        return null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void While_ReassignedInLoopAfterMethodCall()
        {
            var code =
@"
public class Item
{
    public static void Method()
    {
        string folder = Next();
        while (folder != null)
        {
            IsSomething(folder);
            folder = Next();
        }
    }

    public static bool IsSomething([NotNull] string folder)
    {
        return true;
    }

    public static string Next()
    {
        return null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void While_ReassignedInLoopBeforeMethodCall()
        {
            var code =
@"
public class Item
{
    public static void Method()
    {
        string folder = Next();
        while (folder != null)
        {
            folder = Next();
            IsSomething(folder);
        }
    }

    public static bool IsSomething([NotNull] string folder)
    {
        return true;
    }

    public static string Next()
    {
        return null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.AssignmentAfterConditionId);
        }

        [Test]
        public void AnalyzeSetter_Success()
        {
            var code =
@"
public class Item
{
    public string id;

    public string Id
    {
        get
        {
            return id;
        }
        set
        {	if (Set(() => Id, ref this.id, value))
            {
                if (value == null)
                {
                    id = null;
                }
                else
                {
                    M(value);
                }
            }
        }
    }

    private bool Set(Func<string> property, ref string old, string newValue)
    {
        return true;
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AnalyzeSetter_Fails()
        {
            var code =
@"
public class Item
{
    public string id;

    public string Id
    {
        get
        {
            return id;
        }
        set
        {	if (Set(() => Id, ref this.id, value))
            {
                if (value == null)
                {
                    id = null;
                }
                M(value);
            }
        }
    }

    private bool Set(Func<string> property, ref string old, string newValue)
    {
        return true;
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void VariableDeclarator()
        {
            var code =
@"
public class Item
{
    public static void Method(bool x)
    {
        if (x)
        {
            var s = """";
            DoSomething(s);
        }
    }

    public static void DoSomething([NotNull] string s)
    {
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }

        [Test]
        [Ignore("Not supported")]
        public void If_ElseReturn()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public void Method(string s)
    {
        string temp;
        if (s != null)
        {
            temp = s;
        }
        else if (Id == """")
        {
            temp = """";
        }
        else
        {
            return;
        }
        DoSomething(temp);
    }

    public static void DoSomething([NotNull] string s)
    {
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }

        [Test]
        public void IfReturn_WithUnneededElse()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public string Method(string s)
    {
        if (s == null)
        {
            return """";
        }
        else
        {
            if (DoSomething(s)) 
            {
            }

            return """"; 
        }
        return """";
    }

    public static bool DoSomething([NotNull] string s)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }

        [Test]
        public void Lambda_CheckConditionsInOuterBody()
        {
            var code =
@"
public class Item
{
    public bool Method(string s)
    {
        if (s == null)
        {
            return false;
        }

        var temp = new string[0];

        return temp.All(x => DoSomething(s));
    }

    public static bool DoSomething([NotNull] string s)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }

        [Test]
        public void DoubleCast_Success()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public C(Item item)
    {
        if (item.Id != null)
        {
            M((string)(object)item.Id);
        }
    }

    private void M([NotNull]string id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
