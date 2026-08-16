# Arquitetura — MyMarketPlace

Documento de decisões. Para *como rodar*, veja o [README](README.md); para *como funciona cada classe*, os comentários `///` no próprio código.

Aqui está o **porquê**: o que foi escolhido, o que foi descartado e qual o preço de cada decisão.

---

## Índice

1. [Por que microsserviços (e quando não valeria a pena)](#1-por-que-microsserviços)
2. [Clean Architecture: a regra de dependência](#2-clean-architecture)
3. [Comunicação: síncrona x assíncrona](#3-comunicação-entre-serviços)
4. [O padrão Outbox](#4-o-padrão-outbox)
5. [Saga por coreografia](#5-saga-por-coreografia)
6. [Idempotência](#6-idempotência)
7. [Dados: um banco por serviço](#7-dados)
8. [Cache](#8-cache)
9. [Resiliência](#9-resiliência)
10. [Segurança](#10-segurança)
11. [Observabilidade](#11-observabilidade)
12. [Testes](#12-testes)
13. [Limitações conhecidas](#13-limitações-conhecidas)

---

## 1. Por que microsserviços

**Resposta honesta: para um marketplace deste tamanho, um monólito modular seria a escolha certa.**

Microsserviços custam caro — consistência eventual, latência de rede, depuração distribuída, oito pipelines de deploy. Esse custo se paga quando existem times independentes, requisitos de escala muito diferentes por domínio ou necessidade de isolamento de falha.

Este projeto adota microsserviços porque o objetivo é **demonstrar o domínio dessas técnicas**. Mas as escolhas seguem o que faria sentido num sistema real:

| Serviço | Por que separado | Se fosse decisão de negócio |
|---|---|---|
| Identity | Domínio de segurança; ciclo de release próprio | Justifica-se |
| Catalog | Leitura pesada, escala muito diferente do resto | Justifica-se |
| Cart | Dado volátil, alta frequência de escrita, sem valor histórico | Justifica-se |
| Order | Núcleo transacional do negócio | Justifica-se |
| Payment | Isolamento de integração externa e de PCI-DSS | Justifica-se |
| Inventory | Concorrência alta em recurso escasso | Justifica-se |
| Notification | Trabalho em background, tolera atraso | Poderia ser um worker do monólito |

**Sinal de alerta que não existe aqui:** nenhum serviço precisa de outro para responder a uma requisição de leitura. Se separar exigisse três chamadas em cadeia para montar uma tela, o corte estaria errado — seria um monólito distribuído, com todas as desvantagens e nenhuma vantagem.

---

## 2. Clean Architecture

### A regra de dependência

```
        ┌──────────────────────────────────────┐
        │              API                     │  controllers, gRPC, Program.cs
        │   ┌──────────────────────────────┐   │
        │   │       Infrastructure         │   │  EF Core, Redis, HTTP, JWT
        │   │   ┌──────────────────────┐   │   │
        │   │   │    Application       │   │   │  casos de uso, validadores
        │   │   │   ┌──────────────┐   │   │   │
        │   │   │   │   Domain     │   │   │   │  entidades, regras de negócio
        │   │   │   └──────────────┘   │   │   │
        │   │   └──────────────────────┘   │   │
        │   └──────────────────────────────┘   │
        └──────────────────────────────────────┘

        As setas de dependência apontam SEMPRE para dentro.
```

`Order.Domain` não referencia nada além do SharedKernel. `Order.Application` conhece o domínio, não a infraestrutura. `Order.Infrastructure` implementa as interfaces declaradas na Application.

### Como isso é garantido, e não apenas prometido

A regra está codificada nos `.csproj`. `Order.Application` **não pode** usar EF Core diretamente porque a referência não existe — o compilador impede. E `Directory.Build.props` exclui deliberadamente os projetos `*.Domain` e o `SharedKernel` do `FrameworkReference` de ASP.NET Core:

```xml
<ItemGroup Condition="... And !$(MSBuildProjectName.EndsWith('.Domain'))
                      And '$(MSBuildProjectName)' != 'Marketplace.SharedKernel'">
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Se alguém tentar usar `IServiceCollection` dentro de uma entidade, o build quebra. Documentação convence; compilador obriga.

### Onde a pureza foi negociada

`IOrderDbContext` expõe `DbSet<T>` — ou seja, a Application conhece um tipo do EF Core. A alternativa purista seriam repositórios manuais para tudo.

Foi uma escolha consciente: `DbSet` já é um repositório (`IQueryable`) e `SaveChangesAsync` já é Unit of Work. Escrever `IOrderRepository` com vinte métodos que só repassam chamadas para o EF é cerimônia que protege contra uma troca de ORM que quase nunca acontece — e, quando acontece, o repositório também precisa ser reescrito.

### Camadas que não existem

Payment e Notification **não têm** projeto `Domain`. O gateway é um projeto único, sem as quatro camadas. Isso é intencional: nenhum dos dois tem entidade ou invariante própria, e um reverse proxy não tem regra de negócio. Criar projetos vazios por simetria visual gera ruído, não arquitetura.

---

## 3. Comunicação entre serviços

Duas formas, com um critério simples de escolha.

### Síncrona (gRPC): quando a resposta bloqueia a decisão

O `Order` precisa saber o preço do produto **antes** de criar o pedido. Não dá para seguir sem a resposta.

```
Order ──gRPC──► Catalog   "qual o preço e o estoque do produto X?"
Order ──gRPC──► Identity  "este endereço pertence a este usuário?"
```

**Por que gRPC e não REST entre serviços?** Protobuf binário é menor e mais rápido que JSON; HTTP/2 multiplexa várias chamadas na mesma conexão; e o contrato é verificado em **tempo de compilação** — mudar o `.proto` quebra o build, não a produção.

**Contrato com fonte única.** O mesmo `catalog.proto` é compilado duas vezes:

```xml
<!-- Catalog.API -->
<Protobuf Include="Protos\catalog.proto" GrpcServices="Server" />

<!-- Order.Infrastructure -->
<Protobuf Include="..\..\Catalog\Catalog.API\Protos\catalog.proto"
          GrpcServices="Client" Link="Protos\catalog.proto" />
```

Não há referência de projeto entre eles — apenas o arquivo de contrato compartilhado. Com times separados, esses `.proto` viveriam num repositório próprio, distribuído como pacote NuGet versionado.

**Detalhe que quebra em produção:** sem TLS não existe negociação ALPN, então um endpoint marcado `Http1AndHttp2` atende em HTTP/1.1 e o gRPC falha com `HTTP_1_1_REQUIRED`. Por isso Identity e Catalog expõem **duas portas**: 8080 para REST, 8081 marcada explicitamente como `Http2`.

**Preço a pagar:** acoplamento temporal. Se o Catalog cair, nenhum pedido é criado — mitigado por retry e circuit breaker (§9).

### Assíncrona (RabbitMQ): quando o fato já aconteceu

O pedido foi criado. O Payment precisa saber, mas o cliente não deve esperar por isso.

```
Order ──OrderCreated──► [RabbitMQ] ──► Payment
```

O produtor **não sabe quem consome**. Adicionar antifraude, BI ou um novo serviço de fidelidade não altera uma linha do Order.

**Eventos "gordos".** `OrderCreatedEvent` carrega itens, endereço e total — não apenas o `OrderId`. É o padrão *event-carried state transfer*: o consumidor é autossuficiente e processa mesmo com o produtor indisponível. Um evento magro obrigaria o Payment a chamar o Order de volta, recriando exatamente o acoplamento temporal que a fila veio eliminar.

### Uma fila por serviço — a distinção que mais confunde

```
                       ┌── order-payment-approved         (Order)
[exchange] ────────────┼── inventory-payment-approved     (Inventory)
 PaymentApproved       └── notification-payment-approved  (Notification)
```

- **Mesma fila** = trabalho **dividido** entre consumidores (escala horizontal). É o que se quer entre réplicas do *mesmo* serviço.
- **Filas diferentes** ligadas ao mesmo exchange = mensagem **copiada** para cada uma (publish/subscribe). É o que se quer entre serviços *diferentes*.

Este projeto tinha exatamente esse bug: os três serviços tinham uma classe `PaymentApprovedConsumer`, o formatador de nomes gerava a fila `payment-approved` para todos, e o RabbitMQ passou a distribuir cada mensagem para **um** deles em rodízio. De cada três pagamentos, aproximadamente um atualizava o pedido, um reservava estoque e um notificava — nunca os três para o mesmo pedido. Sem erro em log nenhum. A correção foi prefixar as filas com o nome do serviço.

---

## 4. O padrão Outbox

### O problema: dual write

Criar um pedido exige duas ações em sistemas diferentes:

```csharp
await dbContext.SaveChangesAsync();          // 1. PostgreSQL
await publishEndpoint.Publish(orderCreated); // 2. RabbitMQ
```

Não existe transação entre eles. Se o processo cair no meio:

| Falha | Consequência |
|---|---|
| Gravou, não publicou | Pedido pago que nunca vira cobrança nem separação. Some silenciosamente |
| Publicou, não gravou | Estoque reservado para um pedido que não existe |

Inverter a ordem não resolve — apenas troca qual dos dois desastres acontece.

### A solução

O evento é gravado **numa tabela do próprio banco**, na mesma transação da mudança de negócio:

```csharp
dbContext.Orders.Add(order);
dbContext.OutboxMessages.Add(evento);   // mesma transação
await dbContext.SaveChangesAsync();     // atômico: os dois ou nenhum
```

Um `BackgroundService` lê as linhas pendentes a cada 5 segundos e publica no barramento.

```
┌────────────────── transação do Postgres ──────────────────┐
│  INSERT INTO orders (...)                                 │
│  INSERT INTO outbox_messages (...)                        │
└───────────────────────────────────────────────────────────┘
                            │
                            ▼  (assíncrono, a cada 5s)
                  publica no RabbitMQ → marca ProcessedOnUtc
```

### O que a implementação faz além do básico

| Detalhe | Por quê |
|---|---|
| `MessageId` = `Id` da linha do outbox | Reentrega chega com o **mesmo** id, e o consumidor idempotente a descarta. Com id novo a cada tentativa, a deduplicação seria inútil |
| `AttemptCount` com limite de 5 | Mensagem envenenada (payload corrompido) é aposentada em vez de bloquear o lote para sempre |
| `try/catch` **dentro** do laço | Um evento problemático não impede a publicação dos outros do mesmo lote |
| Índice parcial `WHERE ProcessedOnUtc IS NULL` | A consulta roda a cada 5s por serviço. O filtro mantém o índice pequeno mesmo com milhões de linhas já processadas |
| `PeriodicTimer` + tratamento de cancelamento | Desligamento limpo do pod, sem exceção barulhenta a cada SIGTERM |

### O preço

A entrega passa a ser **at-least-once**: se a publicação funciona mas a marcação falha, o evento é reenviado. Por isso todo consumidor precisa ser idempotente (§6).

**Limitação assumida:** com várias réplicas, todas leem o mesmo lote e podem publicar em duplicidade. Como os consumidores são idempotentes, o resultado final continua correto. A solução definitiva seria `SELECT ... FOR UPDATE SKIP LOCKED` ou eleição de líder.

---

## 5. Saga por coreografia

Uma transação de negócio que atravessa quatro serviços. Não existe rollback distribuído — cada um já commitou localmente. O que existe é **compensação**.

```
PendingPayment ──PaymentApproved──► Paid ──StockReserved──► Confirmed
       │                             │
       │ PaymentFailed               │ StockReservationFailed
       ▼                             ▼
  PaymentFailed                  Cancelled  ← precisa de estorno
```

### Coreografia x orquestração

| | Coreografia (adotada) | Orquestração |
|---|---|---|
| Como funciona | Cada serviço reage a eventos e publica o resultado | Um coordenador central envia comandos |
| Acoplamento | Baixo — ninguém conhece os demais | O orquestrador conhece todos |
| Adicionar um passo | Novo serviço assina o evento | Alterar o orquestrador |
| Ver o fluxo inteiro | **Difícil** — está espalhado | Fácil — está num lugar só |
| Depurar | Exige trace distribuído | Log do orquestrador basta |

A escolha aqui foi coreografia por ser mais desacoplada e adequada a um fluxo curto. **Com 10+ passos, orquestração (MassTransit State Machine / Saga) seria melhor** — a dificuldade de enxergar o fluxo cresce mais rápido que o benefício do desacoplamento.

### A decisão mais importante do domínio

`Order.TryTransition` devolve `bool` em vez de lançar exceção:

```csharp
private bool TryTransition(OrderStatus destino, OrderStatus atualExigido, ...)
{
    if (Status != atualExigido) return false;   // ignora, não explode
    ...
}
```

Isso resolve **dois** problemas de uma vez:

1. **Duplicata.** O mesmo `PaymentApproved` chega duas vezes. Com exceção, o MassTransit trataria como falha, tentaria 3 vezes e mandaria para a fila de erro — alarme para uma situação perfeitamente normal.
2. **Chegada fora de ordem.** `StockReserved` chega antes de `PaymentApproved`. O pedido ainda está em `PendingPayment`, a confirmação é simplesmente ignorada, e o estado não é corrompido.

Regra geral para consumidores: **só lance exceção quando tentar de novo pode resolver.** Erro permanente que vira retry é alarme falso.

---

## 6. Idempotência

RabbitMQ entrega *at-least-once*. Se o consumidor processa e cai antes do ACK, a mensagem volta. Sem proteção: cliente cobrado duas vezes, estoque baixado em dobro.

**Mecanismo:** `SET NX` no Redis — operação atômica no servidor.

```csharp
var chave = $"consumer:{nomeDoConsumidor}:message:{messageId}";
var conseguiu = await db.StringSetAsync(chave, agora, TimeSpan.FromDays(7), When.NotExists);
if (!conseguiu) return;   // já processado, ignora
```

**A chave inclui o nome do consumidor** porque o mesmo evento vai para vários serviços. Com chave só por `messageId`, o segundo consumidor acharia que já trabalhou.

**E o nome precisa ser qualificado** (`GetType().FullName`, com namespace). Este foi um bug real: Inventory e Notification têm classes homônimas `PaymentApprovedConsumer`, e o nome curto fazia os dois compartilharem a mesma chave. Sintoma: o estoque era reservado, mas o e-mail de confirmação nunca saía — sem erro em lugar nenhum.

**Limitação honesta.** Isto é uma trava otimista, não uma transação: se o processo cair *depois* de marcar e *antes* de concluir, a reentrega é descartada e o efeito se perde. A versão à prova de falhas grava a marca na mesma transação do banco do consumidor. Redis foi escolhido por ser compartilhado entre serviços que nem sempre têm banco próprio.

**Idempotência também no domínio.** Além da trava, as operações são seguras de repetir por construção: `TryTransition` ignora estado repetido, `EnsureStockAsync` não soma a quantidade duas vezes. Defesa em profundidade — a trava pode falhar, a regra de domínio não.

---

## 7. Dados

### Um banco por serviço

Quatro instâncias de PostgreSQL. Nenhum serviço lê a tabela do outro.

**Por quê:** banco compartilhado é o acoplamento mais forte que existe. Uma migração no Identity quebraria Order, Catalog e Cart ao mesmo tempo, e nenhum time conseguiria evoluir seu esquema sem reunião com os outros três.

**O preço:** não existe `JOIN` entre serviços. Montar "pedidos com nome do usuário" exige compor dados na aplicação ou duplicá-los no evento — e é exatamente por isso que `OrderCreatedEvent` carrega o endereço junto.

### Snapshot x referência

O pedido guarda uma **cópia** do endereço, não o `AddressId`:

```csharp
public AddressSnapshot AddressSnapshot { get; private set; }  // objeto de valor
```

Se o cliente se mudar amanhã, a nota fiscal do pedido de hoje continua mostrando para onde a mercadoria realmente foi. O mesmo vale para nome e preço do produto no `OrderItem`.

É a distinção entre dado **transacional** (imutável após o fato) e dado **mestre** (sempre atual). Confundir os dois é uma das causas mais comuns de divergência contábil em e-commerce.

### Concorrência otimista

Duas pessoas comprando a última unidade ao mesmo tempo:

```
T1: lê disponível = 1          T2: lê disponível = 1
T1: grava 0                    T2: grava 0        ← vendeu duas vezes
```

A solução usa a coluna de sistema `xmin` do PostgreSQL como número de versão:

```sql
UPDATE stock_items SET ... WHERE id = @id AND xmin = @xmin_lido
```

Se outra transação alterou a linha, zero linhas são afetadas e o EF lança `DbUpdateConcurrencyException` — que sobe e é resolvida pelo retry do MassTransit, relendo o saldo atualizado.

**Otimista e não pessimista** (`SELECT FOR UPDATE`) porque conflito real é raro: travar toda leitura para proteger um caso raro custaria throughput em 100% das operações.

### Reserva tudo-ou-nada

O `InventoryRepository` valida **todos** os itens antes de alterar qualquer um. Um pedido de 3 itens em que o último falta não pode reservar os dois primeiros — deixaria unidades presas para uma venda que não vai acontecer.

---

## 8. Cache

Cache-aside (*lazy loading*) no Redis para a leitura de produto por id:

```
1. procura no cache
   ├─ achou   → devolve, sem tocar no banco
   └─ não achou
        2. consulta o Postgres
        3. grava no cache com TTL
        4. devolve
```

**Por que não write-through?** Só entra no cache o que alguém realmente pediu. Num catálogo de 100 mil produtos onde 200 concentram o tráfego, write-through encheria a memória com 99,8% de itens que ninguém consulta.

### Os três perigos, e o que o código faz sobre cada um

| Perigo | Tratamento |
|---|---|
| **Dado obsoleto** | `InvalidateAsync` em toda escrita, **depois** do commit. Invalidar antes abriria janela para repovoar com o dado antigo caso a transação sofresse rollback |
| **Cache stampede** | TTL com *jitter* (10 min + até 60s aleatórios). Sem isso, mil produtos cacheados no mesmo minuto venceriam juntos e o banco levaria a rajada inteira |
| **Redis fora do ar** | Todas as operações de cache em `try/catch` com fallback para o banco. O serviço degrada em **desempenho**, não em disponibilidade |

**O que não é cacheado:** a listagem paginada. O resultado depende de página, tamanho e termo de busca — explosão de chaves com taxa de acerto baixíssima. Cache-aside compensa em leitura por chave, não em consulta filtrada.

**Ausência também não é cacheada.** Guardar "não existe" protegeria contra *cache penetration*, mas exigiria invalidar a marca negativa no cadastro. Complexidade que só se justifica sob ataque real.

---

## 9. Resiliência

Camadas independentes, cada uma para um tipo de falha.

### Chamadas de saída (Polly)

```
CircuitBreaker  (mais externo — corta tudo quando o destino está fora)
  └─ Retry      (repete falha momentânea: 2s, 4s, 8s + jitter)
       └─ chamada gRPC
```

**A ordem importa.** Com o circuito aberto, o retry nem chega a executar.

**Por que os dois?** Retry assume falha *momentânea*; circuit breaker assume que o serviço está *fora*. **Sem o circuit breaker, o retry vira o problema:** se o Catalog cai, cada requisição gera 4 chamadas, cada uma prendendo uma thread em timeout. O Order esgota o pool e cai junto — falha isolada virando queda em cascata.

**Jitter** evita o *thundering herd*: 500 requisições que falharam juntas não podem repetir todas exatamente 2 segundos depois.

### Consumidores (MassTransit)

| Camada | Configuração | Protege de |
|---|---|---|
| Retry exponencial | 3 tentativas, 1s → 15s | Deadlock, timeout momentâneo |
| Circuit breaker | 15% de falha em 1 min (mín. 10 msgs) | Serviço dependente já caindo |
| Kill switch | 50% de falha, religa em 1 min | Enxurrada de mensagens indo para a fila de erro numa queda prolongada |

**Filas `_error` e `_skipped`** são criadas automaticamente: nada é descartado em silêncio, e dá para reprocessar depois de corrigir o bug.

### Kubernetes

**Liveness x readiness é a distinção que mais causa incidente:**

- `liveness` = "o processo travou?" → falhou, o pod é **reiniciado**. Não consulta dependência nenhuma.
- `readiness` = "posso receber tráfego?" → falhou, o pod só sai do balanceador.

**Misturar os dois é caro:** se a liveness verificasse o banco, uma queda momentânea do Postgres reiniciaria todos os pods em cascata — transformando indisponibilidade parcial em total, justamente quando o banco menos aguenta uma enxurrada de reconexões.

`startupProbe` cobre a partida lenta (migração) sem afrouxar a liveness.

---

## 10. Segurança

### Autenticação

| Token | Formato | Vida | Estado |
|---|---|---|---|
| Access | JWT assinado (HS256) | 15 min | Sem estado — qualquer serviço valida offline |
| Refresh | 64 bytes aleatórios, opaco | 30 dias | Com estado — pode ser revogado |

**Access token curto** limita a janela de abuso de um token vazado. **Refresh token longo** evita login constante, mitigado por **rotação**: cada uso revoga o anterior.

**Só o hash do refresh token é persistido** (SHA-256). Se o banco vazar, os tokens roubados não servem para nada.

> SHA-256 puro basta aqui, e BCrypt seria *errado*: o token tem 512 bits de entropia real, não há ataque de dicionário a defender, e o custo alto só tornaria cada renovação mais lenta. Para **senha** a conclusão é a oposta — entropia baixa exige hash lento (BCrypt, work factor 12).

**Limitação assumida:** HS256 (chave simétrica) significa que todo serviço validador conhece a chave capaz de *emitir* tokens. Em produção o correto é RS256: o Identity guarda a chave privada, os demais baixam a pública via JWKS. Foi mantido simétrico para o ambiente de demonstração rodar sem infraestrutura de chaves.

### Autorização por instância de recurso (IDOR)

Nenhum atributo `[Authorize]` responde "este carrinho é seu?". Só olhando o dado.

O projeto tinha três falhas desse tipo. As correções seguiram duas estratégias:

**1. Remover o parâmetro** (preferível — uma rota que não aceita id alheio não pode ser usada errado):

```
GET  /api/carts/{userId}  →  GET  /api/carts/me
GET  /api/users/{id}      →  GET  /api/users/me
```

**2. Verificar quando o id precisa existir na rota:**

```csharp
currentUser.EnsureOwns(id);   // 403 se o token for de outra pessoa
```

**Detalhe de resposta:** `GET /api/orders/{id}` de outro usuário devolve **404**, não 403. Responder "existe, mas não é seu" confirmaria a existência do recurso alheio.

### Outras decisões

- **Login com tempo constante.** Usuário inexistente e senha errada produzem a mesma mensagem *e* o mesmo tempo de resposta — o BCrypt roda contra um hash falso quando o usuário não existe. Sem isso, a diferença de latência (~1ms x ~100ms) entregaria quais e-mails têm conta.
- **Preço vem sempre do servidor.** `CreateOrderRequest` não tem campo de preço. Aceitar `unitPrice` do cliente é a falha clássica que permite comprar um monitor por R$ 0,01.
- **Total é calculado, nunca recebido.** Mesmo raciocínio, dentro do agregado.
- **Detalhe de erro só em desenvolvimento.** Em produção, a mensagem de uma exceção pode conter connection string, caminho de arquivo ou nome de tabela.
- **`ClockSkew = TimeSpan.Zero`.** O padrão do .NET tolera **5 minutos** de diferença de relógio — 30% de vida extra indevida num token de 15 minutos.
- **Containers sem privilégios.** `runAsNonRoot`, `allowPrivilegeEscalation: false`, todas as capabilities removidas.

---

## 11. Observabilidade

Num monólito, uma stack trace conta a história inteira. Aqui, um "criar pedido" atravessa Gateway → Order → (gRPC) Identity → (gRPC) Catalog → RabbitMQ → Payment → Inventory → Notification.

### Os três sinais

| Sinal | Responde | Onde vai |
|---|---|---|
| **Traces** | "onde foram os 800 ms?" | OTLP → Collector → console (Jaeger/Tempo em produção) |
| **Métricas** | "está piorando?" | OTLP → Collector → Prometheus → Grafana |
| **Logs** | "o que aconteceu?" | OTLP → Collector, já com `TraceId`/`SpanId` |

### Por que um Collector no meio

As aplicações falam **um** protocolo (OTLP) e não conhecem o backend:

1. Trocar Jaeger por Tempo, ou Prometheus por Mimir, é mudança de um arquivo de configuração — sem redeploy dos serviços.
2. Amostragem, filtro de dado sensível e atributos comuns ficam centralizados.
3. O Collector absorve indisponibilidade do backend, evitando que exportar telemetria vire latência dentro da aplicação.

### Correlação

O `traceId` aparece **na resposta de erro** (`ProblemDetails.Extensions["traceId"]`). O suporte pega o id que o cliente reportou e abre exatamente aquela requisição no Grafana. A propagação entre processos usa o cabeçalho W3C `traceparent`, inclusive **através da fila** — o trace continua o mesmo depois do RabbitMQ.

### Detalhes que evitam ruído

- Health checks filtrados dos traces: sem isso, 90% dos spans seriam sondas do Kubernetes.
- `service.instance.id` no recurso: distingue réplicas. Sem ele, um pod doente entre cinco some na média.
- Logs com **placeholders nomeados** (`{OrderId}`), nunca interpolação: preserva os valores como campos pesquisáveis.

---

## 12. Testes

97 testes unitários, ~3 segundos, sem Docker.

### O que é testado, e por quê

| Área | Por que vale o teste |
|---|---|
| **Máquina de estados do pedido** | Duplicata e chegada fora de ordem quase nunca aparecem em desenvolvimento e são rotina em produção. Impossível verificar manualmente |
| **Normalização de e-mail** | Reproduz um bug real: o teste falha antes da correção e passa depois |
| **Reserva de estoque** | Reproduz outro bug real (baixa em produto arbitrário) |
| **Invariantes de domínio** | Rápidos, determinísticos, e documentam a regra melhor que qualquer comentário |
| **Consolidação do carrinho** | Lógica de agrupamento com casos de borda não óbvios |

### O que não é testado, e por quê

- **Controllers**: são casca fina (`sender.Send(command)`). Testá-los verificaria o ASP.NET, não o código do projeto.
- **Chamadas gRPC reais**: exigiriam subir dois serviços — é teste de integração, com outro custo e outra frequência.
- **Mapeamento do EF**: o provider InMemory não valida constraint nem tradução de SQL. Para isso o caminho seria Testcontainers com Postgres real.

### Escolhas de ferramenta

**xUnit v3** com runner próprio (Microsoft.Testing.Platform) — o caminho antigo do VSTest deixou de ser suportado no SDK do .NET 10.

**Shouldly** no lugar de FluentAssertions: a v8 do FluentAssertions passou a exigir licença comercial. Shouldly (BSD) oferece a mesma legibilidade sem restrição.

**MediatR fixado na 12.x** e **MassTransit na 8.x** pelo mesmo motivo — as versões seguintes mudaram o modelo de licenciamento.

### Nomes em português

```csharp
[Fact]
public void Aprovacao_de_pagamento_repetida_e_ignorada_sem_erro()
```

O nome do teste é documentação executável. Descrever o **comportamento** de negócio no idioma do domínio vale mais do que a convenção `MethodName_Scenario_ExpectedResult`.

---

## 13. Limitações conhecidas

Registradas de propósito — saber o que falta é parte de conhecer o sistema.

| Limitação | Impacto | Solução |
|---|---|---|
| Outbox sem lock entre réplicas | Publicação duplicada (absorvida pela idempotência) | `SELECT ... FOR UPDATE SKIP LOCKED` ou eleição de líder |
| Rate limit em memória no gateway | Com 3 réplicas, o limite efetivo é 3x | Contador compartilhado no Redis |
| JWT simétrico (HS256) | Todo validador pode emitir token | RS256 + JWKS |
| Sem papéis/permissões | `POST /api/products` exige apenas autenticação | Claims de papel + `[Authorize(Roles=…)]` |
| Migração no startup | N réplicas migram juntas | Job/initContainer do Kubernetes |
| Estorno não implementado | `Cancelled` sem devolver o dinheiro | Comando de compensação para o Payment |
| Carrinho não é limpo após o pedido | Item permanece no carrinho | Consumidor de `OrderCreatedEvent` no Cart |
| Sem testes de integração | Mapeamento do EF e gRPC não verificados automaticamente | Testcontainers |
| Sem versionamento de API | Mudança de contrato quebra clientes | `/v1/` nas rotas + versionamento de eventos |
| Deduplicação otimista | Falha entre marcar e concluir perde o efeito | Tabela `processed_messages` na transação do consumidor |
| Bancos dentro do cluster | Backup e failover manuais | Serviço gerenciado (RDS, Cloud SQL) |

---

## Resumo das decisões

| Decisão | Alternativa descartada | Motivo |
|---|---|---|
| Coreografia | Orquestração | Fluxo curto; menos acoplamento. Com 10+ passos, mudaria |
| Outbox | Publicar direto | Elimina o dual write |
| Evento gordo | Evento magro + callback | Consumidor autossuficiente, sem acoplamento temporal |
| gRPC interno | REST interno | Contrato verificado em compilação, payload menor |
| Cache-aside | Write-through | Só cacheia o que é pedido de fato |
| Concorrência otimista | `SELECT FOR UPDATE` | Conflito é raro; não penaliza o caso comum |
| Banco por serviço | Banco compartilhado | Independência de esquema e de deploy |
| `DbSet` na Application | Repositórios manuais | Evita camada que só repassa chamadas |
| Enum como texto no banco | Enum como int | Legível no suporte; imune a reordenação |
| `bool` na transição | Exceção | Idempotência sem alarme falso |
| Sem camadas vazias | Simetria entre serviços | Estrutura reflete o conteúdo real |
