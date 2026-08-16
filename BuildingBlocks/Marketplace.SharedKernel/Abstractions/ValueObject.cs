namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Classe base dos objetos de valor, comparados pelo conteudo e nao por identidade.
/// </summary>
/// <remarks>
/// <para>
/// Enquanto uma <see cref="Entity"/> e "quem", um objeto de valor e "o que". Duas notas
/// de R$ 10 sao intercambiaveis; dois enderecos com exatamente os mesmos campos sao o
/// mesmo endereco. Por isso a igualdade e estrutural.
/// </para>
/// <para>
/// Uso tipico no projeto: <c>AddressSnapshot</c>, o endereco congelado dentro do pedido.
/// Guardar uma copia (em vez de um ponteiro para o endereco do usuario) garante que a
/// nota fiscal de um pedido antigo continue mostrando o endereco vigente na epoca,
/// mesmo que o cliente se mude depois.
/// </para>
/// </remarks>
public abstract class ValueObject
{
    /// <summary>
    /// Retorna, em ordem, os campos que definem a igualdade deste objeto.
    /// </summary>
    /// <remarks>
    /// Implementacoes normalmente usam <c>yield return</c> para cada propriedade
    /// relevante. A ordem importa: ela participa do calculo do hash code.
    /// </remarks>
    /// <returns>Sequencia de componentes usados na comparacao.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Compara este objeto de valor com outro pelo conteudo.
    /// </summary>
    /// <param name="obj">Objeto a comparar.</param>
    /// <returns><see langword="true"/> quando todos os componentes sao iguais.</returns>
    public override bool Equals(object? obj)
    {
        // A checagem de tipo exato (e nao "is ValueObject") impede que duas subclasses
        // distintas com os mesmos campos sejam consideradas iguais.
        if (obj is not ValueObject other || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Calcula o hash code combinando todos os componentes de igualdade.
    /// </summary>
    /// <remarks>
    /// Contrato obrigatorio do .NET: objetos iguais precisam ter o mesmo hash code.
    /// Sobrescrever <see cref="Equals(object?)"/> sem sobrescrever este metodo faria
    /// o objeto se comportar de forma errada dentro de <c>Dictionary</c> e <c>HashSet</c>.
    /// </remarks>
    /// <returns>Hash code da instancia.</returns>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (current, value) => HashCode.Combine(current, value));
    }

    /// <summary>
    /// Compara dois objetos de valor pelo conteudo.
    /// </summary>
    /// <param name="left">Primeiro operando.</param>
    /// <param name="right">Segundo operando.</param>
    /// <returns><see langword="true"/> quando ambos sao equivalentes.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Compara dois objetos de valor pelo conteudo, negando o resultado.
    /// </summary>
    /// <param name="left">Primeiro operando.</param>
    /// <param name="right">Segundo operando.</param>
    /// <returns><see langword="true"/> quando os objetos diferem.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
