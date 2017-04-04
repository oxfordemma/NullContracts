using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class MissingAttributeTests : TestBase
    {
        public MissingAttributeTests() : base(MainAnalyzer.MissingAttributeId)
        {
        }

        [Test]
        public void Property_MissingAttributeFromInterface()
        {
            var code =
@"
public interface IItem
{
    [NotNull]
    string Id { get; }
}

public class Item : IItem
{
    public string Id { get; } = """";
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MissingAttributeId);
        }

        [Test]
        public void Property_HasAttributeFromInterface()
        {
            var code =
@"
public interface IItem
{
    [NotNull]
    string Id { get; }
}

public class Item : IItem
{
    [NotNull]
    public string Id { get; } = """";
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Method_MissingAttributeFromInterface()
        {
            var code =
@"
public interface IItem
{
    [NotNull]
    string GetId();
}

public class Item : IItem
{
    public string GetId() { return """"; }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MissingAttributeId);
        }

        [Test]
        public void Method_HasAttributeFromInterface()
        {
            var code =
@"
public interface IItem
{
    [NotNull]
    string GetId();
}

public class Item : IItem
{
    [NotNull]
    public string GetId() { return """"; }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Property_MissingAttributeFromBaseClass()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public virtual string Id { get; } = """";
}

public class File : Item
{
    public override string Id { get { return """"; } }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MissingAttributeId);
        }

        [Test]
        public void Property_MissingGetAttributeFromBaseClass()
        {
            var code =
@"
public class Item
{
    
    public virtual string Id { [NotNull] get; } = """";
}

public class File : Item
{
    public override string Id { get { return """"; } }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MissingAttributeId);
        }

        [Test]
        public void Property_HasAttributeFromBaseClass()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public virtual string Id { get; } = """";
}

public class File : Item
{
    [NotNull]
    public override string Id { get { return """"; } }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Method_MissingAttributeFromBaseClass()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public virtual string GetId()
    {
        return """";
    }
}

public class File : Item
{
    public override string GetId() { return """"; }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.MissingAttributeId);
        }

        [Test]
        public void Method_HasAttributeFromBaseClass()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public virtual string GetId()
    {
        return """";
    }
}

public class File : Item
{
    [NotNull]
    public override string GetId() { return """"; }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
