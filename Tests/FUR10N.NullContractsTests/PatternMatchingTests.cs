using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class PatternMatchingTests : TestBase
    {
        [Test]
        public void IsPattern_Success()
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
        if (folder is Folder f)
        {
            return IsSomething(f);
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
        public void MultipleIsPatterns_Success()
        {
            var code =
@"
public class Folder
{
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder, object obj)
    {
        if (folder is Folder f1 && obj is Folder f2)
        {
            return IsSomething(f1) && IsSomething(f2);
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
        public void IsPattern_WithReassignment_Fail()
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
        if (folder is Folder f)
        {
            f = null;
            return IsSomething(f);
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
        public void IsPattern_WithSameAs()
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
        if (folder is Folder f)
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
        public void IsPattern_WithDifferentAs()
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
        if (folder is Folder f)
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
        public void IsPattern_Not_WithSameAs()
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
        if (!(folder is Folder f))
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
        public void IsPattern_Not_WithDifferentAs()
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
        if (!(folder is Folder f))
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
        if (folders.FirstOrDefault() is Folder f)
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
        public void SwitchPattern_Success()
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
        switch(folder)
        {
            case Folder f:
            {
                return IsSomething(f);
            }
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
        public void SwitchPattern_WithReassignment_Fails()
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
        switch(folder)
        {
            case Folder f:
            {
                f = null;
                return IsSomething(f);
            }
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
    }
}
