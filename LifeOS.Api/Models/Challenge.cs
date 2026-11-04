// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Classe base di tutte le challenge di lettura (a numero, alfabeto, in futuro competitiva...).
/// È "astratta": non si crea mai una Challenge generica, solo una delle sottoclassi.
/// Nel database tutte le sottoclassi stanno nella stessa tabella Challenges, con una colonna
/// "Kind" che dice di che tipo è ogni riga (ereditarietà TPH, CLAUDE.md §8).
/// I calcoli li fa il codice in <see cref="Evaluate"/>, non l'IA (CLAUDE.md §12.1 regola 1).
/// </summary>
public abstract class Challenge
{
    /// <summary>Identificativo unico interno, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Nome leggibile, per esempio "60 libri nel 2026" o "Alfabeto 2026".</summary>
    public required string Name { get; set; }

    /// <summary>L'anno della challenge: contano i libri finiti in quell'anno.</summary>
    public int Year { get; set; }

    /// <summary>Quando è stata creata.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Calcola a che punto è la challenge, dati i libri finiti nell'anno (tuoi e delle altre persone)
    /// e la data di oggi. Ogni sottoclasse la implementa con le sue regole.
    /// </summary>
    public abstract ChallengeProgress Evaluate(ChallengeContext context);

    /// <summary>Vero se l'obiettivo è stato raggiunto (scorciatoia su <see cref="Evaluate"/>).</summary>
    public bool IsGoalReached(ChallengeContext context) => Evaluate(context).IsReached;

    /// <summary>
    /// Quanta parte dell'anno è trascorsa a oggi, da 0 (anno futuro) a 1 (anno passato).
    /// Serve a calcolare il "ritmo atteso": quanto dovresti aver fatto per finire in tempo.
    /// </summary>
    protected decimal YearFractionElapsed(DateOnly today)
    {
        var from = new DateOnly(Year, 1, 1);
        var daysInYear = new DateOnly(Year, 12, 31).DayNumber - from.DayNumber + 1;
        var elapsed = Math.Clamp(today.DayNumber - from.DayNumber + 1, 0, daysInYear);
        return (decimal)elapsed / daysInYear;
    }
}
