

using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    internal class TinyTreeViewItem : TreeViewItem
    {
        /// <summary>
        /// Registry this item belongs to
        /// </summary>
        public IRegistry Registry { get; }
        
        /// <summary>
        /// The root module for this item
        /// </summary>
        public TinyModule.Reference MainModule { get; }
        
        /// <summary>
        /// The module that this item resides in
        /// </summary>
        public TinyModule.Reference Module { get; }
        
        public bool Editable { get; set; }

        protected TinyTreeViewItem(IRegistry registry, TinyModule.Reference mainModule, TinyModule.Reference module)
        {
            Registry = registry;
            MainModule = mainModule;
            Module = module;
        }
    }
}

