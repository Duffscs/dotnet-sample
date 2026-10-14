using System;
using System.Globalization;

namespace ConfigurationSourceGen.Validations;

internal static class ValidationGeneratorHelper {
    public static bool TryGetIntArgument(AttributeDefinition attribute, int index, out int value) {

        value = 0;

        return attribute.Arguments.Count > index
            && int.TryParse(
                attribute.Arguments[index],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
    }

    public static string EscapeString(string value) {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    public static void AddError(CodeBuilder builder, string propertyName, string message) {
        string escapedPropertyName = EscapeString(propertyName);
        string escapedMessage = EscapeString(message);

        builder.AddLine(
            $"errors.Add($\"{{path}}.{escapedPropertyName} {escapedMessage}\");");
    }

    public static string FormatArgument(AttributeDefinition attribute, int index) {
        return attribute.Arguments[index];
    }
}