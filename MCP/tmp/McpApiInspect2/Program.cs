using System.Reflection;
using ModelContextProtocol.Server;

Dump(typeof(McpServerResourceAttribute));
Console.WriteLine("---");
Dump(typeof(McpServerPromptAttribute));

static void Dump(Type type)
{
    Console.WriteLine(type.FullName);

    foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
    {
        Console.WriteLine($"CTOR {ctor}");
    }

    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(p => p.Name))
    {
        Console.WriteLine($"PROP {property.PropertyType.FullName} {property.Name}");
    }
}
