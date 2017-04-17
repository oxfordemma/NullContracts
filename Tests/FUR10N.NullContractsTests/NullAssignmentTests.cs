using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class NullAssignmentTests : TestBase
    {
        public NullAssignmentTests()
            : base(MainAnalyzer.NullAssignmentId, MainAnalyzer.PropagateNotNullInCtorsId, MainAnalyzer.ReturnNullId, MainAnalyzer.NotNullAsRefParameterId)
        {
        }

        [Test]
        public void SimpleAssignment()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Link { get; set; } = """";

    public string Id { get; set; }
}

public class C
{
  public void M()
  {
    var item = new Item();
    item.Link = null;
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void AssignToNewCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Link { get; set; } = """";

    public string Id { get; set; }
}

public class C
{
  [NotNull]
  public readonly Item Item;

  public C()
  {
    Item = new Item();
  }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignNotNullMemberToCheckNullValue()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([CheckNull]string id)
    {
        this.Id = id;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignNotNullMemberToNotNullValue()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignNotNullMemberToNestedNotNullValue()
        {
            var code =
@"
public class Link
{
    [NotNull]
    public string Url { get; } = ""url"";
}

[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]Link link)
    {
        this.Id = link.Url;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NullCoalescingOperator_IsNotNull()
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

    public string First = ""first"";

    [NotNull]
    public string Last = ""last"";
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void PassedNullToCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }

    public static void M()
    {
        var item = new Item(null);
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void PassedMultipleNullsToCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id, string first, [NotNull]string last)
    {
        this.Id = id;
    }

    public static void M()
    {
        var item = new Item(null, null, null);
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void PassedPotentialNullToChainedCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }

    public Item(string id, bool x) : this(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.PropagateNotNullInCtorsId);
        }

        [Test]
        public void PassedNotNullToNotNullArgInBaseCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }
}

public class File : Item
{
    public File([NotNull]string id) : base(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void PassedPotentialNullToNotNullArgInBaseCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }
}

public class File : Item
{
    public File(string id) : base(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.PropagateNotNullInCtorsId);
        }

        [Test]
        public void PassLocalVariableThatIsNotAlwaysAssigned()
        {
            var code =
@"
public enum SomeEnum { Value1, Value2 }

public class C
{
    [NotNull]
    public string Provider { get; } = """";

    public C(SomeEnum e)
    {
        string provider = null;
        switch (e)
        {
            case SomeEnum.Value1:
                provider = ""value1"";
                break;
        }

        this.Provider = provider;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void PassLocalVariableThatIsNotAlwaysAssigned2()
        {
            var code =
@"
public enum SomeEnum { Value1, Value2 }

public class C
{
    [NotNull]
    public string Provider { get; } = """";

    public C(SomeEnum e)
    {
        string provider;
        switch (e)
        {
            case SomeEnum.Value1:
                provider = ""value1"";
                break;
        }

        this.Provider = provider;
    }
}
";
            var d = GetDiagnostics(code, true);
            AssertIssues(d);
        }

        [Test]
        public void PassLocalVariableThatIsAlwaysAssigned()
        {
            var code =
@"
public enum SomeEnum { Value1, Value2 }

public class C
{
    [NotNull]
    public string Provider { get; } = """";

    public C(SomeEnum e)
    {
        string provider;
        switch (e)
        {
            case SomeEnum.Value1:
                provider = ""value1"";
                break;
            case SomeEnum.Value2:
            default:
                provider = ""default"";
                break;
        }

        this.Provider = provider;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void PassLocalVariableThatIsAlwaysAssigned2()
        {
            var code =
@"
public enum SomeEnum { Value1, Value2 }

public class C
{
    [NotNull]
    public string Provider { get; } = """";

    public C(SomeEnum e)
    {
        string provider = """";
        switch (e)
        {
            case SomeEnum.Value1:
                provider = ""value1"";
                break;
        }

        this.Provider = provider;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void PassLocalVariableThatIsAlwaysAssigned3()
        {
            var code =
@"
public enum SomeEnum { Value1, Value2 }

public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }
}

public class C
{
    public void M(SomeEnum e)
    {
        string provider;
        switch (e)
        {
            case SomeEnum.Value1:
                provider = ""value1"";
                break;
            case SomeEnum.Value2:
            default:
                provider = ""default"";
                break;
        }

        new Item(provider);
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NullCoalescingExpressionThatIsAlwaysNotNull()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }
}

public class C
{
    [NotNull]
    public string Provider { get; }

    public C(string provider)
    {
        this.Provider = provider ?? ""sf"";
        new Item(provider ?? ""sf"");
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void StringConcatIsNeverNull()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
        this.Id = id;
    }
}

public class C
{
    [NotNull]
    public string Provider { get; }

    public C(string provider)
    {
        this.Provider = provider + ""sf"";
        new Item(provider + ""sf"");
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void SetToMethodThatReturnsValueType()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item()
    {
        this.Id = M();
    }

    private int M()
    {
        return 0;
    }
}
";

            var d = GetDiagnostics(code, true);
            AssertIssues(d);
        }

        [Test]
        public void SetToNullCoalescWithCtorCall()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly object Id;

    public Item(object id)
    {
        this.Id = id ?? new object();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void SetToLambda()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly Action<string> Id;

    public Item(string id)
    {
        this.Id = () => id;
    }
}
";

            var d = GetDiagnostics(code, true);
            AssertIssues(d);
        }

        [Test]
        public void ReassignANotNullParameter_ToNull()
        {
            var code =
@"
public class Item
{
    public void M([NotNull]string id)
    {
        id = null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void ReassignANotNullParameter_ToNotNull()
        {
            var code =
@"
public class Item
{
    public void M([NotNull]string id)
    {
        id = """";
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ParameterFromRegularMethodArgToNotNullMethodArg()
        {
            var code =
@"
public class C
{
    public void M(string id)
    {
        M2(id);
    }

    public void M2([NotNull]string id)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void ConsiderArrayAccessToBeNotNull()
        {
            var code =
@"
public class C
{
    [NotNull]public string Id { get; }

    public C(string[] ids)
    {
        Id = ids[0];
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ConsiderIterationVariableToBeNotNull()
        {
            var code =
@"
public class C
{
    [NotNull]public string Id { get; }

    public C(string[] ids)
    {
        foreach (var i in ids)
        {
            Id = i;
            break;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ConsiderLinqIteratorToBeNotNull()
        {
            var code =
@"
public class C
{
    public C(string[] ids)
    {
        ids.Any(i => Method(i));
    }

    public bool Method([NotNull] string id) { return true; }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void MethodGroup()
        {
            var code =
@"
public class C
{
    public C(string[] ids)
    {
        ids.Any(Method);
    }

    public bool Method([NotNull] string id) { return true; }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignToArrayInitializer()
        {
            var code =
@"
public class C
{
    public C()
    {
        Method(new[] { """"} );
    }

    public void Method([NotNull] string[] ids)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignToInitializedLocalArray()
        {
            var code =
@"
public class C
{
    public C()
    {
        var ids = new string[0];
        Method(ids);
    }

    public void Method([NotNull] string[] ids)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignToInitializedLocalObjectInBranch()
        {
            var code =
@"
public class Item
{
}

public class C
{
    public C(bool x)
    {
        if (x)
        {
            var item = new Item();
            Method(item);
        }
    }

    public void Method([NotNull] Item item)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NotNullIsOnSetter_CannotUseGetter()
        {
            var code =
@"
public class Item
{
    public string Id
    {
        get
        {
            return null;
        }
        [NotNull]
        set
        {
        }
    }

    public Item()
    {
        Method(Id);
    }

    public void Method([NotNull] string id)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void StringSubstringIsAlwaysNotNull()
        {
            var code =
@"
public class C
{
    public C(string s)
    {
        Method(s.Substring(0, 1));
    }

    public void Method([NotNull] string s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void DictionaryValuesIsAlwaysNotNull()
        {
            var code =
@"
public class C
{
    public C(string s)
    {
        var d = new System.Collections.Generic.Dictionary<string, string>();
        Method(d.Values);
        Method(d.Keys);
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void UriToString_NotNull()
        {
            var code =
@"
public class C
{
    public C()
    {
        var uri = new Uri(""/"").ToString();
        Method(uri);
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void GuidToString_NotNull()
        {
            var code =
@"
public class C
{
    public C()
    {
        var uri = Guid.NewGuid().ToString();
        Method(uri);
        var uri2 = Guid.NewGuid().ToString(""D"");
        Method(uri2);
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ToList_NotNull()
        {
            var code =
@"
public class C
{
    public C()
    {
        IEnumerable<string> list = new List<string>();
        Method(list.ToList());
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ToArray_NotNull()
        {
            var code =
@"
public class C
{
    public C()
    {
        IEnumerable<string> list = new List<string>();
        Method(list.ToArray());
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Where_NotNull()
        {
            var code =
@"
public class C
{
    public C()
    {
        IEnumerable<string> list = new List<string>();
        Method(list.Where(i => i != null));
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Select_NotNull()
        {
            var code =
@"
public class C
{
    public C()
    {
        IEnumerable<string> list = new List<string>();
        Method(list.Select(i => i.Length));
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void FallbackToThisKeyword()
        {
            var code =
@"
public class C
{
    public void Load()
    {
        IEnumerable<string> list = null;
        Method(list ?? (object)this);
    }

    public void Method([NotNull] object s)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void PathGetTempPath_NotNull()
        {
            var code =
@"
public class C
{
    [NotNull]
    public readonly string Path;

    public C()
    {
        Path = System.IO.Path.GetTempPath();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void MarshalGetObjectForIUnknown_NotNull()
        {
            var code =
@"
public class C
{
    [NotNull]
    public object Method()
    {
        System.IntPtr obj = new System.IntPtr();
        return System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(obj);
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignToConditional_BothBranchesProtected()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item(bool c, [NotNull] string first, [NotNull] string last)
    {
        Id = c ? first : last;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void AssignToConditional_TrueBranchProtected()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item(bool c, [NotNull] string first, string last)
    {
        Id = c ? first : last;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void AssignToConditional_TrueBranchProtected2()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item(bool c, [NotNull] string first)
    {
        Id = c ? first : null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void AssignToConditional_FalseBranchProtected()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item(bool c, string first, [NotNull] string last)
    {
        Id = c ? first : last;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void AssignToConditional_NeitherBranchProtected()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item(bool c, string first, string last)
    {
        Id = c ? first : last;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void AssignToNullInRefMethod()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item()
    {
        Method(ref Id);
    }

    private void Method(ref string id)
    {
        id = null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NotNullAsRefParameterId);
        }

        [Test]
        public void NotNullExtensionMethod()
        {
            var code =
@"
public static class Extensions
{
    public static void Method([NotNull] this Item item, string id)
    {
    }
}

public class Item
{
    public string Id { get; set; }
}

public class Main
{
    public Item Item { get; set; }
    
    public void Load()
    {
        Item.Method(""Id"");
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }
    }
}
