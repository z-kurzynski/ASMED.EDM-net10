# 🛠️ ASMED Migration Helper Scripts
# Ułatwiają migrację modułów z legacy app do nowego projektu

$LegacyRoot = "A:\source\repos\ASMED-WPF-Application\src\ASMED_5"
$NewRoot = "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI"

# ================================================================================
# FUNKCJA 1: Kopiowanie View z legacy do nowego projektu
# ================================================================================
function Copy-LegacyView {
	<#
	.SYNOPSIS
		Kopiuje pliki View z legacy projektu do nowego

	.EXAMPLE
		Copy-LegacyView -OldModule "wizytyview" -NewModule "Visits"
	#>
	param(
		[Parameter(Mandatory=$true)]
		[string]$OldModule,  # np. "wizytyview"

		[Parameter(Mandatory=$true)]
		[string]$NewModule   # np. "Visits"
	)

	$source = Join-Path $LegacyRoot "Views\$OldModule"
	$dest = Join-Path $NewRoot "Views\$NewModule"

	if (-not (Test-Path $source)) {
		Write-Host "❌ Source not found: $source" -ForegroundColor Red
		return
	}

	if (-not (Test-Path $dest)) {
		New-Item -ItemType Directory -Path $dest -Force | Out-Null
		Write-Host "📁 Created directory: $dest" -ForegroundColor Cyan
	}

	# Kopiuj pliki XAML i code-behind
	$files = Get-ChildItem -Path $source -Filter "*.xaml*"
	foreach ($file in $files) {
		Copy-Item -Path $file.FullName -Destination $dest -Force
		Write-Host "  ✅ Copied: $($file.Name)" -ForegroundColor Green
	}

	Write-Host "`n✨ Migration ready! Next steps:" -ForegroundColor Yellow
	Write-Host "  1. Update namespaces in .cs files" -ForegroundColor Gray
	Write-Host "  2. Update x:Class in .xaml files" -ForegroundColor Gray
	Write-Host "  3. Add DI constructor pattern" -ForegroundColor Gray
	Write-Host "  4. Create ViewModel" -ForegroundColor Gray
}

# ================================================================================
# FUNKCJA 2: Tworzenie nowego ViewModel
# ================================================================================
function New-AsmedViewModel {
	<#
	.SYNOPSIS
		Tworzy nowy ViewModel z templatem

	.EXAMPLE
		New-AsmedViewModel -Name "Visits"
	#>
	param(
		[Parameter(Mandatory=$true)]
		[string]$Name  # np. "Visits"
	)

	$path = Join-Path $NewRoot "ViewModels\$($Name)ViewModel.cs"

	if (Test-Path $path) {
		Write-Host "⚠️  File already exists: $path" -ForegroundColor Yellow
		$response = Read-Host "Overwrite? (y/n)"
		if ($response -ne 'y') {
			return
		}
	}

	$template = @"
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla modułu $Name
/// </summary>
public partial class $($Name)ViewModel : ViewModelBase
{
	private readonly ILogger<$($Name)ViewModel> _logger;

	public $($Name)ViewModel(ILogger<$($Name)ViewModel> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_logger.LogInformation("$($Name)ViewModel initialized");
	}

	#region Properties

	[ObservableProperty]
	private ObservableCollection<object> _items = new();

	[ObservableProperty]
	private object? _selectedItem;

	[ObservableProperty]
	private string _searchText = string.Empty;

	#endregion

	#region Commands

	[RelayCommand]
	private async Task LoadDataAsync()
	{
		try
		{
			// TODO: Implement data loading
			_logger.LogInformation("Loading $Name data...");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading $Name data");
		}
	}

	[RelayCommand]
	private void Search()
	{
		// TODO: Implement search logic
	}

	#endregion
}
"@

	Set-Content -Path $path -Value $template -Encoding UTF8
	Write-Host "✅ Created: $($Name)ViewModel.cs" -ForegroundColor Green
	Write-Host "📂 Location: $path" -ForegroundColor Cyan
}

# ================================================================================
# FUNKCJA 3: Analiza legacy view (bez modyfikacji)
# ================================================================================
function Analyze-LegacyView {
	<#
	.SYNOPSIS
		Analizuje legacy view i pokazuje szczegóły

	.EXAMPLE
		Analyze-LegacyView -Module "wizytyview"
	#>
	param(
		[Parameter(Mandatory=$true)]
		[string]$Module
	)

	$viewPath = Join-Path $LegacyRoot "Views\$Module"

	if (-not (Test-Path $viewPath)) {
		Write-Host "❌ Module not found: $viewPath" -ForegroundColor Red
		return
	}

	Write-Host "`n🔍 ANALYZING: $Module" -ForegroundColor Cyan
	Write-Host "=" * 60 -ForegroundColor Gray

	# Lista plików
	Write-Host "`n📁 Files:" -ForegroundColor Yellow
	Get-ChildItem -Path $viewPath | ForEach-Object {
		Write-Host "  - $($_.Name)" -ForegroundColor Gray
	}

	# Szukaj kontrolek Syncfusion w XAML
	$xamlFiles = Get-ChildItem -Path $viewPath -Filter "*.xaml"
	if ($xamlFiles) {
		Write-Host "`n🎨 Syncfusion Controls Found:" -ForegroundColor Yellow
		foreach ($xaml in $xamlFiles) {
			$content = Get-Content $xaml.FullName -Raw

			if ($content -match 'syncfusion:(\w+)') {
				$matches.Values | Where-Object { $_ -ne $content } | Sort-Object -Unique | ForEach-Object {
					Write-Host "  - $_" -ForegroundColor Green
				}
			}
		}
	}

	# Szukaj ViewModels
	Write-Host "`n🧩 Code-Behind:" -ForegroundColor Yellow
	$csFiles = Get-ChildItem -Path $viewPath -Filter "*.xaml.cs"
	foreach ($cs in $csFiles) {
		$content = Get-Content $cs.FullName -Raw

		# Namespace
		if ($content -match 'namespace\s+([\w\.]+)') {
			Write-Host "  Namespace: $($matches[1])" -ForegroundColor Gray
		}

		# Constructor
		if ($content -match 'public\s+(\w+)\s*\(([^)]*)\)') {
			Write-Host "  Constructor: $($matches[1])($($matches[2]))" -ForegroundColor Gray
		}
	}

	Write-Host "`n" + "=" * 60 -ForegroundColor Gray
}

# ================================================================================
# FUNKCJA 4: Update App.xaml.cs z rejestracją DI
# ================================================================================
function Add-DiRegistration {
	<#
	.SYNOPSIS
		Dodaje rejestrację View i ViewModel do App.xaml.cs

	.EXAMPLE
		Add-DiRegistration -Module "Visits"
	#>
	param(
		[Parameter(Mandatory=$true)]
		[string]$Module
	)

	$appPath = Join-Path $NewRoot "App.xaml.cs"
	$content = Get-Content $appPath -Raw

	$viewRegistration = "        services.AddTransient<Views.$Module.$($Module)View>();"
	$vmRegistration = "        services.AddTransient<ViewModels.$($Module)ViewModel>();"

	Write-Host "📝 Add these lines to App.xaml.cs ConfigureServices:" -ForegroundColor Cyan
	Write-Host $viewRegistration -ForegroundColor Green
	Write-Host $vmRegistration -ForegroundColor Green

	Write-Host "`n⚠️  Remember to:" -ForegroundColor Yellow
	Write-Host "  1. Add IService registration if needed" -ForegroundColor Gray
	Write-Host "  2. Add to MainWindow.xaml TabControl" -ForegroundColor Gray
}

# ================================================================================
# FUNKCJA 5: Migracja kompletnego modułu (orchestrator)
# ================================================================================
function Start-ModuleMigration {
	<#
	.SYNOPSIS
		Rozpoczyna pełną migrację modułu

	.EXAMPLE
		Start-ModuleMigration -OldModule "wizytyview" -NewModule "Visits"
	#>
	param(
		[Parameter(Mandatory=$true)]
		[string]$OldModule,

		[Parameter(Mandatory=$true)]
		[string]$NewModule
	)

	Write-Host "`n🚀 STARTING MIGRATION: $OldModule → $NewModule" -ForegroundColor Cyan
	Write-Host "=" * 60 -ForegroundColor Gray

	# Krok 1: Analiza
	Write-Host "`n📋 Step 1: Analyzing legacy module..." -ForegroundColor Yellow
	Analyze-LegacyView -Module $OldModule

	Read-Host "`nPress Enter to continue"

	# Krok 2: Kopiowanie
	Write-Host "`n📋 Step 2: Copying files..." -ForegroundColor Yellow
	Copy-LegacyView -OldModule $OldModule -NewModule $NewModule

	Read-Host "`nPress Enter to continue"

	# Krok 3: Tworzenie ViewModel
	Write-Host "`n📋 Step 3: Creating ViewModel..." -ForegroundColor Yellow
	New-AsmedViewModel -Name $NewModule

	# Krok 4: Instrukcje DI
	Write-Host "`n📋 Step 4: DI Registration..." -ForegroundColor Yellow
	Add-DiRegistration -Module $NewModule

	# Podsumowanie
	Write-Host "`n✨ MIGRATION SETUP COMPLETE!" -ForegroundColor Green
	Write-Host "`n📝 MANUAL STEPS REQUIRED:" -ForegroundColor Yellow
	Write-Host "  1. Update namespaces in View files" -ForegroundColor Gray
	Write-Host "  2. Add DI constructor pattern to View code-behind" -ForegroundColor Gray
	Write-Host "  3. Update XAML bindings if needed" -ForegroundColor Gray
	Write-Host "  4. Register in App.xaml.cs" -ForegroundColor Gray
	Write-Host "  5. Add to MainWindow.xaml TabControl" -ForegroundColor Gray
	Write-Host "  6. Build and test" -ForegroundColor Gray
	Write-Host "  7. Git commit" -ForegroundColor Gray
}

# ================================================================================
# PRZYKŁADY UŻYCIA
# ================================================================================
Write-Host @"

🎯 ASMED MIGRATION HELPERS LOADED

Available functions:
  Copy-LegacyView -OldModule "wizytyview" -NewModule "Visits"
  New-AsmedViewModel -Name "Visits"
  Analyze-LegacyView -Module "wizytyview"
  Add-DiRegistration -Module "Visits"
  Start-ModuleMigration -OldModule "wizytyview" -NewModule "Visits"

Example workflow:
  Start-ModuleMigration -OldModule "wizytyview" -NewModule "Visits"

"@ -ForegroundColor Cyan

# Export functions
Export-ModuleMember -Function Copy-LegacyView, New-AsmedViewModel, Analyze-LegacyView, Add-DiRegistration, Start-ModuleMigration
