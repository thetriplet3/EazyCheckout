using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FirstFloor.ModernUI.Windows.Controls;

namespace IfsSvnClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ModernWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {                                                
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri) ||
                    string.IsNullOrWhiteSpace(Properties.Settings.Default.SupportPerson))
                {
                    this.ContentSource = linkSVNServer.Source;
                }
            }
            catch (Exception)
            {
                
            }
        }
    }
}
