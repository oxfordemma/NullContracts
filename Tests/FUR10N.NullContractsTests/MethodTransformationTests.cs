using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class MethodTransformationTests : TestBase
    {
        [Test]
        public void Transform_IsNullOrEmpty_ToNullCheck()
        {
            var code =
@"
public class Item
{
    public static bool CanGetItem(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return true;
        }
        return IsSomething(id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void Transform_IsNullOrWhitespace_ToNullCheck()
        {
            var code =
@"
public class Item
{
    public static bool CanGetItem(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return true;
        }
        return IsSomething(id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void Transform_IsNullOrEmpty_WithConditionalAccess_ToNullCheck()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }

    public static bool CanGetItem(Item item)
    {
        if (string.IsNullOrEmpty(item?.Id))
        {
            return true;
        }
        return IsSomething(item, item.Id);
    }

    public static bool IsSomething([NotNull]Item item, [NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void Transform_IsNullOrEmpty_WithOr_Success()
        {
            var code =
@"
public class Item
{
    public static bool CanGetItem(string id)
    {
        if (string.IsNullOrEmpty(id) || id == ""asdf"")
        {
            return true;
        }
        return IsSomething(id);
    }

    public static bool IsSomething([NotNull]string id)
    {
        return id.Length < 5;
    }
}

";

            AssertIssues(GetDiagnostics(code));
        }

        [Test]
        public void Transform_IsNotNullOrEmpty_ToNotNullCheck()
        {
            var code =
@"
public class Item
{
    public static bool CanGetItem(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            return true;
        }
        return IsSomething(id);
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
        public void Transform_IsNotNullOrEmpty_WithAnd_Success()
        {
            var code =
@"
public class Item
{
    public static bool CanGetItem(string id)
    {
        if (!string.IsNullOrEmpty(id) && false)
        {
            return true;
        }
        return IsSomething(id);
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
        public void Uri_IfTryCreate()
        {
            var code =
@"
public class Folder
{
    public string Url { get; set; }
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder)
    {
        Uri folderUri;
        if (Uri.TryCreate(folder.Url, UriKind.Absolute, out folderUri))
        {
            if (IsSomething(folderUri))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsSomething([NotNull] Uri uri)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }

        [Test]
        public void Uri_IfNotTryCreate()
        {
            var code =
@"
public class Folder
{
    public string Url { get; set; }
}

public class Info { }

public class Item
{
    public static bool Method(Folder folder)
    {
        Uri folderUri;
        if (!Uri.TryCreate(folder.Url, UriKind.Absolute, out folderUri))
        {
            if (IsSomething(folderUri))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsSomething([NotNull] Uri uri)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false), MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void Foeach_Uri_IfTryCreate()
        {
            var code =
@"
public class Folder
{
    public string Url { get; set; }
}

public class Info { }

public class Item
{
    public static bool Method(Folder[] folders)
    {
        {
            foreach (var folder in folders)
            {
                Uri folderUri;
                if (Uri.TryCreate(folder.Url, UriKind.Absolute, out folderUri))
                {
                    if (IsSomething(folderUri))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static bool IsSomething([NotNull] Uri uri)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, false));
        }
    }
}
