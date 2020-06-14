using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TabletDriverLib;
using TabletDriverLib.Binding;
using TabletDriverLib.Diagnostics;
using TabletDriverLib.Plugins;
using TabletDriverPlugin;
using TabletDriverPlugin.Resident;
using TabletDriverPlugin.Tablet;
using static System.Console;

namespace OpenTabletDriver.Console
{
    partial class Program
    {
        #region I/O
            
        static async Task LoadSettings(FileInfo file)
        {
            var settings = Settings.Deserialize(file);
            await ApplySettings(settings);
        }

        static async Task SaveSettings(FileInfo file)
        {
            var settings = await GetSettings();
            settings.Serialize(file);
        }

        #endregion

        #region Modify Settings
            
        static async Task SetDisplayArea(float width, float height, float x, float y)
        {
            await ModifySettings(s => 
            {
                s.DisplayWidth = width;
                s.DisplayHeight = height;
                s.DisplayX = x;
                s.DisplayY = y;
            });
        }
        
        static async Task SetTabletArea(float width, float height, float x, float y, float rotation = 0)
        {
            await ModifySettings(s => 
            {
                s.TabletWidth = width;
                s.TabletHeight = height;
                s.TabletX = x;
                s.TabletY = y;
                s.TabletRotation = rotation;
            });
        }

        static async Task SetSensitivity(float xSens, float ySens)
        {
            await ModifySettings(s => 
            {
                s.XSensitivity = xSens;
                s.YSensitivity = ySens;
            });
        }

        static async Task SetResetTime(int ms)
        {
            await ModifySettings(s => s.ResetTime = TimeSpan.FromMilliseconds(ms));
        }

        static async Task SetTipBinding(string name, string property, float threshold)
        {
            await ModifySettings(s => 
            {
                s.TipButton = BindingTools.GetBindingString(name, property);
                s.TipActivationPressure = threshold;
            });
        }

        static async Task SetPenBinding(string name, string property, int index)
        {
            await ModifySettings(s => s.PenButtons[index] = BindingTools.GetBindingString(name, property));
        }

        static async Task SetAuxBinding(string name, string property, int index)
        {
            await ModifySettings(s => s.AuxButtons[index] = BindingTools.GetBindingString(name, property));
        }

        static async Task SetAutoHook(bool isEnabled)
        {
            await ModifySettings(s => s.AutoHook = isEnabled);
        }

        static async Task SetEnableClipping(bool isEnabled)
        {
            await ModifySettings(s => s.EnableClipping = isEnabled);
        }

        static async Task SetLockAspectRatio(bool isEnabled)
        {
            await ModifySettings(s => s.LockAspectRatio = isEnabled);
        }

        static async Task SetOutputMode(string mode)
        {
            await ModifySettings(s => s.OutputMode = mode);
        }

        static async Task SetFilters(IEnumerable<string> filters)
        {
            await ModifySettings(s => s.Filters = new ObservableCollection<string>(filters));
        }

        static async Task SetResidents(IEnumerable<string> residents)
        {
            await ModifySettings(s => s.ResidentPlugins = new ObservableCollection<string>(residents));
        }

        static async Task SetInputHook(bool isHooked)
        {
            await DriverDaemon.InvokeAsync(d => d.SetInputHook(isHooked));
        }

        #endregion

        #region Request Settings
            
        static async Task GetCurrentLog()
        {
            var log = await DriverDaemon.InvokeAsync(d => d.GetCurrentLog());
            foreach (var message in log)
                await Out.WriteLineAsync(Log.GetStringFormat(message));
        }

        static async Task GetAllSettings()
        {
            await GetAreas();
            await GetSensitivity();
            await GetBindings();
            await GetMiscSettings();
            await GetOutputMode();
            await GetFilters();
            await GetResidents();
        }

        static async Task GetAreas()
        {
            var settings = await GetSettings();
            var displayArea = new Area
            {
                Width = settings.DisplayWidth,
                Height = settings.DisplayHeight,
                Position = new Point
                {
                    X = settings.DisplayX,
                    Y = settings.DisplayY
                }
            };
            await Out.WriteLineAsync($"Display area: {displayArea}");
            
            var tabletArea = new Area
            {
                Width = settings.TabletWidth,
                Height = settings.TabletHeight,
                Position = new Point
                {
                    X = settings.TabletX,
                    Y = settings.TabletY
                },
                Rotation = settings.TabletRotation
            };
            await Out.WriteLineAsync($"Tablet area: {tabletArea}");
        }

        static async Task GetSensitivity()
        {
            var settings = await GetSettings();
            await Out.WriteLineAsync($"Horizontal Sensitivity: {settings.XSensitivity}px/mm");
            await Out.WriteLineAsync($"Vertical Sensitivity: {settings.YSensitivity}px/mm");
            await Out.WriteLineAsync($"Reset time: {settings.ResetTime}");
        }

        static async Task GetBindings()
        {
            var settings = await GetSettings();
            await Out.WriteLineAsync($"Tip Binding: '{settings.TipButton ?? "None"}'@{settings.TipActivationPressure}%");            
            await Out.WriteLineAsync($"Pen Bindings: {string.Join(", ", Tools.GetFormattedBindings(settings.PenButtons))}");
            await Out.WriteLineAsync($"Express Key Bindings: {string.Join(", ", Tools.GetFormattedBindings(settings.AuxButtons))}");
        }

        static async Task GetMiscSettings()
        {
            var settings = await GetSettings();
            await Out.WriteLineAsync($"Auto hook: {settings.AutoHook}");
            await Out.WriteLineAsync($"Area clipping: {settings.EnableClipping}");
            await Out.WriteLineAsync($"Lock aspect ratio: {settings.LockAspectRatio}");
        }
        
        static async Task GetOutputMode()
        {
            var settings = await GetSettings();
            await Out.WriteLineAsync("Output Mode: " + settings.OutputMode);
        }

        static async Task GetFilters()
        {
            var settings = await GetSettings();
            var filters = from path in settings.Filters
                select new PluginReference(path);
            await Out.WriteLineAsync("Filters: " + string.Join(", ", filters));
        }

        static async Task GetResidents()
        {
            var settings = await GetSettings();
            var residents = from path in settings.ResidentPlugins
                select new PluginReference(path);
            await Out.WriteLineAsync("Resident Plugins: " + string.Join(", ", residents));
        }

        #endregion

        #region Actions

        static async Task Detect()
        {
            await DriverDaemon.InvokeAsync(d => d.DetectTablets());
        }

        #endregion

        #region List Types
            
        static async Task ListOutputModes()
        {
            await ListTypes<IOutputMode>();
        }
        
        static async Task ListFilters()
        {
            await ListTypes<IFilter>();
        }

        static async Task ListResidents()
        {
            await ListTypes<IResident>();
        }

        static async Task ListBindings()
        {
            await ListTypes<IBinding>();
        }

        #endregion

        #region Scripting

        static async Task GetAllSettingsJson()
        {
            var settings = await GetSettings();
            await Out.WriteLineAsync(JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        static async Task GetDiagnostics()
        {
            var log = await DriverDaemon.InvokeAsync(d => d.GetCurrentLog());
            var diagnostics = new DiagnosticInfo(log);
            await Out.WriteLineAsync(diagnostics.ToString());
        }
            
        #endregion
    }
}