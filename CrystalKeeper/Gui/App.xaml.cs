using CrystalKeeper.Gui;
using System.Windows;

namespace CrystalKeeper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Starts the application.
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            DlgNewProject dlg = new DlgNewProject();
            dlg.Show();
        }
    }
}
