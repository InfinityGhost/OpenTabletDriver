using Eto.Forms;
using JKang.IpcServiceFramework.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTabletDriver.UX.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TabletDriverLib.Contracts;
using TabletDriverPlugin;
using TabletDriverPlugin.Logging;

namespace OpenTabletDriver.UX.Controls
{
    public class LogView : StackLayout, ILogServer
    {
        public LogView()
        {
            this.Orientation = Orientation.Vertical;

            var toolbar = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Padding = 5,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Items = 
                {
                    GenerateFilterControl(),
                    new Button((sender, e) => Copy(GetFilteredMessages()))
                    {
                        Text = "Copy All"
                    }
                }
            };

            var copyCommand = new Command((sender, e) => Copy(messageList.SelectedItems))
            {
                MenuText = "Copy"
            };
            messageList.ContextMenu = new ContextMenu
            {
                Items = 
                {
                    copyCommand
                }
            };
            
            this.Items.Add(new StackLayoutItem(messageList, HorizontalAlignment.Stretch, true));
            this.Items.Add(new StackLayoutItem(toolbar, HorizontalAlignment.Stretch));

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            var currentMessages = from message in await App.DriverDaemon.InvokeAsync(d => d.GetCurrentLog())
                where message is LogMessage
                select message;

            foreach (var message in currentMessages)
                AddItem(message);
            
            var exitHandle = new CancellationTokenSource();
            var serverGuid = await App.DriverDaemon.InvokeAsync(d => d.SetLogOutput(true));
            
            var host = CreateHostBuilder(serverGuid).Build();
            this.ParentWindow.Closing += async (sender, e) =>
            {
                exitHandle.Cancel();
                host.Dispose();
                await App.DriverDaemon.InvokeAsync(d => d.SetLogOutput(false));
            };

            await host.StartAsync(exitHandle.Token);
        }

        private void Copy(IEnumerable<LogMessage> messages)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var message in messages)
            {
                var line = Log.GetStringFormat(message);
                sb.AppendLine(line);
            }
            Clipboard.Instance.Clear();
            Clipboard.Instance.Text = sb.ToString();
        }

        private IHostBuilder CreateHostBuilder(Guid guid) => 
            Host.CreateDefaultBuilder()
                .ConfigureServices(services => 
                {
                    services.AddSingleton<ILogServer, LogView>((s) => this);
                })
                .ConfigureIpcHost(builder => 
                {
                    builder.AddNamedPipeEndpoint<ILogServer>(guid.ToString());
                });

        private Control GenerateFilterControl()
        {
            var filter = new ComboBox();
            var filterItems = EnumTools.GetValues<LogLevel>();
            foreach (var item in filterItems)
                filter.Items.Add(item.GetName());
            filter.SelectedKey = CurrentFilter.GetName();
            filter.SelectedIndexChanged += (sender, e) => 
            {
                CurrentFilter = filterItems[filter.SelectedIndex];
            };

            return filter;
        }

        private GridView<LogMessage> messageList = new GridView<LogMessage>
        {
            AllowMultipleSelection = true,
            Columns =
            {
                new GridColumn
                {
                    HeaderText = "Time",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Property<LogMessage, string>(m => m.Time.ToLongTimeString())
                    }
                },
                new GridColumn
                {
                    HeaderText = "Level",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Property<LogMessage, string>(m => m.Level.GetName())
                    }
                },
                new GridColumn
                {
                    HeaderText = "Group",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Property<LogMessage, string>(m => m.Group)
                    }
                },
                new GridColumn
                {
                    HeaderText = "Message",
                    DataCell = new TextBoxCell
                    {
                        Binding = Binding.Property<LogMessage, string>(m => m.Message)
                    }
                }
            }
        };

        private List<LogMessage> Messages { set; get; } = new List<LogMessage>();

        private LogLevel _currentFilter = LogLevel.Info;
        public LogLevel CurrentFilter
        {
            set
            {
                _currentFilter = value;
                Refresh();
            }
            get => _currentFilter;
        }

        private IEnumerable<LogMessage> GetFilteredMessages()
        {
            return from message in Messages
                where message.Level >= CurrentFilter
                select message;
        }

        private void Update(int index)
        {
            messageList.DataStore = GetFilteredMessages();
            messageList.ReloadData(index);
        }

        private void Update(int startIndex, int endIndex)
        {
            messageList.DataStore = GetFilteredMessages();
            messageList.ReloadData(new Range<int>(startIndex, endIndex));
        }

        private void AddItem(LogMessage message)
        {
            Messages.Add(message);

            if (message.Level >= CurrentFilter)
                Update(Messages.Count - 1);

            if (messageList.SelectedRow == -1)
                messageList.ScrollToRow(GetFilteredMessages().Count() - 1);
        }

        private void Refresh()
        {
            Update(0, Messages.Count - 1);
        }

        public void Post(LogMessage message)
        {
            Application.Instance.AsyncInvoke(() => AddItem(message));
        }
    }
}