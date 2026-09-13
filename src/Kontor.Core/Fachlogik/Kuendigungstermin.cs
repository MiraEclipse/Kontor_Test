namespace Kontor.Core.Fachlogik;

// Kuendigungsmoeglichkeit: der Tag, zu dem der Vertrag frühestens enden kann.
// SpaetesterAbsendetag: der letzte Tag, an dem die Kündigung abgeschickt sein muss, um die
// Kuendigungsmoeglichkeit noch zu erreichen.
public sealed record Kuendigungstermin(DateOnly Kuendigungsmoeglichkeit, DateOnly SpaetesterAbsendetag);
