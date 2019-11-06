using System.Collections.Generic;
using TabletDriverLib.Component;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.IO;
using HidSharp;
using OpenTabletDriverGUI.Models;
using System;
using System.Linq;
using Avalonia.Input;

namespace OpenTabletDriverGUI.ViewModels
{
    public class ConfigurationManagerViewModel : ViewModelBase
    {
        private ObservableCollection<HidDevice> _devices;
        public ObservableCollection<HidDevice> Devices
        {
            set => this.RaiseAndSetIfChanged(ref _devices, value);
            get => _devices;
        }

        private ObservableCollection<TabletProperties> _cfgs;
        public ObservableCollection<TabletProperties> Configurations
        {
            set => this.RaiseAndSetIfChanged(ref _cfgs, value);
            get => _cfgs;
        }

        private TabletProperties _selected;
        public TabletProperties Selected
        {
            set => this.RaiseAndSetIfChanged(ref _selected, value);
            get => _selected;
        }

        private HidDevice _device;
        public HidDevice SelectedDevice
        {
            set => this.RaiseAndSetIfChanged(ref _device, value);
            get => _device;
        }

        public void New()
        {
            if (Configurations == null)
                Configurations = new ObservableCollection<TabletProperties>();
            var config = new TabletProperties()
            {
                TabletName = "Tablet"
            };
            Configurations.Add(config);
            Selected = config;
        }
        
        public void CreateFrom(HidDevice device)
        {
            var config = new TabletProperties()
            {
                TabletName = $"{device.GetManufacturer()} {device.GetFriendlyName()}".Trim(),
                VendorID = device.VendorID,
                ProductID = device.ProductID,
                InputReportLength = (uint)device.GetMaxInputReportLength()
            };
            Configurations.Add(config);
            Selected = config;
        }

        public void Delete(TabletProperties tablet)
        {
            Configurations.Remove(tablet);
        }

        public async void SaveAs(TabletProperties tablet)
        {
            var dialog = FileDialogs.CreateSaveFileDialog($"Saving tablet '{tablet.TabletName}'", "XML Document", "xml");
            var result = await dialog.ShowAsync(App.Current.Windows[1]);
            if (result != null)
            {
                var file = new FileInfo(result);
                tablet.Write(file);
                Log.Info($"Saved tablet configuration to '{file.FullName}'.");
            }
        }

        private string _hawku;

        public string HawkuString
        {
            set => this.RaiseAndSetIfChanged(ref _hawku, value);
            get => _hawku;
        }

        public async Task LoadHawkuDialog(Window window)
        {
            var fd = FileDialogs.CreateOpenFileDialog("Open Hawku Configuration", "Hawku Configuration", "cfg");
            var result = await fd.ShowAsync(window);
            if (result != null)
            {
                var fileInfo = new FileInfo(result[0]);
                if (fileInfo.Exists)
                {
                    using (var fs = fileInfo.OpenRead())
                    using (var sr = new StreamReader(fs))
                    {
                        HawkuString = await sr.ReadToEndAsync();
                    }
                }
            }
        }

        public void ConvertHawku(string input)
        {
            var lines = input.Split(Environment.NewLine, StringSplitOptions.None);
            var configs = ConfigurationConverter.ConvertHawkuConfigurationFile(lines);
            foreach (var config in configs)
                Configurations.Add(config);
        }
    }
}