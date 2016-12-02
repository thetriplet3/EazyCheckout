using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using SharpSvn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlManageTagBranch.xaml
    /// </summary>
    public partial class UserControlManageTagBranch : UserControl
    {
        private BackgroundWorker backgroundWorkerLoad;

        private BitmapImage bi;

        private IfsSvn myIfsSvn;

        private BitmapImage rootBi;

        public UserControlManageTagBranch()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerLoad = new BackgroundWorker();
            this.backgroundWorkerLoad.WorkerSupportsCancellation = true;
            this.backgroundWorkerLoad.DoWork += new DoWorkEventHandler(this.backgroundWorkerLoad_DoWork);
            this.backgroundWorkerLoad.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerLoad_RunWorkerCompleted);

            rootBi = new BitmapImage();
            rootBi.BeginInit();
            rootBi.UriSource = new Uri(@"/EazyCheckout;component/Resources/project.png", UriKind.RelativeOrAbsolute);
            rootBi.EndInit();

            bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(@"/EazyCheckout;component/Resources/folder.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
        }

        private delegate void backgroundWorkerLoad_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

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

                    if (arg.Type == JobType.LoadComponents)
                    {
                        arg.FolderList = myIfsSvn.GetComponentList();
                    }
                    else if (arg.Type == JobType.LoadChildFolders)
                    {
                        arg.FolderList = myIfsSvn.GetFolderList(arg.SelectedTag.Uri);
                    }

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
                            TagArguments arg = e.Result as TagArguments;

                            if (arg.Type == JobType.LoadComponents)
                            {
                                List<SvnComponent> componentList = new List<SvnComponent>();
                                foreach (SvnListEventArgs folder in arg.FolderList)
                                {
                                    if (string.IsNullOrWhiteSpace(folder.Path) == false)
                                    {
                                        componentList.Add(new SvnComponent(folder));
                                    }
                                }

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
                                    treeItemImage.Source = rootBi;
                                    treeItemImage.Margin = new Thickness(3, 1, 3, 1);

                                    treeItemStack.Children.Add(treeItemImage);
                                    treeItemStack.Children.Add(lbl);

                                    nodeItem.Content = treeItemStack;
                                    nodeItem.Tag = component;

                                    nodeItemList.Add(nodeItem);
                                }

                                if (nodeItemList.Count > 0)
                                {
                                    listBoxComponentList.ItemsSource = nodeItemList;
                                }

                                textBoxComponentFilter.Text = Properties.Settings.Default.ManageTagBranch_TextBoxComponentFilter_text;
                            }
                            else if (arg.Type == JobType.LoadChildFolders)
                            {
                                arg.FolderList.RemoveAt(0);

                                StackPanel treeItemStack;
                                TextBlock lbl;
                                Image treeItemImage;

                                TreeViewItem nodeItem;
                                List<TreeViewItem> nodeItemList = new List<TreeViewItem>();
                                foreach (SvnListEventArgs childForlder in arg.FolderList)
                                {
                                    nodeItem = new TreeViewItem();

                                    treeItemStack = new StackPanel();
                                    treeItemStack.Orientation = Orientation.Horizontal;

                                    treeItemImage = new Image();
                                    treeItemImage.Source = bi;
                                    treeItemStack.Children.Add(treeItemImage);

                                    lbl = new TextBlock();
                                    lbl.Text = childForlder.Name;
                                    treeItemStack.Children.Add(lbl);

                                    nodeItem.Header = treeItemStack;
                                    nodeItem.Tag = childForlder;

                                    nodeItem.Expanded += new RoutedEventHandler(nodeItem_Expanded);

                                    nodeItemList.Add(nodeItem);
                                }

                                arg.SelectedFolderView.ItemsSource = nodeItemList;
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
                    //buttonFind.Content = "Find";
                    progressBarMain.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void buttonDeleteProjectBranch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeViewItem selectedFolderView = treeViewComponent.SelectedItem as TreeViewItem;
                if (selectedFolderView != null)
                {
                    TreeViewItem selectedParentFolderView = this.GetSelectedTreeViewItemParent(selectedFolderView);
                    if (selectedParentFolderView != null)
                    {
                        SvnListEventArgs selectedFolder_parentFolder = selectedParentFolderView.Tag as SvnListEventArgs;

                        if (selectedFolder_parentFolder.Name == "project")
                        {
                            SvnListEventArgs selectedFolder = selectedFolderView.Tag as SvnListEventArgs;

                            MessageBoxResult result = ModernDialog.ShowMessage("[" + selectedFolder.Name + "] Are you sure?", "Branch Deleting", MessageBoxButton.YesNo);

                            if (result == MessageBoxResult.Yes)
                            {
                                if (myIfsSvn.DeleteBranch(selectedFolder))
                                {
                                    selectedParentFolderView.IsExpanded = false;
                                    selectedParentFolderView.IsExpanded = true;

                                    ModernDialog.ShowMessage("[" + selectedFolder.Name + "] Deleted.", "Branch Deleted", MessageBoxButton.OK);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private TreeViewItem GetSelectedTreeViewItemParent(TreeViewItem item)
        {
            try
            {
                DependencyObject parent = VisualTreeHelper.GetParent(item);
                while ((parent is TreeViewItem) == false)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                return parent as TreeViewItem;
            }
            catch (Exception)
            {
            }
            return null;
        }

        private void listBoxComponentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                treeViewComponent.Items.Clear();
                ListBoxItem selectedItem = listBoxComponentList.SelectedItem as ListBoxItem;

                if (selectedItem != null &&
                    selectedItem.Tag != null)
                {
                    SvnComponent selectedComponent = selectedItem.Tag as SvnComponent;
                    List<SvnListEventArgs> forlderList = myIfsSvn.GetFolderList(new SvnUriTarget(selectedComponent.Path));

                    StackPanel treeItemStack = new StackPanel();
                    treeItemStack.Orientation = Orientation.Horizontal;

                    Label lbl = new Label();
                    lbl.Content = forlderList[0].Name;

                    Image treeItemImage = new Image();
                    treeItemImage.Source = rootBi;

                    treeItemStack.Children.Add(treeItemImage);
                    treeItemStack.Children.Add(lbl);

                    TreeViewItem rootNodeItem = new TreeViewItem();
                    rootNodeItem.Header = treeItemStack;
                    rootNodeItem.Tag = forlderList[0];
                    rootNodeItem.IsExpanded = true;

                    forlderList.RemoveAt(0);

                    TreeViewItem nodeItem;
                    List<TreeViewItem> nodeItemList = new List<TreeViewItem>();
                    foreach (SvnListEventArgs folder in forlderList)
                    {
                        nodeItem = new TreeViewItem();
                        nodeItem.Name = folder.Name;

                        treeItemStack = new StackPanel();
                        treeItemStack.Orientation = Orientation.Horizontal;

                        lbl = new Label();
                        lbl.Content = folder.Name;

                        treeItemImage = new Image();
                        treeItemImage.Source = bi;

                        treeItemStack.Children.Add(treeItemImage);
                        treeItemStack.Children.Add(lbl);

                        nodeItem.Header = treeItemStack;
                        nodeItem.Tag = folder;

                        nodeItem.Expanded += new RoutedEventHandler(nodeItem_Expanded);

                        nodeItemList.Add(nodeItem);
                    }

                    if (nodeItemList.Count > 0)
                    {
                        rootNodeItem.ItemsSource = nodeItemList;
                        treeViewComponent.Items.Add(rootNodeItem);
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading", MessageBoxButton.OK);
            }
        }

        private bool ListBoxComponentsFilter(object item)
        {
            SvnComponent component = (item as ListBoxItem).Tag as SvnComponent;
            return component.Name.Contains(textBoxComponentFilter.Text);
        }

        private void nodeItem_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeViewItem selectedFolderView = e.OriginalSource as TreeViewItem;
                if (selectedFolderView != null)
                {
                    if (backgroundWorkerLoad.IsBusy == false)
                    {
                        progressBarMain.Visibility = System.Windows.Visibility.Visible;

                        backgroundWorkerLoad.RunWorkerAsync(new TagArguments(JobType.LoadChildFolders)
                        {
                            SelectedFolderView = selectedFolderView,
                            SelectedTag = selectedFolderView.Tag as SvnListEventArgs
                        });
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void textBoxComponentFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                listBoxComponentList.Items.Filter = ListBoxComponentsFilter;

                Properties.Settings.Default.ManageTagBranch_TextBoxComponentFilter_text = textBoxComponentFilter.Text;

                listBoxComponentList.SelectedItem = null;
                listBoxComponentList.SelectedItems.Clear();
                listBoxComponentList.UnselectAll();
            }
            catch (Exception)
            {
            }
        }

        private void treeViewComponent_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                TreeViewItem selectedFolderView = treeViewComponent.SelectedItem as TreeViewItem;
                if (selectedFolderView != null)
                {
                    string selectedFolder_name = (selectedFolderView.Tag as SvnListEventArgs).Name;

                    if (selectedFolder_name != "trunk")
                    {
                        userControlCreateTagFromTrunk.IsEnabled = false;
                    }
                    else
                    {
                        userControlCreateTagFromTrunk.SetSelectedTrunk(selectedFolderView.Tag as SvnListEventArgs);

                        userControlCreateTagFromTrunk.IsEnabled = true;
                    }

                    TreeViewItem selectedParentFolderView = this.GetSelectedTreeViewItemParent(selectedFolderView);

                    if (selectedParentFolderView != null)
                    {
                        string selectedFolder_parentFolder_name = (selectedParentFolderView.Tag as SvnListEventArgs).Name;

                        if (selectedFolder_parentFolder_name != "tags")
                        {
                            userControlCreateBranchFromSelectedTag.IsEnabled = false;
                        }
                        else
                        {
                            userControlCreateBranchFromSelectedTag.SetSelectedTag(selectedFolderView.Tag as SvnListEventArgs);

                            userControlCreateBranchFromSelectedTag.IsEnabled = true;
                        }

                        if (selectedFolder_parentFolder_name != "project")
                        {
                            buttonDeleteProjectBranch.IsEnabled = false;
                        }
                        else
                            buttonDeleteProjectBranch.IsEnabled = true;
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (backgroundWorkerLoad.IsBusy == false)
                {
                    progressBarMain.Visibility = System.Windows.Visibility.Visible;

                    backgroundWorkerLoad.RunWorkerAsync(new TagArguments(JobType.LoadComponents));
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading", MessageBoxButton.OK);
            }
        }
    }
}