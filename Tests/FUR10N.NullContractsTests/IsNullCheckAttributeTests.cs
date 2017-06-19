using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class IsNullCheckAttributeTests : TestBase
    {
        public IsNullCheckAttributeTests() : base(MainAnalyzer.UnneededNullCheckId)
        {
        }

        private string GetNullCheckMethod()
        {
            return
@"
public static class NullCheckExtensions
{
    [IsNullCheck]
    public static bool HasValue(this object value)
    {
        return value != null;
    }
}
";
        }

        [Test]
        public void AlwaysInitialized_UnneededNullCheck()
        {
            var code = GetNullCheckMethod() +
@"
public class Item
{
    [NotNull]
    public object GetNotNullObject()
    {
        return ""obj"";
    }

    public void Something()
    {
        var obj = GetNotNullObject();
        if (obj.HasValue())
        {
            return;
        }
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.UnneededNullCheckId);
        }

        [Test]
        public void SometimesInitialized_NeededNullCheck()
        {
            var code = GetNullCheckMethod() +
@"
public class Item
{
    [NotNull]
    public object GetNotNullObject()
    {
        return ""obj"";
    }

    public void Something()
    {
        var obj = GetNotNullObject();
        if (this.GetType() != null)
        {
            obj = null;
        }
        if (obj.HasValue())
        {
            return;
        }
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
