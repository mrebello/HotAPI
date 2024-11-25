# HotAPI

A biblioteca faz uso dos recursos da biblioteca <a href="https://github.com/mrebello/Hot">Hot</a>, adicionando uma classe para a geração de APIs Web (via Swashbuckle).

Principais recursos implementados:

- Adição de "BindRequired" para todos os parâmetros sem valor default automaticamente
- Utilização do Swashbuckle para gerar a UI e a documentação da API
- Definição do método default (GET/POST/PUT) no arquivo de configuração para métodos que não possuem o atributo definido [HttpGET]/[HttpPOST]/[HttpPUT].
- Documentação através da documentação padrão embutida no fonte do Visual Studio
- Opções de servidor através de arquivo de configuração
- Rotina para atualização facilitada da aplicação em produção
- Implementação usando os padrões do ASP.NET core

## BindRequired
Para fazer o BindRequired de forma 'automática', foi utilizado um *IParameterFilter* que verifica a existência de valor default no parâmetro.

Com isso, a API exposta fica com um comportamento idêntico à assinatura do método declarado no C# (junto com AutoNullable).

## AutoNullable
Versão nova do Swagger não faz o retorno automático de nulo para tipos nuláveis.
Foi adicionado um filtro para adicionar o IsNullable para os métodos que retornam tipos nuláveis.

Com isso, a API exposta fica com um comportamento idêntico à assinatura do método declarado no C# (junto com BindRequired).

(Funcionando apenas para os típos primitivos)

## SwaggerDefaultParameterFrom
O padrão do .NET para parâmetros que não possuem a origem do parâmetro é [FromQuery] para os tipos primitivos.
Esta opção troca o padrão para [FromForm] ou outro ("Form", "Query" ou "Header" aceitos).

## Documentação

Para a geração da documentação é utilizada a documentação de API do próprio Visual Studio.

Porém, para facilitar o _deploy_ da aplicação, o .xml com a desdcrição da API fica como recurso inserido no executável (ou DLL).

## [HttpGET]/[HttpPOST]/[HttpPUT] como _default_

Uma opção no arquivo de configuração da aplicação define se a HotAPI assume o método GET, POST ou PUT como padrão para os métodos que não estiverem com o atributo, fazendo com que a API possa ser gerada a partir de classes que não dependem de atributos específicos do ASP.NET core.

## Expansibilidade

A HotAPI foi construída em cima da arquitetura do ASP.NET core, permitindo que a aplicação se utilize de todos os recursos disponíveis sem limitar aos recursos implementados na HotAPI.

# Configuração

Opções disponíveis para a HotAPI (com os valores defaults embutidos na DLL)

    {
      // ==================
      // Opções para HotAPI
      // ------------------
      "HotAPI": {
        "Builder": {
          "BindRequiredForNonDefault": true, // Adiciona "BindRequired" para parâmetros sem valor default
          "AddEndpointsApiExplorer": true,
          "Controllers": true,
          "SwaggerGen": true,
          "SwaggerGenXML": true, // Gera documentação baseada nos metadados do código
          "SwaggerShowHotAPI": "%(IsDevelopment)%", // Se deve mostrar os endpoints internos da HotAPI
          "SwaggerResolveConflictingActions": true, // Usa options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
          "SwaggerDefaultMethod": "POST" // Método default ("GET","PUT" ou "POST". Se vazio, não assume default) para a UI caso método não possua atributo de método http definido
          "SwaggerAutoBindingRequired": true, // define IsBindedRequired para parâmetros que não possuem valor default
          "SwaggerAutoNullable": true, // define IsNullable para métodos que possuem retorno nullable
          "SwaggerDefaultParameterFrom": "Form" // Define FromForm como padrão de bind para parâmetros de tipos simples (ao invés do padrão FromQuery. Pode ser "Form", "Query" ou "Header"
        },
        "App": {
          "Swagger": true,
          "SwaggerUI": true,
          "UsePhysicalStaticFiles": true, // Lê recursos estáticos de arquivos na pasta wwwroot da aplicação
          "UseEmbeddedStaticFiles": true, // Lê recursos estáticos embuticos na pasta wwwroot da aplicação
          "HttpsRedirection": false,
          "UseAuthentication": false,
          "UseAuthorization": false,
          "UseDeveloperExceptionPage":  true
        }
        // "DevelopmentLaunchUrl": "http://127.0.0.1:11051/swagger" // Página a ser aberta se estiver em ambiente de desenvolvimento ao iniciar a aplicação
      }
    }

Sobre as configurações:

- *BindRequiredForNonDefault*: Como explicado acima, implementado com um filtro na ApiExplorer
- *SwaggerGenXML*: Procura pelo arquivo API.xml embutido na aplicação. Se não for encontrado, procura pelo arquivo .xml com o mesmo nome e mesma pasta do executável. Se não encontrar, gera um erro.
- *SwaggerShowHotAPI*: Esconde da UI do swagger as APIs internas da HotAPI (version, infos e routes). O valor padrão é _true_ caso seja ambiente _Development_.
- *SwaggerResolveConflictingActions*: Se _true_, usa "apiDescriptions => apiDescriptions.First()".
- *DevelopmentLaunchUrl*: Se definido, caso esteja em ambiente _Development_, dispara o browser padrão do sistema (linux/windows) com a ulr definida.

