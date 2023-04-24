# HotAPI

A biblioteca faz uso dos recursos da biblioteca <a href="https://github.com/mrebello/Hot">Hot</a>, adicionando uma classe para a geração de APIs Web (via Swashbuckle).

Principais recursos implementados:

- Adição de "BindRequired" para todos os parâmetros sem valor default automaticamente
- Utilização do Swashbuckle para gerar a UI e a documentação da API
- Opção de não-exigência de [HttpGET] através de um 'mod' no Swashbuckle
- Documentação através da documentação padrão embutida no fonte do Visual Studio
- Opções de servidor através de arquivo de configuração
- Rotina para atualização facilitada da aplicação em produção
- Implementação usando os padrões do ASP.NET core

## BindRequired
Para fazer o BindRequired de forma 'automática', foi utilizado um *IParameterFilter* que verifica a existência de valor default no parâmetro.

Com isso, a API exposta fica com um comportamento idêntico à assinatura do método declarado no C#.

## Documentação

Para a geração da documentação é utilizada a documentação de API do próprio Visual Studio.

Porém, para facilitar o _deploy_ da aplicação, o .xml com a desdcrição da API fica como recurso inserido no executável (ou DLL).

## [HttpGET] como _default_

Uma opção no arquivo de configuração da aplicação define se a HotAPI assume o método GET como padrão para os métodos que não estiverem com o atributo, fazendo com que a API possa ser gerada a partir de classes que não dependem de atributos específicos do ASP.NET core.

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
          "SwaggerDefaultGET": true // Assume método GET para a UI caso método não possua atributo de método http definido
        },
        "App": {
          "Swagger": true,
          "SwaggerUI": true,
          "HttpsRedirection": false
        }
        // "DevelopmentLaunchUrl": "http://127.0.0.1:11051/swagger" // Página a ser aberta se estiver em ambiente de desenvolvimento ao iniciar a aplicação
      }
    }

Sobre as configurações:

- *BindRequiredForNonDefault*: Como explicado acima, implementado com um filtro na ApiExplorer
- *SwaggerGenXML*: Procura pelo arquivo API.xml embutido na aplicação. Se não for encontrado, procura pelo arquivo .xml com o mesmo nome e mesma pasta do executável. Se não encontrar, gera um erro.
- *SwaggerShowHotAPI*: Esconde da UI do swagger as APIs internas da HotAPI (version, infos e routes). O valor padrão é _true_ caso seja ambiente _Development_.
- *SwaggerResolveConflictingActions*: Se _true_, usa "apiDescriptions => apiDescriptions.First()".
- *SwaggerDefaultGET*: Ao invés de gerar o erro padrão do Swagger para método HTTP não definido, assume método GET apenas para a UI.
    Para poder implementar essa opção, foram feitas algumas alterações utilizando o fonte do Swashbuckle, sendo essas alterações incluídas dentro da DLL da HotAPI.
    Apenas ativa essas alterações caso essa opção esteja em _true_.
- *DevelopmentLaunchUrl*: Se definido, caso esteja em ambiente _Development_, dispara o browser padrão do sistema (linux/windows) com a ulr definida.

