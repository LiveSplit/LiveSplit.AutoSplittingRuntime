using LiveSplit.AutoSplittingRuntime;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: ComponentFactory(typeof(ASRFactory))]

namespace LiveSplit.AutoSplittingRuntime
{
    public class ASRFactory : IComponentFactory
    {
        public string ComponentName => "Auto Splitting Runtime";
        public string Description => "Allows auto splitters provided as WebAssembly modules to define the splitting behaviour.";
        public ComponentCategory Category => ComponentCategory.Control;
        public Version Version => Version.Parse("0.0.6");

        public string UpdateName => ComponentName;
        public string UpdateURL => "http://livesplit.org/update/";
        public string XMLURL => "http://livesplit.org/update/Components/update.LiveSplit.AutoSplittingRuntime.xml";

        public IComponent Create(LiveSplitState state) => new ASRComponent(state);
        public IComponent Create(LiveSplitState state, string script) => new ASRComponent(state, script);
    }
}
