using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;



namespace XenDesktopNotification
{

    /// <summary>
    /// Framework for running application as a tray app.
    /// </summary>
    /// <remarks>
    /// Tray app code adapted from "Creating Applications with NotifyIcon in Windows Forms", Jessica Fosler,
    /// http://windowsclient.net/articles/notifyiconapplications.aspx
    /// </remarks>
    public class CustomApplicationContext : ApplicationContext
    {
        private static readonly string IconFileName = "XD.ico";
        public CustomApplicationContext()
        {
            ShowPNAForm();
            //InitializeContext();
        }

        private PNA detailsForm;
        private void ShowPNAForm()
        {
            if (detailsForm == null)
            {
                detailsForm = new PNA();
                //detailsForm.Closed += detailsForm_Closed; // avoid reshowing a disposed form
                detailsForm.Show();
            }
            else { detailsForm.Activate(); }
        }

        private System.ComponentModel.IContainer components;	// a list of components to dispose when the context is disposed
        private NotifyIcon notifyIcon;				            // the icon that sits in the system tray

        private void InitializeContext()
        {
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components)
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = new Icon(IconFileName),
                //Text = DefaultTooltip,
                Visible = true
            };
           
        }

    }


}