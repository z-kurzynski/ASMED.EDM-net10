using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ASMED.EDM.UI.Views.wizytyview;

public partial class SelectPatientDialog : Window
{
    public object? SelectedItem => CandidatesListBox.SelectedItem;

    public SelectPatientDialog(System.Collections.IEnumerable items, string initialFilter = "")
    {
        InitializeComponent();
        _allItems = items.Cast<object?>().ToList();
        CandidatesListBox.ItemsSource = _allItems;

        FilterTextBox.TextChanged += FilterTextBox_TextChanged;
        this.Loaded += SelectPatientDialog_Loaded;

        if (!string.IsNullOrEmpty(initialFilter))
        {
            // ustaw wstępny filtr — spowoduje również selekcję elementów przez TextChanged
            FilterTextBox.Text = initialFilter;
        }
    }

    private void SelectPatientDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        // ustaw fokus na polu filtra i zaznacz cały tekst, aby użytkownik mógł kontynuować wpisywanie
        FilterTextBox.Focus();
        try { FilterTextBox.SelectAll(); } catch { }
        // ustaw pierwszy element jako wybrany, jeśli istnieje
        if (CandidatesListBox.Items.Count > 0)
            CandidatesListBox.SelectedIndex = 0;
    }

    private readonly List<object?> _allItems = new();

    private void FilterTextBox_TextChanged(object? sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var q = FilterTextBox.Text.Trim();
        if (string.IsNullOrEmpty(q))
        {
            CandidatesListBox.ItemsSource = _allItems;
            return;
        }

        var lower = q.ToLowerInvariant();
        var filtered = _allItems.Where(it =>
        {
            if (it == null) return false;
            var props = it.GetType().GetProperties();
            foreach (var p in props)
            {
                var val = p.GetValue(it)?.ToString();
                if (!string.IsNullOrEmpty(val) && val.ToLowerInvariant().Contains(lower, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }).ToList();

        CandidatesListBox.ItemsSource = filtered;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (CandidatesListBox.SelectedItem == null)
        {
            MessageBox.Show("Wybierz pacjenta z listy.", "Brak wyboru", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
