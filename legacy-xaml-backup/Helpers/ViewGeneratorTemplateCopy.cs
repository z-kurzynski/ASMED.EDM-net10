using System;
using System.IO;
using System.Text;

namespace ASMED.WPF.Helpers
{
    public static class ViewGeneratorTemplateCopy
    {
        /// <summary>
        /// Kopiuje istniej¹cy widok (XAML, XAML.CS, ViewModel) pod now¹ nazwê, automatycznie podmieniaj¹c nazwy klas i namespace.
        /// Pozwala na podanie podkatalogów, np. "Skierowania/Skierowania" lub "pacjent/ListaPacjentow".
        /// </summary>
        /// <param name="sourceName">np. "Skierowania/Skierowania" (podkatalog/nazwaWidoku)</param>
        /// <param name="targetName">np. "NowyFolder/NowyWidok" (podkatalog/nazwaNowegoWidoku)</param>
        public static void CopyViewWithViewModel(string sourceName, string targetName)
        {
            string srcRoot = @"A:\source\repos\ASMED-WPF-Application\src\ASMED_3";
            string viewModelsPath = Path.Combine(srcRoot, "ViewModels");

            // Rozbij na katalog i nazwê pliku
            string sourceDir = Path.GetDirectoryName(sourceName.Replace('\\', '/')) ?? "";
            string sourceBase = Path.GetFileName(sourceName.Replace('\\', '/'));
            string targetDir = Path.GetDirectoryName(targetName.Replace('\\', '/')) ?? "";
            string targetBase = Path.GetFileName(targetName.Replace('\\', '/'));

            // Œcie¿ki do katalogów
            string sourceViewsPath = Path.Combine(srcRoot, "Views", sourceDir?.ToLower() ?? "");
            string targetViewsPath = Path.Combine(srcRoot, "Views", targetDir?.ToLower() ?? "");

            // Pliki Ÿród³owe
            string srcXaml = Path.Combine(sourceViewsPath, sourceBase + "View.xaml");
            string srcXamlCs = Path.Combine(sourceViewsPath, sourceBase + "View.xaml.cs");
            string srcVm = Path.Combine(viewModelsPath, sourceBase + "ViewModel.cs");

            // Pliki docelowe
            string dstXaml = Path.Combine(targetViewsPath, targetBase + "View.xaml");
            string dstXamlCs = Path.Combine(targetViewsPath, targetBase + "View.xaml.cs");
            string dstVm = Path.Combine(viewModelsPath, targetBase + "ViewModel.cs");

            Directory.CreateDirectory(targetViewsPath);

            // Namespace dla podkatalogów
            string sourceNamespace = "ASMED.WPF.Views" + (string.IsNullOrEmpty(sourceDir) ? "" : "." + sourceDir.Replace('/', '.').Replace('\\', '.'));
            string targetNamespace = "ASMED.WPF.Views" + (string.IsNullOrEmpty(targetDir) ? "" : "." + targetDir.Replace('/', '.').Replace('\\', '.'));

            // Kopiowanie i podmiana nazw w XAML
            string xamlContent = File.ReadAllText(srcXaml, Encoding.UTF8)
                .Replace(sourceBase + "View", targetBase + "View")
                .Replace("x:Class=\"" + sourceNamespace + "." + sourceBase + "View\"", "x:Class=\"" + targetNamespace + "." + targetBase + "View\"");
            File.WriteAllText(dstXaml, xamlContent, Encoding.UTF8);

            // Kopiowanie i podmiana nazw w code-behind
            string xamlCsContent = File.ReadAllText(srcXamlCs, Encoding.UTF8)
                .Replace(sourceBase + "View", targetBase + "View")
                .Replace(sourceBase + "ViewModel", targetBase + "ViewModel")
                .Replace(sourceNamespace, targetNamespace);
            File.WriteAllText(dstXamlCs, xamlCsContent, Encoding.UTF8);

            // Kopiowanie i podmiana nazw w ViewModelu
            string vmContent = File.ReadAllText(srcVm, Encoding.UTF8)
                .Replace(sourceBase + "ViewModel", targetBase + "ViewModel")
                .Replace("_" + sourceBase.ToLower() + "ViewModel", "_" + targetBase.ToLower() + "ViewModel");
            File.WriteAllText(dstVm, vmContent, Encoding.UTF8);
        }
    }
}
