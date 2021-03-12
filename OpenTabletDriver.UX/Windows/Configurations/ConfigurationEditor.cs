﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using HidSharp;
using OpenTabletDriver.Desktop;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.UX.Controls.Generic;
using OpenTabletDriver.UX.Windows.Configurations.Controls;

namespace OpenTabletDriver.UX.Windows.Configurations
{
    public class ConfigurationEditor : DesktopForm
    {
        public ConfigurationEditor()
            : base(Application.Instance.MainForm)
        {
            base.Title = "Configuration Editor";
            base.ClientSize = new Size(910, 680);

            base.Content = new Splitter
            {
                Orientation = Orientation.Horizontal,
                Panel1MinimumSize = 200,
                Panel1 = this.configList,
                Panel2 = new Scrollable
                {
                    Content = this.configurationSettings,
                    Padding = new Padding(5)
                },
            };

            // MenuBar Commands
            var quitCommand = new Command { MenuText = "Close", Shortcut = Application.Instance.CommonModifier | Keys.W };
            quitCommand.Executed += (sender, e) => Close();

            var loadDirectory = new Command { MenuText = "Load configurations...", Shortcut = Application.Instance.CommonModifier | Keys.O };
            loadDirectory.Executed += (sender, e) => LoadConfigurationsDialog();

            var saveDirectory = new Command { MenuText = "Save configurations", Shortcut = Application.Instance.CommonModifier | Keys.S };
            saveDirectory.Executed += (sender, e) => SaveConfigurationsDialog(new DirectoryInfo(AppInfo.Current.ConfigurationDirectory));

            var saveToDirectory = new Command { MenuText = "Save configurations to...", Shortcut = Application.Instance.CommonModifier | Application.Instance.AlternateModifier | Keys.S };
            saveToDirectory.Executed += (sender, e) => SaveConfigurationsDialog();

            var newConfiguration = new Command { ToolBarText = "New configuration", Shortcut = Application.Instance.CommonModifier | Keys.N };
            newConfiguration.Executed += (sender, e) => configList.CreateConfiguration();

            var deleteConfiguration = new Command { ToolBarText = "Delete configuration" };
            deleteConfiguration.Executed += (sender, e) => configList.DeleteConfiguration();

            var generateConfiguration = new Command { ToolBarText = "Generate configuration..." };
            generateConfiguration.Executed += async (sender, e) => await configList.GenerateConfiguration();

            // Menu
            base.Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new ButtonMenuItem
                    {
                        Text = "&File",
                        Items =
                        {
                            loadDirectory,
                            saveDirectory,
                            saveToDirectory
                        }
                    }
                },
                QuitItem = quitCommand
            };

            base.ToolBar = new ToolBar
            {
                Items =
                {
                    newConfiguration,
                    deleteConfiguration,
                    generateConfiguration
                }
            };

            this.configList.SelectedValueChanged += (sender, e) => configurationSettings.Update(SelectedConfiguration);

            Refresh();
        }

        public void Refresh()
        {
            var configDir = new DirectoryInfo(AppInfo.Current.ConfigurationDirectory);
            var sortedConfigs = from config in ReadConfigurations(configDir)
                orderby config.Name
                select config;

            Configurations = new ObservableCollection<TabletConfiguration>(sortedConfigs);
            SelectedIndex = 0;
        }

        protected ObservableCollection<TabletConfiguration> Configurations
        {
            set => this.configList.Source = value;
            get => this.configList.Source as ObservableCollection<TabletConfiguration>;
        }

        protected int SelectedIndex
        {
            set => this.configList.SelectedIndex = value;
            get => this.configList.SelectedIndex;
        }

        protected TabletConfiguration SelectedConfiguration
        {
            set => this.configList.SelectedItem = value;
            get => this.configList.SelectedItem;
        }

        private ConfigurationList configList = new ConfigurationList();
        private ConfigurationSettings configurationSettings = new ConfigurationSettings();

        private static readonly Regex NameRegex = new Regex("(?<Manufacturer>.+?) (?<TabletName>.+?)$");

        private ObservableCollection<TabletConfiguration> ReadConfigurations(DirectoryInfo dir)
        {
            dir.Refresh();
            if (dir.Exists)
            {
                var configs = from file in dir.GetFiles("*.json", SearchOption.AllDirectories)
                    select Serialization.Deserialize<TabletConfiguration>(file);
                return new ObservableCollection<TabletConfiguration>(configs);
            }
            else
            {
                return new ObservableCollection<TabletConfiguration>();
            }
        }

        private void WriteConfigurations(IEnumerable<TabletConfiguration> configs, DirectoryInfo dir)
        {
            foreach (var config in configs)
            {
                var match = NameRegex.Match(config.Name);
                var manufacturer = match.Groups["Manufacturer"].Value;
                var tabletName = match.Groups["TabletName"].Value;

                var path = Path.Join(dir.FullName, manufacturer, string.Format("{0}.json", tabletName));
                var file = new FileInfo(path);
                if (!file.Directory.Exists)
                    file.Directory.Create();
                Serialization.Serialize(file, config);
            }
        }

        private void LoadConfigurationsDialog()
        {
            var folderDialog = new SelectFolderDialog
            {
                Title = "Open configuration folder..."
            };
            switch (folderDialog.ShowDialog(this))
            {
                case DialogResult.Ok:
                case DialogResult.Yes:
                    var dir = new DirectoryInfo(folderDialog.Directory);
                    Configurations = ReadConfigurations(dir);
                    break;
            }
        }

        /// <summary>
        /// Save configurations to `dir` if present, else show a folder selection dialog.
        /// </summary>
        /// <param name="dir">The directory to save to, can be null.</param>
        private void SaveConfigurationsDialog(DirectoryInfo dir = null)
        {
            if (Configurations == null)
            {
                MessageBox.Show(this, "There are no configurations to save.", MessageBoxType.Information);
                return;
            }

            if (dir == null)
            {
                var folderDialog = new SelectFolderDialog
                {
                    Title = "Save configurations to..."
                };
                switch (folderDialog.ShowDialog(this))
                {
                    case DialogResult.Ok:
                    case DialogResult.Yes:
                        dir = new DirectoryInfo(folderDialog.Directory);
                        break;
                }
            }

            WriteConfigurations(Configurations, dir);
        }

        private class ConfigurationList : ListBox<TabletConfiguration>
        {
            public ConfigurationList()
            {
                this.ItemTextBinding = Binding.Property<TabletConfiguration, string>(t => t.Name);
                this.SelectedIndex = 0;
            }

            public void CreateConfiguration()
            {
                var config = new TabletConfiguration
                {
                    Name = "New Tablet"
                };
                AddConfiguration(config);
            }

            public void AddConfiguration(TabletConfiguration config)
            {
                base.Source.Add(config);
                base.SelectedIndex = base.Source.IndexOf(config);
            }

            public void DeleteConfiguration() => DeleteConfiguration(this.SelectedItem);

            public void DeleteConfiguration(TabletConfiguration config)
            {
                base.SelectedIndex--;
                base.Source.Remove(config);
            }

            public async Task GenerateConfiguration()
            {
                var dialog = new DeviceListDialog();
                if (await dialog.ShowModalAsync(this) is HidDevice device)
                {
                    try
                    {
                        var generatedConfig = new TabletConfiguration
                        {
                            Name = device.GetManufacturer() + " " + device.GetProductName(),
                            DigitizerIdentifiers =
                            {
                                new DigitizerIdentifier
                                {
                                    VendorID = device.VendorID,
                                    ProductID = device.ProductID,
                                    InputReportLength = (uint)device.GetMaxInputReportLength(),
                                    OutputReportLength = (uint)device.GetMaxOutputReportLength()
                                }
                            }
                        };
                        AddConfiguration(generatedConfig);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }
            }
        }

        private class ConfigurationSettings : Panel
        {
            public void Update(TabletConfiguration config)
            {
                base.Content = new StackLayout
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Items =
                    {
                        new InputBox(
                            "Name",
                            () => config.Name,
                            (s) => config.Name = s
                        ),
                        new DigitizerIdentifierEditor(
                            "Digitizer Identifiers",
                            () => config.DigitizerIdentifiers,
                            (o) => config.DigitizerIdentifiers = o
                        ),
                        new AuxiliaryIdentifierEditor(
                            "Auxiliary Device Identifiers",
                            () => config.AuxilaryDeviceIdentifiers,
                            (o) => config.AuxilaryDeviceIdentifiers = o
                        ),
                        new DictionaryEditor(
                            "Attributes",
                            () => config.Attributes,
                            (o) => config.Attributes = o
                        )
                    }
                };
            }
        }
    }
}
