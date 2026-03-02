using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicTools
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.Title = "Vusic Tools";

            cmbResolution.SelectedIndex = 1;   // 1920x1080
            cmbAspectRatio.SelectedIndex = 0;  // 16:9
            cmbFrameRate.SelectedIndex = 4;    // 30 fps
            cmbAudioSampleRate.SelectedIndex = 3;
            chckDefaultOptions.IsChecked = true;
            Projects.Add(new ProjectItem
            {
                ProjectName = "New Project",
                FolderName = "",
                Thumbnail = new BitmapImage(new Uri("ms-appx:///Assets/addicon.png")),
                IsNewProject = true
            });

            // Then add real projects dynamically
            foreach (var project in loadedProjects)
            {
                Projects.Add(project);
            }
            grdViewProjects.ItemsSource = Projects;
        }
        ObservableCollection<ProjectItem> Projects { get; set; } = new ObservableCollection<ProjectItem>();
        ObservableCollection<ProjectItem> loadedProjects { get; set; } = new ObservableCollection<ProjectItem>();

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            //Open Project
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            //Delete Project
        }

        private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            //Export Project
        }

        private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {
            //Rename Project
        }

        private async void grdViewProjects_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as ProjectItem;
            if (item.IsNewProject)
            {
                await dlgNewProject.ShowAsync();
            }
        }

        private void dlgNewProject_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            CreateProject();
        }
        private void CreateProject()
        {
            if(txtProjectName.Text == "")
            {
                txtProjectName.Text = "Project (1)";
            }
            if (string.IsNullOrWhiteSpace(txtProjectFolder.Text))
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                string vusicPath = Path.Combine(documentsPath, "VusicProjects");

                // Create folder if it doesn't exist
                if (!Directory.Exists(vusicPath))
                {
                    Directory.CreateDirectory(vusicPath);
                }

                txtProjectFolder.Text = vusicPath;
            }
            Projects.Add(new ProjectItem
            {
                ProjectName = txtProjectName.Text,
                FolderName = txtProjectName.Text,
                Thumbnail = new BitmapImage(new Uri("ms-appx:///Assets/defaultprojectthumbnail.png")),
                IsNewProject = false
            });
            var newProject = new ProjectDetails();
            newProject= new ProjectDetails
            {
                ProjectName = txtProjectName.Text,
                FolderName = txtProjectName.Text,
                ProjectResolution =cmbResolution.SelectedItem.ToString(),
                AspectRatio = cmbAspectRatio.SelectedItem.ToString(),
                FrameRate = cmbFrameRate.SelectedItem.ToString(),
                AudioSampleRate = cmbAspectRatio.SelectedItem.ToString()
            };
            HomePage.Visibility = Visibility.Collapsed;
            frmMain.Visibility = Visibility.Visible;
            frmMain.Navigate(typeof(ProjectPage), newProject);
        }
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var tgl = sender as ToggleButton;
            if(tgl.IsChecked == true)
            {
                grdMoreOptions.Visibility = Visibility.Visible;
            }
            else
            {
                grdMoreOptions.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var tgl = sender as ToggleButton;
            if (tgl.IsChecked == true)
            {
                grdMoreOptions.Visibility = Visibility.Visible;
            }
            else
            {
                grdMoreOptions.Visibility = Visibility.Collapsed;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //Select Folder
            var picker = new FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                txtProjectFolder.Text = folder.Path;
            }
        }

        private void chckDefaultOptions_Checked(object sender, RoutedEventArgs e)
        {
            if(chckDefaultOptions.IsChecked == true)
            {
                cmbResolution.SelectedIndex = 1;   // 1920x1080
                cmbAspectRatio.SelectedIndex = 0;  // 16:9
                cmbFrameRate.SelectedIndex = 4;    // 30 fps
                cmbAudioSampleRate.SelectedIndex = 3;
            }
        }

        private void chckDefaultOptions_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckDefaultOptions.IsChecked == true)
            {
                cmbResolution.SelectedIndex = 1;   // 1920x1080
                cmbAspectRatio.SelectedIndex = 0;  // 16:9
                cmbFrameRate.SelectedIndex = 4;    // 30 fps
                cmbAudioSampleRate.SelectedIndex = 3;
            }
        }
    }
}
