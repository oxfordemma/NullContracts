using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class ReassignmentTests : TestBase
    {
        public ReassignmentTests() : base(MainAnalyzer.AssignmentAfterConditionId)
        {
        }

        [Test]
        public void AssignmentBeforeCondition_Success()
        {
            var code =
@"
public class Item
{
    public Item(string id)
    {
        id = null;
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
        public void AssignmentAfterCondition_Fails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public void Method()
    {
        if (Id != null)
        {
            Id = null;
            M(Id);
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.AssignmentAfterConditionId);
        }

        [Test]
        public void AssignmentInSeparateBranch_Success()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public void Method()
    {
        if (true)
        {
            Id = null;
        }
        if (Id != null)
        {
            M(Id);
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
        public void AssignmentInParentBranch_Fails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public bool X;

    public void Method()
    {
        if (Id != null)
        {
            if (X)
            {
                Id = null;
            }
            M(Id);
        }
    }

    private void M([NotNull]string s)
    {
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.AssignmentAfterConditionId);
        }
    }
}
