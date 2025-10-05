using System.Reflection;
using System.Text;
using WebAPICoreMandelbrot.Contracts.Responses;

namespace WebAPICoreMandelbrot.TypeScriptGenerator;

public class TypeScriptGenerator
{
    public string GenerateTypeScriptInterfaces()
    {
        var output = new StringBuilder();
        
        // Add header comment
        output.AppendLine("// Auto-generated TypeScript interfaces from C# response classes");
        output.AppendLine("// DO NOT EDIT MANUALLY - This file is generated during build");
        output.AppendLine($"// Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        output.AppendLine();

        // Generate interfaces from actual C# classes
        GenerateInterface(typeof(MandelbrotResponse), output);
        GenerateInterface(typeof(DeviceInfoResponse), output);
        
        // Remove any trailing empty line to prevent formatting issues
        var result = output.ToString().TrimEnd();
        return result + Environment.NewLine; // End with single newline
    }

    private void GenerateInterface(Type responseType, StringBuilder output)
    {
        output.AppendLine($"export interface {responseType.Name} {{");

        var properties = responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .OrderBy(p => p.Name);

        var propertiesArray = properties.ToArray();
        for (int i = 0; i < propertiesArray.Length; i++)
        {
            var property = propertiesArray[i];
            var tsType = MapCSharpTypeToTypeScript(property.PropertyType);
            var isOptional = IsOptionalProperty(property);
            var optionalMarker = isOptional ? "?" : "";
            
            // Use single quotes for string types to match Prettier config
            var formattedType = tsType == "string" ? "string" : tsType;
            output.AppendLine($"    {ToCamelCase(property.Name)}{optionalMarker}: {formattedType};");
        }

        output.AppendLine("}");
        output.AppendLine();
    }

    private string MapCSharpTypeToTypeScript(Type type)
    {
        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return MapCSharpTypeToTypeScript(type.GetGenericArguments()[0]);
        }

        // Handle arrays
        if (type.IsArray)
        {
            var elementType = MapCSharpTypeToTypeScript(type.GetElementType()!);
            return $"{elementType}[]";
        }

        // Handle generic collections
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) || 
                genericDef == typeof(IEnumerable<>) || genericDef == typeof(ICollection<>))
            {
                var elementType = MapCSharpTypeToTypeScript(type.GetGenericArguments()[0]);
                return $"{elementType}[]";
            }
        }

        // Map primitive types
        return type.Name switch
        {
            "String" => "string",
            "Boolean" => "boolean", 
            "Int32" or "Int64" or "Double" or "Single" or "Decimal" => "number",
            "DateTime" or "DateTimeOffset" => "string", // ISO string format
            "Guid" => "string",
            _ => "any" // Fallback for complex types
        };
    }

    private bool IsOptionalProperty(PropertyInfo property)
    {
        // Check if the property type is nullable
        if (property.PropertyType.IsGenericType && 
            property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return true;
        }

        // Check for nullable reference types (C# 8+)
        var nullabilityInfo = new NullabilityInfoContext().Create(property);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            return input;

        return char.ToLower(input[0]) + input[1..];
    }
}