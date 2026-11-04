// Il namespace corrisponde alla cartella LifeOS.Api/Dtos.
namespace LifeOS.Api.Dtos;

/// <summary>
/// Un messaggio della conversazione, così come viaggia tra frontend e API (non è una tabella del DB).
/// "DTO" = Data Transfer Object: una classe che serve solo a trasportare dati.
/// </summary>
public class ChatMessageDto
{
    /// <summary>Chi ha scritto il messaggio: "user" (tu) oppure "assistant" (l'agente).</summary>
    public required string Role { get; set; }

    /// <summary>Il testo del messaggio.</summary>
    public required string Text { get; set; }
}
