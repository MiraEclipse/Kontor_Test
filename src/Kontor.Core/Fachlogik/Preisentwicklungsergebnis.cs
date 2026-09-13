namespace Kontor.Core.Fachlogik;

public sealed record Preisentwicklungsergebnis(
    long AktuellerPreisCent,
    long JahreskostenCent,
    long BisherGezahltCent,
    int TageSeitErsterZahlung,
    decimal SteigerungProzent);
