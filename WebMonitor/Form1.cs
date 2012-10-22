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
using System.Security.Cryptography;


namespace WebMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
                                                    
        Settings settings = new Settings();

        public string Traceroute(string ipAddressOrHostName)
        {
            IPAddress ipAddress = Dns.GetHostEntry(ipAddressOrHostName).AddressList[0];
            
            string result = DateTime.Now.ToString() + ";";
            using (Ping pingSender = new Ping())
            {
                PingOptions pingOptions = new PingOptions();
                byte[] bytes = new byte[32];
                pingOptions.DontFragment = true;
                pingOptions.Ttl = 1;
                int maxHops = 30;
                for (int i = 1; i < maxHops + 1; i++)
                {
                    PingReply pingReply = pingSender.Send(
                        ipAddress,
                        5000,
                        new byte[32], pingOptions);

                    result = result + pingReply.Address.ToString();
                    if (pingReply.Status == IPStatus.Success)
                    {
                        break;
                    }
                    else
                    {
                        result = result + ";";
                    }
                    pingOptions.Ttl++;
                }
            }
            return result;
        }

        private bool SendMail(string server, int port, string mail, string username, string password, string subject, string message, bool SSl, string toMail)
        {
            try
            {
                SmtpClient Smtp = new SmtpClient(server, port);
                Smtp.EnableSsl = SSl; 
                Smtp.Credentials =  new NetworkCredential(username, password);
                MailMessage Message = new MailMessage();
                Message.From = new MailAddress(mail);
                Message.To.Add(new MailAddress(toMail));
                Message.Subject = subject;
                Message.Body = message;
                Smtp.Send(Message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ICMPConnect(string serverIP)  //Провека работы сервера (если сервер работает, то возвращает "ложно" это значит проблема в службе на порте)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data); // Содержимое пакета
            try
            {
                PingReply reply = pingSender.Send(serverIP, 2000, buffer, options); // Отправление пинга
                if (reply.Status == IPStatus.Success)
                {
                    return false;   // Ошибки нет
                }
                else
                {
                    return true;    // Ошибка в сервере или в канале связи
                }
            }
            catch (System.Net.NetworkInformation.PingException)
            {
                return true;
            }
        }

        private void ConnectToServers(object sender, EventArgs e)
        {
            if (NetworkInterface.GetIsNetworkAvailable() == true)
            {
                if (DateTime.Compare(DateTime.Now, settings.beginOfTest.AddMinutes(Convert.ToDouble(numericUpDown5.Value))) <= 0)
                {
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)  //проверяем все серверы
                    {
                        string serverIP = (string)dataGridView1.Rows[i].Cells[0].Value;
                        int port = int.Parse((string)dataGridView1[1, i].Value);
                        TCP tcp = new TCP();
                        bool result = tcp.ConnectSocket(serverIP, port);    //TCP соединение
                        if (result)
                        {
                            dataGridView1[2, i].Value = "Успешно [" + DateTime.Now.GetDateTimeFormats('t')[0] + "]";
                            dataGridView1[2, i].Style.ForeColor = System.Drawing.Color.Green;
                            dataGridView1[7, i].Value = Traceroute(serverIP); //Запоминаем маршрут до сервера
                            dataGridView1[3, i].Value = 0;  //Обнуление ошибок TCP
                            dataGridView1[4, i].Value = 0;  //Обнуление ошибок ICMP
                        }
                        else
                        {
                            dataGridView1[2, i].Value = "Ошибка соединения [" + DateTime.Now.GetDateTimeFormats('t')[0] + "]";
                            dataGridView1[2, i].Style.ForeColor = System.Drawing.Color.Red;
                            int err = Convert.ToInt32(dataGridView1[3, i].Value);
                            dataGridView1[3, i].Value = err + 1;   //Увеличение кол-ва ошибок TCP
                            if (Convert.ToInt32(dataGridView1[3, i].Value) >= settings.GetTCPTriesLimit())
                            {
                                result = ICMPConnect(serverIP); //ICMP соединение 
                                if (result == true)             // Ошибка в сервере или в канале связи
                                {
                                    int warnings = 0;
                                    if (((string)dataGridView1[7, i].Value).Length != 0)      //Трассируем
                                    {
                                        string[] timeAndAdress = ((string)dataGridView1[7, i].Value).Split(';');//если дата свежая
                                        TimeSpan comparison = DateTime.Now - DateTime.Parse(timeAndAdress[0]);
                                        if (timeAndAdress.Length > 3) //Больше минимальной длинны маршрута
                                        {
                                            if (comparison.Days <= 30)
                                            {
                                                for (int j = (timeAndAdress.Length - 1); j > 0; j--)
                                                {
                                                    if (ICMPConnect(timeAndAdress[j]) == true)
                                                    {
                                                        warnings++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (warnings >= 2)  //Ошибка в сети
                                    {
                                        result = true;
                                        dataGridView1[2, i].Value = "Неполадки с сетью [" + DateTime.Now.GetDateTimeFormats('t')[0] + "]";
                                    }
                                    else
                                    {
                                        //Сервер сломался т.к. программа проверила маршрут до него, а сам сервер не работает
                                        // Пишем письма админу сервера и web-сервера

                                        dataGridView1[2, i].Value = "Неполадки с сервером [" + DateTime.Now.GetDateTimeFormats('t')[0] + "]";
                                        dataGridView1[4, i].Value = Convert.ToInt32(dataGridView1[4, i].Value) + 1;
                                        int errors = Convert.ToInt32(dataGridView1[4, i].Value);
                                        if (errors >= settings.GetICMPTriesLimit())
                                        {
                                            if (WindowState == FormWindowState.Minimized)
                                            {
                                                notifyIcon1.ShowBalloonTip(3000, "WebMonitor", "Невозможно связаться с сервером по адресу: " + serverIP, ToolTipIcon.Error);
                                            }
                                            if (settings.usePost == true)
                                            {
                                                if ((((string)dataGridView1.Rows[i].Cells[6].Value).Length != 0) || (((string)dataGridView1.Rows[i].Cells[5].Value).Length != 0))
                                                {
                                                    string textToAdmin = (string)dataGridView1.Rows[i].Cells[8].Value;
                                                    if (textToAdmin.Length == 0)
                                                    {
                                                        textToAdmin = "Сервер с адресом " + serverIP + " не работает.";
                                                    }
                                                    //если не связываемся с сервером, то шлём письмо о не работающем сервере
                                                    bool res = SendMail(settings.GetSMTPServer(), settings.GetSMTPPort(), settings.GetMailFrom(), settings.GetMailUsername(), settings.GetMailPassword(), "Уведомление об ошибке.", textToAdmin, settings.SSL, (string)dataGridView1.Rows[i].Cells[5].Value);
                                                    string textToWebAdmin = (string)dataGridView1.Rows[i].Cells[9].Value;
                                                    if (textToWebAdmin.Length == 0)
                                                    {
                                                        textToWebAdmin = "На сервере с адресом " + serverIP + " служба, использующая порт " + port + " не работает.";
                                                    }
                                                    res = SendMail(settings.GetSMTPServer(), settings.GetSMTPPort(), settings.GetMailFrom(), settings.GetMailUsername(), settings.GetMailPassword(), "Уведомление об ошибке.", textToWebAdmin, settings.SSL, (string)dataGridView1.Rows[i].Cells[6].Value);
                                                    dataGridView1[4, i].Value = 0;  //Обнуление ошибок ICMP
                                                    dataGridView1[3, i].Value = 0;  //Обнуление ошибок TCP
                                                    if (res == false)
                                                    {
                                                        MessageBox.Show("Невозможно отправить письмо, проверьте настройки");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //  Сервер в порядке, а web-сервер сломался т.к. сервер отвечает на пинг, а служба на порте не работает
                                    //  Пишем письмо админу web-сервера

                                    dataGridView1[2, i].Value = "Неполадки с службой на порте [" + DateTime.Now.GetDateTimeFormats('t')[0] + "]";
                                    dataGridView1[4, i].Value = Convert.ToInt32(dataGridView1[4, i].Value) + 1;   //Увеличение кол-ва ошибок ICMP 
                                    if (Convert.ToInt32(dataGridView1[4, i].Value) >= settings.GetICMPTriesLimit())
                                    {
                                        if (WindowState == FormWindowState.Minimized)
                                        {
                                            notifyIcon1.ShowBalloonTip(3000, "WebMonitor", "Невозможно связаться с сервисом по адресу: " + serverIP + " использующий порт " + port, ToolTipIcon.Error);
                                        }
                                        if (settings.usePost == true)
                                        {
                                            if (((string)dataGridView1.Rows[i].Cells[6].Value).Length != 0) 
                                            {
                                                string textToWebAdmin = (string)dataGridView1.Rows[i].Cells[9].Value;
                                                if (textToWebAdmin.Length == 0)
                                                {
                                                    textToWebAdmin = "На сервере с адресом " + serverIP + " служба, использующая порт " + port + " не работает.";
                                                }
                                                //если связываемся с сервером, то шлём письмо о неработающей службе на порте
                                                bool res = SendMail(settings.GetSMTPServer(), settings.GetSMTPPort(), settings.GetMailFrom(), settings.GetMailUsername(), settings.GetMailPassword(), "Уведомление об ошибке.", textToWebAdmin, settings.SSL, (string)dataGridView1.Rows[i].Cells[6].Value);
                                                if (res == false)
                                                {
                                                    MessageBox.Show("Невозможно отправить письмо, проверьте настройки");
                                                }
                                            }
                                        }
                                        dataGridView1[4, i].Value = 0;  //Обнуление ошибок ICMP
                                        dataGridView1[3, i].Value = 0;  //Обнуление ошибок TCP
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    button1_Click(sender, e);
                    notifyIcon1.ShowBalloonTip(3000, "WebMonitor", "Проверка закончена", ToolTipIcon.Info);
                }
            }
            else
            {
                MessageBox.Show("Ваш компьютер не подключен к интернету!");
                button1_Click(sender, e);
            }
        }
 
        #region Save/Load
            public bool LoadSettings()
            {
                StreamReader loadSettings = new StreamReader(Application.StartupPath + "\\settings.ini", false);
                string buffer = loadSettings.ReadLine();
                int countOfSrevers = int.Parse(buffer);
                for (int i = 0; i < countOfSrevers; i++)
                {
                    buffer = loadSettings.ReadLine();
                    string[] ips = buffer.Split(':');
                    buffer = loadSettings.ReadLine();
                    string[] admins = buffer.Split(';');
                    string textToAdmin = loadSettings.ReadLine();
                    string textToWebAdmin = loadSettings.ReadLine();
                    buffer = loadSettings.ReadLine();
                    dataGridView1.Rows.Add(ips[0], ips[1], "", "0", "0", admins[0], admins[1], buffer, textToAdmin, textToWebAdmin);                    
                }
                settings.SetTestInterval(loadSettings.ReadLine());
                settings.SetTCPTriesLimit(loadSettings.ReadLine());
                settings.SetTCPInterval(loadSettings.ReadLine());
                settings.SetICMPTriesLimit(loadSettings.ReadLine());
                if (loadSettings.ReadLine() == "YES")
                {
                    settings.saveSettings = true;
                }
                if (loadSettings.ReadLine() == "Use Post")
                {
                    settings.usePost = true;
                    settings.SetSMTPServer(loadSettings.ReadLine());
                    settings.SetSMTPPort(loadSettings.ReadLine());
                    settings.SetMailFrom(loadSettings.ReadLine());
                    settings.SetMailUsername(loadSettings.ReadLine());
                    settings.SetMailPassword(loadSettings.ReadLine());
                    if (loadSettings.ReadLine() == "YES")
                    {
                        settings.SSL = true;
                    }             
                }
                else
                {
                    settings.usePost = false;
                }
                if (loadSettings.ReadLine() == "YES")
                {
                    settings.AutoStart = true;
                }
                loadSettings.Close();
                return true;
            }
            
            private bool SaveSettings()
            {
                StreamWriter saveSettings = new StreamWriter(Application.StartupPath + "\\settings.ini", false);
                saveSettings.WriteLine(dataGridView1.RowCount);
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    string server = (string)dataGridView1.Rows[i].Cells[0].Value;
                    string port = ((string)dataGridView1[1, i].Value);
                    string admin = ((string)dataGridView1[5, i].Value);
                    string webAdmin = ((string)dataGridView1[6, i].Value);
                    string textToAdmin = ((string)dataGridView1[8, i].Value);
                    string textToWebAdmin = ((string)dataGridView1[9, i].Value);
                    string trace =  ((string)dataGridView1[7, i].Value);

                    saveSettings.WriteLine(server + ":" + port);
                    saveSettings.WriteLine(admin + ";" + webAdmin);
                    saveSettings.WriteLine(textToAdmin);
                    saveSettings.WriteLine(textToWebAdmin);
                    saveSettings.WriteLine(trace);
                }
                saveSettings.WriteLine(settings.GetTestInterval());
                saveSettings.WriteLine(settings.GetTCPTriesLimit());
                saveSettings.WriteLine(settings.GetTCPInterval());
                saveSettings.WriteLine(settings.GetICMPTriesLimit());
                if (settings.saveSettings == true)
                {
                    saveSettings.WriteLine("YES");
                }
                else 
                {
                    saveSettings.WriteLine("NO");      
                }
                if (settings.usePost == true)
                {
                    saveSettings.WriteLine("Use Post");
                    saveSettings.WriteLine(settings.GetSMTPServer());
                    saveSettings.WriteLine(settings.GetSMTPPort());
                    saveSettings.WriteLine(settings.GetMailFrom());
                    saveSettings.WriteLine(settings.GetMailUsername());
                    saveSettings.WriteLine(settings.GetMailPassword());
                    if (settings.SSL == true)
                    {
                        saveSettings.WriteLine("YES");
                    }
                    else
                    {
                        saveSettings.WriteLine("NO");
                    }
                }
                else
                {
                    saveSettings.WriteLine("No Use Post");
                }
                if (settings.AutoStart == true)
                {
                    saveSettings.WriteLine("YES");
                }
                else
                {
                    saveSettings.WriteLine("NO");
                }
                saveSettings.Close();
                return true;
            }
        #endregion
  
        #region Form Load/Close

            private void Form1_Load(object sender, EventArgs e)     // Действия при старте 
            {
                //string a = "" + ((Environment.TickCount/1000)/60);      //время прошедшее с запуска компьютера наверно не нужно
                if (File.Exists(Application.StartupPath + "\\settings.ini"))
                {
                    bool result = LoadSettings(); // попробовать загрузить настройки
                    if (result == true)
                    {
                        if (settings.usePost == true)
                        {
                            checkBox2.Checked = true;
                        }
                        if (settings.saveSettings == true)
                        {
                            checkBox3.Checked = true;
                        }
                        numericUpDown1.Value = settings.GetTCPInterval();
                        numericUpDown2.Value = settings.GetTCPTriesLimit();
                        numericUpDown5.Value = settings.GetTestInterval();
                        numericUpDown4.Value = settings.GetICMPTriesLimit();
                        textBox3.Text = settings.GetSMTPServer();
                        textBox4.Text = textBox4.Text + settings.GetSMTPPort();
                        textBox5.Text = settings.GetMailFrom();
                        textBox6.Text = settings.GetMailUsername();
                        maskedTextBox1.Text = settings.GetMailPassword();
                        textBox7.Text = settings.GetServerAdminEmail();
                        textBox8.Text = settings.GetWebAdminEmail();
                        if (settings.SSL == true)
                        {
                            checkBox4.Checked = true;
                        }
                        if(settings.AutoStart == true)
                        {
                            checkBox5.Checked = true;
                        }
                        RegistryKey reg = Registry.LocalMachine.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
                        string ExePath = System.Windows.Forms.Application.ExecutablePath;
                        string buffer = (string)reg.GetValue("WebMonitor");
                        if (buffer == ExePath)
                        {
                            checkBox1.Checked = true;                            
                        }
                        if (settings.AutoStart == true)
                        {
                            this.ShowInTaskbar = false;
                            WindowState = FormWindowState.Minimized;//Запуск в свёрнутом виде                            
                            button1_Click(sender, e);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Вы можете настроить программу и сохранить эти настройки для следующих запусков", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            private void Form1_FormClosing(object sender, FormClosingEventArgs e) // Действия при закрытии 
            {
                timer1.Enabled = false;
                // На всякий случай сохраникм все поля связанные с почтой ещё раз
                settings.SetSMTPServer(textBox3.Text);
                settings.SetSMTPPort(textBox4.Text);
                settings.SetMailFrom(textBox5.Text);
                settings.SetMailUsername(textBox6.Text);
                settings.SetMailPassword(maskedTextBox1.Text);
                if (settings.saveSettings == true)
                {
                    SaveSettings();
                }
            }

        #endregion

        #region SystemTray

        private void Form1_Resize(object sender, EventArgs e)               //Сворачивание в трей
            {
                if (FormWindowState.Minimized == WindowState)
                {
                    Hide();
                }
            }

            private void toolStripMenuItem2_Click(object sender, EventArgs e)   //1 строка контекстного меню
            {
                button1_Click(sender, e);
            }

            private void toolStripMenuItem3_Click(object sender, EventArgs e)   //2 строка контекстного меню
            {
                Application.Exit(); 
            }

            private void notifyIcon1_DoubleClick_1(object sender, EventArgs e)//Разворачиваем окно
            {
                Show();
                WindowState = FormWindowState.Normal;
            }

        #endregion

        #region Clicks,Ticks&Changes

            private void timer1_Tick(object sender, EventArgs e)        //Повторяющиеся действия при работе программы
            {
                backgroundWorker1.RunWorkerAsync();
            }   
        
            private void checkBox1_CheckedChanged(object sender, EventArgs e)       //Работа с автозагрузкой
            {
                
                if (checkBox1.Checked == true) // Добавление в автозагрузку
                {
                    string ExePath = System.Windows.Forms.Application.ExecutablePath;
                    RegistryKey reg = Registry.LocalMachine.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
                    reg.SetValue("WebMonitor", ExePath);
                    reg.Close();
                }
                else // Удаление из автозагрузки
                {                
                    RegistryKey reg = Registry.LocalMachine.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
                    reg.DeleteValue("WebMonitor");
                    reg.Close();
                }
            }

            private void checkBox2_CheckedChanged(object sender, EventArgs e)       //Вкл/выкл почтовое уведомление
            {
                settings.usePost = checkBox2.Checked;
                if (checkBox2.Checked == true) 
                {
                    for (int i = groupBox2.Height; i <= 250; i=i+10)
                    {
                        groupBox2.Height = i;
                    }                    
                }
                else
                {
                    for (int i = groupBox2.Height; i >= 40; i=i-10)
                    {
                        groupBox2.Height = i;
                    }
                    //textBox7.Visible = false;
                    //textBox8.Visible = false;
                    //richTextBox2.Visible = false;
                    //richTextBox3.Visible = false;
                    //label15.Visible = false;
                    //label16.Visible = false;
                    //label17.Visible = false;
                    //label12.Visible = false;
                }
            }

            private void checkBox3_CheckedChanged(object sender, EventArgs e)
            {
                settings.saveSettings = checkBox3.Checked;
            }

            private void checkBox4_CheckedChanged(object sender, EventArgs e)
            {
                settings.SSL = checkBox4.Checked;
            }

            private void checkBox5_CheckedChanged(object sender, EventArgs e)
            {
                if (checkBox5.Checked == true)
                {
                    settings.AutoStart = true;
                }
                else
                {
                    settings.AutoStart = false;
                }
            }

            private void button1_Click(object sender, EventArgs e)                  //Вкл/выкл проверверку
            {
                if (button1.Text == "Начать проверку")
                {
                    if (NetworkInterface.GetIsNetworkAvailable() == true)
                    {
                        if (dataGridView1.Rows.Count != 0)
                        {
                            notifyIcon1.Text = "WebMonitor [работаю]";
                            contextMenuStrip1.Items[0].Text = "Остановить";
                            button1.Text = "Закончить проверку";
                            settings.SetTCPIntervalFromNumericValue(numericUpDown1.Value);
                            timer1.Interval = settings.GetTCPInterval() * 60000; // Установка времени повтора таймера
                            timer1.Enabled = true;                  // Включаем таймер, в тики которого встроены действия проверки
                            settings.beginOfTest = DateTime.Now;
                            backgroundWorker1.RunWorkerAsync();
                        }
                        else
                        {
                            MessageBox.Show("Задайте хотя бы один сервер!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ваш компьютер не подключен к интернету!");
                    }
                }
                else
                {
                    if (backgroundWorker1.IsBusy == false)
                    {
                        button1.Text = "Начать проверку";
                        contextMenuStrip1.Items[0].Text = "Запустить";
                        notifyIcon1.Text = "WebMonitor";
                        timer1.Enabled = false;
                    }
                    else
                    {

                    }
                }
            }

            private void button4_Click(object sender, EventArgs e)      //Добавление нового элемента на проверку
            {
                bool result = false;
                result = settings.SetServerAdress(textBox1.Text);
                if (result == true)
                {
                    result = settings.SetServerPort(textBox2.Text);
                    if (result == true)
                    {
                        if (textBox7.Text.Length != 0)
                        {
                            result = settings.SetServerAdminEmail(textBox7.Text);
                            MessageBox.Show("Неправильно введён адрес администратора!");
                        }
                        if (result == true)
                        {
                            if (textBox8.Text.Length != 0)
                            {
                                result = settings.SetWebAdminEmail(textBox8.Text);
                                MessageBox.Show("Неправильно введён адрес Web-администратора!");

                            }
                            if (result == true)
                            {
                                string[] addRow = { settings.GetServerAdress(), textBox2.Text, "", "0", "0", textBox7.Text, textBox8.Text, "", richTextBox2.Text, richTextBox3.Text };
                                dataGridView1.Rows.Add(addRow);
                                if ((textBox8.Text.Length == 0) || (textBox7.Text.Length == 0))
                                {
                                    MessageBox.Show("Вы не заполнили адреса администраторов, и если Вы включите почтовое уведомление, то письмо отправлятся не будет");
                                }
                                if ((richTextBox2.Text.Length == 0) || (richTextBox3.Text.Length == 0))
                                {
                                    MessageBox.Show("Не введён текст письма. Текст письма будет содержать строку по умолчанию (На сервере с адресом " + textBox1.Text + " служба, использующая порт " + textBox2.Text + " не работает)");
                                }
                                textBox1.Text = "";
                                textBox2.Text = "";
                                textBox7.Text = "";
                                textBox8.Text = "";
                                richTextBox2.Text = "";
                                richTextBox3.Text = "";
                             }
                        }
                           
                    }
                    else
                    {
                        MessageBox.Show("Ошибка: Номер порта от 1 до 65536");
                    }    
                }
                else
                {
                    MessageBox.Show("Ошибка: Введите номер порта и адрес сервера");
                }
            }
        
            private void button5_Click(object sender, EventArgs e)      //Удаление элемента из проверки
            {

                if (dataGridView1.RowCount != 0)
                {
                    dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
                }
                else
                {
                    if (button1.Text != "Начать проверку")
                    {
                        button1_Click(sender, e);
                        MessageBox.Show("Элементов для проверки не осталось.");
                    }
                }
                if (dataGridView1.RowCount == 0)
                {
                    if (button1.Text != "Начать проверку")
                    {
                        button1_Click(sender, e);
                    }
                }
            }

            private void textBox3_Leave(object sender, EventArgs e)
            {
                bool result = settings.SetSMTPServer(textBox3.Text);
                if (result == false)
                {
                    MessageBox.Show("Неправильное имя почтового сервера!");
                }
            }

            private void textBox4_Leave(object sender, EventArgs e)
            {
                bool result = settings.SetSMTPPort(textBox4.Text);
                if (result == false)
                {
                    MessageBox.Show("Неправильное порт почтового сервера!");
                }
            }

            private void textBox5_Leave(object sender, EventArgs e)
            {
                bool result = settings.SetMailFrom(textBox5.Text);
                if (result == false)
                {
                    MessageBox.Show("Неправильно введён адрес!");
                }
            }

            private void textBox6_Leave(object sender, EventArgs e)
            {
                bool result = settings.SetMailUsername(textBox6.Text);
                if (result == false)
                {
                    MessageBox.Show("Неправильно введёно имя пользователя!");
                }
            }

            private void maskedTextBox1_Leave(object sender, EventArgs e)
            {
                bool result = settings.SetMailPassword(maskedTextBox1.Text);
                if (result == false)
                {
                    MessageBox.Show("Неправильно введён пароль!");
                }
            }

            private void numericUpDown1_ValueChanged(object sender, EventArgs e)    //Изменение интервала времени
            {
                timer1.Interval = Convert.ToInt32(numericUpDown1.Value) * 60000; // Установка времени повтора таймера
                settings.SetTCPIntervalFromNumericValue(numericUpDown1.Value);
            }
            
            private void numericUpDown2_ValueChanged(object sender, EventArgs e)
            {
                settings.SetTCPTriesLimitFromNumericValue(numericUpDown2.Value);
            }

            private void numericUpDown5_ValueChanged(object sender, EventArgs e)
            {
                settings.SetTestIntervalFromNumericValue(numericUpDown5.Value);
            }

            private void numericUpDown4_ValueChanged(object sender, EventArgs e)
            {
                settings.SetICMPTriesLimitFromNumericValue(numericUpDown4.Value);
            }

            private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
            {
                ConnectToServers(sender, e);
            }
        #endregion

    }
}
