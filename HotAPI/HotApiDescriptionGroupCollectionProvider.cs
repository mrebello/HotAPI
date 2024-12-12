using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;

namespace Hot;

public class HotApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider {
    private IApiDescriptionGroupCollectionProvider DefaultModelProvider;
//    private readonly IServiceProvider _serviceProvider;
//    private readonly Type _type;
//    private IApiDescriptionGroupCollectionProvider? _defaultModelProvider;
//    private IApiDescriptionGroupCollectionProvider DefaultModelProvider {
//        get {
//        if (_defaultModelProvider is null) {
//            var x = _serviceProvider.GetServices<IApiDescriptionGroupCollectionProvider>();
//            _defaultModelProvider = x.FirstOrDefault(x => x.GetType() == _type);

    //            if (_defaultModelProvider is null) {
    //                var serviceCollection = new ServiceCollection();
    //                var serviceProvider = serviceCollection.BuildServiceProvider();
    //                _defaultModelProvider = serviceProvider.GetService<IApiDescriptionGroupCollectionProvider>();

    //                if (_defaultModelProvider == null) throw new Exception("ApiDescriptionGroups não encontrado");
    //            }
    //        }
    //        return _defaultModelProvider;
    //    }
    //}

    ApiDescriptionGroupCollection? groupCollection;

    public HotApiDescriptionGroupCollectionProvider(IServiceProvider serviceProvider,
                IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
                IEnumerable<IApiDescriptionProvider> apiDescriptionProviders) {
        //var x2 = serviceProvider.GetServices<IApiDescriptionGroupCollectionProvider>();
        
        IApiDescriptionGroupCollectionProvider?  x = null;
        
        //  como pegar o serviço definido anteriormente desse mesmo tipo?

        if (x==null) {
            x = new ApiDescriptionGroupCollectionProvider(actionDescriptorCollectionProvider, apiDescriptionProviders);
        }
        DefaultModelProvider = x;
        
        //_serviceProvider = serviceProvider;
        //// Microsoft.AspNetCore.Mvc.ApplicationModels.DefaultApplicationModelProvider;
        //_type = Type.GetType("Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroupCollectionProvider, Microsoft.AspNetCore.Mvc.ApiExplorer")!;
        //Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroupCollectionProvider t = new ApiDescriptionGroupCollectionProvider(actionDescriptorCollectionProvider, apiDescriptionProviders);
        //_defaultModelProvider = t;
        //Debug.Assert(_type != null);
    }

    public ApiDescriptionGroupCollection ApiDescriptionGroups => groupCollection ??= novo();

    ApiDescriptionGroupCollection novo() {
        ApiDescriptionGroupCollection g = DefaultModelProvider.ApiDescriptionGroups;
        if (g == null) throw new Exception("***ApiDescriptionGroups não encontrado***");

        string SwaggerDefaultMethod = Config["HotAPI:Builder:SwaggerDefaultMethod"]!;
        var SwaggerDefaultParameterFrom = Config["HotAPI:Builder:SwaggerDefaultParameterFrom"] switch {
            null => null,
            "Form" => BindingSource.Form,
            "Query" => BindingSource.Query,
            "Header" => BindingSource.Header,
            _ => throw new Exception("HotAPI:Builder:SwaggerDefaultParameterFrom deve ser Form, Query ou Header")
        };

        var v = g.Version;
        List<ApiDescriptionGroup> l = new();
        foreach (var item in g.Items) {
            List<ApiDescription> lad = new();
            foreach (var ad in item.Items) {
                if (SwaggerDefaultParameterFrom != null) {
                    foreach (var p in ad.ParameterDescriptions) {
                        if (p.Source != BindingSource.Query) continue;
                        var t = (DefaultModelMetadata)p.ModelMetadata;
                        var at = (ModelAttributes)(t).Attributes;
                        if (at.ParameterAttributes!.Any(x => x.GetType().Name.StartsWith("From"))) continue;
                        p.Source = SwaggerDefaultParameterFrom;
                        p.BindingInfo!.BindingSource = SwaggerDefaultParameterFrom;
                    }
                }
                if (ad.HttpMethod == null && !SwaggerDefaultMethod.IsNullOrEmpty()) ad.HttpMethod = SwaggerDefaultMethod;
                lad.Add(ad);
            }

            l.Add(new(item.GroupName, lad));
        }

        return new(l, v);
    }

}
