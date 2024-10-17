using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.IdentityModel.Tokens;

namespace Hot;

public class HotApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider {
    private readonly IServiceProvider _serviceProvider;
    private readonly Type _type;
    private IApiDescriptionGroupCollectionProvider? _defaultModelProvider;
    private IApiDescriptionGroupCollectionProvider DefaultModelProvider => _defaultModelProvider
                                                                  ??= _serviceProvider.GetServices<IApiDescriptionGroupCollectionProvider>()
                                                                                      .First(x => x.GetType() == _type);

    ApiDescriptionGroupCollection? groupCollection;

    public HotApiDescriptionGroupCollectionProvider(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
        // Microsoft.AspNetCore.Mvc.ApplicationModels.DefaultApplicationModelProvider;
        _type = Type.GetType("Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroupCollectionProvider, Microsoft.AspNetCore.Mvc.ApiExplorer")!;
        Debug.Assert(_type != null);
    }

    public ApiDescriptionGroupCollection ApiDescriptionGroups => groupCollection ??= novo();

    ApiDescriptionGroupCollection novo() {
        ApiDescriptionGroupCollection g = DefaultModelProvider.ApiDescriptionGroups;

        string SwaggerDefaultMethod = Config["HotAPI:Builder:SwaggerDefaultMethod"]!;

        var v = g.Version;
        List<ApiDescriptionGroup> l = new();
        foreach (var item in g.Items) {
            List<ApiDescription> lad = new();
            foreach (var ad in item.Items) {
                if (ad.HttpMethod == null && !SwaggerDefaultMethod.IsNullOrEmpty()) ad.HttpMethod = SwaggerDefaultMethod;

                lad.Add(ad);
            }

            l.Add(new(item.GroupName, lad));
        }

        return new(l, v);
    }

}
