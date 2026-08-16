using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.UnitTests.Infrastructure;

/// <summary>
/// Cria contextos do Identity apoiados no provider em memoria do EF Core.
/// </summary>
/// <remarks>
/// <para>
/// <b>Limites conhecidos do provider InMemory</b> — e importante saber o que ele NAO
/// verifica:
/// </para>
/// <list type="bullet">
///   <item>nao aplica indice unico nem chave estrangeira;</item>
///   <item>nao executa SQL, entao nao valida se a consulta e traduzivel;</item>
///   <item>nao tem transacao de verdade.</item>
/// </list>
/// <para>
/// Ou seja: serve para testar <b>logica de caso de uso</b> (a ordem das operacoes, o
/// que e gravado, qual excecao e lancada), nao comportamento de banco. Para validar
/// mapeamento e constraint o caminho seria SQLite em memoria ou Testcontainers com um
/// Postgres real — teste de integracao, nao unitario.
/// </para>
/// <para>
/// Cada contexto recebe um nome de banco unico (<c>Guid.NewGuid()</c>). Sem isso os
/// testes compartilhariam estado e passariam a falhar dependendo da ordem de execucao —
/// o classico teste "instavel" que ninguem consegue reproduzir.
/// </para>
/// </remarks>
public static class InMemoryIdentityDbContext
{
    /// <summary>
    /// Cria um contexto isolado, com banco em memoria proprio.
    /// </summary>
    /// <returns>Contexto pronto para uso no teste.</returns>
    public static IdentityDbContext Create()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid()}")
            // O provider InMemory avisa que nao suporta transacao; como o teste nao
            // verifica transacionalidade, o aviso e silenciado para nao poluir a saida.
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new IdentityDbContext(options);
    }
}
