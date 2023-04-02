using System;
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

namespace LiveSplit.AutoSplittingRuntime
{
    public partial class ComponentSettings : UserControl
    {
        public string ScriptPath { get; set; }

        public Runtime runtime = null;

        // if true, next path loaded from settings will be ignored
        private bool _ignore_next_path_setting;

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

        // Custom settings
        public Dictionary<string, bool> _custom_settings_state;

        private static readonly LogDelegate log = (messagePtr, messageLen) =>
        {
            var message = ASRString.FromPtrLen(messagePtr, messageLen);
            Log.Info($"[Auto Splitting Runtime] {message}");
        };

        private readonly StateDelegate getState;
        private readonly Action start;
        private readonly Action split;
        private readonly Action reset;
        private readonly SetGameTimeDelegate setGameTime;
        private readonly Action pauseGameTime;
        private readonly Action resumeGameTime;

        public ComponentSettings(TimerModel model)
        {
            InitializeComponent();

            ScriptPath = string.Empty;

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
            _custom_settings_state = new Dictionary<string, bool>();

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
            reset = () => model.Reset();
            setGameTime = (ticks) => model.CurrentState.SetGameTime(new TimeSpan(ticks));
            pauseGameTime = () => model.CurrentState.IsGameTimePaused = true;
            resumeGameTime = () => model.CurrentState.IsGameTimePaused = false;
        }

        public ComponentSettings(TimerModel model, string scriptPath)
            : this(model)
        {
            ScriptPath = scriptPath;
            _ignore_next_path_setting = true;
        }

        public void ReloadRuntime()
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
                    var settingsStore = new SettingsStore();

                    foreach (var pair in _custom_settings_state)
                    {
                        settingsStore.Set(pair.Key, pair.Value);
                    }

                    runtime = new Runtime(
                        ScriptPath,
                        settingsStore,
                        getState,
                        start,
                        split,
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
        }   

        private void BuildTree()
        {
            this.treeCustomSettings.BeginUpdate();
            this.treeCustomSettings.Nodes.Clear();

            if (runtime != null)
            {

                var len = runtime.UserSettingsLength();
                for (ulong i = 0; i < len; i++)
                {
                    var key = runtime.UserSettingGetKey(i);
                    var desc = runtime.UserSettingGetDescription(i);
                    var ty = runtime.UserSettingGetType(i);

                    if (ty != "bool") continue;

                    var value = runtime.UserSettingGetBool(i);

                    var node = new TreeNode(desc)
                    {
                        Tag = key,
                        Checked = value,
                    };
                    this.treeCustomSettings.Nodes.Add(node);
                }
            }

            this.treeCustomSettings.EndUpdate();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settings_node = document.CreateElement("Settings");

            settings_node.AppendChild(SettingsHelper.ToElement(document, "Version", "1.0"));
            settings_node.AppendChild(SettingsHelper.ToElement(document, "ScriptPath", ScriptPath));
            AppendBasicSettingsToXml(document, settings_node);
            AppendCustomSettingsToXml(document, settings_node);

            return settings_node;
        }

        // Loads the settings of this component from Xml. This might happen more than once
        // (e.g. when the settings dialog is cancelled, to restore previous settings).
        public void SetSettings(XmlNode settings)
        {
            var element = (XmlElement)settings;
            if (!element.IsEmpty)
            {
                if (!_ignore_next_path_setting)
                    ScriptPath = SettingsHelper.ParseString(element["ScriptPath"], string.Empty);
                _ignore_next_path_setting = false;
                ParseBasicSettingsFromXml(element);
                ParseCustomSettingsFromXml(element);
            }
            ReloadRuntime();
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
                _custom_settings_state.Clear();
            }

            InitBasicSettings(settings);
        }


        private void AppendBasicSettingsToXml(XmlDocument document, XmlNode settings_node)
        {
            foreach (var item in _basic_settings)
            {
                if (_basic_settings_state.ContainsKey(item.Key.ToLower()))
                {
                    var value = _basic_settings_state[item.Key.ToLower()];
                    settings_node.AppendChild(SettingsHelper.ToElement(document, item.Key, value));
                }
            }
        }

        private void AppendCustomSettingsToXml(XmlDocument document, XmlNode parent)
        {
            XmlElement asr_parent = document.CreateElement("CustomSettings");

            foreach (var setting in _custom_settings_state)
            {
                XmlElement element = SettingsHelper.ToElement(document, "Setting", setting.Value);
                XmlAttribute id = SettingsHelper.ToAttribute(document, "id", setting.Key);
                // In case there are other setting types in the future
                XmlAttribute type = SettingsHelper.ToAttribute(document, "type", "bool");

                element.Attributes.Append(id);
                element.Attributes.Append(type);
                asr_parent.AppendChild(element);
            }

            parent.AppendChild(asr_parent);
        }

        private void ParseBasicSettingsFromXml(XmlElement element)
        {
            foreach (var item in _basic_settings)
            {
                if (element[item.Key] != null)
                {
                    var value = bool.Parse(element[item.Key].InnerText);

                    // If component is not enabled, don't check setting
                    if (item.Value.Enabled)
                        item.Value.Checked = value;

                    _basic_settings_state[item.Key.ToLower()] = value;
                }
            }
        }

        /// <summary>
        /// Parses custom settings, stores them and updates the checked state of already added tree nodes.
        /// </summary>
        /// 
        private void ParseCustomSettingsFromXml(XmlElement data)
        {
            XmlElement custom_settings_node = data["CustomSettings"];

            _custom_settings_state.Clear();

            if (custom_settings_node != null && custom_settings_node.HasChildNodes)
            {
                foreach (XmlElement element in custom_settings_node.ChildNodes)
                {
                    if (element.Name != "Setting")
                        continue;

                    string id = element.Attributes["id"].Value;
                    string type = element.Attributes["type"].Value;

                    if (id != null && type == "bool")
                    {
                        bool value = SettingsHelper.ParseBool(element);
                        _custom_settings_state[id] = value;
                    }
                }
            }

            //// Update tree with loaded state (in case the tree is already populated)
            //UpdateNodesCheckedState(_custom_settings_state);
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
            var key = (string)node.Tag;

            if (node.Checked != value)
            {
                _custom_settings_state[key] = value;
                ReloadRuntime();
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
                ScriptPath = this.txtScriptPath.Text = dialog.FileName;
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
            _custom_settings_state[(string)e.Node.Tag] = e.Node.Checked;
            ReloadRuntime();
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