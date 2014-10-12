using System;
using System.Collections.Generic;
using System.Text;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Sharpoller
{
    public class Commander
    {
        private StreamSocket clientSocket;
        private HostName serverHost;
        private bool connected = false;
        private bool closing = false;

        public TupleList<string, string> Commands = new TupleList<string, string>() 
        { 
            { "On", "POWR1" },
            { "Off", "POWR0" },
            { "Netflix", "RCKY59:#FFD63030" },
            { "Hulu", "RCKY55:#FF2ABD3E" },
            { "Pandora", "RCKY56:#FF2AA4B0" },
            { "Youtube", "RCKY57:#FFC1C1C1" },
            { "Smart", "RCKY39" },
            { "Mute", "VOLM000" },
            { "Return", "RCKY45" },
            { "Exit", "RCKY46" },
            { "Menu", "RCKY38" },
            { "Enter", "RCKY40" },
            { "Up", "RCKY41" },
            { "Down", "RCKY42" },
            { "Left", "RCKY43" },
            { "Right", "RCKY44" },
            { "TV", "ITVD0" },
            { "Bluray", "IAVD1" },
            { "Xbox", "IAVD2" },
            { "Chrome", "IAVD3" },
        };

        public Dictionary<string, string> NameCommandMap = new Dictionary<string, string>();

        public Commander()
        {
            clientSocket = new StreamSocket();

            foreach (Tuple<string, string> nameCommandPair in Commands)
            {
                NameCommandMap.Add(nameCommandPair.Item1, nameCommandPair.Item2.Split(':')[0]);
            }
        }

        private async System.Threading.Tasks.Task InitializeConnection()
        {
            try
            {
                serverHost = new HostName("10.0.0.7");
                // Try to connect to the 
                await clientSocket.ConnectAsync(serverHost, "10002");
                connected = true;

                await SendData("lhoang\rhphan");
            }
            catch (Exception exception)
            {
                // If this is an unknown status, 
                // it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                // Could retry the connection, but for this simple example
                // just close the socket.
                closing = true;
                // the Close method is mapped to the C# Dispose
                clientSocket.Dispose();
                clientSocket = null;
            }
        }

        private async System.Threading.Tasks.Task SendData(string command)
        {
            try
            {
                if (!connected)
                {
                    await InitializeConnection();
                }
                // add a newline to the text to send
                string sendData = command + "\r";
                DataWriter writer = new DataWriter(clientSocket.OutputStream);
                writer.WriteString(sendData);

                // Call StoreAsync method to store the data to a backing stream
                await writer.StoreAsync();

                // detach the stream and close it
                writer.DetachStream();
                writer.Dispose();

            }
            catch (Exception exception)
            {
                // If this is an unknown status, 
                // it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                // Could retry the connection, but for this simple example
                // just close the socket.
                closing = true;
                clientSocket.Dispose();
                clientSocket = null;
                connected = false;
            }
        }

        private async System.Threading.Tasks.Task ExecuteCommand(string command)
        {
            await SendData(command);
        }

        public async System.Threading.Tasks.Task ExecuteVoiceCommand(string speechCommand)
        {
            for (int i = 0; i < Commands.Count; i++)
            {
                if (speechCommand.ToLower().Contains(Commands[i].Item1.ToLower()))
                {
                    await ExecuteCommand(this.FormatCommand(NameCommandMap[Commands[i].Item1]));
                    break;
                }
            }
        }

        public async System.Threading.Tasks.Task ExecuteTextCommand(string command)
        {
            await this.ExecuteCommand(this.FormatCommand(NameCommandMap[command]));
        }

        private string FormatCommand(string command)
        {
            return command.PadRight(8, ' ');
        }
    }
}
