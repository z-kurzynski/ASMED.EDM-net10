using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Models
{
    /// <summary>
    /// Model historii logowania użytkowników
    /// </summary>
    public class LoginHistory : INotifyPropertyChanged
    {
        private int _id;
        private int _userId;
        private string? _username;
        private DateTime _loginTime;
        private DateTime? _logoutTime;
        private string? _computerName;
        private string? _ipAddress;
        private bool _success;
        private string? _failureReason;

        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public int UserId
        {
            get => _userId;
            set { if (_userId != value) { _userId = value; OnPropertyChanged(); } }
        }

        public string? Username
        {
            get => _username;
            set { if (_username != value) { _username = value; OnPropertyChanged(); } }
        }

        public DateTime LoginTime
        {
            get => _loginTime;
            set { if (_loginTime != value) { _loginTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoginTimeDisplay)); } }
        }

        public DateTime? LogoutTime
        {
            get => _logoutTime;
            set { if (_logoutTime != value) { _logoutTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(LogoutTimeDisplay)); OnPropertyChanged(nameof(SessionDuration)); } }
        }

        public string? ComputerName
        {
            get => _computerName;
            set { if (_computerName != value) { _computerName = value; OnPropertyChanged(); } }
        }

        public string? IpAddress
        {
            get => _ipAddress;
            set { if (_ipAddress != value) { _ipAddress = value; OnPropertyChanged(); } }
        }

        public bool Success
        {
            get => _success;
            set { if (_success != value) { _success = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); } }
        }

        public string? FailureReason
        {
            get => _failureReason;
            set { if (_failureReason != value) { _failureReason = value; OnPropertyChanged(); } }
        }

        // Pomocnicze właściwości dla UI
        public string LoginTimeDisplay => LoginTime.ToString("dd.MM.yyyy HH:mm:ss");

        public string LogoutTimeDisplay => LogoutTime.HasValue
            ? LogoutTime.Value.ToString("dd.MM.yyyy HH:mm:ss")
            : "-";

        public string SessionDuration
        {
            get
            {
                if (!LogoutTime.HasValue) return "Aktywna";

                var duration = LogoutTime.Value - LoginTime;
                if (duration.TotalMinutes < 1) return $"{(int)duration.TotalSeconds}s";
                if (duration.TotalHours < 1) return $"{(int)duration.TotalMinutes}min";
                return $"{(int)duration.TotalHours}h {duration.Minutes}min";
            }
        }

        public string StatusDisplay => Success ? "? Sukces" : "? Błąd";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
