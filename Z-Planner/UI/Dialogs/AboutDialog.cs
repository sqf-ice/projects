﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZZero.ZPlanner.Settings;
using ZZero.ZPlanner.ZConfiguration;

namespace ZZero.ZPlanner.UI.Dialogs
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();
            LoadData();
#if ZSANDBOX
            this.Text = "About Z-solver / Licensing";
            textBox1.Text = "About Z-solver";
            //tbVersion.Text = "Z-solver";
            tbTM.Text = "Z-zero® and Z-solver® are registered trademarks of Z-zero, LP.";
#endif
        }

        private void panelLogo_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rect = panelLogo.ClientRectangle;
            rect.Width--;
            rect.Height--;
            //e.Graphics.DrawRectangle(Pens.Black, rect);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void LoadData()
        {
            tbVersion.Text = AboutData.TheAbout.sVersion;
            tbBuild.Text = AboutData.TheAbout.sBuild + Environment.NewLine + AboutData.TheAbout.sDate;

            tbFSVersion.Text = AboutData.TheAbout.sFSVersion;
            tbFSBuild.Text = AboutData.TheAbout.sFSBuild + Environment.NewLine + AboutData.TheAbout.sFSDate;

            tbProgram.Text = AboutData.TheAbout.sProgramLocation;

            //license
            string feature = License.TheLicense.layers;//feature;

#if ZSANDBOX
            tbFeature.Text = "Z-solver";
            textBox10.Visible = false;
            tbLybraryDate.Visible = false;
#else
            tbFeature.Text = "Z-planner";

            DateTime dmlSyncDate = ZPlannerManager.GetDMLSyncDate();
            tbLybraryDate.Text = (dmlSyncDate != DateTime.MinValue) ? dmlSyncDate.ToString("MMMM d, yyyy") : string.Empty;
#endif

            string type = License.TheLicense.type;
            string count = "uncounted";
            if (License.TheLicense.count != "uncounted")
            {
                count = "counted";
            }

            tbStatus.Text = count + " " + type;

            string expiration = "never";
            if (License.TheLicense.expire != "permanent")
            {
                    DateTime ExpirationDate = DateTime.Now.AddDays(Int32.Parse(License.TheLicense.expire));
                    expiration = ExpirationDate.ToString("MMMM d, yyyy");
            }
            tbExpiration.Text = expiration;

            string licSearchPath = License.TheLicense.path;// Environment.GetEnvironmentVariable("ZZERO_LICENSE_FILE");
            if (!String.IsNullOrEmpty(licSearchPath))
            {
                tbLicensePath.Text = licSearchPath;
            }
        }

        private void btBrowse_Click(object sender, EventArgs e)
        {
            string initial_path = tbEditedLicensePath.Text;

            FolderBrowserDialog fbd = new FolderBrowserDialog();

            if (initial_path != String.Empty) fbd.SelectedPath = initial_path;

            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                tbEditedLicensePath.Text = fbd.SelectedPath;
            }

        }

    }
}
