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
using System.Windows.Shapes;

namespace LocoSwap
{
    /// <summary>
    /// Interaction logic for MapInspector.xaml
    /// </summary>
    public partial class RouteInspector : Window
    {
        public class RouteViewModel : ModelBase
        {
            private RouteDetailed _route;
            public RouteDetailed Route
            {
                get => _route;
                set => SetProperty(ref _route, value);
            }
            private string _loadingInformation = "";
            public string LoadingInformation
            {
                get => _loadingInformation;
                set => SetProperty(ref _loadingInformation, value);
            }

            private int _totalRecords = 0;
            public int TotalRecords
            {
                get => _totalRecords;
                set => SetProperty(ref _totalRecords, value);
            }

            private int _totalTiles = 0;
            public int TotalTiles
            {
                get => _totalTiles;
                set => SetProperty(ref _totalTiles, value);
            }


            private int _loadingProgress = 0;
            public int LoadingProgress
            {
                get => _loadingProgress;
                set
                {
                    SetProperty(ref _loadingProgress, value);
                    OnPropertyChanged(new PropertyChangedEventArgs("LoadingGridVisibility"));
                    OnPropertyChanged(new PropertyChangedEventArgs("SaveButtonEnabled"));
                }
            }
            private bool _vehicleScanInProgress = false;
            public Visibility LoadingGridVisibility
            {
                get => LoadingProgress < 100 ? Visibility.Visible : Visibility.Hidden;
            }
            public bool SaveButtonEnabled
            {
                get => LoadingProgress >= 100;
            }

        }

        public RouteViewModel ViewModel { get; private set; }

        public RouteInspector(string routeId)
        {
            ViewModel = new RouteViewModel();
            ViewModel.Route = new RouteDetailed(routeId);

            InitializeComponent();

            ReadRoute();
        }

        public async void ReadRoute()
        {
            IProgress<int> progress = new Progress<int>(value => { ViewModel.LoadingProgress = value; });
            ViewModel.LoadingInformation = LocoSwap.Language.Resources.reading_scenario_files;

            await ViewModel.Route.LoadTiles(progress);
        }
    }
}
