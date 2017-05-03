using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class CtorTests : TestBase
    {
        public CtorTests() : base(MainAnalyzer.NullAssignmentId, MainAnalyzer.MemberNotInitializedId)
        {
        }

        [Test]
        public void NoCtor()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Link { get; } = null;

    [NotNull]public string Id { get; } = ""id"";
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void UninitializedFieldNotSetInCtor()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    [NotNull]
    public readonly string Name;

    public Item()
    {
        Name = ""name"";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void UninitializedStaticField()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public static readonly string Id;

    public Item()
    {
    }

    static Item()
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void FieldNotSetInAllPaths()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    [NotNull]
    public readonly string Name;

    public Item(bool x)
    {
        if (x)
        {
            Id = ""id"";
            return;
        }
        else
        {
            Id = """";
        }
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void MultipleFieldsNotSetInAllPaths()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    [NotNull]
    public readonly string Name;

    public Item(bool x)
    {
        if (x)
        {
            Id = ""id"";
            return;
        }
        else
        {
            Name = """";
        }
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void UninitializedFieldNotSetInAllCtors()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item(bool x)
    {
        if (x)
        {
            Id = ""id"";
        }
    }

    public Item()
    {
        Id = """";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void InitializedFieldNotSetInAllCtors_Success()
        {
            var code =
@"
[NotNull]
public class Item
{
    [NotNull]
    public readonly string Id = """";

    public Item(bool x)
    {
        if (x)
        {
            Id = ""id"";
        }
    }

    public Item()
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void FieldSetInOneChainedCtor()
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

    public Item([NotNull]string id, bool x) : this(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void FieldSetInNoChainedCtor_Error()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item([NotNull]string id)
    {
    }

    public Item([NotNull]string id, bool x) : this(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void MultipleChainedCtors_Success()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public string Id { get; }

    public Item([NotNull]string id)
    {
        this.Id = id;
    }

    public Item([NotNull]string id, bool x, string y) : this(id, x)
    {
    }

    public Item([NotNull]string id, bool x) : this(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void MultipleChainedCtors_MemberNotSet()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public string Id { get; }

    public Item([NotNull]string id)
    {
    }

    public Item([NotNull]string id, bool x) : this(id)
    {
        this.Id = id;
    }

    public Item([NotNull]string id, bool x, string y) : this(id, x)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void InitializedFieldSetInNoChainedCtor_Success()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id = ""id"";

    public Item([NotNull]string id)
    {
    }

    public Item([NotNull]string id, bool x) : this(id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void BaseCtorInitializesTheField()
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
            AssertIssues(d);
        }

        [Test]
        public void NotNullExpressionBodyProperty_Success()
        {
            var code =
@"
public class RenameCompleteMessage
{
    public RenameCompleteMessage([CheckNull]string newItem)
    {
        this.NewItem = newItem;
    }

    [NotNull]
    public string NewItem { get; }

    [NotNull]
    public string Item => NewItem;
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NotNullComputedProperty_Success()
        {
            var code =
@"
public class RenameCompleteMessage
{
    public RenameCompleteMessage([CheckNull]string newItem)
    {
        this.NewItem = newItem;
    }

    [NotNull]
    public string NewItem { get; }

    [NotNull]
    public string Item { get { return NewItem; } }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InitializedInAll_WithNestedClass()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id;

    public Item()
    {
        Id = """";
    }

    public class Info
    {
        [NotNull] public readonly string Key;

        public Info()
        {
            Key = ""key"";
        }
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void StaticCtor_InstanceFieldSetInAllCtors()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public string Id { get; }

    public Item([NotNull]string id)
    {
        this.Id = id;
    }

    static Item()
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void StaticCtor_StaticFieldNotSet()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public static string StaticId { get; }

    [NotNull]
    public string InstanceId { get; }

    public Item([NotNull]string id)
    {
        this.InstanceId = id;
    }

    static Item()
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MemberNotInitializedId);
        }

        [Test]
        public void StaticFieldAndInstanceFieldSet()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public static string StaticId { get; }

    [NotNull]
    public string InstanceId { get; }

    public Item()
    {
        this.InstanceId = ""instance"";
    }

    static Item()
    {
        StaticId = ""static"";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
