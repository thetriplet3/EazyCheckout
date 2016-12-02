using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using NLog;
using System;
using System.Deployment.Application;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlSettings.xaml
    /// </summary>
    public partial class UserControlSettings : UserControl
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private NotifierLync myNotifierLync;

        public UserControlSettings()
        {
            InitializeComponent();
        }

        private void button_BackUpLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please Select Root folder to copy logs";

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    logger.Info("button_BackUpLog: {0}", dialog.SelectedPath);

                    string logDestinationFolderPath = dialog.SelectedPath + @"\logs";
                    string userExperienceDestinationFolderPath = dialog.SelectedPath + @"\userExperience";

                    if (Directory.Exists(logDestinationFolderPath) == false)
                    {
                        Directory.CreateDirectory(logDestinationFolderPath);
                    }
                    if (Directory.Exists(userExperienceDestinationFolderPath) == false)
                    {
                        Directory.CreateDirectory(userExperienceDestinationFolderPath);
                    }

                    string sourceFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\IfsSvnClient";

                    string logSourceFolderPath = sourceFolderPath + @"\logs";
                    string userExperienceSourceFolderPath = sourceFolderPath + @"\userExperience";

                    if (Directory.Exists(logSourceFolderPath))
                    {
                        DirectoryInfo logsSourceFolder = new DirectoryInfo(logSourceFolderPath);
                        foreach (FileInfo logfile in logsSourceFolder.GetFiles())
                        {
                            logfile.CopyTo(logDestinationFolderPath + @"\" + logfile.Name);
                        }
                    }

                    if (Directory.Exists(userExperienceSourceFolderPath))
                    {
                        DirectoryInfo userExperienceSourceFolder = new DirectoryInfo(userExperienceSourceFolderPath);
                        foreach (FileInfo userExperiencefile in userExperienceSourceFolder.GetFiles())
                        {
                            userExperiencefile.CopyTo(userExperienceDestinationFolderPath + @"\" + userExperiencefile.Name);
                        }
                    }

                    ModernDialog.ShowMessage("Log Copy", "Log Files copied.", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error backing up logs", MessageBoxButton.OK);
                logger.Error(ex, "Error backing up logs");
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.Reset();

                ModernDialog.ShowMessage("Settings Reset OK", "Settings Reset", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Resetting Property Settings");
            }
        }

        private void buttonContactSupport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myNotifierLync != null)
                {
                    myNotifierLync.SendMessageToSupport();
                }
                logger.Info("buttonContactSupport: {0}", Properties.Settings.Default.SupportPerson);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error contacting support", MessageBoxButton.OK);
                logger.Error(ex, "Error contacting support");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    Version ProductVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion;

                    textBoxPublishVersion.Text = ProductVersion.ToString();
                }
                myNotifierLync = new NotifierLync();
            }
            catch (Exception)
            {
                buttonContactSupport.IsEnabled = false;
            }
        }
    }
}