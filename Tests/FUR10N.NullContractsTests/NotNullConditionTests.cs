using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class NotNullConditionTests : TestBase
    {
        public NotNullConditionTests() : base()
        {
        }

        [Test]
        [Ignore("Not yet supported")]
        public void Test()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class ItemViewModel
{
    [NotNullCondition(nameof(Item.Id))]
    public Item File { get; set; }
    
    public void Method()
    {
        Method2(File.Id);
    }

    public void Method2([NotNull] string id)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
