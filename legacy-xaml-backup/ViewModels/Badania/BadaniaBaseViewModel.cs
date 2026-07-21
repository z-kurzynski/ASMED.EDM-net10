using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.ViewModels.Badania
{
 public abstract class BadaniaBaseViewModel : INotifyPropertyChanged
 {
 public event PropertyChangedEventHandler? PropertyChanged;
 protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

 public ObservableCollection<string> CennikOptions { get; } = new ObservableCollection<string>();
 public ObservableCollection<string> FilterOptions { get; } = new ObservableCollection<string>
 {
 "All", "Imie", "Nazwisko", "Pesel", "Firma", "ID", "Data"
 };
 }
}