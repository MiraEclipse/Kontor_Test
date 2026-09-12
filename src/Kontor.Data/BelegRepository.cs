using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class BelegRepository : IBelegRepository
{
    private const string KopfSpalten =
        "BelegId, MandantNr, Nummer, Belegart, Belegdatum, KundeId, StorniertVon, Erfasst";

    private const string PositionSpalten =
        "PositionId, MandantNr, BelegId, PosNr, ArtikelId, Bezeichnung, Einheit, MengeTausendstel, EinzelpreisCent, SteuerSatzBp";

    private const string ZeileSpalten =
        "ZeileId, MandantNr, BelegId, KontoNr, BetragCent, SollHaben";

    private readonly Datenbank _datenbank;
    private readonly Kontenzuordnung _konten;

    public BelegRepository(Datenbank datenbank) : this(datenbank, Kontenzuordnung.Standard)
    {
    }

    public BelegRepository(Datenbank datenbank, Kontenzuordnung konten)
    {
        _datenbank = datenbank;
        _konten = konten;
    }

    public IReadOnlyList<Beleg> Liste(int mandantNr, int jahr)
    {
        var belege = new List<Beleg>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + KopfSpalten + @" FROM Beleg
             WHERE MandantNr = @mandant AND Belegdatum >= @von AND Belegdatum <= @bis
             ORDER BY Belegdatum, Nummer;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@von", Feldwerte.Datum(new DateTime(jahr, 1, 1)));
        Befehle.Setze(befehl, "@bis", Feldwerte.Datum(new DateTime(jahr, 12, 31)));
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            belege.Add(LiesKopf(leser));
        }

        return belege;
    }

    public Beleg? Lade(int mandantNr, int belegId)
    {
        using var verbindung = _datenbank.Oeffne();
        return Lade(verbindung, null, mandantNr, belegId);
    }

    public Beleg Buchen(Beleg beleg)
    {
        Belegpruefung.PruefeKopf(beleg);

        var vorgabe = beleg.Zeilen.Count > 0 ? new List<Buchungszeile>(beleg.Zeilen) : null;

        using var verbindung = _datenbank.Oeffne();
        using var transaktion = _datenbank.BeginneSofort(verbindung);

        var gebucht = Schreibe(verbindung, transaktion, beleg, null, vorgabe);

        transaktion.Commit();
        return gebucht;
    }

    public Beleg Stornieren(int mandantNr, int belegId, DateTime belegdatum)
    {
        using var verbindung = _datenbank.Oeffne();
        using var transaktion = _datenbank.BeginneSofort(verbindung);

        var original = Lade(verbindung, transaktion, mandantNr, belegId);
        if (original is null)
        {
            throw new FachlicherFehler($"Beleg {belegId} ist im Mandanten {mandantNr} nicht vorhanden.");
        }

        Stornierung.PruefeStornierbar(original, IstStorniert(verbindung, transaktion, mandantNr, belegId));

        var vorlage = new Beleg
        {
            MandantNr = original.MandantNr,
            Belegart = original.Belegart,
            Belegdatum = belegdatum,
            KundeId = original.KundeId
        };

        foreach (var position in original.Positionen)
        {
            vorlage.Positionen.Add(position);
        }

        Belegpruefung.PruefeKopf(vorlage);

        var storno = Schreibe(verbindung, transaktion, vorlage, belegId, Stornierung.Spiegele(original.Zeilen));

        transaktion.Commit();
        return storno;
    }

    private Beleg Schreibe(
        SqliteConnection verbindung,
        SqliteTransaction transaktion,
        Beleg vorlage,
        int? storniertVon,
        IReadOnlyList<Buchungszeile>? vorgabe)
    {
        var jahr = vorlage.Belegdatum.Year;
        var laufend = ZiehNummer(verbindung, transaktion, vorlage.MandantNr, jahr, vorlage.Belegart);

        var beleg = new Beleg
        {
            MandantNr = vorlage.MandantNr,
            Nummer = Belegnummer.Bilden(vorlage.Belegart, jahr, laufend),
            Belegart = vorlage.Belegart,
            Belegdatum = vorlage.Belegdatum.Date,
            KundeId = vorlage.KundeId,
            StorniertVon = storniertVon,
            Erfasst = vorlage.Erfasst == default ? Feldwerte.Jetzt() : vorlage.Erfasst
        };

        beleg.BelegId = FuegeKopfEin(verbindung, transaktion, beleg);

        var laufendePosNr = 0;
        foreach (var vorlagePosition in vorlage.Positionen)
        {
            laufendePosNr++;
            var position = new Belegposition
            {
                MandantNr = beleg.MandantNr,
                BelegId = beleg.BelegId,
                PosNr = vorlagePosition.PosNr > 0 ? vorlagePosition.PosNr : laufendePosNr,
                ArtikelId = vorlagePosition.ArtikelId,
                Bezeichnung = vorlagePosition.Bezeichnung,
                Einheit = vorlagePosition.Einheit,
                Menge = vorlagePosition.Menge,
                Einzelpreis = vorlagePosition.Einzelpreis,
                SteuerSatz = vorlagePosition.SteuerSatz
            };

            position.PositionId = FuegePositionEin(verbindung, transaktion, position);
            beleg.Positionen.Add(position);
        }

        var zeilen = vorgabe ?? Buchungssatz.Erzeuge(beleg.Belegart, beleg.Positionen, _konten);

        foreach (var vorgabeZeile in zeilen)
        {
            var zeile = new Buchungszeile
            {
                MandantNr = beleg.MandantNr,
                BelegId = beleg.BelegId,
                KontoNr = vorgabeZeile.KontoNr,
                Betrag = vorgabeZeile.Betrag,
                SollHaben = vorgabeZeile.SollHaben
            };

            zeile.ZeileId = FuegeZeileEin(verbindung, transaktion, zeile);
            beleg.Zeilen.Add(zeile);
        }

        Belegpruefung.PruefeSollGleichHaben(LiesZeilen(verbindung, transaktion, beleg.MandantNr, beleg.BelegId));
        return beleg;
    }

    private static int ZiehNummer(
        SqliteConnection verbindung,
        SqliteTransaction transaktion,
        int mandantNr,
        int jahr,
        Belegart belegart)
    {
        var code = Belegarten.Code(belegart);

        using var anlegen = Befehle.Neu(verbindung, transaktion,
            @"INSERT OR IGNORE INTO Nummernkreis (MandantNr, Jahr, Belegart, LetzteNummer)
              VALUES (@mandant, @jahr, @art, 0);");
        Befehle.Setze(anlegen, "@mandant", mandantNr);
        Befehle.Setze(anlegen, "@jahr", jahr);
        Befehle.Setze(anlegen, "@art", code);
        anlegen.ExecuteNonQuery();

        using var hochzaehlen = Befehle.Neu(verbindung, transaktion,
            @"UPDATE Nummernkreis SET LetzteNummer = LetzteNummer + 1
              WHERE MandantNr = @mandant AND Jahr = @jahr AND Belegart = @art;");
        Befehle.Setze(hochzaehlen, "@mandant", mandantNr);
        Befehle.Setze(hochzaehlen, "@jahr", jahr);
        Befehle.Setze(hochzaehlen, "@art", code);
        hochzaehlen.ExecuteNonQuery();

        using var lesen = Befehle.Neu(verbindung, transaktion,
            @"SELECT LetzteNummer FROM Nummernkreis
              WHERE MandantNr = @mandant AND Jahr = @jahr AND Belegart = @art;");
        Befehle.Setze(lesen, "@mandant", mandantNr);
        Befehle.Setze(lesen, "@jahr", jahr);
        Befehle.Setze(lesen, "@art", code);
        var wert = lesen.ExecuteScalar();

        if (wert is null || wert is DBNull)
        {
            throw new DatenbankFehler(
                $"Der Nummernkreis {code} {jahr} des Mandanten {mandantNr} konnte nicht gezogen werden.");
        }

        return Convert.ToInt32(wert);
    }

    private static int FuegeKopfEin(SqliteConnection verbindung, SqliteTransaction transaktion, Beleg beleg)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Beleg (MandantNr, Nummer, Belegart, Belegdatum, KundeId, StorniertVon, Erfasst)
              VALUES (@mandant, @nummer, @art, @datum, @kunde, @storniertVon, @erfasst);");
        Befehle.Setze(befehl, "@mandant", beleg.MandantNr);
        Befehle.Setze(befehl, "@nummer", beleg.Nummer);
        Befehle.Setze(befehl, "@art", Belegarten.Code(beleg.Belegart));
        Befehle.Setze(befehl, "@datum", Feldwerte.Datum(beleg.Belegdatum));
        Befehle.Setze(befehl, "@kunde", Feldwerte.Null(beleg.KundeId));
        Befehle.Setze(befehl, "@storniertVon", Feldwerte.Null(beleg.StorniertVon));
        Befehle.Setze(befehl, "@erfasst", Feldwerte.Zeit(beleg.Erfasst));
        befehl.ExecuteNonQuery();

        return Befehle.LetzteId(verbindung, transaktion);
    }

    private static int FuegePositionEin(SqliteConnection verbindung, SqliteTransaction transaktion, Belegposition position)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Belegposition (MandantNr, BelegId, PosNr, ArtikelId, Bezeichnung, Einheit,
                                         MengeTausendstel, EinzelpreisCent, SteuerSatzBp)
              VALUES (@mandant, @beleg, @posNr, @artikel, @bezeichnung, @einheit, @menge, @preis, @steuer);");
        Befehle.Setze(befehl, "@mandant", position.MandantNr);
        Befehle.Setze(befehl, "@beleg", position.BelegId);
        Befehle.Setze(befehl, "@posNr", position.PosNr);
        Befehle.Setze(befehl, "@artikel", Feldwerte.Null(position.ArtikelId));
        Befehle.Setze(befehl, "@bezeichnung", position.Bezeichnung);
        Befehle.Setze(befehl, "@einheit", position.Einheit);
        Befehle.Setze(befehl, "@menge", Feldwerte.Tausendstel(position.Menge));
        Befehle.Setze(befehl, "@preis", Feldwerte.Cent(position.Einzelpreis));
        Befehle.Setze(befehl, "@steuer", Feldwerte.Bp(position.SteuerSatz));
        befehl.ExecuteNonQuery();

        return Befehle.LetzteId(verbindung, transaktion);
    }

    private static int FuegeZeileEin(SqliteConnection verbindung, SqliteTransaction transaktion, Buchungszeile zeile)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Buchungszeile (MandantNr, BelegId, KontoNr, BetragCent, SollHaben)
              VALUES (@mandant, @beleg, @konto, @betrag, @sollHaben);");
        Befehle.Setze(befehl, "@mandant", zeile.MandantNr);
        Befehle.Setze(befehl, "@beleg", zeile.BelegId);
        Befehle.Setze(befehl, "@konto", zeile.KontoNr);
        Befehle.Setze(befehl, "@betrag", Feldwerte.Cent(zeile.Betrag));
        Befehle.Setze(befehl, "@sollHaben", SollHabenCodes.Code(zeile.SollHaben));
        befehl.ExecuteNonQuery();

        return Befehle.LetzteId(verbindung, transaktion);
    }

    private static bool IstStorniert(
        SqliteConnection verbindung,
        SqliteTransaction transaktion,
        int mandantNr,
        int belegId)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT COUNT(*) FROM Beleg WHERE MandantNr = @mandant AND StorniertVon = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", belegId);
        return Convert.ToInt32(befehl.ExecuteScalar()) > 0;
    }

    private static Beleg? Lade(
        SqliteConnection verbindung,
        SqliteTransaction? transaktion,
        int mandantNr,
        int belegId)
    {
        Beleg beleg;

        using (var befehl = Befehle.Neu(verbindung, transaktion,
                   "SELECT " + KopfSpalten + " FROM Beleg WHERE MandantNr = @mandant AND BelegId = @id;"))
        {
            Befehle.Setze(befehl, "@mandant", mandantNr);
            Befehle.Setze(befehl, "@id", belegId);
            using var leser = befehl.ExecuteReader();

            if (!leser.Read())
            {
                return null;
            }

            beleg = LiesKopf(leser);
        }

        foreach (var position in LiesPositionen(verbindung, transaktion, mandantNr, belegId))
        {
            beleg.Positionen.Add(position);
        }

        foreach (var zeile in LiesZeilen(verbindung, transaktion, mandantNr, belegId))
        {
            beleg.Zeilen.Add(zeile);
        }

        return beleg;
    }

    private static List<Belegposition> LiesPositionen(
        SqliteConnection verbindung,
        SqliteTransaction? transaktion,
        int mandantNr,
        int belegId)
    {
        var positionen = new List<Belegposition>();

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT " + PositionSpalten + @" FROM Belegposition
             WHERE MandantNr = @mandant AND BelegId = @id ORDER BY PosNr;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", belegId);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            positionen.Add(new Belegposition
            {
                PositionId = leser.GetInt32(0),
                MandantNr = leser.GetInt32(1),
                BelegId = leser.GetInt32(2),
                PosNr = leser.GetInt32(3),
                ArtikelId = leser.IsDBNull(4) ? null : leser.GetInt32(4),
                Bezeichnung = leser.GetString(5),
                Einheit = leser.GetString(6),
                Menge = Feldwerte.AusTausendstel(leser.GetInt64(7)),
                Einzelpreis = Feldwerte.AusCent(leser.GetInt64(8)),
                SteuerSatz = Feldwerte.AusBp(leser.GetInt64(9))
            });
        }

        return positionen;
    }

    private static List<Buchungszeile> LiesZeilen(
        SqliteConnection verbindung,
        SqliteTransaction? transaktion,
        int mandantNr,
        int belegId)
    {
        var zeilen = new List<Buchungszeile>();

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT " + ZeileSpalten + @" FROM Buchungszeile
             WHERE MandantNr = @mandant AND BelegId = @id ORDER BY ZeileId;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", belegId);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            zeilen.Add(new Buchungszeile
            {
                ZeileId = leser.GetInt32(0),
                MandantNr = leser.GetInt32(1),
                BelegId = leser.GetInt32(2),
                KontoNr = leser.GetString(3),
                Betrag = Feldwerte.AusCent(leser.GetInt64(4)),
                SollHaben = SollHabenCodes.Aus(leser.GetString(5))
            });
        }

        return zeilen;
    }

    private static Beleg LiesKopf(SqliteDataReader leser) => new()
    {
        BelegId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Nummer = leser.GetString(2),
        Belegart = Belegarten.Aus(leser.GetString(3)),
        Belegdatum = Feldwerte.AusDatum(leser.GetString(4)),
        KundeId = leser.IsDBNull(5) ? null : leser.GetInt32(5),
        StorniertVon = leser.IsDBNull(6) ? null : leser.GetInt32(6),
        Erfasst = Feldwerte.AusZeit(leser.GetString(7))
    };
}
