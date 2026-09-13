using Kontor.Core.Fachlogik;
using Xunit;

namespace Kontor.Tests;

public class KennwortTests
{
    [Fact]
    public void PruefeAkzeptiertDasRichtigeKennwort()
    {
        var salz = Kennwort.NeuesSalz();
        var hash = Kennwort.Hash("Sommerurlaub!42", salz, Kennwort.StandardDurchlaeufe);

        Assert.True(Kennwort.Pruefe("Sommerurlaub!42", hash, salz, Kennwort.StandardDurchlaeufe));
    }

    [Fact]
    public void PruefeWeistEinFalschesKennwortAb()
    {
        var salz = Kennwort.NeuesSalz();
        var hash = Kennwort.Hash("Sommerurlaub!42", salz, Kennwort.StandardDurchlaeufe);

        Assert.False(Kennwort.Pruefe("Winterurlaub!42", hash, salz, Kennwort.StandardDurchlaeufe));
    }

    [Fact]
    public void DieAbleitungIstBeiGleichemSalzUndGleicherDurchlaufzahlReproduzierbar()
    {
        var salz = Kennwort.NeuesSalz();

        var ersterHash = Kennwort.Hash("Sommerurlaub!42", salz, Kennwort.StandardDurchlaeufe);
        var zweiterHash = Kennwort.Hash("Sommerurlaub!42", salz, Kennwort.StandardDurchlaeufe);

        Assert.Equal(ersterHash, zweiterHash);
    }

    [Fact]
    public void UnterschiedlichesSalzErgibtUnterschiedlicheHashesFuerDasselbeKennwort()
    {
        var hashA = Kennwort.Hash("Sommerurlaub!42", Kennwort.NeuesSalz(), Kennwort.StandardDurchlaeufe);
        var hashB = Kennwort.Hash("Sommerurlaub!42", Kennwort.NeuesSalz(), Kennwort.StandardDurchlaeufe);

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void NeuesSalzLiefertSechzehnBytes()
    {
        Assert.Equal(16, Kennwort.NeuesSalz().Length);
    }

    [Fact]
    public void StandardDurchlaeufeSindMindestens210000()
    {
        Assert.True(Kennwort.StandardDurchlaeufe >= 210_000);
    }
}
