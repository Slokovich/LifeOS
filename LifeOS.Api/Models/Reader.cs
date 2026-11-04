// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Un'altra persona che legge e con cui ci si sfida (es. "Fra"). I tuoi libri stanno in Books;
/// quelli di questa persona in <see cref="ReaderBook"/>.
/// </summary>
public class Reader
{
    /// <summary>Identificativo unico interno, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Nome con cui la chiami (unico), per esempio "Fra".</summary>
    public required string Name { get; set; }

    /// <summary>I libri di questa persona.</summary>
    public List<ReaderBook> Books { get; set; } = [];
}
