
using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class UnneededNullCheckTests : TestBase
    {
        public UnneededNullCheckTests() : base(MainAnalyzer.UnneededNullCheckId)
        {
        }

        [Test]
        public void Conditional()
        {
            var code =
@"
public class C
{
  [NotNull]public string X { get; }

  public void M()
  {
    var y = X == null ? 0 : X.Length;
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void Conditional2()
        {
            var code =
@"
public class C
{
  public void M([NotNull] string str)
  {
    var y = str == null ? 0 : str.Length;
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void IfStatement()
        {
            var code =
@"
public class C
{
  [NotNull]public string X { get; }

  public void M()
  {
    if (X == null)
    {
        var y = 0;
    }
    else
    {
        var y = X.Length;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void BinaryStatement()
        {
            var code =
@"
public class C
{
  [NotNull]public string X { get; }

  public void M()
  {
    var check = (X == null);
    if (check)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NestedProperty()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Id { get; } = """";
}

public class C
{
  public void M()
  {
    var item = new Item();
    if (item.Id == null)
    {
        var y = 0;
    }
    else
    {
        var y = item.Id.Length;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NestedProperty2()
        {
            var code =
@"
public class Item
{
    [NotNull]public Link Link { get; }
}

public class Link
{
    [NotNull]public object Url { get; }
}

public class C
{
  public object M()
  {
    var item = new Item();
    if (item.Link == null)
    {
        return 0;
    }
    else
    {
        return item.Link.Url != null ? 0 : 1;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void ElvisOperator()
        {
            var code =
@"
public class Item
{
    [NotNull]public Link Link { get; }
}

public class Link
{
    [NotNull]public object Url { get; }
}

public class C
{
  public object M()
  {
    var item = new Item();
    if (item.Link?.Url == null)
    {
        return 0;
    }
    else
    {
        return 1;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void MethodIsNull()
        {
            var code =
@"
public class Item
{
    [NotNull]public Link GetLink() { return null; }
}

public class Link
{
    [NotNull]public object Url { get; }
}

public class C
{
  public object M()
  {
    var item = new Item();
    if (item.GetLink() == null)
    {
        return 0;
    }
    else
    {
        return 1;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void AssignmentInIf()
        {
            var code =
@"
public class Item
{
    [NotNull]public Link GetLink() { return null; }
}

public class Link
{
    [NotNull]public object Url { get; }
}

public class C
{
  public object M()
  {
    var item = new Item();
    Link temp;
    if ((temp = item.GetLink()) == null)
    {
        return 0;
    }
    else
    {
        return 1;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void CheckMethodArgument()
        {
            var code =
@"
public class C
{
  public object M([NotNull]string arg, string arg2, string arg3)
  {
    if (arg == null || arg2 == null || arg3 == null)
    {
        return 0;
    }
    else
    {
        return 1;
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void AsCheck()
        {
            var code =
@"
public class C
{
  [NotNull]public string X { get; }

  public void M()
  {
    if ((X as object) != null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ArrayIndexCheck()
        {
            var code =
@"
public class C
{
  [NotNull]public string[] X { get; }

  public void M()
  {
    if (X[0] == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ArrayCheck()
        {
            var code =
@"
public class C
{
  [NotNull]public string[] X { get; } = new string[0];

  public void M()
  {
    if (X == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void AwaitCheck()
        {
            var code =
@"
public class C
{
  [NotNull]
  public Task<object> DoAwait()
  {
    return null;
  }

  public async void M()
  {
    if (await DoAwait() == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code, true);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void CtorCheck()
        {
            var code =
@"
public class Item
{
}

public class C
{
  public void M()
  {
    if (new Item() == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void CtorAndMemberAccess()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Link;
}

public class C
{
  public void M()
  {
    if (new Item().Link == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void ElvisInProperty()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Link;

    public int? Length => Link?.Length;
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void DoubleElvisCheck()
        {
            var code =
@"
public class Item
{
    [NotNull]public Link Link { get; }
}

public class Link
{
    [NotNull]public object Url { get; }
}

public class C
{
  public void M()
  {
    var item = new Item();
    if (item?.Link?.Url == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void DoubleElvisMethod()
        {
            var code =
@"
public class Item
{
    [NotNull]public Link Link { get; }
}

public class Link
{
    [NotNull]public object Url() { return null; }
}

public class C
{
  public void M()
  {
    var item = new Item();
    if (item?.Link?.Url() == null)
    {
        this.GetType();
    }
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NullCoalescingOperatorOnNotNullField()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item()
    {
        Id = First ?? Last;
    }

    [NotNull]
    public string First = ""first"";

    [NotNull]
    public string Last = ""last"";
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NotNullParam_GetsOverwritten_IgnoreError()
        {
            var code =
@"
public class Item
{
    public void M([NotNull] string id)
    {
        id = null;
        if (id != null)
        {
            int x = id.Length;
        }
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NullCoalesce_MemberAccess()
        {
            var code =
@"
public class Link
{
    [NotNull]
    public string Id = """";
}

public class Item
{
    public object Id { get; private set; }

    public Item(Link link)
    {
        Id = link.Id ?? ""id"";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NullCoalesce_WithAs()
        {
            var code =
@"
public class Link
{
    [NotNull]
    public string Id = """";
}

public class Item
{
    public object Id { get; private set; }

    public Item(Link link)
    {
        Id = link.Id as object ?? ""id"";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NullCoalesceWithConditional()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public string Id = """";

    [NotNull]
    public string Name = """";

    public string Last;

    public void Load(bool c)
    {
        var x = (c ? Id : Name) ?? Last;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void AllowNullCheckIfThrowingAfterward()
        {
            var code =
@"
public class Item
{

    public void Load([NotNull] string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException();
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void DontAllowNullCheckIfThrowingAfterward()
        {
            var code =
@"
public class Item
{
    [NotNull] public readonly string Id = """";

    public void Load()
    {
        if (Id == null)
        {
            throw new ArgumentNullException();
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void NullCheckOnArgument()
        {
            var code =
@"
public class Item
{

    public void Load([NotNull] string s)
    {
        if (s == null)
        {
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }
    }
}
