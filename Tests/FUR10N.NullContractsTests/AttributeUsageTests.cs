using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class AttributeUsageTests : TestBase
    {
        public AttributeUsageTests() : base(MainAnalyzer.BadAttributeUsageId)
        {
        }

        [Test]
        public void ReadonlyProperty_Allowed()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Id { get; }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ReadonlyField_Allowed()
        {
            var code =
@"
public class Item
{
    [NotNull]public readonly string Id;
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ConstField_Allowed()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public const string Id = """";
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void MethodArguments_Allowed()
        {
            var code =
@"
public class Item
{
    public string GetId([NotNull]string s)
    {
        return s;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void NotReadonlyProperty_Error()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Id { get; set; }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.BadAttributeUsageId);
        }

        [Test]
        public void NotReadonlyField_Error()
        {
            var code =
@"
public class Item
{
    [NotNull]public string Id;
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.BadAttributeUsageId);
        }

        [Test]
        public void Method_Success()
        {
            var code =
@"
public class Item
{
    [NotNull]public string GetId()
    {
        return """";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Method_ValueType_Error()
        {
            var code =
@"
public class Item
{
    [NotNull]public int GetId()
    {
        return 0;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.BadAttributeUsageId);
        }

        [Test]
        public void Class_Error()
        {
            var code =
@"
[NotNull]public class Item
{
    public string GetId()
    {
        return """";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.BadAttributeUsageId);
        }

        [Test]
        public void NonNullableType_Error()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly int Id;
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.BadAttributeUsageId);
        }

        [Test]
        public void Struct_Error()
        {
            var code =
@"
public struct Date
{
    public int Ticks;
}

public class Item
{
    [NotNull]
    public readonly Date day;
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.BadAttributeUsageId);
        }
    }
}
