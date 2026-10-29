using ASMED.EDM.Core.DTOs;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Data.Services;
using Dapper;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Runtime.Versioning;

namespace ASMED.EDM.Data.Repositories;

[SupportedOSPlatform("windows")]
public class BadanieRepository : Repository<Badanie>, IBadanieRepository
{
    private readonly DbConnectionFactory _connFactory;

    public BadanieRepository(AsmedDbContext context, DbConnectionFactory connFactory) : base(context)
    {
        _connFactory = connFactory;
    }

    /// <summary>Wymagana przez interfejs — używaj GetAvailableBadaniaRowsAsync (Dapper).</summary>
    public Task<IEnumerable<Badanie>> GetAvailableBadaniaAsync(int? firmaId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<Badanie>());
    /// <summary>
    /// Dapper — pełny JOIN Badanie + P_Pacjent + Firma.
    /// Zwraca wszystkie niezafakturowane badania (Bad_L_ID=0/NULL, Bad_Fakt_ID=0/NULL).
    /// </summary>
    public async Task<IEnumerable<BadanieRowDto>> GetAvailableBadaniaRowsAsync(int? firmaId = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Badanie.Bad_ID,
                Badanie.Bad_L_ID,
                Badanie.Bad_F_ID,
                Badanie.Bad_P_ID,
                Badanie.Bad_S_ID,
                Badanie.Bad_Data,
                Badanie.Bad_Data_Do,
                Badanie.Bad_Typ,
                Badanie.Bad_Wynik,
                Badanie.Bad_Nr_KS,
                Badanie.Bad_END,
                Badanie.Bad_bn_cennik,
                Badanie.Bad_Razem,
                Badanie.Bad_Cena1,
                Badanie.Bad_Cena2,
                Badanie.Bad_Cena3,
                Badanie.Bad_Cena4,
                Badanie.Bad_Cena5,
                Badanie.Bad_Cena6,
                Badanie.Bad_Cena7,
                Badanie.Bad_Cena8,
                Badanie.Bad_Cena9,
                Badanie.Bad_Cena10,
                P_Pacjent.P_imie,
                P_Pacjent.P_nazwisko,
                P_Pacjent.P_zawod,
                Firma.Nazwa   AS FirmaNazwa,
                Firma.Cennik  AS FirmaCennik
            FROM Badanie
            LEFT JOIN P_Pacjent ON P_Pacjent.P_ID = Badanie.Bad_P_ID
            LEFT JOIN Firma     ON Firma.id        = P_Pacjent.P_Firma_id
            WHERE (Badanie.Bad_L_ID    IS NULL OR Badanie.Bad_L_ID    = 0)
              AND (Badanie.Bad_Fakt_ID IS NULL OR Badanie.Bad_Fakt_ID = 0)
            ORDER BY Badanie.Bad_Data DESC
            """;

        await using var connection = new MySqlConnection(_connFactory.ActiveConnectionString);
        return await connection.QueryAsync<BadanieRowDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }


    public async Task<IEnumerable<Badanie>> GetByListaBadanAsync(int listaBadanId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ListaBadanId == listaBadanId)
            .Include(b => b.Patient)
            .OrderBy(b => b.DataBadania)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Badanie>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.PatientId == patientId)
            .Include(b => b.Patient)
            .OrderByDescending(b => b.DataBadania)
            .ToListAsync(cancellationToken);
    }

    public async Task<Badanie?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Patient)
            .Include(b => b.ListaBadan)
                .ThenInclude(l => l!.Firma)
            .Include(b => b.Faktura)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AssignToListaAsync(IEnumerable<int> badanieIds, int listaBadanId, CancellationToken cancellationToken = default)
    {
        var ids = badanieIds.ToList();
        await _dbSet
            .Where(b => ids.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ListaBadanId, listaBadanId), cancellationToken);
    }

    public async Task UnassignFromListaAsync(IEnumerable<int> badanieIds, CancellationToken cancellationToken = default)
    {
        var ids = badanieIds.ToList();
        await _dbSet
            .Where(b => ids.Contains(b.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.ListaBadanId, (int?)null)
                .SetProperty(b => b.FakturaId, (int?)null),
                cancellationToken);
    }

    public async Task SetFakturaIdAsync(int listaBadanId, int fakturaId, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(b => b.ListaBadanId == listaBadanId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.FakturaId, fakturaId), cancellationToken);
    }
}
