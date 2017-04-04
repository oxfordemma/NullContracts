using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class IsAsTests : TestBase
    {
        [Test]
        public void IsCondition()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder)
    {
        if (folder is Folder)
        {
            return IsSomething(folder);
        }
        return false;
    }

    public static bool IsSomething([NotNull] Folder folder)
    {
        return true;
    }
}

";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void IsCondition_WithCast()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        if (folder is Folder)
        {
            return IsSomething((Folder)folder);
        }
        return false;
    }

    public static bool IsSomething([NotNull] Folder folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void IsNotCondition()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder)
    {
        if (!(folder is Folder))
        {
            return IsSomething(folder);
        }
        return false;
    }

    public static bool IsSomething([NotNull] Folder folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void IsCondition_WithSameAs()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        if (folder is Folder)
        {
            return IsSomething(folder as Folder);
        }
        return false;
    }

    public static bool IsSomething([NotNull] Folder folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void IsCondition_WithDifferentAs()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        if (folder is Folder)
        {
            return IsSomething(folder as string);
        }
        return false;
    }

    public static bool IsSomething([NotNull] object folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void IsCondition_Not_WithSameAs()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        if (!(folder is Folder))
        {
            return IsSomething(folder as Folder);
        }
        return false;
    }

    public static bool IsSomething([NotNull] Folder folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void IsCondition_Not_WithDifferentAs()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        if (!(folder is Folder))
        {
            return IsSomething(folder as string);
        }
        return false;
    }

    public static bool IsSomething([NotNull] object folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void IfNotNull_FollowedByAs()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        if (folder != null)
        {
            return IsSomething(folder as string);
        }
        return false;
    }

    public static bool IsSomething([NotNull] object folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void NotNullConstraint_FollowedByAs()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(object folder)
    {
        Constraint.NotNull(() => folder);
        return IsSomething(folder as string);
    }

    public static bool IsSomething([NotNull] object folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void If_WithMethodCallAndIs()
        {
            var code =
@"
public class Folder
{
}

public class Item
{
    public static bool Method(object[] folders)
    {
        if (folders.FirstOrDefault() is Folder)
        {
            DoSomething("""");
        }
        return true;
    }

    public static void DoSomething([NotNull] string s)
    {
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }

        [Test]
        public void If_ToStringAndIs()
        {
            var code =
@"
public class Folder
{
}

public class Item
{
    public static bool Method(object[] folders)
    {
        if (string.IsNullOrEmpty(folders.ToString()) is bool)
        {
            DoSomething("""");
        }
        return true;
    }

    public static void DoSomething([NotNull] string s)
    {
    }
}
";

            var d = GetDiagnostics(code, false);
            AssertIssues(d);
        }
    }
}
