using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class ShortCircuitTests : TestBase
    {
        [Test]
        public void InReturn_ShortCircuit()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        return item != null && M(item);
    }

    private bool M([NotNull]Item item)
    {
        return item.Id.Length < 5;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InReturn_ShortCircuitWithPrecondition_Success()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        if (item != null)
        {
            return item.Id != null && M(item, item.Id);
        }
        return true;
    }

    private bool M([NotNull]Item item, [NotNull] string id)
    {
        return item.Id.Length < 5 && id == """";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InReturn_ShortCircuitWithPrecondition_Fails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        if (item != null)
        {
            return item.Id == null && M(item, item.Id);
        }
        return true;
    }

    private bool M([NotNull]Item item, [NotNull] string id)
    {
        return item.Id.Length < 5 && id == """";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InReturn_ShortCircuitWithPrecondition_AndPreconditionThatReturns_Fails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        if (item != null)
        {
            if (item.Name == null)
            {
                return false;
            }
            return item.Id == null && M(item, item.Id, item.Name);
        }
        return true;
    }

    private bool M([NotNull]Item item, [NotNull] string id, [NotNull] string name)
    {
        return item.Id.Length < 5 && id == """" && name == """";
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InReturn_WithConditionalAccess_ShortCircuitAnd()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        return item?.Id != null && M(item.Id);
    }

    private bool M([NotNull]string s)
    {
        return s.Length < 5;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InReturn_WithConditionalAccess_ShortCircuitOr()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        return item?.Id == null || M(item.Id);
    }

    private bool M([NotNull]string s)
    {
        return s.Length < 5;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InReturn_WithConditionalAccess_ShortCircuit_Fails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    public bool Test(Item item)
    {
        return item?.Id == null && M(item.Id);
    }

    private bool M([NotNull]string s)
    {
        return s.Length < 5;
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InReturn_ShortCircuitOr()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        return item == null || item.Id == null || IsSomething(item.Id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InReturn_ShortCircuitAnd()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        return item != null && item.Id != null && !IsSomething(item.Id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InReturn_ShortCircuitOrFails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        return item != null || item.Id != null || IsSomething(item.Id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InReturn_ShortCircuitAndFails()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        return item == null && item.Id == null && !IsSomething(item.Id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InReturn_ShortCircuit_WrongOrder()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        return item != null && !IsSomething(item.Id) && item.Id != null;
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InIf_ShortCircuit_WrongOrder()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        if (item != null && !IsSomething(item.Id) && item.Id != null)
        {
            return true;
        }
        return false;
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void InIf_ShortCircuit_RightOrder()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        if (item != null && item.Id != null && !IsSomething(item.Id))
        {
            return true;
        }
        return false;
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void InIf_ShortCircuitOr()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        if (item.Id == null || IsSomething(item.Id))
        {
            return true;
        }
        return false;
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
