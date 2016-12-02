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
using System.ComponentModel;
using SharpSvn;
using SharpSvn.UI;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using IfsSvnClient.Classes;
using FirstFloor.ModernUI.Windows.Controls;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlSearchBranch.xaml
    /// </summary>
    public partial class UserControlSearchBranch : UserControl
    {
        private BackgroundWorker backgroundWorkerFind;

        private delegate void backgroundWorkerFind_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);
        private delegate void backgroundWorkerFind_ProgressChangedDelegate(object sender, ProgressChangedEventArgs e);

        public UserControlSearchBranch()
        {
            InitializeComponent();

            backgroundWorkerFind = new BackgroundWorker();

            backgroundWorkerFind.WorkerReportsProgress = true;
            backgroundWorkerFind.WorkerSupportsCancellation = true;
            backgroundWorkerFind.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerFind_DoWork);
            backgroundWorkerFind.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorkerFind_ProgressChanged);
            backgroundWorkerFind.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerFind_RunWorkerCompleted);            
        }

        private void buttonFind_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                if (backgroundWorkerFind.IsBusy == false &&
                    buttonFind.Content.ToString() == "Find")
                {
                    SearchArguments arg = new SearchArguments(textBoxPath.Text, textBoxPattern.Text);
                    buttonFind.Content = "Cancel";
                    progressBarSearch.Value = 0;
                    textBoxLog.Text = "Stating Log!\r\n";
                    backgroundWorkerFind.RunWorkerAsync(arg);
                }
                else if (buttonFind.Content.ToString() == "Cancel")
                {
                    backgroundWorkerFind.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        private void GetUriList(BackgroundWorker worker, DoWorkEventArgs e, SearchArguments arg)
        {
            using (SvnClient client = new SvnClient())
            {
                // Bind the SharpSvn UI to our client for SSL certificate and credentials
                SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                SvnUI.Bind(client, bindArgs);

                Collection<SvnListEventArgs> componentList;
                client.GetList(arg.ComponentListUri, out componentList);

                Collection<SvnListEventArgs> branchList;
                SvnUriTarget branchListUri;
                double currentProgress = 0;
                double progressIncrement = 0;
                int itemcount = componentList.Count();
                if (itemcount > 0)
                {
                    progressIncrement = 100.00 / itemcount;
                }

                foreach (SvnListEventArgs component in componentList)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        currentProgress += progressIncrement;
                        worker.ReportProgress(Convert.ToInt32(currentProgress));

                        if (string.IsNullOrWhiteSpace(component.Path) == false)
                        {
                            branchListUri = new SvnUriTarget(component.Uri + @"branches/project");

                            try
                            {
                                client.GetList(branchListUri, out branchList);

                                bool match = false;
                                foreach (SvnListEventArgs branch in branchList)
                                {
                                    if (worker.CancellationPending)
                                    {
                                        e.Cancel = true;
                                    }
                                    else
                                    {
                                        match = Regex.IsMatch(branch.Name, arg.SearchPattern, RegexOptions.IgnoreCase);
                                        if (match)
                                        {
                                            worker.ReportProgress(Convert.ToInt32(currentProgress), new SearchResults(component.Path, arg.RootUri.Uri, branch.Uri));
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
        }

        private void backgroundWorkerFind_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Do not access the form's BackgroundWorker reference directly.
                // Instead, use the reference provided by the sender parameter.
                BackgroundWorker worker = sender as BackgroundWorker;

                if (worker.CancellationPending == false)
                {
                    SearchArguments arg = e.Argument as SearchArguments;
                    this.GetUriList(worker, e, arg);
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

        private void backgroundWorkerFind_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.Invoke(new backgroundWorkerFind_RunWorkerCompletedDelegate(backgroundWorkerFind_RunWorkerCompleted), new object[] { sender, e });
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
                        textBoxLog.AppendText("Cancelled!\r\n");
                    }
                    else
                    {
                        textBoxLog.AppendText("Done!\r\n");
                        progressBarSearch.Value = 100;
                    }
                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                }
                finally
                {                    
                    buttonFind.Content = "Find";
                }
            }
        }

        private void backgroundWorkerFind_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (this.Dispatcher.CheckAccess() == false)
                {
                    this.Dispatcher.Invoke(new backgroundWorkerFind_ProgressChangedDelegate(backgroundWorkerFind_ProgressChanged), new object[] { sender, e });
                }
                else
                {
                    progressBarSearch.Value = e.ProgressPercentage;

                    if (e.UserState != null && e.UserState is SearchResults)
                    {
                        textBoxLog.AppendText(e.UserState.ToString());
                        textBoxLog.ScrollToEnd();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
