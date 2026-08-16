# Coleção Postman

Importe `MyMarketPlace.postman_collection.json` no Postman (ou no Insomnia / Bruno, que leem o mesmo formato).

## Como usar

1. Suba o ambiente: `docker compose up -d --build`
2. Rode as pastas **na ordem numerada**.
3. As variáveis (`accessToken`, `userId`, `addressId`, `productId`, `orderId`) são preenchidas automaticamente pelos scripts de teste de cada requisição — não é preciso copiar nada à mão.

Também dá para rodar a coleção inteira de uma vez pelo *Collection Runner*: as 26 requisições incluem asserts e servem como um teste de fumaça do sistema.

## O que cada pasta demonstra

| Pasta | Conceito |
|---|---|
| **1. Catálogo** | Endpoint público, cache-aside, 404 correto |
| **2. Identidade** | Cadastro, normalização de e-mail, 409 em duplicata, rotação de refresh token, ProblemDetails |
| **3. Segurança** | Correções de IDOR — 401 x 403, rota `/me` sem parâmetro de id |
| **4. Carrinho** | `PUT` idempotente, consolidação de itens repetidos, `DELETE` idempotente |
| **5. Pedido e saga** | **O ponto alto**: o status evolui sozinho, e o caminho de compensação |
| **6. Saúde** | Diferença entre liveness e readiness |

## Dica para a demonstração

Na pasta 5, depois de criar o pedido, rode **"Consultar pedido"** três ou quatro vezes seguidas. O status caminha:

```
PendingPayment  →  Paid  →  Confirmed
```

Cada passo é um evento diferente sendo processado por um serviço diferente. Em paralelo, `docker compose logs -f notification-api` mostra as notificações chegando na mesma sequência.

Para o caminho de falha, use **"Pedido acima do limite"**: em poucos segundos o status vira `PaymentFailed`, com o motivo preenchido em `statusReason`.
