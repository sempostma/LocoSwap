using LocoSwap.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace LocoSwap
{
    /// <summary>
    /// Interaction logic for SwapPresetWindow.xaml
    /// </summary>
    public partial class SwapPresetWindow : Window, INotifyPropertyChanged
    {
        public event EventHandler ApplyClicked;
        public event PropertyChangedEventHandler PropertyChanged;

        public List<SwapPresetItem> SelectedItems
        {
            get => PresetList.SelectedItems.Cast<SwapPresetItem>().ToList();
        }

        public bool? IsCheckAll
        {
            get {
                if (SelectedItems.Count == PresetList.Items.Count) return true;
                else if (SelectedItems.Count == 0) return false;
                else return null;
            }
            set {
                if (value == true) PresetList.SelectAll();
                if (value == false) PresetList.UnselectAll();
            }
        }

        public SwapPresetWindow()
        {
            InitializeComponent();

            PresetList.SelectionChanged += PresetList_SelectionChanged;
        }

        private void PresetList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IsCheckAll"));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = PresetList.SelectedItems.Cast<object>().ToArray();
            foreach (SwapPresetItem item in selected)
            {
                Settings.Default.Preset.List.Remove(item);
            }
            Settings.Default.Save();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyClicked?.Invoke(this, new EventArgs());
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
