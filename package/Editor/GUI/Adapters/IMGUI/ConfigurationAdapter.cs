
using System.Linq;
using Unity.Properties;
using UnityEditor;

namespace Unity.Tiny
{
    internal class ConfigurationAdapter : TinyAdapter
        ,IVisitValueAdapter<TinyEntity>
    {
        private ICustomEditorManagerInternal CustomEditors;
        
        public ConfigurationAdapter(TinyContext tinyContext)
            :base(tinyContext)
        {
            CustomEditors = tinyContext.GetManager<ICustomEditorManagerInternal>();
        }

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context)
            where TContainer : class, IPropertyContainer
            => DrawEntityHeader(ref context);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context)
            where TContainer : struct, IPropertyContainer
            => DrawEntityHeader(ref context);

        private bool DrawEntityHeader(ref UIVisitContext<TinyEntity> context)
        {
            var entity = context.Value;
            for (var i = 0; i < entity.Components.Count; ++i)
            {
                var component = context.Value.Components[i];
                var owningModule = TinyContext.Registry.CacheManager.GetModuleOf(component.Type);

                var project = TinyContext.Registry.FindAllByType<TinyProject>().FirstOrDefault();
                if (!project.Module.Dereference(project.Registry).EnumerateDependencies().Contains(owningModule))
                {
                    continue;
                }

                EditorGUI.indentLevel += 2;
                try
                {
                    var key = "Tiny_Configuration_" + component.Type.Id;
                    var showComponent = EditorPrefs.GetBool(key, false);
                    showComponent = EditorGUILayout.Foldout(showComponent, component.Type.Name);
                    if (showComponent)
                    {
                        EditorPrefs.SetBool(key, true);
                        ++EditorGUI.indentLevel;
                        try
                        {
                            var editor = CustomEditors.GetEditor(component.Type);

                            var uiContext = new UIVisitContext<TinyObject>(
                                new VisitContext<TinyObject>
                                    {Index = i, Value = component, Property = TinyEntity.ComponentsProperty},
                                context.Visitor,
                                context.Targets);
                            editor.Visit(ref uiContext);
                        }
                        finally
                        {
                            --EditorGUI.indentLevel;
                        }
                    }
                    else
                    {
                        EditorPrefs.DeleteKey(key);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel -= 2;
                }
            }
            return context.Visitor.StopVisit;
        }
    }
}
