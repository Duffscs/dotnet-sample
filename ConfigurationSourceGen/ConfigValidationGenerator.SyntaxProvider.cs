using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfigurationSourceGen;

public record ToGenerateClass(string Namespace, string Name, List<ToGenerateProperty> Properties, List<AttributeDefinition> Attributes);
public record ToGenerateProperty(string Name, TypeDefinition Type, List<AttributeDefinition> Attributes);
public record TypeDefinition(string Name, string Namespace, bool IsNullable, bool IsEnum, bool IsRaw, List<TypeDefinition> Generics);
public record AttributeDefinition(string Name, string Namespace, List<string> Arguments);

public partial class ConfigValidationGenerator {
    private static ToGenerateClass? PrepareClassData(GeneratorSyntaxContext context) {
        TypeDeclarationSyntax syntax = (TypeDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol rootSymbol)
            return null;

        bool hasAttribute = syntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() == "GenerateConfigValidation");

        if (!hasAttribute)
            return null;

        string ns = rootSymbol.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "global");
        string name = rootSymbol.Name;

        List<ToGenerateProperty> properties = rootSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(p => new ToGenerateProperty(p.Name, BuildTypeDefinition(p.Type), ExtractAttributes(p)))
            .ToList();

        return new ToGenerateClass(ns, name, properties, ExtractAttributes(rootSymbol));
    }
    private static TypeDefinition BuildTypeDefinition(ITypeSymbol type) {
        bool isNullable = IsNullable(type, out type);
        bool isEnum = type.TypeKind == TypeKind.Enum;

        if (type is IArrayTypeSymbol array) {
            TypeDefinition elem = BuildTypeDefinition(array.ElementType);
            return new TypeDefinition("List", "System.Collections.Generic", isNullable, false, true, [elem]);
        }

        string ns = type.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "global");

        if (type is INamedTypeSymbol named) {
            string name = named.Name;
            List<TypeDefinition> generics = [];

            if (named.IsGenericType) {
                foreach (ITypeSymbol arg in named.TypeArguments) {
                    generics.Add(BuildTypeDefinition(arg));
                }
            }

            return new TypeDefinition(name, ns, isNullable, isEnum, IsRaw(type), generics);
        }

        // primitives, enum, etc.
        return new TypeDefinition(
            type.Name,
            ns,
            isNullable,
            isEnum,
            IsRaw(type),
            []
        );
    }

    private static bool IsNullable(ITypeSymbol type, out ITypeSymbol underlyingType) {
        underlyingType = type;

        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            underlyingType = named.TypeArguments[0];
            return true; // nullable value type
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated) {
            underlyingType = type; // nullable reference type
            return true;
        }

        return false;
    }

    private static List<AttributeDefinition> ExtractAttributes(ISymbol symbol) {
        return symbol.GetAttributes()
            .Select(attr => {
                string attrName = attr.AttributeClass?.Name ?? "";
                string attrNamespace = attr.AttributeClass.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "global::");

                List<string> args = [];

                foreach (TypedConstant arg in attr.ConstructorArguments)
                    args.Add(FormatArg(arg));

                foreach (KeyValuePair<string, TypedConstant> kvp in attr.NamedArguments)
                    args.Add($"{kvp.Key} = {FormatArg(kvp.Value)}");

                return new AttributeDefinition(attrName, attrNamespace, args);
            })
            .ToList();
    }

    private static string FormatArg(TypedConstant arg) {
        if (arg.IsNull)
            return "null";

        if (arg.Kind == TypedConstantKind.Array) {
            IEnumerable<string> elems = arg.Values.Select(FormatArg);
            return $"new[] {{ {string.Join(", ", elems)} }}";
        }

        ITypeSymbol? type = arg.Type;
        // Si c’est une string → on échappe
        if (type != null && type.SpecialType == SpecialType.System_String) {
            string escaped = arg.Value?.ToString()?.Replace("\"", "\\\"") ?? "";
            return $"\"{escaped}\"";
        }
        return arg.Value?.ToString() ?? "null";
    }



    private static bool IsRaw(ITypeSymbol type) {
        string fullName = "";
        if (type is INamedTypeSymbol _named) {
            fullName = _named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Primitives gérées via SpecialType (int, string, bool, float, double, ...)
        bool a = type.SpecialType switch {
            SpecialType.System_Boolean or
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Char or
            SpecialType.System_UInt16 or
            SpecialType.System_Int16 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt64 or
            SpecialType.System_Int64 or
            SpecialType.System_Decimal or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_String => true,
            _ when type.TypeKind == TypeKind.Enum => true,
            _ when fullName is "System.DateTime" or
                "System.DateTimeOffset" or
                "System.TimeSpan" or
                "System.Guid" => true,
            _ => false
        };


        if (a)
            return false;

        if (type is INamedTypeSymbol named) {
            // Gestion de Nullable<T>
            INamedTypeSymbol? constructedFrom = named.ConstructedFrom;
            if (constructedFrom != null) {
                string constructedFromDisplay = constructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (constructedFromDisplay == "global::System.Nullable<T>") {
                    // Vérifier le type sous-jacent récursivement
                    ITypeSymbol? underlying = named.TypeArguments.Length > 0 ? named.TypeArguments[0] : null;
                    return !(underlying != null && IsRaw(underlying));
                }

                // Collections génériques "simples" que tu veux considérer comme primitives
                if (constructedFromDisplay == "global::System.Collections.Generic.List<T>" ||
                    constructedFromDisplay == "global::System.Collections.Generic.Dictionary<TKey, TValue>" ||
                    constructedFromDisplay == "global::System.Collections.Generic.HashSet<T>") {
                    return false;
                }
            }
        }

        return true;
    }
}
