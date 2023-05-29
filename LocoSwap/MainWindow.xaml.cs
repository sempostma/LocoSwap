using Ionic.Zip;
using LocoSwap.Properties;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace LocoSwap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Route> Routes { get; } = new ObservableCollection<Route>();
        public ObservableCollection<Scenario> Scenarios { get; } = new ObservableCollection<Scenario>();
        public string WindowTitle { get; set; } = "LocoSwap";

        public Task clearCacheTask = Task.CompletedTask;

        CancellationTokenSource ScanCancellationTokenSource;
        
        public MainWindow()
        {
            InitializeComponent();

            LoadingProgressBar.Visibility = Visibility.Hidden;

            this.DataContext = this;
            this.WindowTitle = "LocoSwap " + Assembly.GetEntryAssembly().GetName().Version.ToString();

            var routeIds = Route.ListAllRoutes();

            ScenarioList.SelectionChanged += ScenarioList_SelectionChanged;

            foreach (var id in routeIds)
            {
                try
                {
                    Route route = new Route(id);
                    route.PropertyChanged += Route_PropertyChanged;
                    Routes.Add(route);
                }
                catch (Exception)
                {

                }
            }
        }

        private void ScenarioList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckForMissingRollingStock();
        }

        private void Route_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsFavorite")
            {
                Route route = sender as Route;
                StringCollection favorite = Properties.Settings.Default.FavoriteRoutes ?? new StringCollection();
                if (Properties.Settings.Default.FavoriteRoutes == null) Properties.Settings.Default.FavoriteRoutes = favorite;
                if (route.IsFavorite)
                {
                    Log.Debug("Adding {0} to favorite..", route.Name);
                    favorite.Add(route.Id);
                }
                else
                {
                    Log.Debug("Removing {0} from favorite..", route.Name);
                    favorite.Remove(route.Id);
                }
                Properties.Settings.Default.Save();
            }
        }

        private void RouteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scenarios.Clear();
            var routeId = ((Route)RouteList.SelectedItem).Id;
            var scenarioIds = Scenario.ListAllScenarios(routeId);
            foreach (var id in scenarioIds)
            {
                try
                {
                    Scenarios.Add(new Scenario(routeId, id));
                }
                catch (Exception ex)
                {
                    Log.Debug("Exception caught when trying to list scenario {0}\\{1}: {2}", routeId, id, ex.Message);
                }
            }
        }

        private async void CheckForMissingRollingStock()
        {
            LoadingProgressBar.Visibility = Visibility.Visible;
            IProgress<int> progress = new Progress<int>(value => { LoadingProgressBar.Value = value; });
            progress.Report(0);
            Scenario scenario = (Scenario)ScenarioList.SelectedItem;

            if (scenario == null) return;

            var statusTask = Task.Run(() =>
            {
                int index = 0;
                scenario.ReadScenario(progress);
                List<Consist> ret = scenario.GetConsists(progress);
                Interlocked.Increment(ref index);

                Dispatcher.Invoke(() =>
                {
                    ScenarioList.Items.Refresh();
                });
            });

            await statusTask;
            progress.Report(100);
            LoadingProgressBar.Visibility = Visibility.Hidden;
            ScenarioList.Items.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (RouteList.SelectedItem == null || ScenarioList.SelectedItem == null) return;
            var routeId = ((Route)RouteList.SelectedItem).Id;
            var scenario = ((Scenario)ScenarioList.SelectedItem);
            var editWindow = new ScenarioEditWindow(scenario);
            editWindow.Show();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (RouteList.SelectedItem == null || ScenarioList.SelectedItem == null) return;
            var scenario = (Scenario)ScenarioList.SelectedItem;
            Process.Start(scenario.ScenarioDirectory);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void ScenarioList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;
            if (dataContext is Scenario)
            {
                if (RouteList.SelectedItem == null) return;
                var routeId = ((Route)RouteList.SelectedItem).Id;
                Scenario scenario = ((Scenario)dataContext);
                var editWindow = new ScenarioEditWindow(scenario);
                editWindow.Show();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // clear ts cache

            string assetsFolder = Path.Combine(Properties.Settings.Default.TsPath, "Assets");

            if (!clearCacheTask.IsCompleted && !clearCacheTask.IsFaulted)
            {
                MessageBox.Show(LocoSwap.Language.Resources.delete_cache_task_not_finished, LocoSwap.Language.Resources.delete_cache_task_not_finished, MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            LoadingProgressBar.Visibility = Visibility.Visible;

            IProgress<int> progress = new Progress<int>(value => { LoadingProgressBar.Value = 100f - 90f / (1f + value / 10f); });

            clearCacheTask = Task.Run(() =>
            {
                try
                {
                    int i = 0;
                    progress.Report(0);

                    foreach (var pakFile in Directory.GetFiles(assetsFolder, "*.pak", SearchOption.AllDirectories))
                    {
                        File.Delete(pakFile);
                        progress.Report(i++);
                    }

                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadingProgressBar.Visibility = Visibility.Hidden;

                        MessageBox.Show(LocoSwap.Language.Resources.removed_cache, LocoSwap.Language.Resources.removed_cache, MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                } catch(Exception error)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadingProgressBar.Visibility = Visibility.Hidden;

                        MessageBox.Show(
                        error.Message,
                        LocoSwap.Language.Resources.msg_message,
                        MessageBoxButton.OK, MessageBoxImage.Error);

                        return;
                    });

                }
            });
        }

        private void OpenRouteInspectorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RouteList.SelectedItem == null) return;
            var routeId = ((Route)RouteList.SelectedItem).Id;
            var editWindow = new RouteInspector(routeId);
            editWindow.Show();
        }

        void ApplyReplacementRulesClick(object sender, RoutedEventArgs e)
        {
            if (RouteList.SelectedItem == null || ScenarioList.SelectedItem == null) return;
            var scenario = (Scenario)ScenarioList.SelectedItem;

            try
            {
                scenario.ApplyPreset(Settings.Default.Preset.List.ToList());

                MessageBox.Show(
                    LocoSwap.Language.Resources.msg_swap_completed,
                    LocoSwap.Language.Resources.msg_message,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            } 
            catch(Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocoSwap.Language.Resources.msg_error,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
