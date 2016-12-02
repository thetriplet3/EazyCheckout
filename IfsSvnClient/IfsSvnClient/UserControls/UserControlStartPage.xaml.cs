using System;
using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlStartPage.xaml
    /// </summary>
    public partial class UserControlStartPage : UserControl, IContent
    {
        public UserControlStartPage()
        {
            InitializeComponent();
        }

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
                    if (comboBoxProductGroupList.HasItems == false)
                    {
                        comboBoxProductGroupList.Items.Add(Properties.Resources.ProductGroup_Projects);
                        comboBoxProductGroupList.Items.Add(Properties.Resources.ProductGroup_ServiceAsset);
                        comboBoxProductGroupList.Items.Add(Properties.Resources.ProductGroup_Other);
                    }
                    if (comboBoxServerUriList.HasItems == false)
                    {
                        comboBoxServerUriList.Items.Add(Properties.Resources.ServerUri_SriLanka);
                        comboBoxServerUriList.Items.Add(Properties.Resources.ServerUri_Sweden);
                    }

                    if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri) == false)
                    {
                        if (Properties.Resources.ServerUri_SriLanka.Contains(Properties.Settings.Default.ServerUri))
                        {
                            comboBoxServerUriList.SelectedValue = Properties.Resources.ServerUri_SriLanka;
                        }
                        else if (Properties.Resources.ServerUri_Sweden.Contains(Properties.Settings.Default.ServerUri))
                        {
                            comboBoxServerUriList.SelectedValue = Properties.Resources.ServerUri_Sweden;
                        }
                    }
                

                    if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SupportPerson) == false)
                    {
                        if (Properties.Settings.Default.SupportPerson == Properties.Resources.SupportPerson_Projects)
                        {
                            comboBoxProductGroupList.SelectedValue = Properties.Resources.ProductGroup_Projects;
                        }
                        else if (Properties.Settings.Default.SupportPerson == Properties.Resources.SupportPerson_ServiceAsset)
                        {
                            comboBoxProductGroupList.SelectedValue = Properties.Resources.ProductGroup_ServiceAsset;
                        }
                        else if (Properties.Settings.Default.SupportPerson == Properties.Resources.SupportPerson_Other)
                        {
                            comboBoxProductGroupList.SelectedValue = Properties.Resources.ProductGroup_Other;
                        }
                    }
                }
            }
            catch (Exception error)
            {
                ModernDialog.ShowMessage(error.Message, "Error Loading", MessageBoxButton.OK);
            }
        }

        public void OnNavigatingFrom(FirstFloor.ModernUI.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri))
                {
                    ModernDialog.ShowMessage("Please Select a Server.", "Server Select", MessageBoxButton.OK);
                    e.Cancel = true;
                }
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SupportPerson))
                {
                    ModernDialog.ShowMessage("Please Select your Product Group.", "Product Group Select", MessageBoxButton.OK);
                    e.Cancel = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboBoxServerUriList.SelectedValue != null)
                {                    
                    Properties.Settings.Default.ServerUri = comboBoxServerUriList.SelectedValue.ToString().Split(new char[] { '|' })[1].Trim();

                    string productGroup = comboBoxProductGroupList.SelectedValue.ToString();
                    if (productGroup == Properties.Resources.ProductGroup_Projects)
                    {
                        Properties.Settings.Default.SupportPerson = Properties.Resources.SupportPerson_Projects;
                    }
                    else if (productGroup == Properties.Resources.ProductGroup_ServiceAsset)
                    {
                        Properties.Settings.Default.SupportPerson = Properties.Resources.SupportPerson_ServiceAsset;
                    }
                    else if (productGroup == Properties.Resources.ProductGroup_Other)
                    {
                        Properties.Settings.Default.SupportPerson = Properties.Resources.SupportPerson_Other;
                    }

                    BBCodeBlock bs = new BBCodeBlock();

                    bs.LinkNavigator.Navigate(new Uri("/UserControls/UserControlCheckOut.xaml", UriKind.Relative), this);
                }
                else
                {
                    ModernDialog.ShowMessage("Please Select a Server.", "Server Select", MessageBoxButton.OK);
                }
            }
            catch (Exception error)
            {
                ModernDialog.ShowMessage(error.Message, FirstFloor.ModernUI.Resources.NavigationFailed, MessageBoxButton.OK);
            }
        }
    }
}