using CommunityToolkit.Mvvm.ComponentModel;
using ASMED.EDM.UI.Models.Orzeczenie;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using ASMED.EDM.Data.Services;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ASMED.EDM.UI.ViewModels.Orzeczenie;

/// <summary>
/// ViewModel dla wewnętrznej zakładki "Historia" w OrzeczenieView.
/// Wyświetla listę badań (skierowania + rejestracja + badanie) dla wybranego pacjenta (P_ID).
/// </summary>
public partial class HistoriaViewModel : ViewModelBase
{
    private readonly ILogger<HistoriaViewModel>? _logger;
    private readonly DbConnectionFactory? _dbFactory;

    public HistoriaViewModel(ILogger<HistoriaViewModel>? logger = null, DbConnectionFactory? dbFactory = null)
    {
        _logger = logger;
        _dbFactory = dbFactory;
    }

    [ObservableProperty]
    private int? _pID;

    [ObservableProperty]
    private string _pacjentImieNazwisko = string.Empty;

    [ObservableProperty]
    private DateTime? _najpozniejszaDataDo;

    [ObservableProperty]
    private ObservableCollection<HistoriaItem> _historia = new();

    partial void OnPIDChanged(int? value)
    {
        LoadHistoriaFromDb();
    }

    /// <summary>
    /// Ładuje historię badań dla aktualnie ustawionego P_ID.
    /// </summary>
    public void LoadHistoriaFromDb()
    {
        if (_dbFactory == null || PID == null)
        {
            Historia = new ObservableCollection<HistoriaItem>();
            PacjentImieNazwisko = string.Empty;
            NajpozniejszaDataDo = null;
            return;
        }

        try
        {
            var loaded = new ObservableCollection<HistoriaItem>();

            using var conn = _dbFactory.CreateConnection();
            conn.Open();

            using (var pCmd = (MySqlCommand)conn.CreateCommand())
            {
                pCmd.CommandText = "SELECT P_imie, P_nazwisko FROM P_Pacjent WHERE P_ID = @pid";
                pCmd.Parameters.AddWithValue("@pid", PID.Value);
                using var pReader = pCmd.ExecuteReader();
                if (pReader.Read())
                {
                    var imie = pReader["P_imie"]?.ToString();
                    var nazwisko = pReader["P_nazwisko"]?.ToString();
                    PacjentImieNazwisko = $"{imie} {nazwisko}".Trim();
                }
                else
                {
                    PacjentImieNazwisko = string.Empty;
                }
            }

            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    s.B_ID, s.B_DataSkierowania,
                    r.R_Data,
                    b.Bad_Data, b.Bad_Data_Do,s.B_TypBadania,
                    s.B_laryngolog, s.B_okulista, s.B_ksiazeczka, s.B_Urlop
                FROM B_Skierowania s
                LEFT JOIN Rejestracja r ON r.R_S_ID = s.B_ID
                LEFT JOIN Badanie b ON b.Bad_S_ID = s.B_ID
                WHERE s.B_Pacjent_ID = @pid
                ORDER BY s.B_DataSkierowania DESC";
            cmd.Parameters.AddWithValue("@pid", PID.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                loaded.Add(new HistoriaItem
                {
                    B_ID = reader["B_ID"] != DBNull.Value ? Convert.ToInt32(reader["B_ID"]) : null,
                    B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : null,
                    R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : null,
                    Bad_Data = reader["Bad_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data"]) : null,
                    Bad_Data_Do = reader["Bad_Data_Do"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data_Do"]) : null,
                    B_Typ = reader["B_TypBadania"] != DBNull.Value ? reader["B_TypBadania"].ToString() : null,

                    B_laryngolog = reader["B_laryngolog"] != DBNull.Value && Convert.ToBoolean(reader["B_laryngolog"]),
                    B_okulista = reader["B_okulista"] != DBNull.Value && Convert.ToBoolean(reader["B_okulista"]),
                    B_ksiazeczka = reader["B_ksiazeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_ksiazeczka"]),
                    B_Urlop = reader["B_Urlop"] != DBNull.Value && Convert.ToBoolean(reader["B_Urlop"]),
                });
            }

            Historia = loaded;
            NajpozniejszaDataDo = loaded
                .Where(x => x.Bad_Data_Do.HasValue)
                .Select(x => x.Bad_Data_Do!.Value)
                .DefaultIfEmpty()
                .Max() is var max && max != default ? max : (DateTime?)null;

            _logger?.LogInformation("HistoriaViewModel: loaded {Count} rows for P_ID={PID}", loaded.Count, PID);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HistoriaViewModel: error loading Historia from DB for P_ID={PID}", PID);
        }
    }
}
