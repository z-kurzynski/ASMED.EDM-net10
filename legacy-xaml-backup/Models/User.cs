using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Models
{
    /// <summary>
    /// Model użytkownika systemu
    /// </summary>
    public class User : INotifyPropertyChanged
    {
        private int _id;
        private string _username = string.Empty;
        private string _passwordHash = string.Empty;
        private string _email = string.Empty;
        private string _fullName = string.Empty;
        private UserRole _role;
        private bool _isActive;
        private DateTime? _createdDate;
        private DateTime? _lastLogin;

        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public string Username
        {
            get => _username;
            set { if (_username != value) { _username = value; OnPropertyChanged(); } }
        }

        public string PasswordHash
        {
            get => _passwordHash;
            set { if (_passwordHash != value) { _passwordHash = value; OnPropertyChanged(); } }
        }

        public string Email
        {
            get => _email;
            set { if (_email != value) { _email = value; OnPropertyChanged(); } }
        }

        public string FullName
        {
            get => _fullName;
            set { if (_fullName != value) { _fullName = value; OnPropertyChanged(); } }
        }

        public UserRole Role
        {
            get => _role;
            set { if (_role != value) { _role = value; OnPropertyChanged(); OnPropertyChanged(nameof(RoleDisplay)); } }
        }

        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); } }
        }

        public DateTime? CreatedDate
        {
            get => _createdDate;
            set { if (_createdDate != value) { _createdDate = value; OnPropertyChanged(); } }
        }

        public DateTime? LastLogin
        {
            get => _lastLogin;
            set { if (_lastLogin != value) { _lastLogin = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastLoginDisplay)); } }
        }

        private DateTime? _endLogin;
        public DateTime? EndLogin
        {
            get => _endLogin;
            set { if (_endLogin != value) { _endLogin = value; OnPropertyChanged(); OnPropertyChanged(nameof(EndLoginDisplay)); } }
        }

        // ✅ Pomocnicze właściwości dla UI
        public string RoleDisplay => Role switch
        {
            UserRole.SuperAdmin => "🔑 Super Admin",
            UserRole.Admin => "👑 Administrator",
            UserRole.Recepcja => "📞 Recepcja",
            UserRole.Lekarz => "👨‍⚕️ Lekarz",
            UserRole.Biuro => "📋 Biuro",
            _ => "❓ Nieznana"
        };

        public string StatusDisplay => IsActive ? "✅ Aktywny" : "❌ Nieaktywny";

        public string LastLoginDisplay => LastLogin.HasValue
            ? LastLogin.Value.ToString("dd.MM.yyyy HH:mm")
            : "Nigdy";

        public string EndLoginDisplay => EndLogin.HasValue
            ? EndLogin.Value.ToString("dd.MM.yyyy HH:mm")
            : "-";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
