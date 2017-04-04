# FUR10N.NullContracts

This is a Roslyn analyzer for enforcing that [NotNull] entities are always assigned.

## Usage
1. Nuget Package: [FUR10N.NullContracts](https://www.nuget.org/packages/FUR10N.NullContracts)
2. Define the classes below in your project.
3. Start applying the NotNull attribute to field/properties/methods in your project.

## Supported attributes and classes (these need to be defined in your project)
```
// Supported on readonly fields, readonly properties, computed properties, method parameters, and methods
public class NotNullAttribute : Attribute
{
}

// Sort of a legacy attribute. Only supported on method parameters
public class CheckNullAttribute : Attribute
{
}

// This lets you override the analyzer if you know something is not null, but the analyzer thinks it is null.
// Usage: Place a call to Constraint.NotNull with the member that the analyzer thinks is null.
public class Constraint
{
    [NotNull]
    public static T NotNull<T>(Expression<Func<T>> expression) where T : class
    {
        var result = expression.Compile()();
        if (result == null)
        {
            throw new Exception();
        }
        return result;
    }
}
```

## Rules:
* NC0000 - Parser failed. The analyzer couldn't parse some C# syntax.
* NC1001 - [NotNull] is only supported on some members and reference types
* NC1002 - A member that was overridden requries the [NotNull] attribute
* NC1003 - Constraints can only be applied to locals or fields/properties
* NC1004 - Cannot modify a field that has a constraint on it
* NC1005 - A member that was checked for null was reassigned after the check
* NC2001 - You checked for null against a member marked with [NotNull]
* NC2002 - There was a constraint on a member that is already checked for null
* NC3001 - Tried to assign a null (or unchecked value) to a [NotNull] member
* NC3002 - A [NotNull] field/property was not initalized during construction
* NC3003 - A chained constructor expects a [NotNull] argument
* NC3004 - Tried to return a value that might be null in a [NotNull] method
* NC3005 - Used a [NotNull] local as a ref parameter

## Examples:
```
public class Item
{
    [NotNull] public string Id { get; }
	
    public Item() // NC3002 - A [NotNull] field/property was not initalized during construction
    {
    }
}
```

```
public class Item
{
    [NotNull] public string Id { get; }

    public Item([NotNull] string id)
    {
        this.Id = id; // Ok
    }
}
```

```
public class Item
{
    [NotNull] public string Id { get; }

    public Item(string id)
    {
        this.Id = id; // NC3001 - Tried to assign a null (or unchecked value) to a [NotNull] member
    }
}
```

```
public class Item
{
    [NotNull] public string Id { get; }

    public Item(string id)
    {
        Constraint.NotNull(() => id);
        this.Id = id; // Ok
    }
}
```

```
public class Item
{
    [NotNull] public string Id { get; }

    public Item(string id)
    {
        if (id == null)
        {
            return;
        }
        this.Id = id; // Ok
    }
}
```

```
public class Item
{
    [NotNull] public string Id { get; }

    public Item(string id)
    {
        this.Id = GetId(); // Ok
    }
    
    [NotNull]
    public static string GetId()
    {
        return "id";
    }
}
```

[More examples](Tests/FUR10N.NullContractsTests)