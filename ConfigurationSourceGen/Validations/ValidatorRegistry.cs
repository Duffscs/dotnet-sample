using System.Collections.Generic;

namespace ConfigurationSourceGen.Validations;

public sealed class ValidatorRegistry {
    public readonly IReadOnlyList<IGenerateValidator> Validators = [
        new AllowedValuesValidator(),
        new MaxLengthValidator(),
        new MinLengthValidator(),
        new RangeValidator(),
        new RegexValidator(),
    ];

    public void Generate(CodeBuilder builder, ToGenerateProperty property, string valueExpression) {
        foreach (IGenerateValidator validator in Validators) {
            foreach (AttributeDefinition attribute in property.Attributes) {
                if (validator.AttributeName == attribute.Name) {
                    validator.Generate(builder, property, attribute, valueExpression);
                }
            }
        }
    }
}