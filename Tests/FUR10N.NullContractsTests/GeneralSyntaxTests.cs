using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class GeneralSyntaxTests : TestBase
    {
        [Test]
        public void ObjectInitializer()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public C()
    {
        var item = new Item { Id = ""id"" };
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ArrayInitializer()
        {
            var code =
@"
public class Item
{
    public object Ids { get; private set; }

    public Item(string[] ids)
    {
        Ids = ids ?? new string[] { };
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ConsecutiveIfs()
        {
            var code =
@"
public class Item
{
    public bool UnlinkAccountAsync(string item)
    {
        string uri;
        if (folder.HasRemoteChildren.GetValueOrDefault() && folder.Redirection != null)
        {
            uri = folder.Redirection.Uri;
        }
        if (uri == null)
        {
            return X(item);
        }

        var c = Checked(uri);
        return c;
    }

    public bool X(string item)
    {
        return true;
    }

    public bool Checked([NotNull] string uri)
    {
        return true;
    }
}

";

            var d = GetDiagnostics(code, true);
            AssertIssues(d);
        }

        [Test]
        public void LockStatement()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        lock (id)
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
        public void AssignToNegative1()
        {
            var code =
@"
public class Item
{
    private int x;

    public Item(int? val)
    {
        x = val ?? -1;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void TupleTest()
        {
            var code =
@"
public class Item
{
    [NotNull]
    private readonly object tuple;

    public Item()
    {
        (var x, var y) = TupleMethod();
    }

    private (string x, string y) TupleMethod()
    {
        return (""x"", ""y"");
    }
}
";
            var d = GetDiagnostics(code, true);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void DefaultStructTest()
        {
            var code =
@"
public class Item
{
    [NotNull]
    private readonly object obj;

    public Item(object o)
    {
        obj = o ?? default(obj);
    }
}
";
            var d = GetDiagnostics(code, true);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }
    }
}
