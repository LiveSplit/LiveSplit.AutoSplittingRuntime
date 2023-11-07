﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.UI;
using System.IO;
using LiveSplit.Options;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using LiveSplit.Model;
using System.Globalization;

namespace LiveSplit.AutoSplittingRuntime
{
    public partial class ComponentSettings : UserControl
    {
        private string scriptPath;
        public string ScriptPath
        {
            get => scriptPath;
            set
            {
                if (value != scriptPath)
                {
                    scriptPath = value;
                    this.ReloadRuntime(null);
                }
            }
        }

        public Runtime runtime = null;

        private Dictionary<string, CheckBox> _basic_settings;

        // Save the state of settings independant of actual ASRSetting objects
        // or the actual GUI components (checkboxes). This is used to restore
        // the state when the script is first loaded (because settings are
        // loaded before the script) or reloaded.
        //
        // State is synchronized with the ASRSettings when a script is
        // successfully loaded, as well as when the checkboxes/tree check
        // state is changed by the user or program. It is also updated
        // when loaded from XML.
        //
        // State is stored from the current script, or the last loaded script
        // if no script is currently loaded.

        // Start/Reset/Split checkboxes
        private Dictionary<string, bool> _basic_settings_state;

        private static readonly LogDelegate log = (messagePtr, messageLen) =>
        {
            var message = ASRString.FromPtrLen(messagePtr, messageLen);
            Log.Info($"[Auto Splitting Runtime] {message}");
        };

        private readonly StateDelegate getState;
        private readonly Action start;
        private readonly Action split;
        private readonly Action skipSplit;
        private readonly Action undoSplit;
        private readonly Action reset;
        private readonly SetGameTimeDelegate setGameTime;
        private readonly Action pauseGameTime;
        private readonly Action resumeGameTime;

        public ComponentSettings(TimerModel model)
        {
            InitializeComponent();

            scriptPath = "";

            this.txtScriptPath.DataBindings.Add("Text", this, "ScriptPath", false,
                DataSourceUpdateMode.OnPropertyChanged);

            _basic_settings = new Dictionary<string, CheckBox>
            {
                // Capitalized names for saving it in XML.
                ["Start"] = checkboxStart,
                ["Reset"] = checkboxReset,
                ["Split"] = checkboxSplit
            };

            _basic_settings_state = new Dictionary<string, bool>();

            getState = () =>
            {
                switch (model.CurrentState.CurrentPhase)
                {
                    case TimerPhase.NotRunning: return 0;
                    case TimerPhase.Running: return 1;
                    case TimerPhase.Paused: return 2;
                    case TimerPhase.Ended: return 3;
                }
                return 0;
            };
            start = () => model.Start();
            split = () => model.Split();
            skipSplit = () => model.SkipSplit();
            undoSplit = () => model.UndoSplit();
            reset = () => model.Reset();
            setGameTime = (ticks) => model.CurrentState.SetGameTime(new TimeSpan(ticks));
            pauseGameTime = () => model.CurrentState.IsGameTimePaused = true;
            resumeGameTime = () => model.CurrentState.IsGameTimePaused = false;
        }

        public ComponentSettings(TimerModel model, string scriptPath)
            : this(model)
        {
            this.ScriptPath = scriptPath;
        }

        public void ReloadRuntime(SettingsMap settingsMap)
        {
            try
            {
                if (runtime != null)
                {
                    runtime.Dispose();
                    runtime = null;
                }

                if (!string.IsNullOrEmpty(ScriptPath))
                {
                    runtime = new Runtime(
                        ScriptPath,
                        settingsMap ?? new SettingsMap(),
                        getState,
                        start,
                        split,
                        skipSplit,
                        undoSplit,
                        reset,
                        setGameTime,
                        pauseGameTime,
                        resumeGameTime,
                        log
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            if (runtime != null)
            {
                try { runtime.Step(); } catch { }
            }
            BuildTree();
        }

        public void BuildTree()
        {
            this.treeCustomSettings.BeginUpdate();
            this.treeCustomSettings.Nodes.Clear();

            if (runtime != null)
            {

                var parent = this.treeCustomSettings.Nodes;
                TreeNode parentNode = null;

                var len = runtime.UserSettingsLength();
                for (ulong i = 0; i < len; i++)
                {
                    var desc = runtime.UserSettingGetDescription(i);
                    var tooltip = runtime.UserSettingGetTooltip(i);
                    var ty = runtime.UserSettingGetType(i);

                    var node = new TreeNode(desc)
                    {
                        ToolTipText = tooltip,
                    };

                    switch (ty)
                    {
                        case "bool":
                            {
                                node.Tag = runtime.UserSettingGetKey(i);
                                node.Checked = runtime.UserSettingGetBool(i);
                                break;
                            }
                        case "title":
                            {
                                var headingLevel = runtime.UserSettingGetHeadingLevel(i);
                                node.Tag = headingLevel;
                                while (parentNode != null && (uint)parentNode.Tag >= headingLevel)
                                {
                                    if (parentNode.Parent != null)
                                    {
                                        parent = parentNode.Parent.Nodes;
                                        parentNode = parentNode.Parent;
                                    }
                                    else
                                    {
                                        parent = this.treeCustomSettings.Nodes;
                                        parentNode = null;
                                    }
                                }
                                break;
                            }
                        default: continue;
                    }

                    parent.Add(node);

                    if (ty == "title")
                    {
                        parent = node.Nodes;
                        parentNode = node;
                    }
                }
            }

            this.treeCustomSettings.EndUpdate();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settings_node = document.CreateElement("Settings");

            settings_node.AppendChild(SettingsHelper.ToElement(document, "Version", "1.0"));
            settings_node.AppendChild(SettingsHelper.ToElement(document, "ScriptPath", ScriptPath));
            AppendCustomSettingsToXml(document, settings_node);

            return settings_node;
        }

        // Loads the settings of this component from Xml. This might happen more than once
        // (e.g. when the settings dialog is cancelled, to restore previous settings).
        public void SetSettings(XmlNode settings)
        {
            var element = (XmlElement)settings;
            SettingsMap settingsMap = null;
            if (!element.IsEmpty)
            {
                scriptPath = SettingsHelper.ParseString(element["ScriptPath"], string.Empty);
                settingsMap = ParseCustomSettingsFromXml(element);
            }
            ReloadRuntime(settingsMap);
        }

        /// <summary>
        /// Populates the component with the settings defined in the ASR script.
        /// </summary>
        public void SetASRSettings(ASRSettings settings)
        {
            InitASRSettings(settings, true);
        }

        /// <summary>
        /// Empties the GUI of all settings (but still keeps settings state
        /// for the next script load).
        /// </summary>
        public void ResetASRSettings()
        {
            InitASRSettings(new ASRSettings(), false);
        }

        private void InitASRSettings(ASRSettings settings, bool script_loaded)
        {
            if (string.IsNullOrWhiteSpace(ScriptPath))
            {
                _basic_settings_state.Clear();
            }

            InitBasicSettings(settings);
        }

        private void AppendCustomSettingsToXml(XmlDocument document, XmlNode parent)
        {
            XmlElement asrParent = document.CreateElement("CustomSettings");

            if (runtime != null)
            {
                using (var settingsMap = runtime.GetSettingsMap())
                {
                    BuildMap(document, asrParent, settingsMap);
                }
            }

            parent.AppendChild(asrParent);
        }

        private static void BuildMap(XmlDocument document, XmlElement parent, SettingsMap settingsMap)
        {
            using (var iter = settingsMap.Iter())
            {
                while (iter.HasCurrent())
                {
                    XmlElement element = document.CreateElement("Setting");

                    element.InnerText = iter.GetBoolValue().ToString(CultureInfo.InvariantCulture);

                    XmlAttribute id = SettingsHelper.ToAttribute(document, "id", iter.GetKey());

                    // In case there are other setting types in the future
                    XmlAttribute typeAttr = SettingsHelper.ToAttribute(document, "type", "bool");

                    element.Attributes.Append(id);
                    element.Attributes.Append(typeAttr);
                    parent.AppendChild(element);

                    iter.Next();
                }
            }
        }

        /// <summary>
        /// Parses custom settings, stores them and updates the checked state of already added tree nodes.
        /// </summary>
        ///
        private SettingsMap ParseCustomSettingsFromXml(XmlElement data)
        {
            try
            {
                XmlElement custom_settings_node = data["CustomSettings"];

                if (custom_settings_node == null)
                {
                    return null;
                }


                return ParseMap(custom_settings_node);
            }
            catch
            {
                return null;
            }

            //// Update tree with loaded state (in case the tree is already populated)
            //UpdateNodesCheckedState(_custom_settings_state);
        }

        private SettingsMap ParseMap(XmlElement mapNode)
        {
            var map = new SettingsMap();

            foreach (XmlElement element in mapNode.ChildNodes)
            {
                if (element.Name != "Setting")
                    return null;

                string id = element.Attributes["id"].Value;

                if (id == null)
                {
                    return null;
                }

                string type = element.Attributes["type"].Value;

                if (type == "bool")
                {
                    bool value = SettingsHelper.ParseBool(element);
                    map.Set(id, value);
                }
                else
                {
                    return null;
                }
            }

            return map;
        }

        private void InitBasicSettings(ASRSettings settings)
        {
            foreach (var item in _basic_settings)
            {
                string name = item.Key.ToLower();
                CheckBox checkbox = item.Value;

                if (settings.IsBasicSettingPresent(name))
                {
                    ASRSetting setting = settings.BasicSettings[name];
                    checkbox.Enabled = true;
                    checkbox.Tag = setting;
                    var value = setting.Value;

                    if (_basic_settings_state.ContainsKey(name))
                        value = _basic_settings_state[name];

                    checkbox.Checked = value;
                    setting.Value = value;
                }
                else
                {
                    checkbox.Tag = null;
                    checkbox.Enabled = false;
                    checkbox.Checked = false;
                }
            }
        }

        /// <summary>
        /// Generic update on all given nodes and their childnodes, ignoring childnodes for
        /// nodes where the Func returns false.
        /// </summary>
        ///
        private void UpdateNodesInTree(Func<TreeNode, bool> func, TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                bool include_child_nodes = func(node);
                if (include_child_nodes)
                    UpdateNodesInTree(func, node.Nodes);
            }
        }

        ///// <summary>
        ///// Update the checked state of all given nodes and their childnodes based on the return
        ///// value of the given Func.
        ///// </summary>
        ///// <param name="nodes">If nodes is null, all nodes of the custom settings tree are affected.</param>
        /////
        //private void UpdateNodesCheckedState(Func<ASRSetting, bool> func, TreeNodeCollection nodes = null)
        //{
        //    if (nodes == null)
        //        nodes = this.treeCustomSettings.Nodes;

        //    UpdateNodesInTree(node => {
        //        var setting = (ASRSetting)node.Tag;
        //        bool check = func(setting);

        //        if (node.Checked != check)
        //            node.Checked = check;

        //        return true;
        //    }, nodes);
        //}

        ///// <summary>
        ///// Update the checked state of all given nodes and their childnodes
        ///// based on a dictionary of setting values.
        ///// </summary>
        /////
        //private void UpdateNodesCheckedState(Dictionary<string, bool> setting_values, TreeNodeCollection nodes = null)
        //{
        //    if (setting_values == null)
        //        return;

        //    UpdateNodesCheckedState(setting => {
        //        string id = setting.Id;

        //        if (setting_values.ContainsKey(id))
        //            return setting_values[id];

        //        return setting.Value;
        //    }, nodes);
        //}

        private void UpdateNodeCheckedState(bool value, TreeNode node)
        {
            if (!(node.Tag is string)) return;

            var key = (string)node.Tag;

            if (node.Checked != value)
            {
                runtime?.SettingsMapSetBool(key, value);
            }
        }


        // Events

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "WebAssembly module (*.wasm)|*.wasm|All Files (*.*)|*.*"
            };
            if (File.Exists(ScriptPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(ScriptPath);
                dialog.FileName = Path.GetFileName(ScriptPath);
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                scriptPath = this.txtScriptPath.Text = dialog.FileName;
            }
        }

        // Basic Setting checked/unchecked
        //
        // This detects both changes made by the user and by the program, so this should
        // change the state in _basic_settings_state fine as well.
        private void methodCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var setting = (ASRSetting)checkbox.Tag;

            if (setting != null)
            {
                setting.Value = checkbox.Checked;
                _basic_settings_state[setting.Id] = setting.Value;
            }
        }

        private void ComponentSettings_Load(object sender, EventArgs e)
        {
            BuildTree();
        }

        // Custom Setting checked/unchecked (only after initially building the tree)
        private void settingsTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!(e.Node.Tag is string)) return;
            var tag = (string)e.Node.Tag;
            runtime?.SettingsMapSetBool(tag, e.Node.Checked);
        }

        private void cmiCheckBranch_Click(object sender, EventArgs e)
        {
            UpdateNodeCheckedState(true, this.treeCustomSettings.SelectedNode);
        }

        private void cmiUncheckBranch_Click(object sender, EventArgs e)
        {
            UpdateNodeCheckedState(false, this.treeCustomSettings.SelectedNode);
        }
    }
}

namespace LiveSplit.UI.Components
{
    /// <summary>
    /// TreeView with fixed double-clicking on checkboxes.
    /// </summary>
    ///
    /// See also:
    /// http://stackoverflow.com/questions/17356976/treeview-with-checkboxes-not-processing-clicks-correctly
    /// http://stackoverflow.com/questions/14647216/c-sharp-treeview-ignore-double-click-only-at-checkbox
    class NewTreeView : System.Windows.Forms.TreeView
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x203) // identified double click
            {
                var local_pos = PointToClient(Cursor.Position);
                var hit_test_info = HitTest(local_pos);

                if (hit_test_info.Location == TreeViewHitTestLocations.StateImage)
                {
                    m.Msg = 0x201; // if checkbox was clicked, turn into single click
                }

                base.WndProc(ref m);
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }
}
