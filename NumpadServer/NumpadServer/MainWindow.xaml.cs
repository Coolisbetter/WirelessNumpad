using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using WindowsInput;

namespace NumpadServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int ServerPort = 35197;
        bool serverRunning = false;
        public ManualResetEvent allDone = new ManualResetEvent(false);
        InputSimulator myInputSim;
        IKeyboardSimulator myKeyboardSim;

        public MainWindow()
        {
            InitializeComponent();
            myInputSim = new InputSimulator();
            myKeyboardSim = myInputSim.Keyboard;
            bt_StartStop_Click(null, null); // start server
            
        }

        private void handleKeyFromClient(string key)
        {
            int keyI = -1;
            int.TryParse(key, out keyI);
            if (keyI >= 0)
            {
                myKeyboardSim.KeyPress((WindowsInput.Native.VirtualKeyCode)keyI);
            }
        }

        #region UI Handlers
        
        private void bt_StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (serverRunning == true)
            {
                lb_Status.Content = "Server is Not Running";
                //StopListeningServer();
                serverRunning = false;
                TextBoxWriteLine("Server Stopped");
            }
            else
            {
                lb_Status.Content = "Server is Running";
                //StartListeningServer();
                serverRunning = true;
                TextBoxWriteLine("Server Started");
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
        #endregion

        #region Socket Code
        public void StopListeningServer()
        {
            if (myListener != null)
            {
                //myListener.Shutdown(SocketShutdown.Receive);
                myListener.Close();
                myListener = null;

            }
        }
        Socket myListener;
        private void StartListeningServer()
        {
            if (myListener != null)
                StopListeningServer();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, ServerPort);

            myListener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                myListener.Bind(localEndPoint);
                myListener.Listen(100);

                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");
                myListener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    myListener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }



        #region Helper Methods
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener;
            Socket handler;
            try
            {
                listener = (Socket)ar.AsyncState;
                handler = listener.EndAccept(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Error getting handler in AcceptCallback");
                return;
            }

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);

            myListener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    myListener);
        }
        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state;
            Socket handler;
            try
            {
                state = (StateObject)ar.AsyncState;
                handler = state.workSocket;
            }
            catch (Exception)
            {
                Console.WriteLine("Error getting handler in ReadCallback");
                return;
            }


            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    content = content.Replace("<EOF>", ""); // Remove the EOF tag
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);

                    IPEndPoint clientIP = handler.RemoteEndPoint as IPEndPoint;
                    string clientIP_S = "";
                    int clientPort_I = 0;

                    if (clientIP != null)
                    {
                        clientIP_S = clientIP.Address.ToString();
                        clientPort_I = clientIP.Port;
                    }
                    Dispatcher.Invoke(() => // Update result window to allow user to continue
                    {
                        String time = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
                        tb_Output.Text += time + ":: Pressing key " + content + " from " + clientIP_S + ":" + clientPort_I + Environment.NewLine;
                        handleKeyFromClient(content);
                    });

                    // Shut down connection now
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        #endregion
        #region Socket Class Object
        public class StateObject
        {
            // Client  socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 1024;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }
        #endregion
        #endregion

    }
}
