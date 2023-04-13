using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.SwaggerGen;

public static class Mod_ExtensionsInternals {
    // -- FROM ApiDescriptionExtensions.cs,63
    internal static string RelativePathSansParameterConstraints(this ApiDescription apiDescription) {
        var routeTemplate = TemplateParser.Parse(apiDescription.RelativePath);
        var sanitizedSegments = routeTemplate
            .Segments
            .Select(s => string.Concat(s.Parts.Select(p => p.Name != null ? $"{{{p.Name}}}" : p.Text)));
        return string.Join("/", sanitizedSegments);
    }
    //--------

    // -- FROM ApiParameterDescriptionExtensions.cs,80

    [Obsolete("Use ParameterInfo(), PropertyInfo() and CustomAttributes() extension methods instead")]
    internal static void GetAdditionalMetadata(
this ApiParameterDescription apiParameter,
ApiDescription apiDescription,
out ParameterInfo parameterInfo,
out PropertyInfo propertyInfo,
out IEnumerable<object> parameterOrPropertyAttributes) {
        parameterInfo = apiParameter.ParameterInfo();
        propertyInfo = apiParameter.PropertyInfo();
        parameterOrPropertyAttributes = apiParameter.CustomAttributes();
    }

    internal static bool IsFromPath(this ApiParameterDescription apiParameter) {
        return (apiParameter.Source == BindingSource.Path);
    }

    internal static bool IsFromBody(this ApiParameterDescription apiParameter) {
        return (apiParameter.Source == BindingSource.Body);
    }

    internal static bool IsFromForm(this ApiParameterDescription apiParameter) {
        var source = apiParameter.Source;
        var elementType = apiParameter.ModelMetadata?.ElementType;

        return (source == BindingSource.Form || source == BindingSource.FormFile)
            || (elementType != null && typeof(IFormFile).IsAssignableFrom(elementType));
    }
}
