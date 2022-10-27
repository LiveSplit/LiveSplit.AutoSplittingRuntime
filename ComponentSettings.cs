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

namespace LiveSplit.AutoSplittingRuntime
{
    public partial class ComponentSettings : UserControl
    {
        public string ScriptPath { get; set; }

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


        public ComponentSettings()
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
        }

        public ComponentSettings(string scriptPath)
            : this()
        {
            ScriptPath = scriptPath;
            _ignore_next_path_setting = true;
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settings_node = document.CreateElement("Settings");

            settings_node.AppendChild(SettingsHelper.ToElement(document, "Version", "1.0"));
            settings_node.AppendChild(SettingsHelper.ToElement(document, "ScriptPath", ScriptPath));
            AppendBasicSettingsToXml(document, settings_node);

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
            }
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

        //private void UpdateNodeCheckedState(Func<ASRSetting, bool> func, TreeNode node)
        //{
        //    var setting = (ASRSetting)node.Tag;
        //    bool check = func(setting);

        //    if (node.Checked != check)
        //        node.Checked = check;
        //}


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
    }
}
