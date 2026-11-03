// Il namespace corrisponde alla cartella LifeOS.Web/Models.
namespace LifeOS.Web.Models;

/// <summary>
/// Riepilogo di un import CSV così come lo vede il frontend: copia di LifeOS.Api.Services.ImportResult.
/// I nomi devono coincidere con quelli del JSON inviato da POST /api/books/import.
/// </summary>
public class ImportResultItem
{
    /// <summary>Libri nuovi inseriti.</summary>
    public int Added { get; set; }

    /// <summary>Libri già presenti e aggiornati.</summary>
    public int Updated { get; set; }

    /// <summary>Righe del CSV ignorate (senza Book Id, titolo o autore).</summary>
    public int Skipped { get; set; }
}
