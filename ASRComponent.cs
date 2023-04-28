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
using Timer = System.Timers.Timer;

namespace LiveSplit.AutoSplittingRuntime
{
    public class ASRComponent : LogicComponent
    {
        private readonly TimerModel model;
        private readonly ComponentSettings settings;
        private Timer updateTimer;

        private string oldScriptPath;

        private const int MillisecondsPerSecond = 1000;

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

            InitializeUpdateTimer();
        }

        public ASRComponent(LiveSplitState state, string scriptPath)
        {
            model = new TimerModel() { CurrentState = state };

            settings = new ComponentSettings(model, scriptPath);

            InitializeUpdateTimer();
        }

        private void InitializeUpdateTimer()
        {
            updateTimer = new Timer() { Interval = 15 };
            updateTimer.Elapsed += (sender, args) => UpdateTimerElapsed();
            updateTimer.Enabled = true;
        }

        public override string ComponentName => "Auto Splitting Runtime";

        public override void Dispose()
        {
            updateTimer?.Dispose();
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
            // Handle the script reload on the UI thread to properly update the custom settings tree

            // FIXME: Handle potential race conditions?
            if (settings.ScriptPath != oldScriptPath)
            {
                oldScriptPath = settings.ScriptPath;

                // Try to load the new autosplitter
                // Run it once to let it register it's settings
                // Whatever happens, we need to rebuild the custom settings tree
                try
                {
                    settings.ReloadRuntime();
                    settings.runtime.Step();
                }
                finally
                {
                    settings.BuildTree();
                }
            }
        }

        public void UpdateTimerElapsed()
        {
            // This refresh timer behavior is similar to the ASL refresh timer

            // Disable timer, to wait for execution of this iteration to
            // finish. This can be useful if blocking operations like
            // showing a message window are used.
            updateTimer.Enabled = false;

            try
            {
                settings.runtime?.Step();

                // Poll the tick rate and modify the update interval if it has been changed
                double tickRate = settings.runtime.TickRate().TotalMilliseconds;

                if (tickRate != updateTimer.Interval)
                    updateTimer.Interval = tickRate;
            }
            finally
            {
                updateTimer.Enabled = true;
            }
        }
    }
}
