// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MSModel;

namespace vexclass
{
    public partial class ClassificationForm : Form
    {
        public ClassificationForm()
        {
            this.MemorySafetyModel = new MSModel.MemorySafetyModel();

            InitializeComponent();

            this.transitiveProfileTreeView.MemorySafetyModel = this.MemorySafetyModel;
            this.transitiveProfileTreeView.ProfileSelected += new ProfileTreeNodeSelectedDelegate(transitiveProfileTreeView_ProfileSelected);

            this.profilePropertyGrid1.ProfilePropertyValueChanged += new ProfilePropertyValueChangedDelegate(profilePropertyGrid1_ProfilePropertyValueChanged);
            
            this.CurrentVulnerability = new Vulnerability();
        }


        void profilePropertyGrid1_ProfilePropertyValueChanged(string name)
        {
            if (name == "Base" || 
                name == "Content (src)" || 
                name == "Content (dest)" || 
                name == "Displacement" || 
                name == "Extent" || 
                name == "Name" ||
                name == "MSRC" ||
                name == "CVE")
            {
                this.transitiveProfileTreeView.RefreshProfile();
            }
        }

        void transitiveProfileTreeView_ProfileSelected(Profile profile)
        {
            if (profile is Vulnerability)
            {
                this.profilePropertyGrid1.Profile = profile;
                this.profilePropertyGrid1.Enabled = true;
                this.profileTagControl1.Profile = null;
                this.profileTagControl1.Enabled = false;
            }
            else
            {
                this.profilePropertyGrid1.Profile = profile;
                this.profileTagControl1.Profile = profile;

                if (profile == null)
                {
                    this.profilePropertyGrid1.Enabled = false;
                    this.profileTagControl1.Enabled = false;
                }
                else
                {
                    this.profilePropertyGrid1.Enabled = true;
                    this.profileTagControl1.Enabled = true;
                }
            }
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void saveToFileButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                FileName = "vulndesc.xml",
                DefaultExt = "xml",
                Title = "Save vulnerability description to file..."
            };

            DialogResult result = sfd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.CurrentVulnerability.SaveToFile(sfd.FileName);
            }
        }
        

        public Vulnerability CurrentVulnerability
        {
            get
            {
                return this.currentVulnerability;
            }
            set
            {
                this.currentVulnerability = value;
                this.transitiveProfileTreeView.VulnerabilityProfile = value;
            }
        }
        private Vulnerability currentVulnerability;

        public MemorySafetyModel MemorySafetyModel { get; private set; }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}