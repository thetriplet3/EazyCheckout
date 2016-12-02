using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using SharpSvn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlCreateTagFromTrunk.xaml
    /// </summary>
    public partial class UserControlCreateTagFromTrunk : UserControl
    {
        private BackgroundWorker backgroundWorkerLoad;

        private delegate void backgroundWorkerLoad_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

        private SvnListEventArgs selectedTrunk;

        private IfsSvn myIfsSvn;

        public UserControlCreateTagFromTrunk()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerLoad = new BackgroundWorker();
            this.backgroundWorkerLoad.WorkerSupportsCancellation = true;
            this.backgroundWorkerLoad.DoWork += new DoWorkEventHandler(this.backgroundWorkerLoad_DoWork);
            this.backgroundWorkerLoad.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerLoad_RunWorkerCompleted);
        }

        internal void SetSelectedTrunk(SvnListEventArgs trunk)
        {
            this.selectedTrunk = trunk;
            textBoxTagName.Text = myIfsSvn.GetNewTagName(this.selectedTrunk);
        }

        private void backgroundWorkerLoad_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Do not access the form's BackgroundWorker reference directly.
                // Instead, use the reference provided by the sender parameter.
                BackgroundWorker worker = sender as BackgroundWorker;

                if (worker.CancellationPending == false)
                {
                    TagArguments arg = e.Argument as TagArguments;

                    if (arg.Type == JobType.CreateTag)
                    {
                        e.Result = myIfsSvn.CreateTag(arg.SelectedTrunk, arg.TagName);
                    }
                }

                // If the operation was canceled by the user,
                // set the DoWorkEventArgs.Cancel property to true.
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                }
            }
            catch (Exception)
            {
                //throw the exception so that RunWorkerCompleted can catch it.
                throw;
            }
        }

        private void backgroundWorkerLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.Invoke(new backgroundWorkerLoad_RunWorkerCompletedDelegate(backgroundWorkerLoad_RunWorkerCompleted), new object[] { sender, e });
            }
            else
            {
                try
                {
                    if (e.Error != null)
                    {
                        ModernDialog.ShowMessage(e.Error.Message, "Error setting Log", MessageBoxButton.OK);
                    }
                    else if (e.Cancelled)
                    {
                        //    textBoxLog.AppendText("Cancelled!\r\n");
                    }
                    else
                    {
                        if (e.Result != null)
                        {
                            if (e.Result is bool)
                            {
                                if ((bool)e.Result)
                                {
                                    ModernDialog.ShowMessage("OK", "Creating Tag", MessageBoxButton.OK);
                                }
                                else
                                {
                                    ModernDialog.ShowMessage("Was not Created.", "Creating Tag", MessageBoxButton.OK);
                                }
                            }                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                }
                finally
                {
                    progressBarMain.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void buttonCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (backgroundWorkerLoad.IsBusy == false)
                {                    
                    progressBarMain.Visibility = System.Windows.Visibility.Visible;

                    backgroundWorkerLoad.RunWorkerAsync(new TagArguments(JobType.CreateTag) { SelectedTrunk = this.selectedTrunk, TagName = textBoxTagName.Text.Trim() });
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Creating Tag", MessageBoxButton.OK);
            }
        }
    }
}
