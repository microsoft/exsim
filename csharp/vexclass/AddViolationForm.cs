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
    public partial class AddViolationForm : Form
    {
        internal class ViolationComboBoxItem
        {
            public Violation Violation { get; set; }

            public override string ToString()
            {
                return String.Format("{0}  [{1}]", this.Violation.Symbol, this.Violation.Description);
            }
        }

        public AddViolationForm(MemorySafetyModel model)
        {
            this.Model = model;

            InitializeComponent();

            this.Load += new EventHandler(AddViolationForm_Load);

            this.violationProfileDropDown.SelectedValueChanged += new EventHandler(violationProfileDropDown_SelectedValueChanged);
        }

        void violationProfileDropDown_SelectedValueChanged(object sender, EventArgs e)
        {
            ViolationComboBoxItem selectedValue = this.violationProfileDropDown.SelectedItem as ViolationComboBoxItem;

            if (selectedValue != null)
            {
                this.SelectedViolation = selectedValue.Violation.CloneViolation();

                this.violationPropertyGrid.Profile = this.SelectedViolation;
            }
        }

        void AddViolationForm_Load(object sender, EventArgs e)
        {
            foreach (Violation v in this.Model.ViolationModel.Violations.OrderBy(x => x.Symbol))
            {
                if (this.AllowedMethods != null && this.AllowedMethods.Contains(v.Method) != true)
                {
                    continue;
                }

                this.violationProfileDropDown.Items.Add(new ViolationComboBoxItem() { Violation = v });
            }
        }

        public IEnumerable<MemoryAccessMethod> AllowedMethods { get; set; }
        public Violation SelectedViolation { get; set; }
        private MemorySafetyModel Model { get; set; }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.SelectedViolation = null;
            this.Close();
        }
    }
}
