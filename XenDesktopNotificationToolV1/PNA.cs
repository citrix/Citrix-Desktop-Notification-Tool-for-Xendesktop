using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Xml;
using System.Timers;

namespace XenDesktopNotification
{
    public partial class PNA : Form
    {
        Thread t;
        string[] ddcs;
        System.Timers.Timer timer;
        public PNA()
        {
            InitializeComponent();
        }
      
        void ThreadMethod()
        {
             ddcs = controllers.Text.Split(',');

            foreach (string ddc in ddcs)
            {
                List<Alert> alertlist = getLastTwoHourAlerts(ddc);
                showAlerts(alertlist);
            }

            timer = new System.Timers.Timer(60000);
            timer.Elapsed += HandleTimer;
            timer.Start();
        }

        private  void HandleTimer(object sender,ElapsedEventArgs e)
        {
            foreach (string ddc in ddcs)
            {
                List<Alert> alertlist = getNewAlerts(ddc);
                showAlerts(alertlist);
            }
        }


        private List<Alert> getNewAlerts(string ddc)
        {
            string endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string endTime = DateTime.UtcNow.ToString("HH:mm:ss");
            string startTime = DateTime.UtcNow.AddMinutes(-1).ToString("HH:mm:ss");
            string odatastart = endDate + "T" + startTime;
            string odataend = endDate + "T" + endTime;
           string Odataurl = "http://" + ddc + "/Citrix/Monitor/OData/v3/data/Notifications()?";// Notifications()?$filter=NotificationRule/IsAggregate eq false and NotificationPriority eq 1 and LifecycleState eq 1
            string param = "$filter=NotificationRule/IsAggregate eq false  and LifecycleState eq 1 and FirstTimeNotificationTriggered gt datetime'" + odatastart + "'";// +" or NotificationStateChangeDate gt datetime'"+ odatastart+"'";
            string Odataquery = Odataurl + param;
            string Odatadetails = "http://" + ddc + "/Citrix/Monitor/OData/v3/methods/GetNotificationDetails()?";

            XmlDocument odataresult = doOdataquery(Odataquery);
            XmlNamespaceManager ns = new XmlNamespaceManager(odataresult.NameTable);
            ns.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            XmlNodeList entry = odataresult.GetElementsByTagName("entry");
            //XmlNodeList alertlist = content[0].ChildNodes;
            List<Alert> alertobjlist = new List<Alert>();
            foreach (XmlNode node in entry)
            {
                XmlNode Id = node.SelectSingleNode(".//d:Id", ns);
                string notificationId = Id.InnerText;
                string odataquery = "notificationId=" + "'" + notificationId + "'" + "&sortField='Date'&sortOrder='Desc'&offset=0&count=50";
                XmlDocument result = doOdataquery(Odatadetails + odataquery);
                // XmlNodeList parentNode = result.GetElementsByTagName("GetNotificationDetails");
                XmlNode scope = result.SelectSingleNode(".//d:ScopeName", ns);
                XmlNode source = result.SelectSingleNode(".//d:TargetName", ns);
                XmlNode category = result.SelectSingleNode(".//d:Category", ns);
                XmlNode condition = result.SelectSingleNode(".//d:Threshold", ns);
                XmlNode state = result.SelectSingleNode(".//d:NotificationState", ns);
                Alert obj = new Alert();
                obj.scope = scope.InnerText;
                obj.source = source.InnerText;
                obj.condition = category.InnerText + " >= " + condition.InnerText;

                switch (state.InnerText)
                {
                    case "1": obj.severity = "Warning"; break;
                    case "2": obj.severity = "Critical"; break;
                    case "3": continue;
                    case "4": continue;
                    default: continue;
                }
                alertobjlist.Add(obj);

            }
            return alertobjlist;
        }


        private void showAlerts(List<Alert> alertlist)
        {
            if (alertlist != null)
            {
                foreach (Alert alert in alertlist)
                {
                    ShowNotification("XD Notifications", "There is a " + alert.severity + " Alert" + "\nSource: " + alert.source + "\nCondition : " + alert.condition + " ");
                    Thread.Sleep(10000);
                    Console.Write(alert);
                }
            }
        }


        private List<Alert> getLastTwoHourAlerts(string ddcip)
        {
            string endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string endTime = DateTime.UtcNow.ToString("HH:mm:ss");
            string startTime = DateTime.UtcNow.AddHours(-2).ToString("HH:mm:ss");
            string odatastart = endDate + "T" + startTime;
            string odataend = endDate + "T" + endTime;
            string Odataurl = "http://" + ddcip + "/Citrix/Monitor/OData/v3/methods/GetNotificationSummary()?";// notificationTypeFilterString=''&notificationSourceTypeFilter=''&sourceId=''&categoryFilterString=''&startDate=datetime'2015-10-28T13:07:00'&endDate=datetime'2015-10-28T15:07:00'&sortField='Date'&sortOrder='Desc'&offset=0&count=50";
            string param = "notificationTypeFilterString=" + "'" + "'&" + "source=" + "'" + "'&" + "categoryFilterString=" + "'" + "'&" + "startDate=datetime" + "'" + odatastart + "'&" + "endDate=datetime" + "'" + odataend + "'&" + "sortField='Date'&sortOrder='Asc'&offset=0&count=50";
            string Odataquery = Odataurl + param;
            try
            {
                XmlDocument odataresult = doOdataquery(Odataquery);
                XmlNamespaceManager ns = new XmlNamespaceManager(odataresult.NameTable);
                ns.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
                ns.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
                XmlNodeList entry = odataresult.GetElementsByTagName("entry");
                Console.WriteLine(entry.Count);
                //XmlNodeList alertlist = content[0].ChildNodes;
                List<Alert> alertobjlist = new List<Alert>();

                foreach (XmlNode node in entry)
                {

                    Alert obj = new Alert();
                    XmlNode scope = node.SelectSingleNode(".//d:Scope", ns);
                    XmlNode source = node.SelectSingleNode(".//d:SourceName", ns);
                    XmlNode category = node.SelectSingleNode(".//d:Category", ns);
                    XmlNode condition = node.SelectSingleNode(".//d:Threshold", ns);
                    XmlNode state = node.SelectSingleNode(".//d:NotificationState", ns);
                    XmlNode pname = node.SelectSingleNode(".//d:PolicyName", ns);
                    XmlNode id = node.SelectSingleNode(".//d:NotificationId", ns);
                    obj.scope = scope.InnerText;
                    obj.source = source.InnerText;
                    obj.condition = category.InnerText + " >= " + condition.InnerText;
                    Console.WriteLine(state.InnerText);
                    Console.WriteLine(pname.InnerText);
                    Console.WriteLine(id.InnerText);
                    switch (state.InnerText)
                    {
                        case "1": obj.severity = "Warning"; break;
                        case "2": obj.severity = "Critical"; break;
                        case "3": continue;
                        case "4": continue;
                        default: continue;
                    }
                    alertobjlist.Add(obj);
                    Console.Write(scope.Value);

                }
                //Console.Write(odataresult);
                return alertobjlist;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Data);
                return null;
            }
        }

        private System.Xml.XmlDocument doOdataquery(string Odataurl)
        {
            try
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                wc.Credentials = new NetworkCredential(userName.Text + "@" + domain.Text, pwd.Text);
                string result = wc.DownloadString(Odataurl);

                XmlDocument Odataxml = new XmlDocument();
                Odataxml.LoadXml(result);
                return Odataxml;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException);
                //return e.InnerException;
                return null;

            }

        }
        private void ShowNotification(String Title = "Director PNA", string Message = "PNA Message", int StayTime = 80000)
        {
            try
            {

                notifyIcon1.BalloonTipTitle = Title;
                notifyIcon1.BalloonTipText = Message;
                notifyIcon1.ShowBalloonTip(StayTime);
                //notifyIcon1.DoubleClick += new System.EventHandler(notifyIcon1_MouseDoubleClick);
                //notifyIcon1.ContextMenu = new ContextMenu();
               // notifyIcon1.ContextMenu.MenuItems.Add(new MenuItem("Option 1", new EventHandler(optionHandler)));

            }
            catch (Exception E2)
            {
                MessageBox.Show(E2.ToString());
            }
        }

       
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you Sure to Stop XenDesktop Alerts?", "Stop Alerts", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                notifyIcon1.Visible = false;
                t.Abort();

                this.Close();
            }
        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
               t = new Thread(ThreadMethod);
               t.Start();
            }
            catch (Exception E1)
            {
                MessageBox.Show(E1.ToString());

            }
            MessageBox.Show("Connected to Director Service at: " + director.Text);
            this.Visible = false;

        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }


        private void PNA_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try {
                System.Diagnostics.Process.Start("http://" + director.Text + "/director");
            }catch(Exception ex)
            {
                MessageBox.Show("Could not open the Url " + "http://" + director.Text + "/director");
            }
        }
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Will Close Your Application 
            notifyIcon1.Dispose();
            timer.Enabled = false;
            Application.Exit();
        }

        private void userName_TextChanged(object sender, EventArgs e)
        {

        }

        private void pwd_TextChanged(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }
    }
    public class Alert
    {
        public string scope { get; set; }
        public string source { get; set; }
        public string condition { get; set; }
        public string sitename { get; set; }
        public string severity { get; set; }

    }
}
