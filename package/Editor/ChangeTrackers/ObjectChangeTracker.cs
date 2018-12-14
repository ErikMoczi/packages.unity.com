

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal static class ObjectChangeTracker
    {
        public delegate void ChangeHandler(IEnumerable<Object> modifications);

        private static event ChangeHandler s_Handlers = delegate{};

        public static void AddHandler(ChangeHandler handler)
        {
            s_Handlers += handler;
        }

        public static void RemoveHandler(ChangeHandler handler)
        {
            s_Handlers -= handler;
        }

        [TinyInitializeOnLoad]
        private static void Init()
        {
            TinyEditorApplication.OnLoadProject += HandleProjectLoaded;
            TinyEditorApplication.OnCloseProject += HandleProjectUnloaded;
        }

        private static void HandleProjectLoaded(TinyProject project, TinyContext context)
        {
            Undo.postprocessModifications += HandlePostProcessModification;
        }

        private static void HandleProjectUnloaded(TinyProject project, TinyContext context)
        {
            Undo.postprocessModifications -= HandlePostProcessModification;
        }

        private static UndoPropertyModification[] HandlePostProcessModification(UndoPropertyModification[] mods)
        {
            var modifications = ListPool<UnityEngine.Object>.Get();
            try
            {
                modifications.AddRange(mods.Select(m => m.currentValue?.target).NotNull().Distinct());
                foreach (var handler in s_Handlers.GetInvocationList())
                {
                    try
                    {
                        handler.Method.Invoke(handler.Target, new object[] {modifications});
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
                return mods;
            }
            finally
            {
                ListPool<Object>.Release(modifications);
            }
        }
    }
}

