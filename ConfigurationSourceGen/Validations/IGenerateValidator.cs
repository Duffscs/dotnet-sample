using System.Collections.Generic;
using System.Collections.Immutable;

namespace ConfigurationSourceGen.Validations;

public interface IGenerateValidator {
    /// <summary>
    /// Gets the names of the attributes handled by this validator.
    /// </summary>
    string AttributeName { get; }

    string GenerateAttributeSource(string ns);

    /// <summary>
    /// Generates the validation code for the specified property.
    /// </summary>
    /// <param name="builder">The code builder used to generate the source.</param>
    /// <param name="property">The property being validated.</param>
    /// <param name="valueExpression">The C# expression representing the value to validate.</param>
    void Generate(CodeBuilder builder, ToGenerateProperty property, AttributeDefinition attribute, string valueExpression);
}