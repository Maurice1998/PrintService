using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace HttpPrint
{ 
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        private void serviceInstaller2_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            ServiceController sc = new ServiceController("PrintService");
            if (sc.Status.Equals(ServiceControllerStatus.Stopped))
            {
                sc.Start();
            }
        }
    }
}
