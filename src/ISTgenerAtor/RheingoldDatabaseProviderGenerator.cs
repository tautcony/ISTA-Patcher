// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTgenerAtor;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class RheingoldDatabaseProviderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var schemaTextProvider = context.AdditionalTextsProvider
            .Select(static (file, token) =>
            {
                var profile = SchemaProfile.Get(file.Path);
                if (profile is null)
                {
                    return null;
                }

                var content = file.GetText(token)?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return null;
                }

                return new SchemaInput(file.Path, content!, profile);
            })
            .Where(static x => x is not null)!;

        context.RegisterSourceOutput(schemaTextProvider, static (productionContext, input) =>
        {
            try
            {
                Generate(productionContext, input!);
            }
            catch (Exception ex)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "ISTGEN001",
                        title: "Rheingold model generation failed",
                        messageFormat: "Failed to generate models from schema '{0}': {1}",
                        category: "ISTgenerAtor",
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None,
                    input!.Profile.DisplaySchemaPath,
                    ex.Message));
            }
        });
    }

    private static void Generate(SourceProductionContext context, SchemaInput input)
    {
        var schema = XDocument.Parse(input.Content);
        var xs = XNamespace.Get("http://www.w3.org/2001/XMLSchema");

        var targetNamespace = schema.Root?.Attribute("targetNamespace")?.Value
            ?? throw new InvalidOperationException("XSD targetNamespace is required.");

        var rootNullability = ParseRootNullability(schema, xs);
        var enumDefinitions = ParseEnums(schema, xs);
        var complexDefinitions = ParseComplexTypes(schema, xs);
        var complexNames = new HashSet<string>(complexDefinitions.Select(static x => x.Name), StringComparer.Ordinal);

        foreach (var enumType in enumDefinitions.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            var source = EmitEnum(input.Profile, targetNamespace, enumType);
            context.AddSource($"{input.Profile.ModelNamespace}.{enumType.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        foreach (var complexType in complexDefinitions.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            var isNullableRoot = rootNullability.TryGetValue(complexType.Name, out var nullableRoot) && nullableRoot;
            var source = EmitClass(input.Profile, targetNamespace, complexType, isNullableRoot, complexNames);
            context.AddSource($"{input.Profile.ModelNamespace}.{complexType.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static Dictionary<string, bool> ParseRootNullability(XDocument schema, XNamespace xs)
    {
        return schema.Root!
            .Elements(xs + "element")
            .Where(static x => x.Attribute("type") is not null)
            .Select(static x => new
            {
                TypeName = StripNamespace(x.Attribute("type")!.Value),
                IsNullable = bool.TryParse(x.Attribute("nillable")?.Value, out var nullable) && nullable,
            })
            .GroupBy(static x => x.TypeName, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.Last().IsNullable, StringComparer.Ordinal);
    }

    private static List<EnumDefinition> ParseEnums(XDocument schema, XNamespace xs)
    {
        return schema.Root!
            .Elements(xs + "simpleType")
            .Select(typeElement =>
            {
                var name = typeElement.Attribute("name")?.Value ?? throw new InvalidOperationException("simpleType name is required.");
                var values = typeElement
                    .Elements(xs + "restriction")
                    .Elements(xs + "enumeration")
                    .Select(v => v.Attribute("value")?.Value)
                    .OfType<string>()
                    .ToList();

                return new EnumDefinition(name, values);
            })
            .Where(static x => x.Values.Count > 0)
            .ToList();
    }

    private static List<ComplexTypeDefinition> ParseComplexTypes(XDocument schema, XNamespace xs)
    {
        return schema.Root!
            .Elements(xs + "complexType")
            .Select(typeElement =>
            {
                var name = typeElement.Attribute("name")?.Value ?? throw new InvalidOperationException("complexType name is required.");

                var elements = typeElement
                    .Element(xs + "sequence")?
                    .Elements(xs + "element")
                    .Select((element, order) =>
                    {
                        var elementName = element.Attribute("name")?.Value ?? throw new InvalidOperationException($"Element name missing in type {name}.");
                        var xsdType = element.Attribute("type")?.Value ?? "xs:string";
                        var minOccurs = int.TryParse(element.Attribute("minOccurs")?.Value, out var min) ? min : 1;
                        var maxOccurs = element.Attribute("maxOccurs")?.Value;
                        var isList = string.Equals(maxOccurs, "unbounded", StringComparison.OrdinalIgnoreCase) || (int.TryParse(maxOccurs, out var max) && max > 1);
                        var isNullable = bool.TryParse(element.Attribute("nillable")?.Value, out var nillable) && nillable;
                        var dataType = ExtractDataType(xsdType);

                        return new ElementDefinition(
                            elementName,
                            StripNamespace(xsdType),
                            dataType,
                            minOccurs == 0,
                            isList,
                            isNullable,
                            order);
                    })
                    .ToList() ?? new List<ElementDefinition>();

                var attributes = typeElement
                    .Elements(xs + "attribute")
                    .Select(attribute =>
                    {
                        var attributeName = attribute.Attribute("name")?.Value ?? throw new InvalidOperationException($"Attribute name missing in type {name}.");
                        var xsdType = attribute.Attribute("type")?.Value ?? "xs:string";
                        var use = attribute.Attribute("use")?.Value;

                        return new AttributeDefinition(
                            attributeName,
                            StripNamespace(xsdType),
                            ExtractDataType(xsdType),
                            string.Equals(use, "required", StringComparison.OrdinalIgnoreCase));
                    })
                    .ToList();

                return new ComplexTypeDefinition(name, elements, attributes);
            })
            .ToList();
    }

    private static string EmitEnum(SchemaProfile profile, string targetNamespace, EnumDefinition enumType)
    {
        var builder = new StringBuilder();
        AppendHeader(builder, profile.DisplaySchemaPath);
        builder.AppendLine($"namespace {profile.ModelNamespace};");
        builder.AppendLine();
        builder.AppendLine("using System.Xml.Serialization;");
        builder.AppendLine();
        builder.AppendLine($"[XmlType(Namespace = \"{targetNamespace}\")]");
        builder.AppendLine("[Serializable]");
        builder.AppendLine($"public enum {enumType.Name}");
        builder.AppendLine("{");
        foreach (var value in enumType.Values)
        {
            builder.AppendLine($"    {value},");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string EmitClass(
        SchemaProfile profile,
        string targetNamespace,
        ComplexTypeDefinition type,
        bool isNullableRoot,
        HashSet<string> complexTypeNames)
    {
        var builder = new StringBuilder();
        AppendHeader(builder, profile.DisplaySchemaPath);
        builder.AppendLine($"namespace {profile.ModelNamespace};");
        builder.AppendLine();
        builder.AppendLine("using System.Runtime.Serialization;");
        builder.AppendLine("using System.Xml.Schema;");
        builder.AppendLine("using System.Xml.Serialization;");
        builder.AppendLine();
        builder.AppendLine($"[XmlType(Namespace = \"{targetNamespace}\")]");
        builder.AppendLine($"[XmlRoot(Namespace = \"{targetNamespace}\", IsNullable = {(isNullableRoot ? "true" : "false")})]");
        builder.AppendLine($"[DataContract(Name = \"{type.Name}\", Namespace = \"{targetNamespace}\")]");
        builder.AppendLine("[Serializable]");
        builder.AppendLine($"public partial class {type.Name}");
        builder.AppendLine("{");

        var ctorAssignments = type.Elements
            .Where(element => element.IsList || complexTypeNames.Contains(element.TypeName))
            .Select(element => new
            {
                element.Name,
                Assignment = element.IsList
                    ? $"new List<{ResolveClrType(element.TypeName, false, false, IsReferenceType(element.TypeName, complexTypeNames))}>()"
                    : $"new {element.TypeName}()",
            })
            .ToList();

        if (ctorAssignments.Count > 0)
        {
            builder.AppendLine($"    public {type.Name}()");
            builder.AppendLine("    {");
            foreach (var assignment in ctorAssignments.OrderBy(static x => x.Name, StringComparer.Ordinal))
            {
                builder.AppendLine($"        this.{assignment.Name} = {assignment.Assignment};");
            }

            builder.AppendLine("    }");
            builder.AppendLine();
        }

        foreach (var element in type.Elements.OrderBy(static x => x.Order))
        {
            var xmlElementArgs = new List<string> { $"\"{element.Name}\"" };
            if (profile.UseUnqualifiedElements)
            {
                xmlElementArgs.Add("Form = XmlSchemaForm.Unqualified");
            }

            if (!string.IsNullOrWhiteSpace(element.DataType))
            {
                xmlElementArgs.Add($"DataType = \"{element.DataType}\"");
            }

            if (element.IsNullable)
            {
                xmlElementArgs.Add("IsNullable = true");
            }

            xmlElementArgs.Add($"Order = {element.Order}");
            builder.AppendLine($"    [XmlElement({string.Join(", ", xmlElementArgs)})]");
            builder.AppendLine("    [DataMember]");
            var elementReferenceType = IsReferenceType(element.TypeName, complexTypeNames);
            var elementClrType = ResolveClrType(element.TypeName, element.IsOptional, element.IsList, elementReferenceType);
            builder.AppendLine($"    public {elementClrType} {element.Name} {{ get; set; }}");
            builder.AppendLine();
        }

        foreach (var attribute in type.Attributes)
        {
            var xmlAttributeArgs = new List<string>();
            if (!string.IsNullOrWhiteSpace(attribute.DataType))
            {
                xmlAttributeArgs.Add($"DataType = \"{attribute.DataType}\"");
            }

            builder.AppendLine(xmlAttributeArgs.Count == 0 ? "    [XmlAttribute]" : $"    [XmlAttribute({string.Join(", ", xmlAttributeArgs)})]");
            builder.AppendLine("    [DataMember]");
            var attributeReferenceType = IsReferenceType(attribute.TypeName, complexTypeNames);
            var attributeClrType = !attribute.IsRequired && !attributeReferenceType
                ? ResolveClrType(attribute.TypeName, false, false, attributeReferenceType)
                : ResolveClrType(attribute.TypeName, !attribute.IsRequired, false, attributeReferenceType);
            builder.AppendLine($"    public {attributeClrType} {attribute.Name} {{ get; set; }}");
            builder.AppendLine();

            if (!attribute.IsRequired && !attributeReferenceType)
            {
                builder.AppendLine("    [XmlIgnore]");
                builder.AppendLine("    [DataMember]");
                builder.AppendLine($"    public bool {attribute.Name}Specified {{ get; set; }}");
                builder.AppendLine();
            }
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, string schemaPath)
    {
        builder.AppendLine("// SPDX-License-Identifier: GPL-3.0-or-later");
        builder.AppendLine("// SPDX-FileCopyrightText: Copyright 2026 TautCony");
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine($"//   Source: {schemaPath}");
        builder.AppendLine("//   This file is generated by ISTgenerAtor. Do not edit manually.");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#pragma warning disable CS8618");
        builder.AppendLine();
    }

    private static string StripNamespace(string xsdType)
    {
        var index = xsdType.IndexOf(':');
        return index >= 0 ? xsdType.Substring(index + 1) : xsdType;
    }

    private static string? ExtractDataType(string xsdType)
    {
        var typeName = StripNamespace(xsdType);
        return typeName is "anyURI" or "language" or "integer" or "base64Binary" ? typeName : null;
    }

    private static bool IsReferenceType(string typeName, HashSet<string> complexTypeNames)
    {
        return typeName is "string" or "anyURI" or "language" or "integer" or "base64Binary" || complexTypeNames.Contains(typeName);
    }

    private static string ResolveClrType(string typeName, bool isOptional, bool isList, bool isReferenceType)
    {
        var primitive = typeName switch
        {
            "string" => "string",
            "date" or "dateTime" => "DateTime",
            "boolean" => "bool",
            "integer" => "string",
            "anyURI" => "string",
            "language" => "string",
            "base64Binary" => "byte[]",
            _ => typeName,
        };

        if (isList)
        {
            return $"List<{ResolveClrType(typeName, false, false, isReferenceType)}>";
        }

        if (isReferenceType && (primitive == "string" || primitive == "byte[]"))
        {
            return isOptional ? $"{primitive}?" : primitive;
        }

        if (isReferenceType)
        {
            return primitive;
        }

        return isOptional ? $"{primitive}?" : primitive;
    }

    private sealed class SchemaInput
    {
        public SchemaInput(string path, string content, SchemaProfile profile)
        {
            this.Path = path;
            this.Content = content;
            this.Profile = profile;
        }

        public string Path { get; }

        public string Content { get; }

        public SchemaProfile Profile { get; }
    }

    private sealed class SchemaProfile
    {
        private SchemaProfile(string modelNamespace, string displaySchemaPath, bool useUnqualifiedElements)
        {
            this.ModelNamespace = modelNamespace;
            this.DisplaySchemaPath = displaySchemaPath;
            this.UseUnqualifiedElements = useUnqualifiedElements;
        }

        public string ModelNamespace { get; }

        public string DisplaySchemaPath { get; }

        public bool UseUnqualifiedElements { get; }

        public static SchemaProfile? Get(string path)
        {
            var fileName = Path.GetFileName(path);
            if (string.Equals(fileName, "dealer-master-data.xsd", StringComparison.OrdinalIgnoreCase))
            {
                return new SchemaProfile(
                    "ISTAlter.Models.Rheingold.DatabaseProvider",
                    "Models/Rheingold/Schemas/dealer-master-data.xsd",
                    useUnqualifiedElements: true);
            }

            if (string.Equals(fileName, "license-info.xsd", StringComparison.OrdinalIgnoreCase))
            {
                return new SchemaProfile(
                    "ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework",
                    "Models/Rheingold/Schemas/license-info.xsd",
                    useUnqualifiedElements: false);
            }

            return null;
        }
    }

    private sealed class EnumDefinition
    {
        public EnumDefinition(string name, IReadOnlyList<string> values)
        {
            this.Name = name;
            this.Values = values;
        }

        public string Name { get; }

        public IReadOnlyList<string> Values { get; }
    }

    private sealed class ComplexTypeDefinition
    {
        public ComplexTypeDefinition(string name, IReadOnlyList<ElementDefinition> elements, IReadOnlyList<AttributeDefinition> attributes)
        {
            this.Name = name;
            this.Elements = elements;
            this.Attributes = attributes;
        }

        public string Name { get; }

        public IReadOnlyList<ElementDefinition> Elements { get; }

        public IReadOnlyList<AttributeDefinition> Attributes { get; }
    }

    private sealed class ElementDefinition
    {
        public ElementDefinition(string name, string typeName, string? dataType, bool isOptional, bool isList, bool isNullable, int order)
        {
            this.Name = name;
            this.TypeName = typeName;
            this.DataType = dataType;
            this.IsOptional = isOptional;
            this.IsList = isList;
            this.IsNullable = isNullable;
            this.Order = order;
        }

        public string Name { get; }

        public string TypeName { get; }

        public string? DataType { get; }

        public bool IsOptional { get; }

        public bool IsList { get; }

        public bool IsNullable { get; }

        public int Order { get; }
    }

    private sealed class AttributeDefinition
    {
        public AttributeDefinition(string name, string typeName, string? dataType, bool isRequired)
        {
            this.Name = name;
            this.TypeName = typeName;
            this.DataType = dataType;
            this.IsRequired = isRequired;
        }

        public string Name { get; }

        public string TypeName { get; }

        public string? DataType { get; }

        public bool IsRequired { get; }
    }
}
