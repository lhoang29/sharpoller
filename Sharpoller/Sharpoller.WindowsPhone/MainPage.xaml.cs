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
using System.Collections.ObjectModel;
using Windows.UI;

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
        private bool connected = false;
        private bool closing = false;

        TupleList<string, string> commands = new TupleList<string, string>() 
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

        Dictionary<string, string> nameCommandMap = new Dictionary<string, string>();

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            clientSocket = new StreamSocket();

            foreach (Tuple<string, string> nameCommandPair in commands)
            {
                nameCommandMap.Add(nameCommandPair.Item1, nameCommandPair.Item2.Split(':')[0]);
            }

            HashSet<string> existingCommands = new HashSet<string>();
            this.AddCommands(grid.Children, existingCommands);

            ObservableCollection<Button> buttons = new ObservableCollection<Button>();
            for (int i = 0; i < commands.Count; i++)
            {
                if (existingCommands.Contains(commands[i].Item1))
                {
                    continue;
                }

                Button b = new Button();
                b.Content = commands[i].Item1;
                b.Tag = commands[i].Item1;
                b.MinWidth = 120;
                b.Click += Command_Click;

                string[] commandParts = commands[i].Item2.Split(':');
                if (commandParts.Length >= 2)
                {
                    b.Background = new SolidColorBrush(ColorFromHex(commandParts[1]));
                }
                
                buttons.Add(b);
            }
            view.ItemsSource = buttons;
        }

        private void AddCommands(UIElementCollection collection, HashSet<string> existingCommands)
        {
            foreach (UIElement item in collection)
            {
                if (item is Panel)
                {
                    this.AddCommands(((Panel)item).Children, existingCommands);
                }
                else
                {
                    existingCommands.Add(((Control)item).Tag as string);
                }
            }
        }

        private async void StartRecognizing_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of SpeechRecognizer.
            var speechRecognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();

            // Compile the dictation grammar by default.
            await speechRecognizer.CompileConstraintsAsync();

            // Start recognition.
            Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeWithUIAsync();

            for (int i = 0; i < commands.Count; i++)
            {
                if (speechRecognitionResult.Text.ToLower().Contains(commands[i].Item1))
                {
                    await this.ExecuteCommand(commands[i].Item2);
                    break;
                }
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            await InitializeConnection();
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

        //private async void Send_Click(object sender, RoutedEventArgs e)
        //{
        //    await SendData(SendText.Text);
        //}

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
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
                //writer.WriteUInt32(writer.MeasureString(sendData)); // Gets the UTF-8 string length.
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

        private async void Command_Click(object sender, RoutedEventArgs e)
        {
            await this.ExecuteCommand(nameCommandMap[((Control)sender).Tag as string].PadRight(8, ' '));
        }

        private static Color ColorFromHex(string hexaColor)
        {
            return Color.FromArgb(
                Convert.ToByte(hexaColor.Substring(1, 2), 16),
                Convert.ToByte(hexaColor.Substring(3, 2), 16),
                Convert.ToByte(hexaColor.Substring(5, 2), 16),
                Convert.ToByte(hexaColor.Substring(7, 2), 16)
            );
        }
    }
}
