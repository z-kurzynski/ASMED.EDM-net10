using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels.Badania;
using ASMED.EDM.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.Graphics.Printing.Workflow;

namespace ASMED.EDM.UI.Views.badania;

public partial class BadaniaEditNewView : UserControl
{
    public BadaniaEditNewView()
    {
        Console.WriteLine("🚀 BadaniaEditNewView: Constructor started");
        InitializeComponent();
        Console.WriteLine("🔧 BadaniaEditNewView: InitializeComponent completed");

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                var dbFactory = app.Host.Services.GetService<DbConnectionFactory>();
                DataContext = new BadaniaEditNewViewModel(dbFactory);
                Console.WriteLine("✅ BadaniaEditNewView: DataContext set with DbConnectionFactory");
            }
            else
            {
                // Fallback bez DI (nie powinno się zdarzyć)
                DataContext = new BadaniaEditNewViewModel(null);
                Console.WriteLine("⚠️ BadaniaEditNewView: DataContext set WITHOUT DbConnectionFactory");
            }
        }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BadaniaEditNewViewModel vm)
        {
            vm.FilterText = string.Empty;
        }
    }

    private void Lista_Badan_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Navigate to Lista Badań
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Event handler for TextBox text changes (e.g., Nr Księgi)
        // Binding handles the update automatically
    }

    private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Event handler for price TextBox changes
        // Auto-calculate total if needed
        if (DataContext is BadaniaEditNewViewModel vm)
        {
           vm.RecalculateTotalPrice();
        }
    }

    private void ToggleNewExamination_Click(object sender, RoutedEventArgs e)
    {
        // Handle toggle button clicks for examination types
        if (sender is Button button && DataContext is BadaniaEditNewViewModel vm)
        {
            string? examType = button.Tag?.ToString();
            if (!string.IsNullOrEmpty(examType))
            {
                vm.ToggleExamination(examType);
            }
        }
    }

    private void DeleteBadanie_Click(object sender, RoutedEventArgs e)
    {
        // Handle delete button click
        if (DataContext is BadaniaEditNewViewModel vm)
        {
            
        }
    }
}
