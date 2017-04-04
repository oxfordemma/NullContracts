using FUR10N.NullContracts;
using NUnit.Framework;

namespace FUR10N.NullContractsTests
{
    [TestFixture]
    public class AwaitTests : TestBase
    {
        [Test]
        public void Await_OnNullMethod()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    [NotNull]
    public async Task<Item> Method()
    {
        return await GetAsync();
    }

    private Task<Item> GetAsync()
    {
        return Task.FromResult(new Item());
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void Await_OnNotNullMethod()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    [NotNull]
    public async Task<Item> Method()
    {
        return await GetAsync();
    }

    [NotNull]
    private Task<Item> GetAsync()
    {
        return Task.FromResult(new Item());
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }

        [Test]
        public void Await_OnNullMethod_WithConfigurAwait()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    [NotNull]
    public async Task<Item> Method()
    {
        return await GetAsync().ConfigureAwait(false);
    }

    private Task<Item> GetAsync()
    {
        return Task.FromResult(new Item());
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d, MainAnalyzer.ReturnNullId);
        }

        [Test]
        public void Await_OnNotNullMethod_WithConfigurAwait()
        {
            var code =
@"
public class Item
{
    public string Id { get; set; }
}

public class C
{
    [NotNull]
    public async Task<Item> Method()
    {
        return await GetAsync().ConfigureAwait(false);
    }

    [NotNull]
    private Task<Item> GetAsync()
    {
        return Task.FromResult(new Item());
    }
}
";
            var d = GetDiagnostics(code);
            AssertIssues(d);
        }
    }
}
