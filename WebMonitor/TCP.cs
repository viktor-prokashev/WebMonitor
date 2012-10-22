using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Net;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;

namespace WebMonitor
{
    public class TCP
    {
        public bool ConnectSocket(string serverIP, int port)   //TCP соединение 
        {
            bool result;
            Socket socketToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult resultOfConnect = socketToServer.BeginConnect(serverIP, port, null, socketToServer);
            if (resultOfConnect.AsyncWaitHandle.WaitOne(2000) == true)
            {
                result = true; // Всё хорошо
                socketToServer.Disconnect(true);

            }
            else
            {
                result = false; // Ошибка
            }
            return result;
        }
    }
}
