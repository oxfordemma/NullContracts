using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class ConstraintTests : TestBase
    {
        public ConstraintTests()
            : base(MainAnalyzer.InvalidConstraintId, MainAnalyzer.AssignmentAfterConstraintId, MainAnalyzer.NullAssignmentId, MainAnalyzer.UnneededConstraintId)
        {
        }

        [Test]
        public void ConstraintOnMemberAccessByExpression()
        {
            var code =
@"
public class Folder
{
    public string Id { get; set; }
}

public class Item
{
    public static bool Method(Folder folder)
    {
        Constraint.NotNull(() => folder.Id);
        return IsSomething(folder.Id);
    }

    public static bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void ConstraintOnMemberAccessByValue()
        {
            var code =
                @"
public class Folder
{
    public string Id { get; set; }
}

public class Item
{
    public static bool Method(Folder folder)
    {
        Constraint.NotNull(folder.Id, nameof(folder.Id));
        return IsSomething(folder.Id);
    }

    public static bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void ConstraintOnOneField_RewriteADifferentField()
        {
            var code =
@"
public class Folder
{
    public string Id { get; set; }

    public string Name { get; set; }
}

public class Item
{
    public static bool Method(Folder folder)
    {
        Constraint.NotNull(() => folder.Id);
        folder.Id = null;
        folder.Name = null;

        return IsSomething(folder.Id);
    }

    public static bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d, MainAnalyzer.AssignmentAfterConstraintId);
        }

        [Test]
        public void ConstraintOnIdentifier()
        {
            var code =
@"

public class Item
{
    public string Id { get; set; }

    public bool Method()
    {
        Constraint.NotNull(() => Id);
        return IsSomething(Id);
    }

    public bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void FailsOnMethod()
        {
            var code =
@"
public class Folder
{
    public string GetId() { return """"; }
}

public class Item
{
    public static bool Method(Folder folder)
    {
        Constraint.NotNull(() => folder.GetId());
        return IsSomething(folder.GetId());
    }

    public static bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.InvalidConstraintId, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void Constraint_FollowedByNullAssignment()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public bool Method()
    {
        Constraint.NotNull(() => Id);
        Id = null;
        return IsSomething(Id);
    }

    public bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d, MainAnalyzer.AssignmentAfterConstraintId);
        }

        [Test]
        public void Constraint_ArrayAccess()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool Method(Item[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Constraint.NotNull(() => items[i].Id);
            IsSomething(items[i].Id);
        }
        return false;
    }

    public static bool IsSomething([NotNull]string id)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }

        [Test]
        public void Assignment_UnneededConstraint()
        {
            var code =
@"
public class Item
{
    public static bool Method(Item item)
    {
        if (item == null)
        {
            return false;
        }
        Constraint.NotNull(() => item);
        return IsSomething(item);
    }

    public static bool IsSomething([NotNull] Item item)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d, MainAnalyzer.UnneededConstraintId);
        }

        [Test]
        public void Return_UnneededConstraint()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public static Item Method(Item item)
    {
        if (item == null)
        {
            return new Item();
        }
        Constraint.NotNull(() => item);
        return item;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d, MainAnalyzer.UnneededConstraintId);
        }

        [Test]
        public void UnneededConstraintOnNotNullField()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id = """";

    [NotNull]
    public string Method()
    {
        Constraint.NotNull(() => Id);
        return Id;
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d, MainAnalyzer.UnneededConstraintId);
        }
    }
}
