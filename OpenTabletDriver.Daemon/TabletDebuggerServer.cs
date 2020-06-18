﻿using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using TabletDriverPlugin;
using TabletDriverPlugin.Tablet;

namespace OpenTabletDriver.Daemon
{
    internal class DeviceDebuggerServer : IDisposable
    {
        public DeviceDebuggerServer()
        {
            PipeServer = new NamedPipeServerStream(Identifier.ToString());
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await PipeServer.WaitForConnectionAsync();
            Log.Debug($"Started device debugger server {{{Identifier}}}");
            JsonWriter = new JsonTextWriter(new StreamWriter(PipeServer));
            await JsonWriter.WriteStartArrayAsync();
        }

        public void HandlePacket(object sender, IDeviceReport report)
        {
            if (PipeServer.IsConnected && JsonWriter != null)
            {
                Serializer.Serialize(JsonWriter, report);
                JsonWriter.Flush();
            }
        }

        public void Dispose()
        {
            JsonWriter.WriteEndArray();
            PipeServer.Disconnect();
            PipeServer.Dispose();
            PipeServer = null;
            
            Log.Debug($"Stopped tablet debugger server {{{Identifier}}}.");
        }

        public Guid Identifier { private set; get; } = Guid.NewGuid();

        private NamedPipeServerStream PipeServer { set; get; }
        private JsonTextWriter JsonWriter { set; get; }
        private JsonSerializer Serializer { set; get; } = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All
        };
    }
}