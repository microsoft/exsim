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
using System.Reflection;

using MSModel;

namespace vexclass
{
    enum TransitiveProfileTreeViewImageIndex : int
    {
        None = 0,
        Unknown = 1,
        Yes = 2,
        No = 3,
        Max = 4
    }

    public delegate void NewViolationDelegate(Profile profile);
    public delegate void ProfileTreeNodeSelectedDelegate(Profile profile);

    public partial class TransitiveProfileTreeView : UserControl
    {
        public event NewViolationDelegate NewProfile;
        public event ProfileTreeNodeSelectedDelegate ProfileSelected;

        public TransitiveProfileTreeView()
        {
            InitializeComponent();

            this.treeView.ImageList = new ImageList();
            this.treeView.ImageList.Images.Add(
                Image.FromStream(
                    typeof(TransitiveProfileTreeView).Assembly.GetManifestResourceStream(@"vexclass.Resources.blank.png")),
                Color.White);
            this.treeView.ImageList.Images.Add(
                Image.FromStream(
                    typeof(TransitiveProfileTreeView).Assembly.GetManifestResourceStream(@"vexclass.Resources.questionmark.png")),
                Color.White);
            this.treeView.ImageList.Images.Add(
                Image.FromStream(
                    typeof(TransitiveProfileTreeView).Assembly.GetManifestResourceStream(@"vexclass.Resources.greencheck.png")),
                Color.White);
            this.treeView.ImageList.Images.Add(
                Image.FromStream(
                    typeof(TransitiveProfileTreeView).Assembly.GetManifestResourceStream(@"vexclass.Resources.redx.png")),
                Color.White);

            this.treeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(treeView_NodeMouseClick);
            this.treeView.MouseUp += new MouseEventHandler(treeView_MouseUp);
            this.treeView.KeyPress += new KeyPressEventHandler(treeView_KeyPress);
            this.treeView.AfterSelect += new TreeViewEventHandler(treeView_AfterSelect);
            
            //
            // Initialize the root vulnerability profile.
            //

            this.VulnerabilityProfile = new Vulnerability();
        }

        void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this.ProfileSelected != null)
            {
                if (e.Node is ConcreteViolationTreeNode)
                {
                    this.ProfileSelected((e.Node as ConcreteViolationTreeNode).Violation);
                }
                else if (e.Node is FlawTreeNode)
                {
                    this.ProfileSelected((e.Node as FlawTreeNode).Flaw);
                }
                else if (e.Node is VulnerabilityTreeNode)
                {
                    this.ProfileSelected((e.Node as VulnerabilityTreeNode).Vulnerability);
                }
                else
                {
                    this.ProfileSelected(null);
                }
            }
        }

        void treeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.treeView.SelectedNode is FlawTreeNode)
            {
                return;
            }
            else if (this.treeView.SelectedNode is GroupViolationTreeNode)
            {
                GroupViolationTreeNode groupTreeNode = this.treeView.SelectedNode as GroupViolationTreeNode;

                if (e.KeyChar == 'n' || e.KeyChar == 'N')
                {
                    groupTreeNode.NextState(TransitiveProfileTreeViewImageIndex.No);
                    e.Handled = true;
                }
                else if (e.KeyChar == 'y' || e.KeyChar == 'Y')
                {
                    groupTreeNode.NextState(TransitiveProfileTreeViewImageIndex.Yes);
                    e.Handled = true;
                }
                else if (e.KeyChar == 'u' || e.KeyChar == 'U' || e.KeyChar == '?')
                {
                    groupTreeNode.NextState(TransitiveProfileTreeViewImageIndex.Unknown);
                    e.Handled = true;
                }
            }
            else if (this.treeView.SelectedNode is ProfileTreeNode)
            {
                ProfileTreeNode treeNode = this.treeView.SelectedNode as ProfileTreeNode;

                if (e.KeyChar == 'n' || e.KeyChar == 'N')
                {
                    treeNode.NextState(TransitiveProfileTreeViewImageIndex.No);
                    e.Handled = true;
                }
                else if (e.KeyChar == 'u' || e.KeyChar == 'U' || e.KeyChar == '?')
                {
                    treeNode.NextState(TransitiveProfileTreeViewImageIndex.Unknown);
                    e.Handled = true;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete || keyData == Keys.Back)
            {
                if (this.treeView.SelectedNode is ConcreteViolationTreeNode)
                {
                    ConcreteViolationTreeNode violationTreeNode = this.treeView.SelectedNode as ConcreteViolationTreeNode;

                    if (this.treeView.SelectedNode.Parent is GroupViolationTreeNode)
                    {
                        GroupViolationTreeNode groupTreeNode = violationTreeNode.Parent as GroupViolationTreeNode;

                        groupTreeNode.RemoveViolationNode(violationTreeNode);

                        return true;
                    }
                    else if (this.treeView.SelectedNode.Parent is FlawTreeNode)
                    {
                        FlawTreeNode flawTreeNode = violationTreeNode.Parent as FlawTreeNode;

                        flawTreeNode.Flaw.TransitiveViolations.Remove(violationTreeNode.Violation);
                        flawTreeNode.Nodes.Remove(violationTreeNode);

                        return true;
                    }
                    else
                    {
                        this.treeView.Nodes.Remove(violationTreeNode);
                    }
                }
                else if (this.treeView.SelectedNode is GroupViolationTreeNode)
                {
                    GroupViolationTreeNode groupTreeNode = this.treeView.SelectedNode as GroupViolationTreeNode;

                    groupTreeNode.NextState(TransitiveProfileTreeViewImageIndex.Unknown);
                }
                else if (this.treeView.SelectedNode is FlawTreeNode)
                {
                    FlawTreeNode flawTreeNode = this.treeView.SelectedNode as FlawTreeNode;

                    if (this.treeView.SelectedNode.Parent is FlawTreeNode)
                    {
                        FlawTreeNode parentFlawTreeNode = flawTreeNode.Parent as FlawTreeNode;

                        parentFlawTreeNode.Flaw.TransitiveFlaws.Remove(flawTreeNode.Flaw);
                        parentFlawTreeNode.Nodes.Remove(flawTreeNode);

                        return true;
                    }
                    else if (this.treeView.SelectedNode.Parent is VulnerabilityTreeNode)
                    {
                        VulnerabilityTreeNode parentVulnerabilityTreeNode = flawTreeNode.Parent as VulnerabilityTreeNode;

                        parentVulnerabilityTreeNode.Vulnerability.RootFlaws.Remove(flawTreeNode.Flaw);
                        parentVulnerabilityTreeNode.Nodes.Remove(flawTreeNode);
                    }
                    else
                    {
                        this.treeView.Nodes.Remove(flawTreeNode);
                    }
                }
            }

            return false;
        }

        void treeView_MouseUp(object sender, MouseEventArgs e)
        {
            ProfileTreeNode node = this.treeView.GetNodeAt(e.Location) as ProfileTreeNode;

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (node != null)
                {
                    this.treeView.SelectedNode = node;
                    node.ShowContextMenu(e.Location);
                }
                else
                {
                    this.TreeViewContextMenu.Show(this.treeView, e.Location);
                }
            }
        }

        void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ProfileTreeNode node = e.Node as ProfileTreeNode;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                node.OnMouseClick(e);
            }
        }

        public void RefreshProfile()
        {
            ProfileTreeNode treeNode = this.treeView.SelectedNode as ProfileTreeNode;

            if (treeNode == null)
            {
                return;
            }

            treeNode.Refresh();
        }

        public void NotifyNewProfile(Profile profile)
        {
            if (this.NewProfile != null)
            {
                this.NewProfile(profile);
            }
        }

        public Vulnerability VulnerabilityProfile
        {
            get
            {
                return this.vulnerabilityProfile;
            }
            set
            {
                this.vulnerabilityProfile = value;

                this.treeView.Nodes.Clear();

                TreeNode node =
                    new VulnerabilityTreeNode(
                        this.vulnerabilityProfile
                        );

                this.treeView.Nodes.Add(node);
                this.treeView.SelectedNode = node;

                if (this.vulnerabilityProfile != null)
                {
                    foreach (Flaw flaw in this.vulnerabilityProfile.RootFlaws)
                    {
                        node.Nodes.AddFlawNode(new FlawTreeNode(flaw));
                    }

                    foreach (Violation violation in this.vulnerabilityProfile.RootViolations)
                    {
                        node.Nodes.AddViolationNode(new ConcreteViolationTreeNode(violation));
                    }
                }
            }
        }
        private Vulnerability vulnerabilityProfile;

        public IEnumerable<Profile> RootProfiles
        {
            get
            {
                foreach (TreeNode rootNode in this.treeView.Nodes)
                {
                    if (rootNode is ConcreteViolationTreeNode)
                    {
                        ConcreteViolationTreeNode concreteNode = rootNode as ConcreteViolationTreeNode;
                        yield return concreteNode.Violation;
                    }
                    else if (rootNode is FlawTreeNode)
                    {
                        FlawTreeNode flawNode = rootNode as FlawTreeNode;
                        yield return flawNode.Flaw;
                    }
                }
            }
        }

        public MemorySafetyModel MemorySafetyModel
        {
            get { return this.memorySafetyModel; }
            set
            {
                this.memorySafetyModel = value;

                this.Simulation = Simulation.GetAllTechniquesSimulation(memorySafetyModel);
            }
        }
        private MemorySafetyModel memorySafetyModel;

        private ContextMenuStrip TreeViewContextMenu { get; set; }

        public Simulation Simulation { get; set; }
    }

    enum ViolationGroupTreeNodeState
    {
        Yes,
        No,
        Unknown
    }

    internal class ProfileTreeNode : TreeNode
    {
        public ProfileTreeNode(
            TransitiveProfileTreeViewImageIndex initialImage
            )
        {
            this.ImageIndex = (int)initialImage;
            this.SelectedImageIndex = this.ImageIndex;
        }

        public Violation ParentViolation
        {
            get
            {
                if (this.Parent != null)
                {
                    if (this.Parent is ConcreteViolationTreeNode)
                    {
                        ConcreteViolationTreeNode node = this.Parent as ConcreteViolationTreeNode;

                        return node.Violation;
                    }
                    else if (this.Parent is GroupViolationTreeNode)
                    {
                        return (this.Parent as GroupViolationTreeNode).ParentViolation;
                    }
                }

                return null;
            }
        }

        public Flaw ParentFlaw
        {
            get
            {
                if (this.Parent != null)
                {
                    if (this.Parent is FlawTreeNode)
                    {
                        FlawTreeNode node = this.Parent as FlawTreeNode;

                        return node.Flaw;
                    }
                    else if (this.Parent is GroupViolationTreeNode)
                    {
                        return (this.Parent as GroupViolationTreeNode).ParentFlaw;
                    }
                }

                return null;
            }
        }

        public Vulnerability ParentVulnerability
        {
            get
            {
                if (this.Parent != null)
                {
                    if (this.Parent is VulnerabilityTreeNode)
                    {
                        VulnerabilityTreeNode node = this.Parent as VulnerabilityTreeNode;

                        return node.Vulnerability;
                    }
                    else if (this.Parent is GroupViolationTreeNode)
                    {
                        return (this.Parent as GroupViolationTreeNode).ParentVulnerability;
                    }
                }

                return null;
            }
        }

        public TransitiveProfileTreeView TreeViewDerived
        {
            get
            {
                return (TransitiveProfileTreeView)this.TreeView.Parent;
            }
        }

        public TransitiveProfileTreeViewImageIndex State
        {
            get
            {
                return (TransitiveProfileTreeViewImageIndex)this.ImageIndex;
            }
        }

        protected virtual ContextMenuStrip RefreshContextMenuStrip()
        {
            return null;
        }

        public void OnMouseClick(TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                TreeViewHitTestInfo hitInfo = this.TreeView.HitTest(e.Location);

                if (hitInfo.Location == TreeViewHitTestLocations.Image)
                {
                    NextState();
                }
            }
        }

        public void ShowContextMenu(Point location)
        {
            ContextMenuStrip strip = this.RefreshContextMenuStrip();

            if (strip != null)
            {
                strip.Show(this.TreeView, location);
            }
        }

        public void NextState(TransitiveProfileTreeViewImageIndex? specificState = null, bool showWarning = true)
        {
            TransitiveProfileTreeViewImageIndex nextStateIndex;

            if (this.ImageIndex == (int)TransitiveProfileTreeViewImageIndex.None)
            {
                return;
            }

            if (specificState != null)
            {
                nextStateIndex = specificState.Value;
            }
            else
            {
                if (this.ImageIndex == (int)TransitiveProfileTreeViewImageIndex.Unknown)
                {
                    nextStateIndex = TransitiveProfileTreeViewImageIndex.No;
                }
                else
                {
                    nextStateIndex = TransitiveProfileTreeViewImageIndex.Unknown;
                }
            }

            if (OnStateChanging(nextStateIndex, showWarning) == false)
            {
                return;
            }

            this.ImageIndex = (int)nextStateIndex;
            this.SelectedImageIndex = this.ImageIndex;

            OnStateChange(nextStateIndex);

            Refresh();
        }

        protected virtual bool OnStateChanging(TransitiveProfileTreeViewImageIndex newImageState, bool showWarning)
        {
            return true;
        }

        protected virtual void OnStateChange(TransitiveProfileTreeViewImageIndex newImageState)
        {
        }

        public virtual void Refresh()
        {
            UpdateText();
        }

        public virtual void OnNodeAddedToTree()
        {
            Refresh();
        }

        public virtual void UpdateText()
        {
        }

        private void AddFeatureToFlaw(Flaw flaw, Guid featureGuid)
        {
            foreach (MSModel.Feature p in this.TreeViewDerived.MemorySafetyModel.VulnerabilityFeatureModel.GetProfileAndParentsByGuid(featureGuid))
            {
                flaw.Features.Add(p);
            }
        }

        protected void PopulateCommonFlawMenuItems(ToolStripMenuItem strip)
        {
            strip.DropDownItems.Add("Unspecified flaw", null, (o, e2) =>
            {
                FlawTreeNode treeNode = new FlawTreeNode();
                this.Nodes.AddFlawNode(treeNode);
            });

            //
            // Buffer overruns
            //

            ToolStripMenuItem boStrip = new ToolStripMenuItem("Buffer Overrun");

            boStrip.DropDownItems.Add("Stack", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Stack buffer overrun"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{830865FC-DE1C-4B33-B55C-52B24D3CFECA}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Stack memory write",
                        BaseRegionType = MemoryRegionType.Stack,
                        BaseState = MemoryAccessParameterState.Fixed,
                        Direction = MemoryAccessDirection.Forward,
                        AddressingMode = MemoryAddressingMode.Relative
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            boStrip.DropDownItems.Add("Heap", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Heap buffer overrun"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{DAD6D45C-2BAA-4DAF-B32E-A175ED40413F}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Heap memory write",
                        BaseRegionType = MemoryRegionType.Heap,
                        BaseState = MemoryAccessParameterState.Fixed,
                        Direction = MemoryAccessDirection.Forward,
                        AddressingMode = MemoryAddressingMode.Relative
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            boStrip.DropDownItems.Add("Heap (truncated allocation size)", null, (o, e2) =>
            {
                FlawTreeNode tflawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Truncated allocation size"
                });

                AddFeatureToFlaw(tflawNode.Flaw, new Guid("{94A5C868-C581-4968-8AF4-C6E4EB83F1EC}"));
                AddFeatureToFlaw(tflawNode.Flaw, new Guid("{B386C473-3346-41F4-A32D-D2F4B88318E8}"));

                tflawNode.Flaw.TransitiveExecuteListIsComplete = true;
                tflawNode.Flaw.TransitiveWriteListIsComplete = true;
                tflawNode.Flaw.TransitiveReadListIsComplete = true;

                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Heap buffer overrun"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{DAD6D45C-2BAA-4DAF-B32E-A175ED40413F}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Heap memory write",
                        BaseRegionType = MemoryRegionType.Heap,
                        BaseState = MemoryAccessParameterState.Fixed,
                        Direction = MemoryAccessDirection.Forward,
                        AddressingMode = MemoryAddressingMode.Relative
                    });

                tflawNode.Nodes.AddFlawNode(flawNode);
                this.Nodes.AddFlawNode(tflawNode);
            });

            strip.DropDownItems.Add(boStrip);

            //
            // Indexed write
            //


            ToolStripMenuItem idxStrip = new ToolStripMenuItem("Indexed Write");

            idxStrip.DropDownItems.Add("Stack", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Stack buffer out-of-bounds write"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{0B450267-AC89-4D07-809B-85867BB33CC1}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Stack indexed memory write",
                        BaseRegionType = MemoryRegionType.Stack,
                        BaseState = MemoryAccessParameterState.Fixed,
                        DisplacementState = MemoryAccessParameterState.Controlled,
                        Direction = MemoryAccessDirection.Forward,
                        AddressingMode = MemoryAddressingMode.Relative
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            idxStrip.DropDownItems.Add("Heap", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Heap buffer out-of-bounds write"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{DB0B1FDD-7D79-4E5F-9B86-07032E862E7A}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Heap indexed memory write",
                        BaseRegionType = MemoryRegionType.Heap,
                        BaseState = MemoryAccessParameterState.Fixed,
                        DisplacementState = MemoryAccessParameterState.Controlled,
                        Direction = MemoryAccessDirection.Forward,
                        AddressingMode = MemoryAddressingMode.Relative
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            strip.DropDownItems.Add(idxStrip);

            //
            // Memory management
            //

            ToolStripMenuItem memStrip = new ToolStripMenuItem("Memory Management");

            strip.DropDownItems.Add(memStrip);

            memStrip.DropDownItems.Add("Double free", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Double free"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{C23024EA-3127-45E6-9755-7FD84011289C}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            ToolStripMenuItem huafStrip = new ToolStripMenuItem("Use after free (Heap)");

            memStrip.DropDownItems.Add(huafStrip);

            huafStrip.DropDownItems.Add("Leading to unknown", null, (o, e2) =>
            {
                FlawTreeNode pflawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Premature free"
                });

                AddFeatureToFlaw(pflawNode.Flaw, new Guid("{6F333B8F-FC34-45DA-A76A-FA63F912701B}"));

                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Use after free"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{C5980D52-9286-496D-AD18-733CAC1D6E30}"));
                
                pflawNode.Flaw.TransitiveExecuteListIsComplete = true;
                pflawNode.Flaw.TransitiveWriteListIsComplete = true;
                pflawNode.Flaw.TransitiveReadListIsComplete = true;

                pflawNode.Nodes.AddFlawNode(flawNode);
                this.Nodes.AddFlawNode(pflawNode);
            });

            huafStrip.DropDownItems.Add("Leading to read AV", null, (o, e2) =>
            {
                FlawTreeNode pflawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Premature free"
                });

                AddFeatureToFlaw(pflawNode.Flaw, new Guid("{6F333B8F-FC34-45DA-A76A-FA63F912701B}"));

                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Use after free"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{C5980D52-9286-496D-AD18-733CAC1D6E30}"));

                pflawNode.Flaw.TransitiveExecuteListIsComplete = true;
                pflawNode.Flaw.TransitiveWriteListIsComplete = true;
                pflawNode.Flaw.TransitiveReadListIsComplete = true;
                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Read)
                    {
                        Name = "Uninitialized memory read",
                        BaseRegionType = MemoryRegionType.Heap,
                        BaseState = MemoryAccessParameterState.Fixed,
                        ContentSrcState = MemoryAccessParameterState.Uninitialized
                    });

                pflawNode.Nodes.AddFlawNode(flawNode);
                this.Nodes.AddFlawNode(pflawNode);
            });

            huafStrip.DropDownItems.Add("Leading to write AV", null, (o, e2) =>
            {
                FlawTreeNode pflawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Premature free"
                });

                AddFeatureToFlaw(pflawNode.Flaw, new Guid("{6F333B8F-FC34-45DA-A76A-FA63F912701B}"));

                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Use after free"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{C5980D52-9286-496D-AD18-733CAC1D6E30}"));

                pflawNode.Flaw.TransitiveExecuteListIsComplete = true;
                pflawNode.Flaw.TransitiveWriteListIsComplete = true;
                pflawNode.Flaw.TransitiveReadListIsComplete = true;
                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Uninitialized memory write",
                        BaseRegionType = MemoryRegionType.Heap,
                        BaseState = MemoryAccessParameterState.Fixed,
                        ContentDstState = MemoryAccessParameterState.Uninitialized
                    });

                pflawNode.Nodes.AddFlawNode(flawNode);
                this.Nodes.AddFlawNode(pflawNode);
            });

            huafStrip.DropDownItems.Add("Leading to C++ virtual method call", null, (o, e2) =>
            {
                FlawTreeNode pflawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Premature free"
                });

                AddFeatureToFlaw(pflawNode.Flaw, new Guid("{6F333B8F-FC34-45DA-A76A-FA63F912701B}"));

                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Use after free"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{C5980D52-9286-496D-AD18-733CAC1D6E30}"));

                pflawNode.Flaw.TransitiveExecuteListIsComplete = true;
                pflawNode.Flaw.TransitiveWriteListIsComplete = true;
                pflawNode.Flaw.TransitiveReadListIsComplete = true;
                flawNode.Flaw.TransitiveExecuteListIsComplete = true;

                Violation vtableReadViolation;
                Violation vtableDerefViolation;

                flawNode.Flaw.TransitiveViolations.Add(
                    vtableReadViolation = new Violation(MemoryAccessMethod.Read)
                    {
                        Name = "Read C++ virtual table pointer",
                        BaseRegionType = MemoryRegionType.Heap,
                        BaseState = MemoryAccessParameterState.Fixed,
                        ContentSrcState = MemoryAccessParameterState.Uninitialized,
                        ExtentState = MemoryAccessParameterState.Fixed,
                        DisplacementState = MemoryAccessParameterState.Fixed,
                        ContentDataType = MemoryContentDataType.CppVirtualTablePointer,
                        ContentContainerDataType = MemoryContentDataType.CppObject
                    });

                vtableReadViolation.TransitiveViolations.Add(
                    new TransitiveViolation()
                    {
                        Violation = vtableDerefViolation = new Violation(MemoryAccessMethod.Read)
                        {
                            Name = "Read C++ virtual table element",
                            BaseState = MemoryAccessParameterState.Uninitialized,
                            ContentSrcState = MemoryAccessParameterState.Unknown,
                            ExtentState = MemoryAccessParameterState.Fixed,
                            DisplacementState = MemoryAccessParameterState.Fixed,
                            ContentDataType = MemoryContentDataType.FunctionPointer,
                            ContentContainerDataType = MemoryContentDataType.CppVirtualTablePointer
                        }
                    });

                vtableDerefViolation.TransitiveViolations.Add(
                    new TransitiveViolation()
                    {
                        Violation = new Violation(MemoryAccessMethod.Execute)
                        {
                            Name = "Call virtual method",
                            BaseState = MemoryAccessParameterState.Unknown,
                            ContentSrcState = MemoryAccessParameterState.Unknown,
                            ExtentState = MemoryAccessParameterState.Nonexistant,
                            DisplacementState = MemoryAccessParameterState.Nonexistant,
                            ContentDataType = MemoryContentDataType.Code,
                            ControlTransferMethod = ControlTransferMethod.VirtualMethodCall
                        }
                    });

                pflawNode.Nodes.AddFlawNode(flawNode);
                this.Nodes.AddFlawNode(pflawNode);
            });

            memStrip.DropDownItems.Add(huafStrip);

            //
            // Uninitialized use
            //

            strip.DropDownItems.Add("Uninitialized use", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Uninitialized use"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{4CCE988C-CB61-48FD-BAEC-AF6C354303EF}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            //
            // NULL dereference
            //

            ToolStripMenuItem nullStrip = new ToolStripMenuItem("NULL dereference");

            strip.DropDownItems.Add(nullStrip);

            nullStrip.DropDownItems.Add("Read near null", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "NULL dereference (read)"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{D8149CDF-DBF9-46C2-A5E2-032EBAAC5448}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;
                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Read)
                    {
                        Name = "Read near NULL",
                        BaseRegionType = MemoryRegionType.Null,
                        BaseState = MemoryAccessParameterState.Fixed,
                        DisplacementState = MemoryAccessParameterState.Fixed
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            nullStrip.DropDownItems.Add("Write near null", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "NULL dereference (write)"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{F954417C-C775-4ED4-9D21-C6DF367AC59C}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;
                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Write)
                    {
                        Name = "Write near NULL",
                        BaseRegionType = MemoryRegionType.Null,
                        BaseState = MemoryAccessParameterState.Fixed,
                        DisplacementState = MemoryAccessParameterState.Fixed
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            nullStrip.DropDownItems.Add("Execute near null", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "NULL dereference (execute)"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{73C5AEA5-541C-4998-A2F9-3144968DF77B}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;
                flawNode.Flaw.TransitiveViolations.Add(
                    new Violation(MemoryAccessMethod.Execute)
                    {
                        Name = "Execute near NULL",
                        BaseRegionType = MemoryRegionType.Null,
                        BaseState = MemoryAccessParameterState.Fixed,
                        DisplacementState = MemoryAccessParameterState.Nonexistant,
                        ExtentState = MemoryAccessParameterState.Nonexistant
                    });

                this.Nodes.AddFlawNode(flawNode);
            });

            //
            // Other
            //

            ToolStripMenuItem otherStrip = new ToolStripMenuItem("Other");

            otherStrip.DropDownItems.Add("Type Confusion", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Type Confusion"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{A81B278F-2B0A-49F6-9CB0-6A993E257C52}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("XSS", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "XSS"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{9BDF059C-277A-4CEE-87FF-5034BCADFB54}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("DLL Planting", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "DLL planting"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{F6EDF95F-05E1-4791-8594-AD5EC6F39151}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("Command Execution", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Command Execution"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{541231C2-82D1-4A62-8501-C610F7D6A8C3}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("Logic Flaw", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Logic Flaw"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{5BC79E01-8A5A-4D22-8870-053778AE356C}"));

                flawNode.Flaw.TransitiveExecuteListIsComplete = true;
                flawNode.Flaw.TransitiveWriteListIsComplete = true;
                flawNode.Flaw.TransitiveReadListIsComplete = true;

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("Kernel double fetch", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Kernel double fetch"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{2DE230B8-E4C8-4844-A800-DFFFAD0BE8FF}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("Win32k user mode callback", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Win32k user mode callback"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{2D533BEF-254F-4CAB-8BC7-0780212B6ED8}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("Other", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Other"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{CF68DABF-45BF-486A-99FB-51832C50EFA1}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            otherStrip.DropDownItems.Add("Unknown", null, (o, e2) =>
            {
                FlawTreeNode flawNode = new FlawTreeNode(new Flaw()
                {
                    Name = "Unknown"
                });

                AddFeatureToFlaw(flawNode.Flaw, new Guid("{EB4C4511-290E-4DB2-BD65-62AA6A5B2DDE}"));

                this.Nodes.AddFlawNode(flawNode);
            });

            strip.DropDownItems.Add(otherStrip);
        }
    }

    internal class VulnerabilityTreeNode : ProfileTreeNode
    {
        public VulnerabilityTreeNode(Vulnerability vulnerability)
            : base(TransitiveProfileTreeViewImageIndex.None)
        {
            if (vulnerability == null)
            {
                this.Vulnerability = new Vulnerability();
            }
            else
            {
                this.Vulnerability = vulnerability;
            }

            this.UpdateText();
        }

        protected override ContextMenuStrip RefreshContextMenuStrip()
        {
            ContextMenuStrip strip = new ContextMenuStrip();

            //
            // Add flaw shortcuts...
            //

            ToolStripMenuItem flawStrip = new ToolStripMenuItem("Add root cause flaw...");

            PopulateCommonFlawMenuItems(flawStrip);

            strip.Items.Add(flawStrip);

            //
            // Add violation shortcuts...
            //

            ToolStripMenuItem violationStrip = new ToolStripMenuItem("Add root cause violation...");

            violationStrip.DropDownItems.Add("read AV", null, (o, e2) =>
            {
                ConcreteViolationTreeNode treeNode =
                    new ConcreteViolationTreeNode(
                        new Violation(MemoryAccessMethod.Read));

                this.Nodes.AddViolationNode(treeNode);
            });

            violationStrip.DropDownItems.Add("write AV", null, (o, e2) =>
            {
                ConcreteViolationTreeNode treeNode =
                    new ConcreteViolationTreeNode(
                        new Violation(MemoryAccessMethod.Write));

                this.Nodes.AddViolationNode(treeNode);
            });

            violationStrip.DropDownItems.Add("execute AV", null, (o, e2) =>
            {
                ConcreteViolationTreeNode treeNode =
                    new ConcreteViolationTreeNode(
                        new Violation(MemoryAccessMethod.Execute));

                this.Nodes.AddViolationNode(treeNode);
            });

            strip.Items.Add(violationStrip);

            return strip;
        }

        public override void UpdateText()
        {
            StringBuilder builder = new StringBuilder();

            if (this.Vulnerability.CVE != null)
            {
                this.Text = this.Vulnerability.CVE;
            }
            else if (this.Vulnerability.MSRC != null)
            {
                this.Text = String.Format("MSRC {0}", this.Vulnerability.MSRC);
            }
            else
            {
                this.Text = "Unspecified vulnerability";
            }
        }

        public Vulnerability Vulnerability { get; private set; }
    }

    internal class FlawTreeNode : ProfileTreeNode
    {
        public FlawTreeNode(Flaw flaw = null)
            : base(TransitiveProfileTreeViewImageIndex.None)
        {
            if (flaw != null)
            {
                this.Flaw = flaw;
            }
            else
            {
                this.Flaw = new Flaw();
            }

            UpdateText();
        }

        protected override ContextMenuStrip RefreshContextMenuStrip()
        {
            ContextMenuStrip strip = new ContextMenuStrip();

            ToolStripMenuItem flawStrip = new ToolStripMenuItem("Add child flaw...");

            PopulateCommonFlawMenuItems(flawStrip);

            strip.Items.Add(flawStrip);

            strip.Items.Add("Insert preceding flaw", null, (o, e2) =>
            {
                if (this.ParentFlaw != null)
                {
                    Flaw parentFlaw = this.ParentFlaw;
                    TreeNode originalParentNode = this.Parent;

                    this.Parent.Nodes.Remove(this);
                    parentFlaw.TransitiveFlaws.Remove(this.Flaw);

                    FlawTreeNode newParentNode = new FlawTreeNode();

                    newParentNode.Flaw.TransitiveFlaws.Add(this.Flaw);

                    originalParentNode.Nodes.AddFlawNode(newParentNode);
                }
                else if (this.ParentVulnerability != null)
                {
                    Vulnerability parentVulnerability = this.ParentVulnerability;
                    TreeNode originalParentNode = this.Parent;

                    this.Parent.Nodes.Remove(this);
                    parentVulnerability.RootFlaws.Remove(this.Flaw);

                    FlawTreeNode newParentNode = new FlawTreeNode();

                    newParentNode.Flaw.TransitiveFlaws.Add(this.Flaw);

                    originalParentNode.Nodes.AddFlawNode(newParentNode);
                }
            });

            return strip;
        }

        public override void OnNodeAddedToTree()
        {
            base.OnNodeAddedToTree();

            if (this.ParentFlaw != null)
            {
                if (this.ParentFlaw.TransitiveFlaws.Any(x => x == this.Flaw) == false)
                {
                    this.ParentFlaw.TransitiveFlaws.Add(this.Flaw);
                }
            }
            else if (this.ParentVulnerability != null)
            {
                if (this.ParentVulnerability.RootFlaws.Any(x => x == this.Flaw) == false)
                {
                    this.ParentVulnerability.RootFlaws.Add(this.Flaw);
                }
            }

            //
            // Add transitive flaws/violations from this node.
            //

            List<Flaw> transitiveFlaws = new List<Flaw>(this.Flaw.TransitiveFlaws);

            foreach (Flaw transitiveFlaw in transitiveFlaws)
            {
                bool exists =
                    (from TreeNode tn in this.Nodes
                     where tn is FlawTreeNode
                     where (tn as FlawTreeNode).Flaw == transitiveFlaw
                     select tn).Any();

                if (exists == false)
                {
                    this.Nodes.AddFlawNode(new FlawTreeNode(transitiveFlaw));
                }
            }

            this.ReadGroupTreeNode = new GroupViolationTreeNode(MemoryAccessMethod.Read);
            this.WriteGroupTreeNode = new GroupViolationTreeNode(MemoryAccessMethod.Write);
            this.ExecuteGroupTreeNode = new GroupViolationTreeNode(MemoryAccessMethod.Execute);

            this.Nodes.AddViolationNode(this.ReadGroupTreeNode);
            this.Nodes.AddViolationNode(this.WriteGroupTreeNode);
            this.Nodes.AddViolationNode(this.ExecuteGroupTreeNode);

            List<Violation> transitiveViolations = new List<Violation>(this.Flaw.TransitiveViolations);

            foreach (Violation transitiveViolation in transitiveViolations)
            {
                TreeNodeCollection nodeCollection;

                switch (transitiveViolation.Method)
                {
                    case MemoryAccessMethod.Read:
                        nodeCollection = this.ReadGroupTreeNode.Nodes;
                        break;

                    case MemoryAccessMethod.Write:
                        nodeCollection = this.WriteGroupTreeNode.Nodes;
                        break;

                    case MemoryAccessMethod.Execute:
                        nodeCollection = this.ExecuteGroupTreeNode.Nodes;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                bool exists =
                    (from TreeNode tn in nodeCollection
                     where tn is ConcreteViolationTreeNode
                     where (tn as ConcreteViolationTreeNode).Violation == transitiveViolation
                     select tn).Any();

                if (exists == false)
                {
                    nodeCollection.AddViolationNode(new ConcreteViolationTreeNode(transitiveViolation));
                }
            }

            this.Expand();

            //
            // Set the transitive completeness image states.
            //

            this.ReadGroupTreeNode.NextState(
                (this.Flaw.TransitiveReadViolations.Count() == 0)
                    ? ((this.Flaw.TransitiveReadListIsComplete == true) ? TransitiveProfileTreeViewImageIndex.No : TransitiveProfileTreeViewImageIndex.Unknown)
                    : TransitiveProfileTreeViewImageIndex.Yes
                );

            this.WriteGroupTreeNode.NextState(
                (this.Flaw.TransitiveWriteViolations.Count() == 0)
                    ? ((this.Flaw.TransitiveWriteListIsComplete == true) ? TransitiveProfileTreeViewImageIndex.No : TransitiveProfileTreeViewImageIndex.Unknown)
                    : TransitiveProfileTreeViewImageIndex.Yes
                );

            this.ExecuteGroupTreeNode.NextState(
                (this.Flaw.TransitiveExecuteViolations.Count() == 0)
                    ? ((this.Flaw.TransitiveExecuteListIsComplete == true) ? TransitiveProfileTreeViewImageIndex.No : TransitiveProfileTreeViewImageIndex.Unknown)
                    : TransitiveProfileTreeViewImageIndex.Yes
                );
        }

        public override void UpdateText()
        {
            if (this.Flaw.Name != null)
            {
                this.Text = String.Format("Flaw: {0}", this.Flaw.Name);
            }
            else
            {
                this.Text = "Unspecified flaw";
            }
        }

        public Flaw Flaw { get; private set; }

        public GroupViolationTreeNode ReadGroupTreeNode { get; set; }
        public GroupViolationTreeNode WriteGroupTreeNode { get; set; }
        public GroupViolationTreeNode ExecuteGroupTreeNode { get; set; }
    }

    /// <summary>
    /// Leading to X...
    /// </summary>
    internal class GroupViolationTreeNode : ProfileTreeNode
    {
        public GroupViolationTreeNode(
            MemoryAccessMethod method,
            TransitiveProfileTreeViewImageIndex initialImage = TransitiveProfileTreeViewImageIndex.Unknown
            )
            : base(initialImage)
        {
            this.Method = method;
        }

        public MemoryAccessMethod Method { get; private set; }

        private void ShowAddViolationForm()
        {
            AddViolationForm form = new AddViolationForm(this.TreeViewDerived.MemorySafetyModel);

            form.AllowedMethods = new List<MemoryAccessMethod>(new[] { this.Method });
            form.ShowDialog();

            if (form.SelectedViolation != null)
            {
                AddViolationNode(form.SelectedViolation);
            }
        }

        private ProfileTreeNode AddViolationNode(Violation violation)
        {
            ConcreteViolationTreeNode treeNode = new ConcreteViolationTreeNode(violation);

            this.Nodes.AddViolationNode(treeNode);

            //
            // Add "other" tree node to allow us to track unqualified variants of a particular violation.
            //

            if (this.Nodes.Count == 1)
            {
                TransitiveProfileTreeViewImageIndex initialIndex;

                if ((this.ParentViolation != null) &&
                    (this.ParentViolation.IsTransitiveViolationComplete(this.Method)))
                {
                    initialIndex = TransitiveProfileTreeViewImageIndex.No;
                }
                else if ((this.ParentFlaw != null) &&
                    (this.ParentFlaw.IsTransitiveViolationComplete(this.Method)))
                {
                    initialIndex = TransitiveProfileTreeViewImageIndex.No;
                }
                else
                {
                    initialIndex = TransitiveProfileTreeViewImageIndex.Unknown;
                }

                this.Nodes.AddViolationNode(new OtherViolationTreeNode(this.Method, initialIndex));
            }

            treeNode.Expand();

            this.Expand();

            if (this.State != TransitiveProfileTreeViewImageIndex.Yes)
            {
                NextState(TransitiveProfileTreeViewImageIndex.Yes);
            }

            this.TreeViewDerived.NotifyNewProfile(violation);

            return treeNode;
        }

        public void RemoveViolationNode(TreeNode node)
        {
            //
            // Disassociate this violation with its parent (if it has one).
            //

            ProfileTreeNode violationTreeNode = node as ProfileTreeNode;

            if (violationTreeNode is ConcreteViolationTreeNode)
            {
                ConcreteViolationTreeNode concreteNode = violationTreeNode as ConcreteViolationTreeNode;

                if (concreteNode.ParentViolation != null)
                {
                    TransitiveViolation trans;

                    while (true)
                    {
                        trans = concreteNode.ParentViolation.TransitiveViolations.Where(x => x.Violation == concreteNode.Violation).FirstOrDefault();

                        if (trans == null)
                        {
                            break;
                        }

                        concreteNode.ParentViolation.TransitiveViolations.Remove(trans);
                    }
                }
            }

            this.Nodes.Remove(node);

            // 
            // We are down to just the "other" node.  Flush.
            //

            if (this.Nodes.Count == 1)
            {
                this.Nodes.Clear();

                NextState(TransitiveProfileTreeViewImageIndex.Unknown);
            }
        }

        public void RemoveViolationNodes()
        {
            List<TreeNode> nodes = new List<TreeNode>();

            //
            // Snap to a loca list as we will be modifying the collection.
            //

            foreach (TreeNode node in this.Nodes)
            {
                nodes.Add(node);
            }

            foreach (TreeNode node in nodes)
            {
                if (node is ConcreteViolationTreeNode)
                {
                    RemoveViolationNode(node);
                }
            }
        }

        protected override bool OnStateChanging(TransitiveProfileTreeViewImageIndex newImageState, bool showWarning)
        {
            switch (newImageState)
            {
                case TransitiveProfileTreeViewImageIndex.No:
                case TransitiveProfileTreeViewImageIndex.Unknown:
                    if (showWarning && this.Nodes.Count > 0)
                    {
                        DialogResult res = MessageBox.Show(
                            "You have violations associated with this node which will be removed if you proceed.  Do you wish to proceed?",
                            "Nodes will be removed",
                            MessageBoxButtons.YesNo
                            );

                        if (res != DialogResult.Yes)
                        {
                            //
                            // Return without updating the state.
                            //

                            return false;
                        }

                        RemoveViolationNodes();
                    }
                    break;

                default:
                    break;
            }

            if (this.Nodes.Count == 0)
            {
                if (this.ParentViolation != null)
                {
                    switch (newImageState)
                    {
                        case TransitiveProfileTreeViewImageIndex.No:
                            this.ParentViolation.SetTransitiveViolationComplete(this.Method, true);
                            break;

                        case TransitiveProfileTreeViewImageIndex.Unknown:
                            this.ParentViolation.SetTransitiveViolationComplete(this.Method, false);
                            break;
                    }
                }
                else if (this.ParentFlaw != null)
                {
                    switch (newImageState)
                    {
                        case TransitiveProfileTreeViewImageIndex.No:
                            this.ParentFlaw.SetTransitiveViolationComplete(this.Method, true);
                            break;

                        case TransitiveProfileTreeViewImageIndex.Unknown:
                            this.ParentFlaw.SetTransitiveViolationComplete(this.Method, false);
                            break;
                    }
                }
            }

            return true;
        }

        protected override void OnStateChange(TransitiveProfileTreeViewImageIndex newImageState)
        {
            if (this.ParentViolation != null)
            {
                switch (newImageState)
                {
                    case TransitiveProfileTreeViewImageIndex.No:
                        this.ParentViolation.SetTransitiveViolationComplete(this.Method, true);
                        break;

                    case TransitiveProfileTreeViewImageIndex.Unknown:
                        this.ParentViolation.SetTransitiveViolationComplete(this.Method, false);
                        break;
                }
            }
            else if (this.ParentFlaw != null)
            {
                switch (newImageState)
                {
                    case TransitiveProfileTreeViewImageIndex.No:
                        this.ParentFlaw.SetTransitiveViolationComplete(this.Method, true);
                        break;

                    case TransitiveProfileTreeViewImageIndex.Unknown:
                        this.ParentFlaw.SetTransitiveViolationComplete(this.Method, false);
                        break;
                }
            }
        }

        public override void Refresh()
        {
            base.Refresh();

            foreach (ProfileTreeNode treeNode in this.Nodes)
            {
                treeNode.Refresh();
            }
        }

        public override void UpdateText()
        {
            this.Text = String.Format("Leads to {0} AV...", this.Method.ToString().ToLower());
        }

        protected override ContextMenuStrip RefreshContextMenuStrip()
        {
            ContextMenuStrip strip = new ContextMenuStrip();

            strip.Items.Add("...with default state", null, (o, e) =>
            {
                AddViolationNode(new Violation(this.Method));
            });

            strip.Items.Add("...with controlled base", null, (o, e) =>
            {
                AddViolationNode(new Violation(
                    this.Method,
                    baseState: MemoryAccessParameterState.Controlled
                    ));
            });

            strip.Items.Add("...with controlled content", null, (o, e) =>
            {
                AddViolationNode(new Violation(
                    this.Method,
                    contentSrcState: MemoryAccessParameterState.Controlled
                    ));
            });

            if (this.Method == MemoryAccessMethod.Read || this.Method == MemoryAccessMethod.Write)
            {
                strip.Items.Add("...with controlled displacement", null, (o, e) =>
                {
                    AddViolationNode(new Violation(
                        this.Method,
                        displacementState: MemoryAccessParameterState.Controlled));
                });

                strip.Items.Add("...with controlled extent", null, (o, e) =>
                {
                    AddViolationNode(new Violation(
                        this.Method,
                        extentState: MemoryAccessParameterState.Controlled));
                });
            }

            bool showPossibilitiesStrip = true;

            if (this is RootViolationGroupTreeNode == true)
            {
                showPossibilitiesStrip = false;
            }
            else
            {
                // add violation chain

                // add violation from list of possible violations given parent (from sub menu)
                // add all possible violations given parent (if parent present)

                // track transitions derived from
            }

            strip.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem chainStrip = new ToolStripMenuItem();
            chainStrip.Text = "Add violation chain...";

            foreach (TransitionChain chain in this.TreeViewDerived.Simulation.GetTransitionChains(this.Method))
            {
                TransitionChain localChain = chain; // capture for lambda

                chainStrip.DropDownItems.Add(
                    chain.ToString(),
                    null,
                    (o, e) =>
                    {
                        this.AddViolationNode(localChain.Violations.First());
                    }
                    );
            }

            if (chainStrip.DropDownItems.Count > 0)
            {
                strip.Items.Add(chainStrip);
            }

            if (showPossibilitiesStrip)
            {
                var transitions =
                    from TransitionInformation ti in this.PossibleViolationTransitions
                    select new { TransitionInfo = ti, Label = String.Format("{0} [{1}]", ti.Transition.Label, ti.PostViolation.Symbol) };

                if (transitions.Count() > 0)
                {
                    ToolStripMenuItem possibilitiesStrip = new ToolStripMenuItem();
                    possibilitiesStrip.Text = "Add possible violation...";

                    List<Violation> allPossibilities = new List<Violation>(transitions.Select(x => x.TransitionInfo.PostViolation));

                    possibilitiesStrip.DropDownItems.Add(
                        "Add all possibilities", null, (o, e) =>
                            {
                                foreach (Violation v in allPossibilities)
                                {
                                    // TODO: track transition.

                                    this.AddViolationNode(v.CloneViolation());
                                }
                            });

                    possibilitiesStrip.DropDownItems.Add(new ToolStripSeparator());

                    foreach (var transition in transitions.OrderBy(x => x.Label))
                    {
                        Violation newViolation = transition.TransitionInfo.PostViolation; // capture outside of lambda

                        possibilitiesStrip.DropDownItems.Add(
                            transition.Label, null, (o, e) =>
                            {
                                // TODO: track transition.

                                this.AddViolationNode(
                                    newViolation.CloneViolation()
                                    );
                            });
                    }

                    strip.Items.Add(possibilitiesStrip);
                }
            }

            strip.Items.Add("Add specific violation...", null, (o, e) =>
            {
                ShowAddViolationForm();
            });

            return strip;
        }

        private IEnumerable<TransitionInformation> PossibleViolationTransitions
        {
            get
            {
                if (this.ParentViolation == null)
                {
                    return new List<TransitionInformation>();
                }

                string key = String.Format("{0}", this.ParentViolation.EquivalenceClass);

                if (!ViolationTransitionsCache.ContainsKey(key))
                {
                    List<TransitionInformation> transitions = new List<TransitionInformation>();

                    Simulation simulation = this.TreeViewDerived.Simulation;

                    Target target = new Target()
                    {
                        // TODO: use app/os invariants?
                        Violation = this.ParentViolation.CloneViolation()
                    };

                    GlobalSimulationContext globalContext = new GlobalSimulationContext(target);

                    globalContext.AssumeContentInitializationPossible = true;

                    SimulationContext initialContext = new SimulationContext(globalContext);

                    Simulator simulator = new Simulator(simulation, initialContext);

                    transitions = new List<TransitionInformation>();

                    simulator.RunOnce((transitionInfo) =>
                    {
                        transitions.Add(transitionInfo);
                    });

                    ViolationTransitionsCache[key] = transitions;
                }

                //
                // Skip transitions to memory access methods other than the one we care about.
                //

                return ViolationTransitionsCache[key].Where(x => x.PostViolation.Method == this.Method);
            }
        }

        public static Dictionary<string, List<TransitionInformation>> ViolationTransitionsCache = new Dictionary<string, List<TransitionInformation>>();
    }

    /// <summary>
    /// Violation group of type read, write or execute.
    /// </summary>
    internal class RootViolationGroupTreeNode : GroupViolationTreeNode
    {
        public RootViolationGroupTreeNode(
            MemoryAccessMethod method
            )
            : base(method)
        {
        }

        public override void UpdateText()
        {
            this.Text = String.Format("{0}", this.Method);
        }
    }

    /// <summary>
    /// Other violation of X...
    /// </summary>
    internal class OtherViolationTreeNode : GroupViolationTreeNode
    {
        public OtherViolationTreeNode(
            MemoryAccessMethod method,
            TransitiveProfileTreeViewImageIndex initialImage = TransitiveProfileTreeViewImageIndex.Unknown
            )
            : base(method, initialImage)
        {
        }

        public override void UpdateText()
        {
            GroupViolationTreeNode group = this.Parent as GroupViolationTreeNode;

            this.Text = String.Format("Other {0} violations possible?", group.Method.ToString().ToLower());
        }

        protected override ContextMenuStrip RefreshContextMenuStrip()
        {
            return null;
        }
    }

    /// <summary>
    /// A specific description of a violation
    /// </summary>
    internal class ConcreteViolationTreeNode : ProfileTreeNode
    {
        public ConcreteViolationTreeNode(
            Violation violation
            )
            : base(TransitiveProfileTreeViewImageIndex.Yes)
        {
            this.Violation = violation;

            UpdateText();
        }

        public Violation Violation { get; set; }

        public GroupViolationTreeNode ReadGroupTreeNode { get; set; }
        public GroupViolationTreeNode WriteGroupTreeNode { get; set; }
        public GroupViolationTreeNode ExecuteGroupTreeNode { get; set; }

        public override void OnNodeAddedToTree()
        {
            base.OnNodeAddedToTree();

            this.ReadGroupTreeNode = new GroupViolationTreeNode(MemoryAccessMethod.Read);
            this.WriteGroupTreeNode = new GroupViolationTreeNode(MemoryAccessMethod.Write);
            this.ExecuteGroupTreeNode = new GroupViolationTreeNode(MemoryAccessMethod.Execute);

            switch (this.Violation.Method)
            {
                case MemoryAccessMethod.Read:
                    this.Nodes.AddViolationNode(this.ReadGroupTreeNode);
                    this.Nodes.AddViolationNode(this.WriteGroupTreeNode);
                    this.Nodes.AddViolationNode(this.ExecuteGroupTreeNode);
                    break;

                case MemoryAccessMethod.Write:
                    this.Nodes.AddViolationNode(this.ReadGroupTreeNode);
                    this.Nodes.AddViolationNode(this.ExecuteGroupTreeNode);
                    break;

                case MemoryAccessMethod.Execute:
                    this.Nodes.AddViolationNode(this.ExecuteGroupTreeNode);
                    break;
            }

            if (this.ParentViolation != null)
            {
                if (this.ParentViolation.TransitiveViolations.Any(x => x.Violation == this.Violation) == false)
                {
                    this.ParentViolation.AddTransitiveViolation(this.Violation);
                }
            }
            else if (this.ParentFlaw != null)
            {
                if (this.ParentFlaw.TransitiveViolations.Any(x => x == this.Violation) == false)
                {
                    this.ParentFlaw.TransitiveViolations.Add(this.Violation);
                }
            }
            else if (this.ParentVulnerability != null)
            {
                if (this.ParentVulnerability.RootViolations.Any(x => x == this.Violation) == false)
                {
                    this.ParentVulnerability.RootViolations.Add(this.Violation);
                }
            }

            List<TransitiveViolation> transList = new List<TransitiveViolation>(this.Violation.TransitiveViolations);

            foreach (TransitiveViolation trans in transList)
            {
                switch (trans.Violation.Method)
                {
                    case MemoryAccessMethod.Read:
                        this.ReadGroupTreeNode.Nodes.AddViolationNode(new ConcreteViolationTreeNode(trans.Violation));
                        break;

                    case MemoryAccessMethod.Write:
                        this.WriteGroupTreeNode.Nodes.AddViolationNode(new ConcreteViolationTreeNode(trans.Violation));
                        break;

                    case MemoryAccessMethod.Execute:
                        this.ExecuteGroupTreeNode.Nodes.AddViolationNode(new ConcreteViolationTreeNode(trans.Violation));
                        break;

                    default:
                        break;
                }
            }

            this.Expand();

            //
            // Set the transitive completeness image states.
            //

            this.ReadGroupTreeNode.NextState(
                (this.Violation.TransitiveReadViolations.Count() == 0)
                    ? ((this.Violation.TransitiveReadListIsComplete == true) ? TransitiveProfileTreeViewImageIndex.No : TransitiveProfileTreeViewImageIndex.Unknown)
                    : TransitiveProfileTreeViewImageIndex.Yes
                );

            this.WriteGroupTreeNode.NextState(
                (this.Violation.TransitiveWriteViolations.Count() == 0)
                    ? ((this.Violation.TransitiveWriteListIsComplete == true) ? TransitiveProfileTreeViewImageIndex.No : TransitiveProfileTreeViewImageIndex.Unknown)
                    : TransitiveProfileTreeViewImageIndex.Yes
                );

            this.ExecuteGroupTreeNode.NextState(
                (this.Violation.TransitiveExecuteViolations.Count() == 0)
                    ? ((this.Violation.TransitiveExecuteListIsComplete == true) ? TransitiveProfileTreeViewImageIndex.No : TransitiveProfileTreeViewImageIndex.Unknown)
                    : TransitiveProfileTreeViewImageIndex.Yes
                );
        }

        protected override bool OnStateChanging(TransitiveProfileTreeViewImageIndex newImageState, bool showWarning)
        {
            return false;
        }

        public override void UpdateText()
        {
            if (this.Violation != null)
            {
                if (this.Violation.Name != null)
                {
                    this.Text = String.Format("Violation: {0} ({1})", this.Violation.Symbol, this.Violation.Name);
                }
                else
                {
                    this.Text = String.Format("Violation: {0}", this.Violation.Symbol);
                }
            }
            else
            {
                this.Text = "Unspecified violation";
            }
        }

        public override void Refresh()
        {
            base.Refresh();

            if (this.oldEqvuialenceClass == null || this.oldEqvuialenceClass != this.Violation.EquivalenceClass)
            {
                //
                // Refresh transitive inheritance.  Do this before refreshing the child groups as
                // this may result in their equivalence class changing.
                //

                foreach (TransitiveViolation trans in this.Violation.TransitiveViolations)
                {
                    if (trans.TransitionDescriptor == null || trans.TransitionDescriptor.Transition == null)
                    {
                        continue;
                    }

                    trans.TransitionDescriptor.Transition.Primitive.InheritParameterState(this.Violation, trans.Violation);
                }

                // 
                // Also, refresh child group nodes as their context menus may need to be updated.
                //

                if (this.ReadGroupTreeNode != null)
                {
                    this.ReadGroupTreeNode.Refresh();
                }

                if (this.WriteGroupTreeNode != null)
                {
                    this.WriteGroupTreeNode.Refresh();
                }

                if (this.ExecuteGroupTreeNode != null)
                {
                    this.ExecuteGroupTreeNode.Refresh();
                }

                this.oldEqvuialenceClass = this.Violation.EquivalenceClass;
            }
        }
        private string oldEqvuialenceClass;
    }

    internal static class TreeNodeCollectionExtension
    {
        internal static void AddViolationNode(this TreeNodeCollection collection, ProfileTreeNode violationNode)
        {
            collection.Add((TreeNode)violationNode);

            violationNode.OnNodeAddedToTree();

            if (violationNode.Parent != null)
            {
                violationNode.Parent.Expand();
            }
        }

        internal static void AddFlawNode(this TreeNodeCollection collection, FlawTreeNode flawNode)
        {
            collection.Insert(0, (TreeNode)flawNode);

            flawNode.OnNodeAddedToTree();

            if (flawNode.Parent != null)
            {
                flawNode.Parent.Expand();
            }
        }
    }
}
