using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using SharpSvn;
using SharpSvn.UI;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlFindStaticComponents.xaml
    /// </summary>
    public partial class UserControlFindStaticComponents : UserControl
    {
        private BackgroundWorker backgroundWorkerFind;

        private delegate void backgroundWorkerFind_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

        private delegate void backgroundWorkerFind_ProgressChangedDelegate(object sender, ProgressChangedEventArgs e);

        public UserControlFindStaticComponents()
        {
            InitializeComponent();

            backgroundWorkerFind = new BackgroundWorker();

            backgroundWorkerFind.WorkerReportsProgress = true;
            backgroundWorkerFind.WorkerSupportsCancellation = true;
            backgroundWorkerFind.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerFind_DoWork);
            backgroundWorkerFind.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorkerFind_ProgressChanged);
            backgroundWorkerFind.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerFind_RunWorkerCompleted);
        }

        private void buttonShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (backgroundWorkerFind.IsBusy == false &&
                    buttonShow.Content.ToString() == "Show")
                {
                    SearchArguments arg = new SearchArguments(Properties.Settings.Default.ServerUri);
                    progressBarSearch.Value = 0;
                    buttonShow.Content = "Cancel";
                    buttonUnion.IsEnabled = false;

                    backgroundWorkerFind.RunWorkerAsync(arg);
                }
                else if (buttonShow.Content.ToString() == "Cancel")
                {
                    backgroundWorkerFind.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
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
                    this.LoadStaticcompornetList(worker, e, arg);
                    e.Result = arg;
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

        private void LoadStaticcompornetList(BackgroundWorker worker, DoWorkEventArgs e, SearchArguments arg)
        {
            using (SvnClient client = new SvnClient())
            {
                // Bind the SharpSvn UI to our client for SSL certificate and credentials
                SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                SvnUI.Bind(client, bindArgs);

                Collection<SvnListEventArgs> componentList;
                client.GetList(arg.ComponentListUri, out componentList);

                SvnListEventArgs root = componentList.Single(c => c.Name == arg.ComponentListUri.FileName);
                componentList.Remove(root);

                double currentProgress = 0;
                double progressIncrement = 0;
                int itemcount = componentList.Count();
                if (itemcount > 0)
                {
                    progressIncrement = 100.00 / itemcount;
                }

                MemoryStream ms = new MemoryStream();
                StreamReader rs;
                SvnUriTarget deployIniUrl;
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
                            deployIniUrl = new SvnUriTarget(component.Uri + @"trunk/deploy.ini");

                            ms.SetLength(0);
                            if (client.Write(deployIniUrl, ms, new SvnWriteArgs() { ThrowOnError = false }))
                            {
                                ms.Position = 0;
                                rs = new StreamReader(ms);
                                string fileContent = rs.ReadToEnd();

                                int startIndex = fileContent.IndexOf("\r\n[Connections]\r\n");
                                if (startIndex != -1)
                                {
                                    startIndex += "\r\n[Connections]\r\n".Length;
                                    int endIndex = fileContent.IndexOf("[", startIndex);
                                    if (endIndex == -1)
                                    {
                                        endIndex = fileContent.Length;
                                    }

                                    fileContent = fileContent.Substring(startIndex, endIndex - startIndex);

                                    string[] componentArray = fileContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    List<string> componentStaticList = new List<string>();
                                    foreach (string componentValue in componentArray)
                                    {
                                        if (componentValue.Contains("STATIC"))
                                        {
                                            componentStaticList.Add(componentValue.Substring(0, componentValue.IndexOf("=STATIC")).ToLowerInvariant());
                                        }
                                    }

                                    arg.ComponentDictionary.Add(component.Name, componentStaticList);
                                }
                            }
                        }
                    }
                }
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
                    else
                    {
                        SearchArguments arg = e.Result as SearchArguments;

                        Dictionary<string, string> view = new Dictionary<string, string>();

                        List<string> staticComponentList;
                        foreach (KeyValuePair<string, List<string>> item in arg.ComponentDictionary)
                        {
                            staticComponentList = this.GetStaticComponentList(item.Key, arg.ComponentDictionary);
                            staticComponentList.Sort();

                            view.Add(item.Key, string.Join("; ", staticComponentList));
                        }

                        dataGridComponentTable.ItemsSource = view;
                    }
                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                }
                finally
                {
                    buttonShow.Content = "Show";
                    buttonUnion.IsEnabled = true;
                }
            }
        }

        private List<string> GetStaticComponentList(string componentName, Dictionary<string, List<string>> componentDictionary)
        {
            List<string> staticComponentList = new List<string>();
            staticComponentList.AddRange(componentDictionary[componentName]);

            List<string> childStaticComponentList;
            foreach (string staticComponetName in componentDictionary[componentName])
            {
                if (componentDictionary.ContainsKey(staticComponetName))
                {
                    childStaticComponentList = this.GetStaticComponentList(staticComponetName, componentDictionary);
                    foreach (string chilStaticComponetName in childStaticComponentList)
                    {
                        if (staticComponentList.Contains(chilStaticComponetName) == false)
                        {
                            staticComponentList.Add(chilStaticComponetName);
                        }
                    }
                }
            }
            return staticComponentList;
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
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void buttonUnion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> unionList = new List<string>();
                string[] itemArray;
                foreach (KeyValuePair<string, string> item in dataGridComponentTable.SelectedItems)
                {
                    if (unionList.Contains(item.Key) == false)
                    {
                        unionList.Add(item.Key);
                    }

                    itemArray = item.Value.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string itemValue in itemArray)
                    {
                        if (unionList.Contains(itemValue) == false)
                        {
                            unionList.Add(itemValue);
                        }
                    }
                }
                unionList.Sort();
                textBoxunion.Text = string.Join("; ", unionList);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
            }
        }
    }
}