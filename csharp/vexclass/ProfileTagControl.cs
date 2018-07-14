// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Controls;

using MSModel;

namespace vexclass
{
    public partial class ProfileTagControl : System.Windows.Forms.UserControl
    {
        public ProfileTagControl()
        {
            InitializeComponent();


            ScrollViewer scrollViewer = new ScrollViewer()
            {
                CanContentScroll = true
            };

            this.TagPanel = new FeaturePanel();

            this.TagPanel.InitializeFromModel(this.FeatureModel);

            scrollViewer.Content = this.TagPanel;

            this.elementHost.Child = scrollViewer;
        }

        public virtual FeatureModel FeatureModel
        {
            get
            {
                if (this.featureModel == null)
                {
                    MemorySafetyModel model = new MemorySafetyModel();
                    this.featureModel = model.VulnerabilityFeatureModel;
                }

                return this.featureModel;
            }
        }
        private FeatureModel featureModel;

        public Profile Profile
        {
            get
            {
                return this.TagPanel.Profile;
            }
            set
            {
                this.TagPanel.Profile = value;
            }
        }

        private FeaturePanel TagPanel { get; set; }
    }

    public class ExploitProfileTagControl : ProfileTagControl
    {
        public ExploitProfileTagControl()
        {
            MemorySafetyModel model = new MemorySafetyModel();

        }

        public override FeatureModel FeatureModel
        {
            get
            {
                if (this.featureModel == null)
                {
                    MemorySafetyModel model = new MemorySafetyModel();
                    this.featureModel = model.ExploitFeatureModel;
                }

                return this.featureModel;
            }
        }
        private FeatureModel featureModel;
    }
}
