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

        private Runtime runtime;

        private readonly StateDelegate getState;
        private readonly Action start;
        private readonly Action split;
        private readonly Action reset;
        private readonly SetGameTimeDelegate setGameTime;
        private readonly Action pauseGameTime;
        private readonly Action resumeGameTime;

        private static readonly LogDelegate log = (messagePtr, messageLen, logLevel) =>
        {
            var message = ASRString.FromPtrLen(messagePtr, messageLen);
            switch (logLevel)
            {
                case 1: Log.Error(message); break;
                case 2: Log.Warning(message); break;
                case 3: Log.Info(message); break;
                default: break;
            }
        };

        static ASRComponent()
        {
            try
            {
                ASRLoader.LoadASR();
                Runtime.SetLogger(log);
            }
            catch { }
        }

        public ASRComponent(LiveSplitState state)
        {
            model = new TimerModel() { CurrentState = state };

            settings = new ComponentSettings();

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

            runtime = null;
        }

        public ASRComponent(LiveSplitState state, string scriptPath)
            : this(state)
        {
            settings = new ComponentSettings(scriptPath);
        }

        public override string ComponentName => "Auto Splitting Runtime";

        public override void Dispose()
        {
            if (runtime != null)
            {
                runtime.Dispose();
            }
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
                try
                {
                    oldScriptPath = settings.ScriptPath;

                    if (runtime != null)
                    {
                        runtime.Dispose();
                        runtime = null;
                    }

                    if (!string.IsNullOrEmpty(settings.ScriptPath))
                    {
                        runtime = new Runtime(
                            settings.ScriptPath,
                            getState,
                            start,
                            split,
                            reset,
                            setGameTime,
                            pauseGameTime,
                            resumeGameTime
                        );
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            if (runtime != null)
            {
                runtime.Step();
            }
        }
    }
}
