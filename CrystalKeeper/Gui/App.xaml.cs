using CrystalKeeper.Core;
using CrystalKeeper.Gui;
using System.IO;
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
            //Loads associated extension files and files by command-line.
            if (e.Args.Length >= 1)
            {
                //Loads the project and displays the main interface with it.
                if (File.Exists(e.Args[0]))
                {
                    MainDisplay display = new MainDisplay(
                        Project.Load(e.Args[0]), e.Args[0]);

                    display.Show();
                }

                //Displays a new project dialog on failure to load.
                else
                {
                    DlgNewProject dlg = new DlgNewProject();
                    dlg.Show();
                }
            }

            //Displays a new project dialog for new files.
            else
            {
                DlgNewProject dlg = new DlgNewProject();
                dlg.Show();
            }
        }
    }
}
