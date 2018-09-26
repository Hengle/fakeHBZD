using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Net;
using System.IO;

namespace Xinli
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static event EventHandler<MessageArgs> PartEvent;

        public MainWindow()
        {
            InitializeComponent();
            PartEvent += OnStep;
            if (tool.GetConfigValue("ISPCode") != "")
            {
                password.Text = tool.GetConfigValue("ISPCode");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                main.username = username.Text;
                main.password = password.Text;
                main.remoteip = RouterIP.Text;
                main.connect();
            }
            catch(Exception ex)
            {
                log(ex.Message);
            }
        }
        
        //private string get_phi()
        //{
        //    String url = "http://192.168.1.1";
        //    string location = null;
        //    //斐讯
        //    CookieContainer cc = new CookieContainer();
        //    GET(url + "/cgi-bin/luci");
        //    Post(url + "/cgi-bin/luci/admin/login", "action_mode=apply&action_url=http%3A%2F%2F192.168.2.1%2Fcgi-bin%2Fluci&remember=on&username=admin&password=YWRtaW4%3D", ref cc, ref location);
        //    string page = GET(url + location + "more_sysstatus");
        //    return page.Substring(page.IndexOf("<span id=\"wan_ip\">") + 18, page.IndexOf("<span>子网掩码：</span>") - page.IndexOf("<span id=\"wan_ip\">") - 67);
        //}

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                RouterIP.Text = tool.getIP();
            }
            catch (Exception ex1)
            {
                log(ex1.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                password.Text = main.getISPCode();
                tool.SetConfigValue("ISPCode", password.Text);
            }
            catch(MyException ex)
            {
                log(ex.Message);
            }
        }

        public class MessageArgs : EventArgs
        {
            public MessageArgs(string message)
            {
                this.TxtMessage = message;
            }

            public string TxtMessage { get; set; }
        }

        public static void inFunction(String LogMsg)
        {
            MessageArgs messageArg = new MessageArgs(LogMsg);
            if (PartEvent != null)
            {
                PartEvent(new object(), messageArg);
            }
        }

        public void OnStep(Object sender, MessageArgs messageArg)
        {
            new Thread(() =>
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    log(messageArg.TxtMessage);
                }));
            }).Start();
        }

        void log(string str)
        {
            logText.AppendText(str + "\r\n");
        }
    }
}
