using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Extensions;
using System.ComponentModel;

namespace Hot;

public class HotAPIServer : SelfHostedService {
    private WebApplicationBuilder? _builder = null;
    private WebApplication? _app = null;

    /// <summary>
    /// Overwrite to define WebApplicationOptions
    /// </summary>
    /// <returns></returns>
    public virtual WebApplicationOptions? WebApplicationOptions() { return null; }

    ILogger Log = LogCreate("HotAPI");

    /// <summary>
    /// WebApplicationBuilder padrão. Deve ser configurado no método Configure()
    /// </summary>
    static readonly object builder_lock = new Object();

    public WebApplicationBuilder builder {
        get {
            lock (builder_lock) {
                if (_builder == null) {
                    _builder = Cria_Builder();
                }
                if (_app != null)
                    throw new Exception("Acesso ao builder após app ter sido criada.");
            }
            return _builder;
        }
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
                    if (builder == null)
                        throw new Exception("Erro ao criar o builder.");
                    _app = Cria_App();
                }
            }
            return _app;
        }
    }



    /// <summary>
    /// Filtro para Metadados de Biding do MVC
    /// </summary>
    public class RequiredBindingMetadataProvider : IBindingMetadataProvider {
        static bool SwaggerAutoBindRequired = Config["HotAPI:Builder:SwaggerAutoBindingRequired"]!.ExpandConfig().ToBool();
        void IBindingMetadataProvider.CreateBindingMetadata(BindingMetadataProviderContext context) {
            if (SwaggerAutoBindRequired) {
                var p = context.Key.ParameterInfo;
                if (p != null) {
                    if (p.HasDefaultValue == false)
                        context.BindingMetadata.IsBindingRequired = true;
                }
            }
        }
    }

    /// <summary>
    /// Filtro para Parâmetros do SwaggerUI
    /// </summary>
    class Opt_SwaggerParameterFilter : IParameterFilter {
        static bool SwaggerAutoBindRequired = Config["HotAPI:Builder:SwaggerAutoBindingRequired"]!.ExpandConfig().ToBool();
        void IParameterFilter.Apply(OpenApiParameter parameter, ParameterFilterContext context) {
            if (SwaggerAutoBindRequired) {

                if (parameter.Schema.Default == null)
                    parameter.Required = true;
            }
        }
    }

    class Opt_SwaggerDocumentFilter : IDocumentFilter {
        static bool SwaggerShowHotAPI = Config["HotAPI:Builder:SwaggerShowHotAPI"]!.ExpandConfig().ToBool();
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context) {
            if (!SwaggerShowHotAPI)
                swaggerDoc.Paths.Where(x => x.Key.StartsWith("/HotAPI/"))
                    .ToList().ForEach(x => swaggerDoc.Paths.Remove(x.Key));
        }
    }


    class Opt_SwaggerOperationFilter : IOperationFilter {
        static bool SwaggerAutoNullable = Config["HotAPI:Builder:SwaggerAutoNullable"]!.ExpandConfig().ToBool();
        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            if (SwaggerAutoNullable) {
                // Ajusta retorno de nullable
                var t = context.MethodInfo.ReturnType;
                bool return_nullable = (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));

                // O ajuste acima não vale para tipos complexos (retorno de classe com ? no final - nullável)
                MethodInfo method = context.MethodInfo;
                // -- ISSO A GLOBO NÃO MOSTRA!!!!!
                // 
                // Para saber se o método é nulável, deve verificar:  (descoberto no Copilot + depuração)
                // ((RuntimeParameterInfo) method.ReturnTypeCustomAttributes).Member.CustomAttributes.First().ConstructorArguments[0] == 2
                // Porém RuntimeParameterInfo não é acessível, então deve-se usar reflection para acessar o valor   
                try {
                    var x = method.ReturnTypeCustomAttributes.GetType().GetProperty("Member").GetValue(method.ReturnTypeCustomAttributes);
                    var x2 = ((IEnumerable)x.GetType().GetProperty("CustomAttributes").GetValue(x));
                    foreach (object x3 in x2) {
                        var x4 = ((IEnumerable)x3.GetType().GetProperty("ConstructorArguments").GetValue(x3));
                        foreach (object x5 in x4) {
                            var x6 = ((CustomAttributeTypedArgument)x5).Value;
                            if (((x6 as byte?) ?? 0) == 2) return_nullable = true;
                        }
                    }
                } catch (Exception) {
                }
                // ----

                foreach (var item in operation.Responses) {
                    foreach (var item1 in item.Value.Content) {
                        if (return_nullable)
                            item1.Value.Schema.Nullable = true;
                    }

                }
            }
        }
    }


    public class Opt_SwaggerNullableSchemaFilter : ISchemaFilter {
        static bool SwaggerAutoNullable = Config["HotAPI:Builder:SwaggerAutoNullable"]!.ExpandConfig().ToBool();

        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (SwaggerAutoNullable) {
                // Ajusta retorno de nullable
                if (context.Type.IsGenericType && context.Type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    schema.Nullable = true;
                }
            }
        }
    }

    ///// <summary>
    ///// Lista métodos encontrados
    ///// </summary>
    //public class ApiExplorerConvention : IActionModelConvention {
    //    static bool SwaggerShowHotAPI = Config["SwaggerShowHotAPI"].ToBool();
    //    public void Apply(ActionModel action) {
    //        Log.Create("HotAPI").LogInformation($"ApiExplorer: {action.ActionMethod}");
    //    }
    //}


    WebApplicationBuilder Cria_Builder() {
        WebApplicationBuilder b;

        WebApplicationOptions? AppOptions = WebApplicationOptions();
        b = (AppOptions is null) ? WebApplication.CreateBuilder() : WebApplication.CreateBuilder(AppOptions);

        b.Configuration.AddConfiguration(HotConfiguration.configuration);
        b.Services.AddLogging(HotLog.LoggingCreate);
        b.Services.AddSingleton<IApiDescriptionGroupCollectionProvider>(s => new HotApiDescriptionGroupCollectionProvider(s));

        if (Config["HotAPI:Builder:SwaggerGen"]!.ToBool()) {
            Action<SwaggerGenOptions>? optSwaggerGen = options => {
                if (Config["HotAPI:Builder:SwaggerGenXML"]!.ToBool()) {
                    var stream = Config.GetAsmStream("API.xml");
                    if (stream != null) {
                        options.IncludeXmlComments(() => new System.Xml.XPath.XPathDocument(stream), true);
                    } else {
                        string filename = Path.ChangeExtension(Config[ConfigConstants.ExecutableFullName]!, ".xml");
                        if (File.Exists(filename)) {
                            options.IncludeXmlComments(filename, true);
                        } else {
                            HotLog.log.Log.LogError("Documentação .xml não encontrada: " + filename);
                        };
                    }
                }
                if (Config["HotAPI:Builder:BindRequiredForNonDefault"]!.ToBool())
                    options.ParameterFilter<Opt_SwaggerParameterFilter>();

                if (Config["HotAPI:Builder:SwaggerResolveConflictingActions"]!.ToBool())
                    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                options.DocumentFilter<Opt_SwaggerDocumentFilter>();
                options.OperationFilter<Opt_SwaggerOperationFilter>();
                options.SchemaFilter<Opt_SwaggerNullableSchemaFilter>();
                options.UseAllOfForInheritance();
                options.UseAllOfToExtendReferenceSchemas();
                options.SupportNonNullableReferenceTypes();


                //options.SwaggerDoc(Config[ConfigConstants.Version], new OpenApiInfo {
                //    Version = Config[ConfigConstants.Version],
                //    Title = Config[ConfigConstants.ServiceDisplayName],
                //    Description = Config[ConfigConstants.ServiceDescription],
                //});
            };
            b.Services.AddSwaggerGen(optSwaggerGen);
        }

        Action<MvcOptions>? optMvc = o => {
            if (Config["HotAPI:Builder:BindRequiredForNonDefault"]!.ToBool())
                o.ModelMetadataDetailsProviders.Add(new RequiredBindingMetadataProvider());
            //o.Conventions.Add(new ApiExplorerConvention());
        };
        b.Services.AddMvc(optMvc);

        if (Config["HotAPI:Builder:Controllers"]!.ToBool())
            b.Services.AddControllers();

        if (Config["HotAPI:Builder:AddEndpointsApiExplorer"]!.ToBool())
            b.Services.AddEndpointsApiExplorer();

#if NET8_0_OR_GREATER
        if (Config["HotAPI:App:UseAuthentication"]!.ToBool())   // Se usa autenticação, adiciona CascadingAuthenticationState automaticamente
            b.Services.AddCascadingAuthenticationState();
#endif

        Config_Builder(b);

        return b;
    }

    WebApplication Cria_App() {
        WebApplication app = builder.Build();

        //var _apiDescriptionsProvider = app.Services.GetService<IApiDescriptionGroupCollectionProvider>();
        //var applicableApiDescriptions = _apiDescriptionsProvider!.ApiDescriptionGroups.Items;

        Config.__Set_ServiceProvider(app.Services, 675272);    // Seta provedor de serviços para os serviços da WebApplication ao invés do SelfHost

        string pathBase = Config["IgnorePrefix"] ?? "";
        if (pathBase != "") {
            app.UsePathBase(pathBase);
            //app.Use((context, next) => {
            //    context.Request.PathBase = new PathString(pathBase);
            //    return next();
            //});
        }

        Action<SwaggerUIOptions> optSwaggerUIOptions = option => {
            option.EnableDeepLinking();
        };

        if (Config["HotAPI:App:Swagger"]!.ToBool())
            app.UseSwagger();
        if (Config["HotAPI:App:SwaggerUI"]!.ToBool())
            app.UseSwaggerUI();
        if (Config["HotAPI:App:HttpsRedirection"]!.ToBool())
            app.UseHttpsRedirection();

        List<IFileProvider> fileProviders = new();
        if (Config["HotAPI:App:UsePhysicalStaticFiles"]!.ToBool()) {
            var hostEnvironment = app.Services.GetService<IHostEnvironment>();
            var path = Path.Combine(hostEnvironment!.ContentRootPath, "wwwroot");
            if (Directory.Exists(path))
                fileProviders.Add(new PhysicalFileProvider(path));
        }
        if (Config["HotAPI:App:UseEmbeddedStaticFiles"]!.ToBool()) {
            fileProviders.Add(new EmbeddedFileProvider(Config.GetAsmResource, Config.GetAsmResource.GetName().Name + ".wwwroot"));
        }
        if (fileProviders.Count > 0) {
            var compositeProvider = new CompositeFileProvider(fileProviders.ToArray());
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = compositeProvider,
                ServeUnknownFileTypes = true,
                DefaultContentType = "text/plain"
            });
        }

        if (Config["HotAPI:App:UseDeveloperExceptionPage"]!.ToBool())
            app.UseDeveloperExceptionPage();

        if (Config["HotAPI:App:UseAuthentication"]!.ToBool())
            app.UseAuthentication();

        if (Config["HotAPI:App:UseAuthorization"]!.ToBool()) {
            app.UseAuthorization();
            app.MapControllers().RequireAuthorization(); // --> Colocado pelo AddMvc()
        } else {
            app.MapControllers(); // --> Colocado pelo AddMvc()
        }

        app.MapGet("/HotAPI/version", version);
        app.MapGet("/HotAPI/infos", infos);
        app.MapGet("/HotAPI/routes", routes);
        app.MapPut("/HotAPI/autoupdate", AutoUpdate_ReceiveFile);

        Config_App(app);

        return app;
    }

    ~HotAPIServer() {
        Log.LogInformation("~HotAPI: Fechando aplicação");
        app?.DisposeAsync();
    }

    /// <summary>
    /// Método que pode ser implementado para configurar builder (opcional).
    /// Chamado antes de b.Build()
    /// </summary>
    public virtual void Config_Builder(WebApplicationBuilder builder) {
    }


    /// <summary>
    /// Método que pode ser implementado para configurar app (opcional).
    /// Chamado antes de app.Run()
    /// </summary>
    public virtual void Config_App(WebApplication app) {
    }


    [RequestSizeLimit(100_000_000)]
    /// <summary>
    /// Utilizado pelo recurso de 'autoupdate' da HotAPI.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task AutoUpdate_ReceiveFile(HttpContext context) {
        string configsecret = Config[ConfigConstants.Update.Secret]!;
        string secret = context.Request.Headers["UpdateSecret"].ToString() ?? "";

        if (configsecret != secret) {
            Log.LogError($"UpdateSecret inválido. IP: {context.IP_Origem()}");
            await context.ForbidAsync();
            await context.Response.WriteAsync("Não autorizado.");

        } else {   // Recebe arquivo atualizado e salva na pasta do executável (se não tiver permissão, não pode atualizar)
            long size = 0;
            string tmpfile = Path.GetDirectoryName(Config[ConfigConstants.ExecutableFullName]) + Path.DirectorySeparatorChar + Path.GetRandomFileName();
            try {
                await using var f = File.Create(tmpfile);
                await context.Request.Body.CopyToAsync(f);
                size = f.Length;
                f.Close();
            } catch (Exception e) {
                Log.LogError(e, "Erro ao salvar arquivo da atualização \"" + tmpfile + "\".");
            }

            // Se salvou o arquivo corretamente
            if (size > 0) {
                await context.Response.WriteAsync("Atualização recebida.");
                Hot.AutoUpdate.AutoUpdate_Process(tmpfile);
            } else {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Erro ao processar atualização.");
            }
        }
    }



    public override Task StartAsync(CancellationToken cancellationToken) {
        base.StartAsync(cancellationToken);

        if (app == null)
            throw new Exception("Erro ao criar app");

        Task r = app.StartAsync(cancellationToken);

        var s = Config["HotAPI:DevelopmentLaunchUrl"];
        if (s != null && Config[ConfigConstants.Environment] == "Development") {
            StartURL(s);
        }

        return r;
    }


    public override Task StopAsync(CancellationToken cancellationToken) {
        return app.StopAsync(cancellationToken);
    }


    /// <summary>
    /// Retorna com a lista de endpoints aceitos.
    /// </summary>
    /// <param name="endpointSources"></param>
    /// <returns></returns>
    public string routes(IEnumerable<EndpointDataSource> endpointSources) {
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
            String? rota = (endpoint as RouteEndpoint)?.RoutePattern.RawText.Envelop("(", ")");
            s += $"{endpoint} {rota}=> {httpMethodsMetadata?.HttpMethods.Dump().TrimEnd('\n')}\n";

            // There are many more metadata types available...
        }
        return s; //.Replace(Environment.NewLine, "<br>");
    }

    /// <summary>
    /// Devolve a versão atual da aplicação e bibliotecas Hot.
    /// </summary>
    /// <returns></returns>
    public string version() => Hot.AutoUpdate.Version();

    /// <summary>
    /// Devolve informações relativas à aplicação em execução.
    /// </summary>
    /// <returns></returns>
    public string infos() => Config.Infos();


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