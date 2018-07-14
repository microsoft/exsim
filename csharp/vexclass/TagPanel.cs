// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows;
using System.Xml;
using System.Reflection;
using System.Runtime.InteropServices;

using MSModel;

namespace vexclass
{
    public delegate void TreeChangeEventHandler(object sender, EventArgs e);
    public delegate void OutputChangeEventHandler(object sender, EventArgs e);

    public class FeatureDetail
    {
        public FeatureDetail(MSModel.Feature featureProfile)
        {
            this.FeatureProfile = featureProfile;
        }

        public MSModel.Feature FeatureProfile { get; private set; }
        public string DisplayName
        {
            get
            {
                return this.FeatureProfile.Name;
            }
        }

        public string Keyword
        {
            get
            {
                return this.FeatureProfile.Symbol;
            }
        }

        public string Symbol
        {
            get
            {
                return this.FeatureProfile.FullSymbol;
            }
        }

        public Guid Guid
        {
            get
            {
                return this.FeatureProfile.Guid;
            }
        }
    }

    public interface FTreeItem
    {
        string HelpText { get; set; }
        void Add(FTreeItem f);
        event TreeChangeEventHandler TreeChange;
        FTreeItem TParent { get; set; }
        List<FTreeItem> Children { get; /*set;*/ }
        bool Selected { get; set; }
        void OnTreeChange(EventArgs e);
        TextBlock HelpWin { get; set; }
        void RecurseFeature(string s, StackPanel c);
        FeaturePanel Panel { get; set; }
        FeatureDetail Detail { get; set; }
        FeatureDetail[] Details { get; }
        void LoadGuid(Guid guid);
        void Reset();
        TabItem TabItem { get; set; }

    }

    public class Property : ComboBox, FTreeItem
    {
        public event TreeChangeEventHandler TreeChange;

        public Property(FeaturePanel panel, FeatureDetail detail)
        {
            this.Panel = panel;
            this.Detail = detail;
            this.Children = new List<FTreeItem>();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (SelectedIndex != -1)
            {
                TParent.Selected = true;
                OnTreeChange(EventArgs.Empty);
            }

            base.OnSelectionChanged(e);
        }

        public virtual void OnTreeChange(EventArgs e)
        {
            if (TreeChange != null)
            {
                TreeChange(this, e);
            }
        }

        public void Add(FTreeItem f)
        {
            this.Children.Add(f);
        }

        public void LoadGuid(Guid guid)
        {
            foreach (Feature f in this.Children)
            {
                if (f.Detail.Guid == guid)
                {
                    foreach (Label item in this.Items)
                    {
                        if ((string)item.Content == f.Detail.DisplayName)
                            this.SelectedValue = item;
                    }
                    break;
                }
            }
        }

        public void Reset()
        {
            SelectedIndex = -1;
        }

        public void RecurseFeature(string s, StackPanel p)
        {
            StackPanel ns = new StackPanel();
            p.Children.Add(ns);
            Label text = new Label();
            text.Content = Detail.DisplayName;
            ns.Children.Add(text);
            ns.Children.Add(this);
            foreach (FTreeItem f in this.Children)
            {
                Label l = new Label();
                l.Content = f.Detail.DisplayName;
                this.Items.Add(l);
            }
        }

        public FeatureDetail[] Details
        {
            get
            {
                List<FeatureDetail> l = new List<FeatureDetail>();
                if (SelectedIndex != -1)
                {
                    string f = (string)((Label)Items[SelectedIndex]).Content;
                    foreach (Feature c in this.Children)
                    {
                        if (f == c.Detail.DisplayName)
                            l.Add(c.Detail);
                    }
                }
                return l.ToArray();
            }
            set { }
        }

        public bool Selected
        {
            get { return false; }
            set { }
        }

        public FTreeItem TParent { get; set; }
        public List<FTreeItem> Children { get; set; }
        public TextBlock HelpWin { get; set; }
        public string HelpText { get; set; }
        public FeaturePanel Panel { get; set; }
        public FeatureDetail Detail { get; set; }
        public TabItem TabItem { get; set; }
    }

    public class Feature : ToggleButton, FTreeItem
    {
        public event TreeChangeEventHandler TreeChange;

        public Feature(FeaturePanel panel, FeatureDetail detail)
        {
            this.Children = new List<FTreeItem>();
            this.Content = detail.DisplayName;
            this.Panel = panel;
            this.Detail = detail;
            this.TParent = null;
            this.m_selected = false;

            this.Loaded += new RoutedEventHandler(Feature_Loaded);
        }

        void Feature_Loaded(object sender, RoutedEventArgs e)
        {
            this.OffBackground = this.Background;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (HelpWin != null)
            {
                HelpWin.Text = HelpText;
            }

        }

        public virtual void OnTreeChange(EventArgs e)
        {
            if (TreeChange != null)
            {
                TreeChange(this, e);
            }
        }

        const int SnapSize = 20;
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged && sizeInfo.NewSize.Width % SnapSize != 0)
                Width = Math.Round(sizeInfo.NewSize.Width / SnapSize) * SnapSize + SnapSize;
            base.OnRenderSizeChanged(sizeInfo);
        }

        protected override void OnClick()
        {
            base.OnClick();
            Selected = (bool)this.IsChecked;
        }

        public void PropagateTc(object sender, EventArgs e)
        {
            if (TParent != null)
            {
                TParent.OnTreeChange(e);
            }
        }

        public void Add(FTreeItem f)
        {
            this.Children.Add(f);
            f.TParent = this;
            f.TreeChange += PropagateTc;
        }

        public void LoadGuid(Guid g)
        {
            if (g.CompareTo(Detail.Guid) == 0)
                Selected = true;
            else
            {
                foreach (FTreeItem c in this.Children)
                    c.LoadGuid(g);
            }
        }

        public void Reset()
        {
            this.Selected = false;
            foreach (FTreeItem c in this.Children)
                c.Reset();
        }

        public void RecurseFeature(string tab, StackPanel p)
        {
            StackPanel hs = new StackPanel();
            p.Children.Add(hs);
            hs.Orientation = Orientation.Horizontal;
            hs.Children.Add(this);

            if (this.Children.Count > 0)
            {
                StackPanel c = new StackPanel();
                hs.Children.Add(c);
                foreach (FTreeItem cf in this.Children)
                {
                    cf.HelpWin = this.HelpWin;
                    cf.RecurseFeature(tab + "   ", c);
                }
            }
        }

        public FeaturePanel Panel { get; set; }
        public FeatureDetail Detail { get; set; }
        public TabItem TabItem { get; set; }

        public string Keyword { get; set; }

        public FTreeItem TParent { get; set; }
        public string HelpText { get; set; }
        public TextBlock HelpWin { get; set; }
        public List<FTreeItem> Children { get; set; }

        public FeatureDetail[] Details
        {
            get
            {
                List<FeatureDetail> l = new List<FeatureDetail>();
                if (Selected)
                {
                    l.Add(Detail);
                    foreach (FTreeItem c in this.Children)
                        l.AddRange(c.Details);

                }
                return l.ToArray();
            }
        }

        private static System.Windows.Media.Brush OnBackground =
                        new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(190, 230, 253)
                            );

        private System.Windows.Media.Brush OffBackground;

        public bool Selected
        {
            get
            {
                return m_selected;
            }
            set
            {
                bool update = (m_selected != value);
                m_selected = value;
                if (TParent != null && value != false)
                    TParent.Selected = m_selected;

                if (!value)
                {
                    foreach (FTreeItem c in this.Children)
                    {
                        c.Selected = false;
                    }
                }
                IsChecked = value;

                if (update)
                {
                    OnTreeChange(EventArgs.Empty);

                    if (this.Selected)
                    {
                        this.Background = OnBackground;

                        if (this.TabItem != null)
                        {
                            this.TabItem.Background = OnBackground;
                        }

                        //
                        // Associate this feature profile with the active profile for the panel.
                        //

                        if ((this.Panel.Resetting == false) && 
                            (this.Panel.Profile != null) &&
                            (this.Panel.Profile.Features.Contains(this.Detail.FeatureProfile) == false))
                        {
                            this.Panel.Profile.Features.Add(this.Detail.FeatureProfile);
                        }
                    }
                    else
                    {
                        this.Background = OffBackground;

                        if (this.TabItem != null)
                        {
                            this.TabItem.Background = OffBackground;
                        }

                        //
                        // Disassociate this feature profile with the active profile for the panel.
                        //

                        if ((this.Panel.Resetting == false) &&
                            (this.Panel.Profile != null) &&
                            (this.Panel.Profile.Features.Contains(this.Detail.FeatureProfile) == true))
                        {
                            this.Panel.Profile.Features.Remove(this.Detail.FeatureProfile);
                        }
                    }
                }
            }
        }
        private bool m_selected;
    }

    [ComImport]
    [Guid("CB5BDC81-93C1-11CF-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IObjectSafety
    {
        [PreserveSig]
        int GetInterfaceSafetyOptions(ref Guid riid, out int pdwSupportedOptions, out int pdwEnabledOptions);

        [PreserveSig]
        int SetInterfaceSafetyOptions(ref Guid riid, int dwOptionSetMask, int dwEnabledOptions);
    }

    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IFeaturePanel
    {
        //[DispId(FeaturePanel.DISPID_VALUE)]
        string Value { get; set; }

        //[DispId(FeaturePanel.DISPID_ENABLED)]
        bool Enabled { get; set; }

    }

    [ComImport]
    [Guid("63FCEB03-2740-423E-89F5-0323386E508C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyNotifySink
    {
        int OnChanged(int dispId);

        [PreserveSig]
        int OnRequestEdit(int dispId);
    }
    public delegate int PropertyNotifySinkHandler(int dispId);

    [Guid("12BC9A13-3282-40EB-B7F7-FC3242639A09")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(IPropertyNotifySink))]
    public class FeaturePanel : TabControl, IFeaturePanel
    {
        public event OutputChangeEventHandler OutputChange;


        protected virtual void OnOutputChange(EventArgs e)
        {
            if (OutputChange != null)
                OutputChange(this, e);
        }
        public List<Feature> BaseFeatures;
        public TextBlock HelpWin;
        // public event PropertyNotifySinkHandler OnChanged;
        // public event PropertyNotifySinkHandler OnRequestEdit;

        public FeaturePanel()
        {
            BaseFeatures = new List<Feature>();
        }

        public FTreeItem FindFeatureProfileTreeItem(MSModel.Feature featureProfile, FTreeItem node = null)
        {
            if (node == null)
            {
                foreach (Feature f in BaseFeatures)
                {
                    FTreeItem childNode = FindFeatureProfileTreeItem(featureProfile, f);

                    if (childNode != null)
                    {
                        return childNode;
                    }
                }
            }
            else
            {
                if (node.Detail.FeatureProfile.Guid == featureProfile.Guid ||
                    node.Detail.FeatureProfile.FullSymbol == featureProfile.FullSymbol)
                {
                    return node;
                }

                foreach (FTreeItem fti in node.Children)
                {
                    FTreeItem childNode = FindFeatureProfileTreeItem(featureProfile, fti);

                    if (childNode != null)
                    {
                        return childNode;
                    }
                }
            }

            return null;
        }

        public void Reset()
        {
            this.Resetting = true;

            foreach (Feature f in this.BaseFeatures)
            {
                f.Reset();
            }

            this.Resetting = false;
        }

        public bool Resetting { get; private set; }

        string MakeCSV(string[] sa)
        {
            string sret = "";
            foreach (string s in sa)
            {
                if (sret != "")
                    sret += ",";
                sret += s;
            }
            return sret;
        }

        public string Value
        {
            get
            {
                List<FeatureDetail> details = new List<FeatureDetail>();
                foreach (Feature f in BaseFeatures)
                {
                    details.AddRange(f.Details);
                }
                List<string> guids = new List<string>();
                foreach (FeatureDetail f in details)
                    guids.Add(f.Guid.ToString());
                return MakeCSV(guids.ToArray());
            }
            set
            {
                Reset();
                string[] v = value.Split(',');
                foreach (string g in v)
                    LoadGuid(new Guid(g));
            }
        }
        public bool IsSet(string sguid)
        {
            Guid guid = new Guid(sguid);
            foreach (FeatureDetail f in Details)
            {
                if (guid.CompareTo(f.Guid) == 0)
                    return true;
            }
            return false;
        }

        public FeatureDetail[] Details
        {
            get
            {
                List<FeatureDetail> details = new List<FeatureDetail>();
                foreach (Feature f in BaseFeatures)
                {
                    details.AddRange(f.Details);
                }
                return details.ToArray();
            }
            set
            {
                Reset();
                foreach (FeatureDetail fd in value)
                {
                    LoadGuid(fd.Guid);
                }
            }
        }

        public bool Enabled
        {
            get { return true; }
            set { }
        }

        public Profile Profile
        {
            get
            {
                return this.profile;
            }
            set
            {
                if (this.profile != null)
                {
                    Reset();
                }

                this.profile = value;

                //
                // Update the selected state for items in the tree that correspond to a feature profile.
                //

                if (this.profile != null)
                {
                    foreach (Profile p in this.profile.Features.ToList())
                    {
                        if (p is MSModel.Feature == false)
                        {
                            continue;
                        }

                        MSModel.Feature featureProfile = p as MSModel.Feature;

                        FTreeItem item = FindFeatureProfileTreeItem(featureProfile);

                        if (item == null)
                        {
                            //this.profile.Features.Remove(p);  // this line should be removed -- we don't want to remove features that aren't in the model under normal circumstances.
                            continue;
                        }

                        item.Selected = true;
                    }
                }
            }
        }
        private Profile profile;

        struct InitializationWorkItem
        {
            public FTreeItem ParentTreeItem { get; set; }
            public Profile Profile { get; set; }
        }

        public void InitializeFromModel(IModel model)
        {
            List<FTreeItem> rootFeatures = new List<FTreeItem>();

            Queue<InitializationWorkItem> workItemQueue = new Queue<InitializationWorkItem>(
                from Profile profile in model.ProfilesRaw
                where profile.Parent.Parent == null
                select new InitializationWorkItem() { Profile = profile }
                );

            while (workItemQueue.Count > 0)
            {
                InitializationWorkItem workItem = workItemQueue.Dequeue();
                MSModel.Feature profile = workItem.Profile as MSModel.Feature;

                FeatureDetail fd = new FeatureDetail(profile);
                FTreeItem treeItem;

                if (profile.IsPropertyFeature)
                {
                    treeItem = new Property(this, fd);
                }
                else
                {
                    treeItem = new Feature(this, fd);
                }

                if (workItem.ParentTreeItem != null)
                {
                    workItem.ParentTreeItem.Add(treeItem);
                }
                else
                {
                    rootFeatures.Add(treeItem);
                }

                foreach (Profile childProfile in profile.Children)
                {
                    workItemQueue.Enqueue(new InitializationWorkItem() { ParentTreeItem = treeItem, Profile = childProfile });
                }
            }

            foreach (Feature f in rootFeatures)
            {
                StackPanel p = new StackPanel();
                f.TabItem = new TabItem();
                f.TabItem.Header = f.Content;
                f.TabItem.Content = p;
                f.HelpWin = this.HelpWin;
                this.Items.Add(f.TabItem);
                f.RecurseFeature("", p);
                f.TreeChange += OnTreeChange;

                this.BaseFeatures.Add(f);
            }

            this.SelectedItem = rootFeatures.First().TabItem;
        }

        public void OnTreeChange(object arg, EventArgs e)
        {
            if (OutputChange != null)
                OutputChange(this, EventArgs.Empty);
        }

        public void LoadGuid(Guid guid)
        {
            foreach (Feature f in BaseFeatures)
            {
                f.LoadGuid(guid);
            }
        }
    }
}
