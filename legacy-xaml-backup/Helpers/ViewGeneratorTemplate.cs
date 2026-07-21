using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace ASMED.WPF.Helpers
{
    public static class ViewGeneratorTemplate
    {
        public static void GenerateView(string name, string viewsDir, string viewModelsDir)
        {
            string srcRoot = @"A:\source\repos\ASMED-WPF-Application\src\ASMED_3";
            string viewsPath = Path.Combine(srcRoot, "Views", name.ToLower());
            string viewModelsPath = Path.Combine(srcRoot, "ViewModels");

            string xamlName = name + "View.xaml";
            string xamlCsName = name + "View.xaml.cs";
            string vmName = name + "ViewModel.cs";
            string ns = "ASMED.WPF";

            string xaml = "<UserControl x:Class=\"" + ns + ".Views." + name + "View\"\n"
                + "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n"
                + "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n"
                + "    xmlns:vm=\"clr-namespace:" + ns + ".ViewModels\"\n"
                + "    Height=\"Auto\" Width=\"Auto\" VerticalAlignment=\"Top\">\n"
                + "    <Grid>\n"
                + "         <Grid.RowDefinitions >\n"
                + "            <RowDefinition Height = \"80\" />\n"
                + "            <RowDefinition Height =\" *\" />\n"
                + "        </Grid.RowDefinitions >\n"
                + "      <StackPanel Grid.Row=\"0\" Margin=\"10\">\r\n"
                + "          <TextBlock Text=\"" + name + "View  row 0- szablon\" FontSize=\"24\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Top\"/>\n"
                + "      </StackPanel>\n"
                + "      <StackPanel Grid.Row=\"1\" Margin=\"10\">\r\n"
                + "        <TextBlock Text=\"" + name + "View  row 1- szablon\" FontSize=\"24\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Top\"/>\n"
                + "      </StackPanel>\n"
                + "    </Grid>\n"
                + "</UserControl>\n";

            string xamlCs = "using " + ns + ".ViewModels;\n"
                + "using System.Windows.Controls;\n\n"
                + "namespace " + ns + ".Views\n"
                + "{\n"
                + "    public partial class " + name + "View : UserControl\n"
                + "    {\n"
                + "        public " + name + "View()\n"
                + "        {\n"
                + "            InitializeComponent();\n"
                + "            DataContext = new " + name + "ViewModel();\n"
                + "        }\n"
                + "    }\n"
                + "}\n";

            string viewModel = "using System.ComponentModel;\n"
                + "using System.Runtime.CompilerServices;\n\n"
                + "namespace " + ns + ".ViewModels\n"
                + "{\n"
                + "    public class " + name + "ViewModel : INotifyPropertyChanged\n"
                + "    {\n"
                + "        public event PropertyChangedEventHandler PropertyChanged;\n"
                + "        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)\n"
                + "        {\n"
                + "            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));\n"
                + "        }\n\n"
                + "        private string _testText = \"" + name + "ViewModel działa\";\n"
                + "        public string TestText\n"
                + "        {\n"
                + "            get => _testText;\n"
                + "            set\n"
                + "            {\n"
                + "                if (_testText != value)\n"
                + "                {\n"
                + "                    _testText = value;\n"
                + "                    OnPropertyChanged();\n"
                + "                }\n"
                + "            }\n"
                + "        }\n"
                + "    }\n"
                + "}\n";

            Directory.CreateDirectory(viewsPath);
            Directory.CreateDirectory(viewModelsPath);
            File.WriteAllText(Path.Combine(viewsPath, xamlName), xaml, System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(viewsPath, xamlCsName), xamlCs, System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(viewModelsPath, vmName), viewModel, System.Text.Encoding.UTF8);
        }
    }
}
