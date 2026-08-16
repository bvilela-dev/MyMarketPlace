namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Resultado de uma operacao que pode falhar, sem valor de retorno.
/// </summary>
/// <remarks>
/// <para>
/// O padrao <b>Result</b> torna a falha esperada parte da assinatura do metodo. Excecao
/// custa caro (captura de stack trace) e, pior, e invisivel para quem le a assinatura:
/// <c>Task&lt;Order&gt;</c> nao avisa que pode explodir. Ja
/// <c>Task&lt;Result&lt;Order&gt;&gt;</c> obriga quem chama a tratar os dois caminhos.
/// </para>
/// <para>
/// Convencao adotada no projeto: <b>Result</b> para falhas previsiveis de negocio
/// ("saldo insuficiente"); <b>excecao</b> apenas para o que e realmente excepcional
/// (banco fora do ar, bug).
/// </para>
/// </remarks>
public class Result
{
    /// <summary>
    /// Inicializa um resultado.
    /// </summary>
    /// <param name="isSuccess">Indica se a operacao teve sucesso.</param>
    /// <param name="error">Mensagem de falha, quando houver.</param>
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Indica que a operacao terminou com sucesso.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica que a operacao falhou. Acucar sintatico para <c>!IsSuccess</c>.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Motivo da falha; <see langword="null"/> quando a operacao teve sucesso.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    /// <returns>Resultado bem-sucedido.</returns>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Cria um resultado de falha.
    /// </summary>
    /// <param name="error">Motivo da falha.</param>
    /// <returns>Resultado com falha.</returns>
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Resultado de uma operacao que devolve um valor quando bem-sucedida.
/// </summary>
/// <typeparam name="T">Tipo do valor retornado.</typeparam>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Valor produzido pela operacao; <see langword="null"/> quando houve falha.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Cria um resultado de sucesso carregando o valor.
    /// </summary>
    /// <param name="value">Valor retornado pela operacao.</param>
    /// <returns>Resultado bem-sucedido.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Cria um resultado de falha, sem valor.
    /// </summary>
    /// <param name="error">Motivo da falha.</param>
    /// <returns>Resultado com falha.</returns>
    /// <remarks>
    /// O <c>new</c> aqui esconde <see cref="Result.Failure(string)"/> de proposito:
    /// sem ele, <c>Result&lt;Order&gt;.Failure("x")</c> devolveria um <c>Result</c>
    /// sem o tipo generico e nao compilaria no ponto de uso.
    /// </remarks>
    public static new Result<T> Failure(string error) => new(false, default, error);
}
