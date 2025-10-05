using System.Diagnostics.CodeAnalysis;

// Mediator class is now internal, so no naming conflict warning
[assembly: SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Extension class for DI registration", Scope = "type", Target = "~T:Mediator.DependencyInjection")]
