# Stock Quote Alert

Aplicativo de linha de comando em .NET 9 que monitora o preço de um ativo via BRAPI e dispara alertas por e-mail quando o valor atinge os limites configurados.

## Pré-requisitos
- [.NET SDK 9.0.306](https://dotnet.microsoft.com/download) (Caso deseja usar sem Docker)
- Chave de API válida da [BRAPI](https://brapi.dev/)
- Servidor SMTP (próprio ou de um provedor) com credenciais de envio

## Configuração

### 1. Defina sua chave da BRAPI
Edite o arquivo `Program.cs` e substitua o valor de `BRAPI_KEY` pela sua chave pessoal:
```csharp
private static string BRAPI_KEY = "YOUR_BRAPI_KEY";
```
Sem esse passo o aplicativo não consiguirá realizar requisições à API da BRAPI para obter informações sobre o preço do ativo.

### 2. Preencha o `config.json`
Forneça os dados do seu servidor SMTP no arquivo `config.json` na raiz do projeto:
```json
{
  "smtpHost": "smtp.seuprovedor.com",
  "smtpPort": 587,
  "ussSsl": false,
  "username": "usuario",
  "password": "senha",
  "fromEmail": "alertas@seu-dominio.com",
  "toEmail": "destinatario@exemplo.com"
}
```
Campos essenciais:
- `smtpHost` / `smtpPort`: endereço e porta do servidor.
- `ussSsl`: informe `true` se sua porta exigir SSL/TLS imediato.
- `username` / `password`: credenciais de envio.
- `fromEmail`: remetente exibido no alerta.
- `toEmail`: e-mail que receberá as notificações.

Sem essas configurações o aplicativo não conseguirá enviar notificações por e-mail.

### 3. Construir o projeto
```bash
docker build -t stock-quote-alert .
```

Após esse passo o projeto está pronto para ser executado.

## Execução
Utilize o `docker run stock-quote-alert` passando o ticker e os limites desejados:
```bash
docker run stock-quote-alert PETR4 32.1 30 -c config.json
```
Argumentos (na ordem):
1. `STOCK_TICKER`: código do ativo na B3/BRAPI.
2. `SELL_PRICE`: preço que dispara alerta de venda.
3. `BUY_PRICE`: preço que dispara alerta de compra.
4. `-c, --config`: caminho para o arquivo de configuração SMTP (Opcional, caso não seja informado o aplicativo irá tentar achar um arquivo config.json na mesma pasta que o executável).

Execute `docker run stock-quote-alert --help` para ver o guia completo.

## Fluxo de funcionamento
1. O serviço consulta a BRAPI a cada 5 minutos.
2. Quando o preço atual é menor ou igual ao limite de compra ou maior ou igual ao limite de venda, um e-mail é enviado usando os dados do `config.json`.
3. O processo segue em execução até que você pressione `Ctrl+C`.

## Decisões de projeto
O sistema foi projetado para ser simples, visto que apenas é um aplicativo de linha de comando, porém possui uma arquitetura facilmente expansivel.
Por meio da utilização de interfaces, o sistema é facilmente expansivel para novos tipos de notificações, como SMS, e também é expansivel para outros tipos de provedores de preços da bolsa, além do BRAPI, bastando implementar novas classes que implementam as interfaces INotificationSender e IPriceProvider, respectivamente.
Além disso se decidiu implementar um message bus que conecta o serviço que realiza as consultas à BRAPI com os handlers que envia os e-mails. A motivação para essa escolha vem para alinhar com o objetivo de deixar a arquitetura do projeto expansivel além de fazer uma clara separação de responsabilidades, visto que não é responsabilidade da aplicação o envio de notifacações, com isso sendo feito por meio de serviços externos, como o servidor SMTP. Atualmente a implementação do message bus é in memory, porém pode ser facilmente trocada bastando implementar uma nova classe que segue a interface IEventBus com o serviço de fila de mensagens desejado.
Numa visão geral, o projeto é encabeçado por Program.cs, que chama as classes necessárias responsaveis pela comunicação pela linha de comando, após validar os argumentos passados, cria uma instância do serviço junto com todos os objetos que precisam ser inicializados anteriormente, como o objeto PriceProvider, responsável pelo acesso do preços do ativo.
O projeto é todo composto por metódos assíncronos para se aproveitar melhor do tempo da CPU, visto que dependendo da frequência escolhida para o pooling, o sistema estará ocioso na maioria do tempo. Dentro do serviço é realizdo a inicialização do message bus, passando os handlers necessários para os eventos decididos, que basicamente são de notificações de compra e venda, além de um evento para notificar erros. Após é inicializado o pooling que fica verificando continuamente o preço do ativo e caso o preço alcança algum dos limites informados no início da aplicação, cria um evento que é enviado pelo message bus para os handlers de notificação, que enviam as notificações para os destinatários usando email, porém facilmente substituivel por outros serviços de notificação, como SMS, push notifications, etc.
Usando tasks assíncronas e o message bus, o sistema só envia o email num tempo que for ideal, visto que por mais que o alerta seja importante, por conta da utilização de serviços externos para realizar isso, não é ideal travar o sistema esperando pela realização da notificação caso fosse implementado usando soluções síncronas.
Sobre a utilização do BRAPI, essa é uma api de preços da bolsa brasileira, que possui simples utilização além de um plano gratuito. Por conta desse plano gratuito, só é possível obter dados com atualização de 30 minutos, porém dentro do projeto decidiu utilizar um intervalo de 5 minutos para pooling para demonstrar a funcionalidade do sistema como a notificação por e-mail e que o sistema é capaz de verificar continuamente o preço do ativo.
Para teste do projeto foi utilizado o serviço de e-mail transacionais do Brevo, que disponibiliza um generoso plano gratuito para servidores SMTP que mais importante que aceita emails de qualquer domínio, não precisando ser domínios comerciais.
