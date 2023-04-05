using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Hot {

    public class HotAPIServer : SelfHostedService {
        private WebApplicationBuilder? _builder = null;
        private WebApplication? _app = null;

        ILogger L = Log.Create("HotAPI");

        /// <summary>
        /// WebApplicationBuilder padrão. Deve ser configurado no método Configure()
        /// </summary>
        public WebApplicationBuilder builder {
            get {
                if (_builder == null) {
                    _builder = Cria_Builder();
                }
                if (_app != null) throw new Exception("Acesso ao builder após app ter sido criada.");
                return _builder;
            }
        }


        /// <summary>
        /// Filtro para Metadados de Biding do MVC
        /// </summary>
        public class RequiredBindingMetadataProvider : IBindingMetadataProvider {
            void IBindingMetadataProvider.CreateBindingMetadata(BindingMetadataProviderContext context) {
                var p = context.Key.ParameterInfo;
                if (p != null) {
                    if (p.HasDefaultValue == false)
                        context.BindingMetadata.IsBindingRequired = true;
                }
            }
        }

        /// <summary>
        /// Filtro para Parâmetros do SwaggerUI
        /// </summary>
        class Opt_SwaggerParameterFilter : IParameterFilter {
            void IParameterFilter.Apply(OpenApiParameter parameter, ParameterFilterContext context) {
                if (parameter.Schema.Default == null) parameter.Required = true;
            }
        }


        /// <summary>
        /// Lista métodos encontrados
        /// </summary>
        public class ApiExplorerConvention : IActionModelConvention {
            public void Apply(ActionModel action) {
                Log.Create("HotAPI").LogInformation($"ApiExplorer: {action.ActionMethod}");
            }
        }


        WebApplicationBuilder Cria_Builder() {
            WebApplicationBuilder b;

            b = WebApplication.CreateBuilder(Environment.GetCommandLineArgs());
            b.Configuration.AddConfiguration(HotConfiguration.configuration);
            b.Services.AddLogging(HotLog.LoggingCreate);

            if (Config["HotAPI:Builder:SwaggerGen"].ToBool()) {
                Action<SwaggerGenOptions>? optSwaggerGen = options => {
                    if (Config["HotAPI:Builder:SwaggerGenXML"].ToBool()) {
                        string filename = Path.ChangeExtension(Config[ConfigConstants.ExecutableFullName], ".xml");
                        if (File.Exists(filename)) {
                            options.IncludeXmlComments(filename, true);
                        } else {
                            Log.LogError("Documentação .xml não encontrada: " + filename);
                        };
                    }
                    if (Config["HotAPI:Builder:BindRequiredForNonDefault"].ToBool())
                        options.ParameterFilter<Opt_SwaggerParameterFilter>();

                    if (Config["HotAPI:Builder:SwaggerResolveConflictingActions"].ToBool())
                        options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                    //options.SwaggerDoc(Config[ConfigConstants.Version], new OpenApiInfo {
                    //    Version = Config[ConfigConstants.Version],
                    //    Title = Config[ConfigConstants.ServiceDisplayName],
                    //    Description = Config[ConfigConstants.ServiceDescription],
                    //});
                };
                b.Services.AddSwaggerGen(optSwaggerGen);
            }

            Action<MvcOptions>? optMvc = o => {
                if (Config["HotAPI:Builder:BindRequiredForNonDefault"].ToBool())
                    o.ModelMetadataDetailsProviders.Add(new RequiredBindingMetadataProvider());
            };
            b.Services.AddMvc(optMvc);

            if (Config["HotAPI:Builder:Controllers"].ToBool())
                b.Services.AddControllers();

            if (Config["HotAPI:Builder:AddEndpointsApiExplorer"].ToBool())
                b.Services.AddEndpointsApiExplorer();

            return b;
        }

        /// <summary>
        /// WebApplication criada a partir do builder automaticamente no primeiro acesso.
        /// Deve ser configurada após as configurações do builder no método Configure()
        /// </summary>
        static readonly object app_lock = new Object();
        public WebApplication app {
            get {
                lock (app_lock) {
                    if (_app == null) {
                        if (builder == null) throw new Exception("Erro ao criar o builder.");
                        _app = Cria_App();

                        Initialize();
                    }
                }
                return _app;
            }
        }

        WebApplication Cria_App() {
            WebApplication app = builder.Build();

            //var _apiDescriptionsProvider = app.Services.GetService<IApiDescriptionGroupCollectionProvider>();
            //var applicableApiDescriptions = _apiDescriptionsProvider!.ApiDescriptionGroups.Items;

            Config.__Set_ServiceProvider(app.Services, 675272);    // Seta provedor de serviços para os serviços da WebApplication ao invés do SelfHost

            Action<SwaggerUIOptions> optSwaggerUIOptions = option => {
                option.EnableDeepLinking();
            };

            if (Config["HotAPI:App:Swagger"].ToBool())
                app.UseSwagger();
            if (Config["HotAPI:App:SwaggerUI"].ToBool())
                app.UseSwaggerUI();
            if (Config["HotAPI:App:HttpsRedirection"].ToBool())
                app.UseHttpsRedirection();

            app.MapControllers(); // --> Colocado pelo AddMvc()

            app.MapGet("/version", Hot.AutoUpdate.Version);
            app.MapGet("/infos", Hot.AutoUpdate.Infos);
            app.MapGet("/routes", ListRoutes);

            return app;
        }

        ~HotAPIServer() {
            L.LogInformation("~HotAPI: Fechando aplicação");
            app?.DisposeAsync();
        }

        /// <summary>
        /// Método que pode ser implementado para configurar builder (opcional) e app (obrigatório).
        /// </summary>
        public virtual void Initialize() {
        }


        public virtual void Version(HttpListenerContext context) {
            context.Response.Send(Config[HotConfiguration.ConfigConstants.AppName] + '\t' + Config[HotConfiguration.ConfigConstants.Version]);
        }

        public virtual void Config_Changed(object state) {
            //var new_Prefixes = Prefixes_from_config();
            //if (!prefixes.SequenceEqual(new_Prefixes)) {
            //    Log.LogWarning("Configuração de Prefixes foi alterada. Reiniciando Listener.");
            //    prefixes = new_Prefixes;
            //    await StopAsync(default);
            //    await StartAsync(default);
            //}

            //var new_IgnorePrefix = IgnorePrefix_from_config();
            //if (new_IgnorePrefix != ignorePrefix) {
            //    Log.LogWarning("ignorePrefix foi alterado.");
            //    ignorePrefix = new_IgnorePrefix;
            //}
            Log.LogInformation("RECONFIGURADO");
        }

        async void Config_Changed_trap(object state) {
            Config_Changed(state);
            ((IConfiguration)Config).GetReloadToken().RegisterChangeCallback(Config_Changed_trap, default);
        }


        public override Task StartAsync(CancellationToken cancellationToken) {
            ((IConfiguration)Config).GetReloadToken().RegisterChangeCallback(Config_Changed_trap, default);
            if (app == null)
                throw new Exception("Erro ao criar app");

            Task r = app.StartAsync(cancellationToken);

            var s = Config["HotAPI:DevelopmentLaunchUrl"];
            if (s != null && HotConfiguration.config.Config[HotConfiguration.ConfigConstants.Environment] == "Development") {
                StartURL(s);
            }

            return r;
        }


        public override Task StopAsync(CancellationToken cancellationToken) {
            return app.StopAsync(cancellationToken);
        }


        public string ListRoutes(IEnumerable<EndpointDataSource> endpointSources) {
            var s = "";
            var endpoints = endpointSources.SelectMany(es => es.Endpoints);
            foreach (var endpoint in endpoints) {
                // s += endpoint.Dump();
                //if (endpoint is RouteEndpoint routeEndpoint) {
                //    s += routeEndpoint.RoutePattern.RawText + Environment.NewLine;
                //    s += routeEndpoint.RoutePattern.PathSegments + Environment.NewLine;
                //    s += routeEndpoint.RoutePattern.Parameters + Environment.NewLine; 
                //    s += routeEndpoint.RoutePattern.InboundPrecedence + Environment.NewLine;
                //    s += routeEndpoint.RoutePattern.OutboundPrecedence + Environment.NewLine;
                //}

                var routeNameMetadata = endpoint.Metadata.OfType<Microsoft.AspNetCore.Routing.RouteNameMetadata>().FirstOrDefault();

                var httpMethodsMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault();
                //s += httpMethodsMetadata?.HttpMethods + Environment.NewLine; // [GET, POST, ...]

                s += $"{endpoint} => {httpMethodsMetadata?.HttpMethods.Dump().TrimEnd('\n')}\n";

                // There are many more metadata types available...
            }
            return s; //.Replace(Environment.NewLine, "<br>");
        }
    }
    //public class ResponseTime {
    //    RequestDelegate _next;

    //    public ResponseTime(RequestDelegate next) {
    //        _next = next;
    //    }

    //    public async Task Invoke(HttpContext context) {
    //        var sw = new Stopwatch();
    //        sw.Start();
    //        await _next.Invoke(context);
    //        sw.Stop();
    //        Debug.WriteLine("Tempo: " + sw.ElapsedMilliseconds);
    //    }
    //}

}