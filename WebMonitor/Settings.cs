using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;

namespace WebMonitor
{
    public class Settings
    {
        public Settings() { } //Конструктор /*+*/
        #region ClassFields
        public bool AutoStart = false;  //Автозапуск приложения при загрузке ОС /*+*/
        public bool usePost = false; //Использовать почтовое уведомление        /*+*/Добавил
        public bool saveSettings = false;//                                     /*+*/Добавил
        public bool SSL = false; //Использовать SSL при отправке почты          /*+*/
        public DateTime beginOfTest; //                                         /*+*/Добавил
        private string ServerAdress;  //Адрес сервера для тестирования          //Используется для проверки
        private int ServerPort = 80;  //Порт сервера для тестирования           //Используется для проверки
        private int TestInterval = 60;  //Основной интервал тестирования        /*+*/
        private int TCPInterval = 1;  //Интервал опроса по TCP                  /*+*/ 
        private int TCPTriesLimit = 3;  //Кол-во попыток соединения по TCP      /*+*/  
        private int ICMPInterval = 1;  //Интервал опроса по ICMP                /*-*/
        private int ICMPTriesLimit = 3;  //Кол-во попыток соединения по ICMP    /*+*/
        private string SettingsPath = Environment.CurrentDirectory + @"\WebMonitor.conf";//Имя файла+путь для настроек /*+*/
        private string MailFrom;  //E-mail от которого отправляется письмо      /*+*/
        private string MailUsername;  //Имя пользователя для отправки почты     /*+*/
        private string MailPassword;  //Пароль для отправки почты               /*+*/
        private string SMTPServer;  //Адрес SMTP сервера                        /*+*/
        private int SMTPPort = 25;  //Порт SMTP сервера                         /*+*/
        private string ServerAdminEmail; //E-mail администратора сервера        /*+*/
        private string WebAdminEmail; //E-mail администратора Web-сервера       /*+*/      
        public DateTime[] PointDateTime = new DateTime[10];
        public string[] PointAdress = new string[10];

        #endregion
        #region ClassMethods
        #region TopSecret
        /// <summary>
        /// ВНИМАНИЕ!!! НЕ ИСПОЛЬЗОВАТЬ!!!
        /// </summary>
        public class Options
        {
            public Options() { }
            public bool AutoStart;
            public bool SSL;
            public string ServerAdress;
            public int ServerPort;
            public int TestInterval;
            public int TCPInterval;
            public int TCPTriesLimit;
            public int ICMPInterval;
            public int ICMPTriesLimit;
            public string MailFrom;
            public string MailUsername;
            public string MailPassword;
            public string SMTPServer;
            public int SMTPPort;
            public string ServerAdminEmail;
            public string WebAdminEmail;
            public DateTime[] PointDateTime = new DateTime[10];
            public string[] PointAdress = new string[10];
        }
        #endregion
        /// <summary>
        /// Сохранение настроек в файл
        /// </summary>
        /// <returns>True, если все получилось; False, если возникла ошибка</returns>
        public bool SaveSetToFile()
        {
            Options OP = new Options();
            OP.AutoStart = this.AutoStart;
            OP.SSL = this.SSL;
            OP.ServerAdress = this.ServerAdress;
            OP.ServerPort = this.ServerPort;
            OP.TestInterval = this.TestInterval;
            OP.TCPInterval = this.TCPInterval;
            OP.TCPTriesLimit = this.TCPTriesLimit;
            OP.ICMPInterval = this.ICMPInterval;
            OP.ICMPTriesLimit = this.ICMPTriesLimit;
            OP.MailFrom = this.MailFrom;
            OP.MailUsername = this.MailUsername;
            OP.MailPassword = this.MailPassword;
            OP.SMTPServer = this.SMTPServer;
            OP.SMTPPort = this.SMTPPort;
            OP.ServerAdminEmail = this.ServerAdminEmail;
            OP.WebAdminEmail = this.WebAdminEmail;
            this.PointDateTime.CopyTo(OP.PointDateTime, 0);
            this.PointAdress.CopyTo(OP.PointAdress, 0);
            try
            {
                XmlSerializer XmlSer = new XmlSerializer(OP.GetType());
                StreamWriter Writer = new StreamWriter(SettingsPath);
                XmlSer.Serialize(Writer, OP);
                Writer.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error saving settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        /// <summary>
        /// Чтение настроек из файла
        /// </summary>
        /// <returns>True, если все получилось; False, если возникла ошибка</returns>
        public bool LoadSetfromFile()
        {
            Options OP = new Options();
            try
            {
                if (File.Exists(SettingsPath))
                {
                    XmlSerializer XmlSer = new XmlSerializer(typeof(Options));
                    FileStream Set = new FileStream(SettingsPath, FileMode.Open);
                    OP = (Options)XmlSer.Deserialize(Set);
                    Set.Close();
                    this.AutoStart = OP.AutoStart;
                    this.SSL = OP.SSL;
                    this.ServerAdress = OP.ServerAdress;
                    this.ServerPort = OP.ServerPort;
                    this.TestInterval = OP.TestInterval;
                    this.TCPInterval = OP.TCPInterval;
                    this.TCPTriesLimit = OP.TCPTriesLimit;
                    this.ICMPInterval = OP.ICMPInterval;
                    this.ICMPTriesLimit = OP.ICMPTriesLimit;
                    this.MailFrom = OP.MailFrom;
                    this.MailUsername = OP.MailUsername;
                    this.MailPassword = OP.MailPassword;
                    this.SMTPServer = OP.SMTPServer;
                    this.SMTPPort = OP.SMTPPort;
                    this.ServerAdminEmail = OP.ServerAdminEmail;
                    this.WebAdminEmail = OP.WebAdminEmail;
                    OP.PointDateTime.CopyTo(this.PointDateTime, 0);
                    OP.PointAdress.CopyTo(this.PointAdress, 0);
                    return true;
                }
                else return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error loading settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        /// <summary>
        /// Устанавливает адрес проверяемого сервера
        /// </summary>
        /// <param name="Adress">Адрес проверяемого сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetServerAdress(string Adress) 
        {
            if (Regex.IsMatch(Adress, @"^(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])$") || Regex.IsMatch(Adress, @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,6}$"))
            {
                this.ServerAdress = Adress.ToLower();
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Возвращает адрес проверяемого сервера
        /// </summary>
        /// <returns>Адрес проверяемого сервера</returns>
        public string GetServerAdress ()
        {
            return this.ServerAdress;
        }
        /// <summary>
        /// Устанавливает порт проверяемого сервера
        /// </summary>
        /// <param name="Port">Порт проверяемого сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetServerPort(string Port) 
        {
            if (Regex.IsMatch(Port, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(Port)) > 0 && (Convert.ToInt32(Port)) < 65536)
                    {
                        this.ServerPort = Convert.ToInt32(Port);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает порт проверяемого сервера
        /// </summary>
        /// <returns>Порт проверяемого сервера</returns>
        public int GetServerPort()
        {
            return this.ServerPort;
        }
        /// <summary>
        /// Устанавливает основной интервал опроса сервера
        /// </summary>
        /// <param name="Interval">Основной интервал опроса сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetTestInterval(string Interval) 
        {
            if (Regex.IsMatch(Interval, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(Interval)) > 0 && (Convert.ToInt32(Interval)) < (Math.Abs(Int32.MaxValue / 60000 - 1)))
                    {
                        this.TestInterval = Convert.ToInt32(Interval);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает основной интервал опроса сервера
        /// </summary>
        /// <returns>Основной интервал опроса сервера</returns>
        public int GetTestInterval()
        {
            return this.TestInterval;
        }
        /// <summary>
        /// Устанавливает интервал опроса сервера по TCP
        /// </summary>
        /// <param name="Interval">Интервал опроса сервера по TCP</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetTCPInterval(string Interval)
        {
            if (Regex.IsMatch(Interval, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(Interval)) > 0 && (Convert.ToInt32(Interval)) < (Math.Abs(Int32.MaxValue / 60000 - 1)))
                    {
                        this.TCPInterval = Convert.ToInt32(Interval);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает интервал опроса сервера по TCP
        /// </summary>
        /// <returns>Интервал опроса сервера по TCP</returns>
        public int GetTCPInterval()
        {
            return this.TCPInterval;
        }
        /// <summary>
        /// Устанавливает количество попыток отправки TCP запроса
        /// </summary>
        /// <param name="TriesLimit">Количество попыток отправки TCP запроса</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetTCPTriesLimit(string TriesLimit)
        {
            if (Regex.IsMatch(TriesLimit, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(TriesLimit)) > 0 && (Convert.ToInt32(TriesLimit)) < (Math.Abs(Int32.MaxValue / 60000 - 1)))
                    {
                        this.TCPTriesLimit = Convert.ToInt32(TriesLimit);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает количество попыток отправки TCP запроса
        /// </summary>
        /// <returns>Количество попыток отправки TCP запроса</returns>
        public int GetTCPTriesLimit()
        {
            return this.TCPTriesLimit;
        }
        /// <summary>
        /// Устанавливает интервал опроса сервера по ICMP
        /// </summary>
        /// <param name="Interval">Интервал опроса сервера по ICMP</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetICMPInterval(string Interval) 
        {
            if (Regex.IsMatch(Interval, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(Interval)) > 0 && (Convert.ToInt32(Interval)) < (Math.Abs(Int32.MaxValue / 60000 - 1)))
                    {
                        this.ICMPInterval = Convert.ToInt32(Interval);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает интервал опроса сервера по ICMP
        /// </summary>
        /// <returns>Интервал опроса сервера по ICMP</returns>
        public int GetICMPInterval()
        {
            return this.ICMPInterval;
        }
        /// <summary>
        /// Устанавливает количество попыток отправки ICMP запроса
        /// </summary>
        /// <param name="TriesLimit">Количество попыток отправки ICMP запроса</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetICMPTriesLimit(string TriesLimit) 
        {
            if (Regex.IsMatch(TriesLimit, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(TriesLimit)) > 0 && (Convert.ToInt32(TriesLimit)) < (Math.Abs(Int32.MaxValue / 60000 - 1)))
                    {
                        this.ICMPTriesLimit = Convert.ToInt32(TriesLimit);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает количество попыток отправки ICMP запроса
        /// </summary>
        /// <returns>Количество попыток отправки ICMP запроса</returns>
        public int GetICMPTriesLimit()
        {
            return this.ICMPTriesLimit;
        }
        /// <summary>
        /// Устанавливает e-mail отправителя уведомлений
        /// </summary>
        /// <param name="From">E-mail отправителя уведомлений</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetMailFrom(string From) 
        {
            if (Regex.IsMatch(From, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"))
            {
                this.MailFrom = From.ToLower();
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Возвращает e-mail отправителя уведомлений
        /// </summary>
        /// <returns>E-mail отправителя уведомлений</returns>
        public string GetMailFrom()
        {
            return this.MailFrom;
        }
        /// <summary>
        /// Устанавливает имя пользователя для соединения с SMTP сервером
        /// </summary>
        /// <param name="Username">Имя пользователя для соединения с SMTP сервером</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetMailUsername(string Username) 
        {
            if (Regex.IsMatch(Username, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*") || Regex.IsMatch(Username, @"^([a-zA-Z])[a-zA-Z_-]*[\w_-]*[\S]$|^([a-zA-Z])[0-9_-]*[\S]$|^[a-zA-Z]*[\S]$"))
            {
                this.MailUsername = Username;
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Возвращает имя пользователя для соединения с SMTP сервером
        /// </summary>
        /// <returns>Имя пользователя для соединения с SMTP сервером</returns>
        public string GetMailUsername()
        {
            return this.MailUsername;
        }
        /// <summary>
        /// Устанавливает пароль для соединения с SMTP сервером
        /// </summary>
        /// <param name="Password">Пароль для соединения с SMTP сервером</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetMailPassword(string Password) 
        {
            //if (Regex.IsMatch(Password, @"^([a-zA-Z])[a-zA-Z_-]*[\w_-]*[\S]$|^([a-zA-Z])[0-9_-]*[\S]$|^[a-zA-Z]*[\S]$"))
            //{
                this.MailPassword = Password;
                return true;
           // }
           // else return false;
        }
        /// <summary>
        /// Возвращает пароль для соединения с SMTP сервером
        /// </summary>
        /// <returns>Пароль для соединения с SMTP сервером</returns>
        public string GetMailPassword()
        {
            return this.MailPassword;
        }
        /// <summary>
        /// Устанавливает адрес SMTP сервера
        /// </summary>
        /// <param name="Server">Адрес SMTP сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetSMTPServer(string Server) 
        {
            if (Regex.IsMatch(Server, @"^(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])$") || Regex.IsMatch(Server, @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,6}$") || Regex.IsMatch(Server, "localhost"))
            {
                this.SMTPServer = Server.ToLower();
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Возвращает адрес SMTP сервера
        /// </summary>
        /// <returns>Адрес SMTP сервера</returns>
        public string GetSMTPServer ()
        {
            return this.SMTPServer;
        }
        /// <summary>
        /// Устанавливает порт SMTP сервера
        /// </summary>
        /// <param name="Port">Порт SMTP сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetSMTPPort(string Port) 
        {
            if (Regex.IsMatch(Port, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(Port)) > 0 && (Convert.ToInt32(Port)) < 65536)
                    {
                        this.SMTPPort = Convert.ToInt32(Port);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Возвращает порт SMTP сервера
        /// </summary>
        /// <returns>Порт SMTP сервера</returns>
        public int GetSMTPPort()
        {
            return this.SMTPPort;
        }
        /// <summary>
        /// Устанавливает e-mail администратора сервера
        /// </summary>
        /// <param name="Email">E-mail администратора сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetServerAdminEmail(string Email) 
        {
            if (Regex.IsMatch(Email, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"))
            {
                this.ServerAdminEmail = Email.ToLower();
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Возвращает e-mail администратора сервера
        /// </summary>
        /// <returns>E-mail администратора сервера</returns>
        public string GetServerAdminEmail()
        {
            return this.ServerAdminEmail;
        }
        /// <summary>
        /// Устанавливает e-mail администратора Web-сервера
        /// </summary>
        /// <param name="Email">E-mail администратора Web-сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetWebAdminEmail(string Email) 
        {
            if (Regex.IsMatch(Email, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"))
            {
                this.WebAdminEmail = Email.ToLower();
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Возвращает e-mail администратора Web-сервера
        /// </summary>
        /// <returns>E-mail администратора Web-сервера</returns>
        public string GetWebAdminEmail()
        {
            return this.WebAdminEmail;
        }
        /// <summary>
        /// Устанавливает основной интервал опроса сервера в минутах
        /// </summary>
        /// <param name="Interval">Основной интервал опроса сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetTestIntervalFromNumericValue(decimal value)
        {
            this.TestInterval = Convert.ToInt32(value);
            return true;
        }
        /// <summary>
        /// Устанавливает основной интервал опроса сервера по TCP в минутах
        /// </summary>
        /// <param name="Interval">Основной интервал опроса сервера</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetTCPIntervalFromNumericValue(decimal value)
        {
            this.TCPInterval = Convert.ToInt32(value);
            return true;
        }
        /// <summary>
        /// Устанавливает количество попыток отправки TCP запроса
        /// </summary>
        /// <param name="TriesLimit">Количество попыток отправки TCP запроса</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetTCPTriesLimitFromNumericValue(decimal value)
        {
            this.TCPTriesLimit = Convert.ToInt32(value);
            return true;
        }
        /// <summary>
        /// Устанавливает количество попыток отправки ICMP запроса
        /// </summary>
        /// <param name="TriesLimit">Количество попыток отправки TCP запроса</param>
        /// <returns>True, если формат аргумента верный; False, если формат аргумента неверный</returns>
        public bool SetICMPTriesLimitFromNumericValue(decimal value)
        {
            this.ICMPTriesLimit = Convert.ToInt32(value);
            return true;
        }

        #endregion
    }
}
