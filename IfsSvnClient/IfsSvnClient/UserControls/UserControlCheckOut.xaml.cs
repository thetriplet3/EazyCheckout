using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using NLog;
using SharpSvn;
using SharpSvn.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlCheckOut.xaml
    /// </summary>
    public partial class UserControlCheckOut : UserControl, IContent
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string CHECKOUT_BUTTON_TEXT = "Checkout";
        private bool _cancelCheckout = false;
        private Stopwatch _checkoutStopwatch;
        private BackgroundWorker backgroundWorkerCheckOut;

        private BitmapImage cancelImage;
        private BitmapImage checkOutImage;
        private BitmapImage componentImage;
        private BitmapImage projectImage;

        private IfsSvn myIfsSvn;
        private SvnUriTarget projectsUri;

        private Dictionary<string, SvnComponent> _toBeCheckedoutDictionary;
        private bool _isFiltering = false;

        public UserControlCheckOut()
        {
            InitializeComponent();

            this.myIfsSvn = new IfsSvn();
            this._toBeCheckedoutDictionary = new Dictionary<string, SvnComponent>();

            this.backgroundWorkerCheckOut = new BackgroundWorker();
            this.backgroundWorkerCheckOut.WorkerSupportsCancellation = true;
            this.backgroundWorkerCheckOut.DoWork += new DoWorkEventHandler(this.backgroundWorkerCheckOut_DoWork);
            this.backgroundWorkerCheckOut.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerCheckOut_RunWorkerCompleted);

            this.projectImage = new BitmapImage();
            this.projectImage.BeginInit();
            this.projectImage.UriSource = new Uri(@"/EazyCheckout;component/Resources/project.png", UriKind.RelativeOrAbsolute);
            this.projectImage.EndInit();

            this.componentImage = new BitmapImage();
            this.componentImage.BeginInit();
            this.componentImage.UriSource = new Uri(@"/EazyCheckout;component/Resources/folder.png", UriKind.RelativeOrAbsolute);
            this.componentImage.EndInit();

            this.checkOutImage = new BitmapImage();
            this.checkOutImage.BeginInit();
            this.checkOutImage.UriSource = new Uri(@"/EazyCheckout;component/Resources/checkout.png", UriKind.RelativeOrAbsolute);
            this.checkOutImage.EndInit();

            this.cancelImage = new BitmapImage();
            this.cancelImage.BeginInit();
            this.cancelImage.UriSource = new Uri(@"/EazyCheckout;component/Resources/cancel.png", UriKind.RelativeOrAbsolute);
            this.cancelImage.EndInit();
        }

        private delegate void backgroundWorkerCheckOut_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

        private delegate void client_NotifyDelegate(object sender, SvnNotifyEventArgs e);

        private delegate void logDelegate(string message, bool showLessLogInformation);

        private ButtonState ButtonState
        {
            get
            {
                string buttonText = ((buttonCheckOut.Content as StackPanel).Children[1] as Label).Content.ToString();
                if (buttonText == this.CHECKOUT_BUTTON_TEXT)
                {
                    return ButtonState.CheckOut;
                }
                else
                {
                    return ButtonState.Cancel;
                }
            }
            set
            {
                ImageSource source = null;
                string buttonText = string.Empty;
                if (value == Classes.ButtonState.CheckOut)
                {
                    buttonText = this.CHECKOUT_BUTTON_TEXT;
                    source = this.checkOutImage;
                }
                else
                {
                    buttonText = "Cancel";
                    source = this.cancelImage;
                }
                ((buttonCheckOut.Content as StackPanel).Children[0] as Image).Source = source;
                ((buttonCheckOut.Content as StackPanel).Children[1] as Label).Content = buttonText;
            }
        }

        private void backgroundWorkerCheckOut_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Do not access the form's BackgroundWorker reference directly.
                // Instead, use the reference provided by the sender parameter.
                BackgroundWorker worker = sender as BackgroundWorker;

                if (worker.CancellationPending == false)
                {
                    CheckOutArguments arg = e.Argument as CheckOutArguments;

                    if (arg.Type == JobType.Load)
                    {
                        e.Result = myIfsSvn.GetProjectList();
                    }
                    else if (arg.Type == JobType.CheckOut)
                    {
                        this.CheckOutProject(arg);
                        e.Result = JobType.CheckOut;
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

        private void backgroundWorkerCheckOut_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                ListBox myListBox = listBoxProjectList as ListBox;

                if (e.Error != null)
                {
                    ModernDialog.ShowMessage(e.Error.Message, "Error Checking out Components", MessageBoxButton.OK);
                    if (e.Error is SvnException)
                    {
                        SvnException svnEx = e.Error as SvnException;
                        logger.Error(svnEx, string.Format("Error Checking out Components SvnErrorCode = {0} ", svnEx.SvnErrorCode));
                    }
                    else
                    {
                        logger.Error(e.Error, "Error Checking out Components");
                    }
                }
                else
                {
                    if (e.Result != null)
                    {
                        if (e.Result is List<SvnListEventArgs>)
                        {
                            this.LoadProjectList(e.Result as List<SvnListEventArgs>);
                            this.Log("Every thing is loaded now.", checkBoxShowMoreInfor.IsChecked.Value);
                        }
                        else
                        {
                            this._checkoutStopwatch.Stop();

                            this.Log("Finished. [" + DateTime.Now.ToString() + "] Time it took: [" + this._checkoutStopwatch.Elapsed + "]", false);
                            this.ValidateWorkSpacePath();
                        }
                    }
                }
                this.ButtonState = ButtonState.CheckOut;
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error backgroundWorker issue", MessageBoxButton.OK);
            }
            finally
            {
                progressBarMain.Visibility = System.Windows.Visibility.Hidden;
                this._cancelCheckout = false;
                Mouse.OverrideCursor = null;
            }
        }

        private void buttonCheckOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.ButtonState == ButtonState.CheckOut)
                {
                    this.StartCheckOut();

                    logger.Info("buttonCheckOut: checkout, Show More Log information {1}", checkBoxShowMoreInfor.IsChecked);
                }
                else if (this.ButtonState == ButtonState.Cancel)
                {
                    this._cancelCheckout = true;

                    logger.Info("buttonCheckOut: Cancel");
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Checking out Components", MessageBoxButton.OK);
                logger.Error(ex, "Error Checking out Components");
            }
        }

        private void buttonClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //textBoxLog.Clear();
                textBlockLog.Inlines.Clear();

                logger.Info("buttonClearLog");
            }
            catch (Exception)
            {
            }
        }

        private void buttonComponentsSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBoxComponents.SelectAll();

                logger.Info("buttonComponentsSelectAll");
            }
            catch (Exception)
            {
            }
        }

        private void buttonComponentsSelectCheckedOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SelectCheckedOutComponents();

                logger.Info("buttonComponentsSelectCheckedOut");
            }
            catch (Exception)
            {
            }
        }

        private void buttonComponentsUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ClearComponentSelection();

                logger.Info("buttonComponentsUnselectAll");
            }
            catch (Exception)
            {
            }
        }

        private void ClearComponentSelection()
        {
            listBoxComponents.SelectedItem = null;
            listBoxComponents.SelectedItems.Clear();
            listBoxComponents.UnselectAll();
            this._toBeCheckedoutDictionary.Clear();
            textBlockSelectedComponentList.Text = string.Empty;
        }

        private void buttonCopyLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBlockLog.Text) == false)
                {
                    Clipboard.SetText(textBlockLog.Text);

                    ModernDialog.ShowMessage("Log copied to clipboard", "Log Copied", MessageBoxButton.OK);

                    logger.Info("buttonCopyLog");
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonGoToPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Info("buttonGoToPath: {0}", textBoxWorkSpace.Text);

                System.Diagnostics.Process.Start("explorer", textBoxWorkSpace.Text);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error navigation to folder path", MessageBoxButton.OK);
                logger.Error(ex, "Error navigation to folder path");
            }
        }

        private void buttonProjectRoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please Select your Project-Root folder";

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    textBoxProjectRoot.Text = dialog.SelectedPath;
                    Properties.Settings.Default.ProjectRoot = textBoxProjectRoot.Text;
                }

                logger.Info("buttonProjectRoot: {0}", textBoxProjectRoot.Text);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error selecting Project-Root folder", MessageBoxButton.OK);
                logger.Error(ex, "Error selecting Project-Root folder");
            }
        }

        private void CheckOutProject(CheckOutArguments arg)
        {
            using (SvnClient client = new SvnClient())
            {
                // Bind the SharpSvn UI to our client for SSL certificate and credentials
                SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                SvnUI.Bind(client, bindArgs);

                if (arg.ShowMoreLogInformation)
                {
                    client.Notify += new EventHandler<SvnNotifyEventArgs>(client_Notify);
                }
                client.Cancel += new EventHandler<SvnCancelEventArgs>(client_Cancel);

                Uri rootUri = client.GetRepositoryRoot(arg.ProjectWorkspaceUri);

                myIfsSvn.CheckOut(client, arg.ProjectNbprojectUri, arg.CheckOutPathNbproject, new SvnCheckOutArgs() { ThrowOnError = false, IgnoreExternals = true }, arg.CleanUpWhenLocked);

                myIfsSvn.CheckOut(client, arg.ProjectWorkspaceUri, arg.CheckOutPathWorkspace, new SvnCheckOutArgs() { IgnoreExternals = true }, arg.CleanUpWhenLocked);
                myIfsSvn.CheckOut(client, arg.ProjectDocumentUri, arg.CheckOutPathDocument, new SvnCheckOutArgs() { IgnoreExternals = true }, arg.CleanUpWhenLocked);

                this.Log("Starting Component Checkout [" + DateTime.Now.ToString() + "]", arg.ShowMoreLogInformation);

                Uri componentUri;
                if (arg.HasDocCompornents)
                {
                    componentUri = new Uri(Properties.Resources.ServerDocumentationOnlinedocframework.Replace("^/", rootUri.AbsoluteUri));

                    myIfsSvn.CheckOut(client, componentUri, arg.CheckOutPathDocumentEn, null, arg.CleanUpWhenLocked);
                }

                foreach (SvnComponent component in arg.CompornentArray)
                {
                    componentUri = new Uri(component.Path.Replace("^/", rootUri.AbsoluteUri));

                    this.Log(component.Name, arg.ShowMoreLogInformation);

                    if (component.Type == SvnComponent.SvnComponentType.Work)
                    {
                        myIfsSvn.CheckOut(client, componentUri, arg.CheckOutPathWorkspace + @"\" + component.Name, null, arg.CleanUpWhenLocked);
                    }
                    else if (component.Type == SvnComponent.SvnComponentType.Document)
                    {
                        List<SvnListEventArgs> folderList = myIfsSvn.GetFolderList(componentUri);
                        folderList.RemoveAt(0);
                        foreach (SvnListEventArgs folder in folderList)
                        {
                            myIfsSvn.CheckOut(client, folder.Uri, arg.CheckOutPathDocumentEn + @"\" + folder.Name, null, arg.CleanUpWhenLocked);
                        }
                    }
                }
            }
        }

        private void client_Cancel(object sender, SvnCancelEventArgs e)
        {
            try
            {
                e.Cancel = this._cancelCheckout;
                if (e.Cancel)
                {
                    backgroundWorkerCheckOut.CancelAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        private void client_Notify(object sender, SvnNotifyEventArgs e)
        {
            try
            {
                if (myScrollViewer.Dispatcher.CheckAccess())
                {
                    if (e.Error != null)
                    {
                        textBlockLog.Inlines.Add(new Run(e.Error.Message + "\r\n"));
                    }
                    else
                    {
                        textBlockLog.Inlines.Add(new Italic(new Run(e.Action.ToString())) { Foreground = Brushes.Gray });
                        if (e.Action == SvnNotifyAction.UpdateCompleted)
                        {
                            textBlockLog.Inlines.Add(new Run(" " + e.Path));
                        }
                        else
                        {
                            textBlockLog.Inlines.Add(new Run(" \t" + e.Path));
                        }
                        textBlockLog.Inlines.Add(new LineBreak());
                    }
                    myScrollViewer.ScrollToEnd();
                }
                else
                {
                    myScrollViewer.Dispatcher.Invoke(new client_NotifyDelegate(client_Notify), new object[] { sender, e });
                }
            }
            catch (Exception)
            {
            }
        }

        private void listBoxComponents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (this._isFiltering == false)
                {
                    if (e.AddedItems.Count > 0 || e.RemovedItems.Count > 0)
                    {
                        var addedComponentList = e.AddedItems.Cast<ListBoxItem>().Select(i => i.Tag as SvnComponent);

                        foreach (SvnComponent component in addedComponentList)
                        {
                            if (this._toBeCheckedoutDictionary.ContainsKey(component.Name) == false)
                            {
                                this._toBeCheckedoutDictionary.Add(component.Name, component);
                            }
                        }

                        var removedComponentList = e.RemovedItems.Cast<ListBoxItem>().Select(i => i.Tag as SvnComponent);

                        foreach (SvnComponent component in removedComponentList)
                        {
                            if (this._toBeCheckedoutDictionary.ContainsKey(component.Name))
                            {
                                this._toBeCheckedoutDictionary.Remove(component.Name);
                            }
                        }

                        string selectedCompomentList;
                        if (this._toBeCheckedoutDictionary.Count <= 15)
                        {
                            selectedCompomentList = string.Join(", ", this._toBeCheckedoutDictionary.Keys);
                        }
                        else
                        {
                            selectedCompomentList = string.Format("{0} selected ", this._toBeCheckedoutDictionary.Count);
                        }

                        textBlockSelectedComponentList.Inlines.Clear();
                        textBlockSelectedComponentList.Inlines.Add(new Italic(new Run(selectedCompomentList)) { Foreground = Brushes.Purple });
                    }
                    else
                    {
                        this.ClearComponentSelection();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private bool ListBoxComponentsFilter(object item)
        {
            SvnComponent component = (item as ListBoxItem).Tag as SvnComponent;
            return component.Name.Contains(textBoxComponentFilter.Text);
        }

        private void listBoxProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listBoxComponents.ItemsSource = null;
                ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;

                if (seletedNode != null &&
                    seletedNode.Tag != null)
                {
                    SvnProject seletedProject = seletedNode.Tag as SvnProject;
                    List<SvnComponent> componentList = myIfsSvn.GetComponentListFromExternals(seletedProject);
                    componentList.AddRange(myIfsSvn.GetDocumentComponentListFromExternals(seletedProject));

                    StackPanel treeItemStack;
                    TextBlock lbl;
                    Image treeItemImage;
                    ListBoxItem nodeItem;
                    List<ListBoxItem> nodeItemList = new List<ListBoxItem>();

                    foreach (SvnComponent component in componentList)
                    {
                        nodeItem = new ListBoxItem();
                        nodeItem.Name = component.Name.Replace("-", "_").Replace(".", "_");

                        treeItemStack = new StackPanel();
                        treeItemStack.Orientation = Orientation.Horizontal;

                        lbl = new TextBlock();
                        lbl.Text = component.Name;
                        lbl.Margin = new Thickness(3, 1, 3, 1);

                        treeItemImage = new Image();
                        treeItemImage.Source = componentImage;
                        treeItemImage.Margin = new Thickness(3, 1, 3, 1);

                        treeItemStack.Children.Add(treeItemImage);
                        treeItemStack.Children.Add(lbl);

                        nodeItem.Content = treeItemStack;
                        nodeItem.Tag = component;

                        nodeItemList.Add(nodeItem);
                    }

                    if (nodeItemList.Count > 0)
                    {
                        listBoxComponents.ItemsSource = nodeItemList;
                    }

                    textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\" + seletedProject.Name + @"\" + Properties.Resources.CheckOutPath_WorkSpace;

                    Properties.Settings.Default.SelectedProject = seletedProject.Name;

                    this.ClearComponentSelection();
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading Components", MessageBoxButton.OK);
                logger.Error(ex, "Error Loading Components");
            }
        }

        private bool ListBoxProjectsFilter(object item)
        {
            SvnProject project = (item as ListBoxItem).Tag as SvnProject;
            return project.Name.Contains(textBoxProjectsFilter.Text);
        }

        private void LoadProjectList(List<SvnListEventArgs> nodeList)
        {
            if (nodeList != null && nodeList.Count > 0)
            {
                nodeList.RemoveAt(0);

                StackPanel treeItemStack;
                TextBlock lbl;
                Image treeItemImage;
                ListBoxItem nodeItem;
                List<ListBoxItem> nodeItemList = new List<ListBoxItem>();
                foreach (SvnListEventArgs project in nodeList)
                {
                    nodeItem = new ListBoxItem();
                    nodeItem.Name = project.Name.Replace("-", "_").Replace(".", "_");

                    treeItemStack = new StackPanel();
                    treeItemStack.Orientation = Orientation.Horizontal;

                    lbl = new TextBlock();
                    lbl.Text = project.Name;
                    lbl.Margin = new Thickness(3, 1, 3, 1);

                    treeItemImage = new Image();
                    treeItemImage.Source = projectImage;
                    treeItemImage.Margin = new Thickness(3, 1, 3, 1);

                    treeItemStack.Children.Add(treeItemImage);
                    treeItemStack.Children.Add(lbl);

                    nodeItem.Content = treeItemStack;
                    nodeItem.Tag = new SvnProject(project);

                    nodeItemList.Add(nodeItem);
                }

                if (nodeItemList.Count > 0)
                {
                    listBoxProjectList.ItemsSource = nodeItemList;
                }

                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SelectedProject) == false &&
                    Properties.Settings.Default.SelectedProject != "ProjectsRoot")
                {
                    foreach (ListBoxItem item in listBoxProjectList.Items)
                    {
                        if ((item.Tag as SvnProject).Name == Properties.Settings.Default.SelectedProject)
                        {
                            listBoxProjectList.SelectedItem = item;
                            listBoxProjectList.ScrollIntoView(item);
                            break;
                        }
                    }
                }

                textBoxProjectsFilter.Text = Properties.Settings.Default.TextBoxProjectsFilter_text;
                textBoxComponentFilter.Text = Properties.Settings.Default.TextBoxComponentFilter_text;

                this.SelectCheckedOutComponents();
            }
        }

        private void Log(string message, bool showMoreLogInformation)
        {
            try
            {
                if (myScrollViewer.Dispatcher.CheckAccess())
                {
                    if (showMoreLogInformation)
                    {
                        textBlockLog.Inlines.Add(new Bold(new Run(message + "\r\n")));
                    }
                    else
                    {
                        textBlockLog.Inlines.Add(new Run(message + "\r\n"));
                    }
                    myScrollViewer.ScrollToEnd();
                }
                else
                {
                    myScrollViewer.Dispatcher.Invoke(new logDelegate(Log), new object[] { message, showMoreLogInformation });
                }
            }
            catch (Exception)
            {
            }
        }

        private bool SelectCheckedOutComponents()
        {
            listBoxComponents.SelectedItem = null;
            listBoxComponents.SelectedItems.Clear();
            if (this.ValidateWorkSpacePath())
            {
                string workSpace = textBoxWorkSpace.Text;
                if (workSpace.EndsWith(@"\") == false)
                {
                    workSpace += @"\";
                }

                SvnComponent component;
                foreach (ListBoxItem item in listBoxComponents.Items)
                {
                    component = item.Tag as SvnComponent;
                    item.IsSelected = Directory.Exists(workSpace + component.Name);
                    if (item.IsSelected)
                    {
                        listBoxComponents.SelectedItems.Add(item);
                    }
                }
            }

            if (listBoxComponents.SelectedItems.Count > 0)
            {
                listBoxComponents.ScrollIntoView(listBoxComponents.SelectedItems[0]);
            }

            return (listBoxComponents.SelectedItems.Count > 0);
        }

        private void SelectTobeCheckedOutComponents()
        {
            listBoxComponents.SelectedItem = null;
            listBoxComponents.SelectedItems.Clear();

            SvnComponent component;
            foreach (ListBoxItem item in listBoxComponents.Items)
            {
                component = item.Tag as SvnComponent;
                item.IsSelected = this._toBeCheckedoutDictionary.ContainsKey(item.Name);
                if (item.IsSelected)
                {
                    listBoxComponents.SelectedItems.Add(item);
                }
            }

            if (listBoxComponents.SelectedItems.Count > 0)
            {
                listBoxComponents.ScrollIntoView(listBoxComponents.SelectedItems[0]);
            }
        }

        private void StartCheckOut()
        {
            try
            {
                if (backgroundWorkerCheckOut.IsBusy == false)
                {
                    if (listBoxProjectList.SelectedItem != null &&
                        (listBoxProjectList.SelectedItem as ListBoxItem).Tag != null &&
                        this._toBeCheckedoutDictionary.Count > 0)
                    {
                        this._checkoutStopwatch = Stopwatch.StartNew();
                        this.Log("Starting [" + DateTime.Now.ToString() + "]", false);

                        progressBarMain.Visibility = System.Windows.Visibility.Visible;
                        this.ButtonState = ButtonState.Cancel;

                        string projectPath = ((listBoxProjectList.SelectedItem as ListBoxItem).Tag as SvnProject).Path;

                        string checkOutPathProject = textBoxProjectRoot.Text + @"\";
                        ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;
                        if (seletedNode.Tag != null)
                        {
                            if (seletedNode.Tag is SvnProject)
                            {
                                checkOutPathProject += (seletedNode.Tag as SvnProject).Name + @"\";
                            }
                        }
                        Mouse.OverrideCursor = Cursors.Wait;
                        backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.CheckOut,
                                                                                      checkBoxShowMoreInfor.IsChecked.Value,
                                                                                      true,
                                                                                      projectPath,
                                                                                      checkOutPathProject,
                                                                                      this._toBeCheckedoutDictionary.Values.ToArray()));
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void textBoxComponentFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                this._isFiltering = true;

                listBoxComponents.Items.Filter = ListBoxComponentsFilter;

                if (Properties.Settings.Default.TextBoxComponentFilter_text != textBoxComponentFilter.Text)
                {
                    logger.Info("textBoxComponentFilter: {0}", textBoxComponentFilter.Text);
                }

                Properties.Settings.Default.TextBoxComponentFilter_text = textBoxComponentFilter.Text;

                this.SelectTobeCheckedOutComponents();

                this._isFiltering = false;
            }
            catch (Exception)
            {
            }
        }

        private void textBoxProjectRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string projectRoot = textBoxProjectRoot.Text;
                if (projectRoot.EndsWith(@"\") == false)
                {
                    projectRoot += @"\";
                }
                if (Directory.Exists(projectRoot))
                {
                    textBoxProjectRoot.BorderBrush = (new BrushConverter().ConvertFrom("#FFCCCCCC") as SolidColorBrush);
                }
                else
                {
                    textBoxProjectRoot.BorderBrush = Brushes.Red;
                }

                textBoxWorkSpace.Text = projectRoot;
                ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;
                if (seletedNode.Tag != null)
                {
                    if (seletedNode.Tag is SvnProject)
                    {
                        textBoxWorkSpace.Text += (seletedNode.Tag as SvnProject).Name + @"\" + Properties.Resources.CheckOutPath_WorkSpace;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void textBoxProjectsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                listBoxProjectList.Items.Filter = this.ListBoxProjectsFilter;

                if (Properties.Settings.Default.TextBoxProjectsFilter_text != textBoxProjectsFilter.Text)
                {
                    logger.Info("textBoxProjectsFilter: {0}", textBoxProjectsFilter.Text);
                }

                Properties.Settings.Default.TextBoxProjectsFilter_text = textBoxProjectsFilter.Text;
            }
            catch (Exception)
            {
            }
        }

        private void textBoxWorkSpace_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ValidateWorkSpacePath();
            }
            catch (Exception)
            {
            }
        }

        private bool ValidateWorkSpacePath()
        {
            buttonGoToPath.IsEnabled = Directory.Exists(textBoxWorkSpace.Text);
            if (buttonGoToPath.IsEnabled)
            {
                buttonGoToPath.Opacity = 1;
                textBoxWorkSpace.BorderBrush = (new BrushConverter().ConvertFrom("#FFCCCCCC") as SolidColorBrush);
            }
            else
            {
                buttonGoToPath.Opacity = 0.5;
                textBoxWorkSpace.BorderBrush = Brushes.Red;
            }
            return buttonGoToPath.IsEnabled;
        }

        #region Navigation

        public void OnFragmentNavigation(FirstFloor.ModernUI.Windows.Navigation.FragmentNavigationEventArgs e)
        {
        }

        public void OnNavigatedFrom(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {
        }

        public void OnNavigatedTo(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                if (e.NavigationType == FirstFloor.ModernUI.Windows.Navigation.NavigationType.New)
                {
                    if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri) == false)
                    {
                        projectsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/projects");

                        textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\";
                        if (backgroundWorkerCheckOut.IsBusy == false)
                        {
                            progressBarMain.Visibility = System.Windows.Visibility.Visible;

                            backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.Load));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading Page", MessageBoxButton.OK);
                logger.Error(ex, "Error Loading Page");
            }
        }

        public void OnNavigatingFrom(FirstFloor.ModernUI.Windows.Navigation.NavigatingCancelEventArgs e)
        {
        }

        #endregion
    }
}