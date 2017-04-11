
using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class NeedNullCheckTests : TestBase
    {
        public NeedNullCheckTests() : base(MainAnalyzer.NeedNullCheckId)
        {
        }

        [Test]
        public void NeedNullCheck()
        {
            var code =
@"
public class Item
{
    [CanBeNull] public string Id { get; set; }

    public void Load()
    {
        Id.GetType();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NeedNullCheckId);
        }

        [Test]
        public void HasNeedNullCheck()
        {
            var code =
@"
public class Item
{
    [CanBeNull] public string Id { get; set; }

    public void Load()
    {
        Id?.GetType();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ExtensionMethod_CanBeNull()
        {
            var code =
@"
public class Item
{
    [CanBeNull] public string Id { get; set; }

    public void Load()
    {
        Id?.GetType();
    }
}

public static class Extensions
{
    public static void Method([CanBeNull] this Item item)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
