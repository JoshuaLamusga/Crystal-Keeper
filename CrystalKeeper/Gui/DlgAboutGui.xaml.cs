using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Interaction logic for DlgAboutGui.xaml
    /// </summary>
    public partial class DlgAboutGui : Window
    {
        public DlgAboutGui()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Returns the version number of the application.
        /// </summary>
        private string GetVersion()
        {
            return FileVersionInfo.GetVersionInfo
                (Assembly.GetExecutingAssembly().Location)
                .ProductVersion;
        }
    }
}
