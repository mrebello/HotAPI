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
