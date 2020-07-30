﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace NumpadClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AsyncClient client = new AsyncClient();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            Key keyNative = e.Key;
            WindowsInput.Native.VirtualKeyCode keyModded = (WindowsInput.Native.VirtualKeyCode)((int)keyNative)+22;
            if (((int)keyModded) >= 96 && ((int)keyModded) <= 105)
            {
                TextBoxWriteLine("Sending " + keyModded + " ("+(int)keyModded + ") to server.");
                client.SendUpdateRequest(keyModded);
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
