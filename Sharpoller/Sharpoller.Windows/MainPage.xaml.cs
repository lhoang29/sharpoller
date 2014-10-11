using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Sharpoller
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StreamSocket clientSocket;
        private HostName serverHost;
        private string serverHostnameString;
        private string serverPort;
        private bool connected = false;
        private bool closing = false;

        public MainPage()
        {
            this.InitializeComponent();
            clientSocket = new StreamSocket();
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (connected)
            {
                StatusText.Text = "Already connected";
                return;
            }

            try
            {
                OutputView.Text = "";
                StatusText.Text = "Trying to connect ...";

                serverHost = new HostName(ServerHostname.Text);
                // Try to connect to the 
                await clientSocket.ConnectAsync(serverHost, ServerPort.Text);
                connected = true;
                StatusText.Text = "Connection established" + Environment.NewLine;

                await SendData("lhoang\rhphan");
                //await ReceiveData();
            }
            catch (Exception exception)
            {
                // If this is an unknown status, 
                // it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                StatusText.Text = "Connect failed with error: " + exception.Message;
                // Could retry the connection, but for this simple example
                // just close the socket.

                closing = true;
                // the Close method is mapped to the C# Dispose
                clientSocket.Dispose();
                clientSocket = null;
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                StatusText.Text = "Must be connected to send!";
                return;
            }

            await SendData(SendText.Text);
            //await ReceiveData();
        }

        private async System.Threading.Tasks.Task SendData(string command)
        {
            try
            {
                OutputView.Text = "";
                StatusText.Text = "Trying to send data ...";

                // add a newline to the text to send
                string sendData = command + "\r";
                DataWriter writer = new DataWriter(clientSocket.OutputStream);
                //writer.WriteUInt32(writer.MeasureString(sendData)); // Gets the UTF-8 string length.
                writer.WriteString(sendData);

                // Call StoreAsync method to store the data to a backing stream
                await writer.StoreAsync();

                StatusText.Text = "Data was sent" + Environment.NewLine;

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

                StatusText.Text = "Send data or receive failed with error: " + exception.Message;

                // Could retry the connection, but for this simple example
                // just close the socket.
                closing = true;
                clientSocket.Dispose();
                clientSocket = null;
                connected = false;
            }
        }

        private async System.Threading.Tasks.Task ReceiveData()
        {
            // Now try to receive data from server
            try
            {
                OutputView.Text = "";
                StatusText.Text = "Trying to receive data ...";

                DataReader reader = new DataReader(clientSocket.InputStream);
                // Set inputstream options so that we don't have to know the data size
                reader.InputStreamOptions = InputStreamOptions.Partial;

                await reader.LoadAsync(512);

                string receivedStrings = "";

                // Keep reading until we consume the complete stream. 
                while (reader.UnconsumedBufferLength > 0)
                {
                    // Note that the call to readString requires a length of "code units"  
                    // to read. This is the reason each string is preceded by its length  
                    // when "on the wire". 
                    uint bytesToRead = reader.ReadUInt32();
                    receivedStrings += reader.ReadString(bytesToRead) + "\n";
                } 

                reader.DetachStream();
                reader.Dispose();
            }
            catch (Exception exception)
            {
                // If this is an unknown status, 
                // it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                StatusText.Text = "Receive failed with error: " + exception.Message;

                // Could retry, but for this simple example
                // just close the socket.
                closing = true;
                clientSocket.Dispose();
                clientSocket = null;
                connected = false;
            }
        }
    }
}
