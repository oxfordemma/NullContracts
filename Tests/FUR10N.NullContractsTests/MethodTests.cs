using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class MethodTests : TestBase
    {
        [Test]
        public void NotNullInAllBranches()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public static object Method(object uri)
    {
        if (uri is Uri)
        {
            return uri;
        }
        return new object();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ReturnNull()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public static object Method(object uri)
    {
        return null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void ReturnNotNullField()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly string Id = ""id"";

    [NotNull]
    public object Method()
    {
        return Id;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ReturnNotNullMethod()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public object Method()
    {
        return Method();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Return_WithReturnInLambda()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public object Method()
    {
        var lambda = new Func<object>(() =>
        {
            return null;
        });
        return Method();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void BadReturnInSwitch()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public object Method(int i)
    {
        switch (i)
        {
            case 1:
                return null;
        }
        return new object();
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void ComputedProperty_AttributeOnProperty_Success()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly Uri _url = new Uri(""/"");

    [NotNull]
    public Uri Url
    {
        get
        {
            return _url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
}

        [Test]
        public void LazyProperty_Success()
        {
            var code =
        @"
public class Item
{
    public Uri _url;

    [NotNull]
    public Uri Url
    {
        get
        {
            return _url ?? (_url = new Uri(""/""));
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

[Test]
        public void ComputedProperty_AttributeOnGetter_Success()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly Uri _url = new Uri(""/"");

    public Uri Url
    {
        [NotNull]
        get
        {
            return _url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ComputedProperty_AttributeOnProperty_Fails()
        {
            var code =
@"
public class Item
{
    public Uri _url;

    [NotNull]
    public Uri Url
    {
        get
        {
            return _url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void ComputedProperty_AttributeOnGetter_Fails()
        {
            var code =
@"
public class Item
{
    public Uri _url;

    public Uri Url
    {
        [NotNull]
        get
        {
            return _url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void Property_OnlyGetterHasAttribute()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly Uri _url = new Uri(""/"");

    public Uri Url
    {
        [NotNull]
        get
        {
            return _url;
        }
        set
        {
            Method(value);
        }
    }

    private void Method([NotNull]Uri arg)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void Property_AttributeOnProperty_SetterPasses()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public readonly Uri _url = new Uri(""/"");

    [NotNull]
    public Uri Url
    {
        get
        {
            return _url;
        }
        set
        {
            Method(value);
        }
    }

    private void Method([NotNull]Uri arg)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Property_AttributeOnSetter_SetterPasses()
        {
            var code =
@"
public class Item
{
    public string Id
    {
        get
        {
            return """";
        }
        [NotNull]
        set
        {
            Method(value);
        }
    }

    private void Method([NotNull]string id)
    {
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Property_SetNotNullPropertyToNull()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public string Id
    {
        get
        {
            return """";
        }
        set
        {
        }
    }

    private void Method()
    {
        Id = null;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.NullAssignmentId);
        }

        [Test]
        public void Property_AttributeOnSetter_GetterCanReturnNull()
        {
            var code =
@"
public class Item
{
    public Uri Url
    {
        get
        {
            return null;
        }
        [NotNull]
        set
        {
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ConstraintInParenthesizedLambdaLambda()
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
        await SafeActionExecute(
            async () =>
                {
                    await Task.Delay(1);
                    Constraint.NotNull(() => folder.Id);
                    return IsSomething(folder.Id);
                },
            RequestItemsBreadcrumb.GenerateLink).ConfigureAwait(false);
        return true;
    }

    public static bool IsSomething([NotNull]string folder)
    {
        return true;
    }
}
";

            AssertIssues(GetDiagnostics(code, true));
        }

        [Test]
        public void ExpressionBodyProperty_Success()
        {
            var code =
@"
public class Item
{
    [NotNull]
    public Uri Url => new Uri(""/"");
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ExpressionBodyProperty_ShortCircuit_Success()
        {
            var code =
@"
public class Item
{
    private string id;

    public bool Allowed => id != null && Method(id);

    public bool Method([NotNull] string x)
    {
        return true;
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void ExpressionBodyProperty_Failed()
        {
            var code =
@"
public class Item
{
    private Uri url;

    [NotNull]
    public Uri Url => url;
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void Property_ReturnsUnassignedField_Failed()
        {
            var code =
@"
public class Item
{
    private bool x;    

    private string url;

    [NotNull]
    public string Url
    {
        get
        {
            if (x)
            {
                url = """";
            }
            return url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        [Ignore("Not yet supported")]
        public void Property_ReturnsUnassignedField_Success()
        {
            var code =
@"
public class Item
{
    private string url;

    [NotNull]
    public string Url
    {
        get
        {
            if (url == null)
            {
                url = """";
            }
            return url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Property_ReturnsUnassignedProperty_Failed()
        {
            var code =
@"
public class Item
{
    private bool x;    

    private string url { get; set; }

    [NotNull]
    public string Url
    {
        get
        {
            if (x)
            {
                url = """";
            }
            return url;
        }
    }
}
";

            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }
    }
}
