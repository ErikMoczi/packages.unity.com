

using System;

namespace Unity.Tiny
{
    internal class TinyBuildReportPanel : TinyPanel
    {
        [Serializable]
        internal class State
        {
            public TinyTreeState TreeState;
        }

        private IRegistry Registry { get; }

        private TinyModule.Reference MainModule { get; }

        public TinyBuildReportPanel(IRegistry registry, TinyModule.Reference mainModule, State state)
        {
            Registry = registry;
            MainModule = mainModule;

            if (state.TreeState == null)
            {
                state.TreeState = new TinyTreeState();
            }

            // @TODO Find a way to move this to the base class
            state.TreeState.Init(TinyBuildReportTreeView.CreateMultiColumnHeaderState());

            var treeView = new TinyBuildReportTreeView(state.TreeState, new TinyBuildReportTreeModel(registry, mainModule));
            AddElement(treeView);
        }
    }
}

