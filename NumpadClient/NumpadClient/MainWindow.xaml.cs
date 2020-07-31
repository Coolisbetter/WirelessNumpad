using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsInput;

namespace NumpadClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AsyncClient client;
        public MainWindow()
        {
            InitializeComponent();
            client = new AsyncClient(this);
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            String serverHost = tb_host.Text;
            if (serverHost.Length == 0)
            {
                TextBoxWriteLine("Error. IP/Host field is empty.");
                return;
            }
            int serverPort = 35197;
            if (int.TryParse(iUD_Port.Text, out serverPort) == false)
            {
                TextBoxWriteLine("Error. Invalid port number, using 35197");
                serverPort = 35197;
            }

            Key keyNative = e.Key;
            WindowsInput.Native.VirtualKeyCode keyModded = (WindowsInput.Native.VirtualKeyCode)((int)keyNative)+22;
            if (((int)keyModded) >= 96 && ((int)keyModded) <= 105)
            {
                TextBoxWriteLine("Sending " + keyModded + " ("+(int)keyModded + ") to " + serverHost + ":" + serverPort);
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    client.SendUpdateRequest(keyModded, serverHost, serverPort);
                }).Start();
                
            }
            else
            {
                TextBoxWriteLine("Only numpad 0-9 supported");
            }

        }

        public void TextBoxWriteLine(string output)
        {
            bool scrollToEnd = tb_Output.CaretIndex == tb_Output.Text.Length;
            String time = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            tb_Output.Text += time + ":: " + output + Environment.NewLine;
            if (scrollToEnd)
            {
                tb_Output.CaretIndex = tb_Output.Text.Length;
                tb_Output.ScrollToEnd();
            }
        }
    }

}
