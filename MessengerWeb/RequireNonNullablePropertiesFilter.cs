using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MessengerWeb;

// The frontend generates its API types from Swagger. Without "required" every field of
// every result would be optional there, although the JSON always carries non-nullable ones.
public sealed class RequireNonNullablePropertiesFilter : ISchemaFilter
{
    private readonly NullabilityInfoContext _nullability = new();

    public void Apply(
        IOpenApiSchema schema,
        SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema objectSchema || objectSchema.Properties == null)
        {
            return;
        }

        var properties = context.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in objectSchema.Properties.Keys)
        {
            if (!properties.TryGetValue(name, out var property)
                || _nullability.Create(property).ReadState != NullabilityState.NotNull)
            {
                continue;
            }

            objectSchema.Required ??= new HashSet<string>();
            objectSchema.Required.Add(name);
        }
    }
}
