using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.AutoSplittingRuntime
{
    public class ASRComponent : LogicComponent
    {
        private readonly TimerModel model;
        private readonly ComponentSettings settings;

        private string oldScriptPath;

        static ASRComponent()
        {
            try
            {
                ASRLoader.LoadASR();
            }
            catch { }
        }

        public ASRComponent(LiveSplitState state)
        {
            model = new TimerModel() { CurrentState = state };

            settings = new ComponentSettings(model);
        }

        public ASRComponent(LiveSplitState state, string scriptPath)
        {
            model = new TimerModel() { CurrentState = state };

            settings = new ComponentSettings(model, scriptPath);
        }

        public override string ComponentName => "Auto Splitting Runtime";

        public override void Dispose()
        {
            settings.runtime?.Dispose();
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            this.settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (settings.ScriptPath != oldScriptPath)
            {
                oldScriptPath = settings.ScriptPath;
                settings.ReloadRuntime();
            }

            settings.runtime?.Step();
        }
    }
}
