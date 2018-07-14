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

using MSModel;

namespace vexclass
{
    public delegate void ProfilePropertyValueChangedDelegate(string name);

    public partial class ProfilePropertyGrid : UserControl
    {
        public event ProfilePropertyValueChangedDelegate ProfilePropertyValueChanged;

        public ProfilePropertyGrid()
        {
            InitializeComponent();

            this.Load += new EventHandler(ProfilePropertyGrid_Load);

            this.propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(propertyGrid_PropertyValueChanged);
        }

        void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (this.ProfilePropertyValueChanged != null)
            {
                this.ProfilePropertyValueChanged(e.ChangedItem.Label);
            }
        }

        void ProfilePropertyGrid_Load(object sender, EventArgs e)
        {
            //
            // Load the properties associated with the profile.
            //

            this.propertyGrid.SelectedObject = this.Profile;
        }

        public Profile Profile
        {
            get { return this.propertyGrid.SelectedObject as Profile; }
            set
            {
                this.propertyGrid.SelectedObject = value;
                this.propertyGrid.Refresh();
            }
        }
    }
}
