// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Tests name can contain underscores")]
[assembly: SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Tests doesn't need call GC")]
[assembly: SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Tests doesn't need")]
