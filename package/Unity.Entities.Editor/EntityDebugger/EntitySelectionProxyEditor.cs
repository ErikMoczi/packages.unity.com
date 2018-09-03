using UnityEditor;

namespace Unity.Entities.Editor
{
    [CustomEditor(typeof(EntitySelectionProxy))]
    public class EntitySelectionProxyEditor : UnityEditor.Editor
    {
        private EntityIMGUIVisitor visitor;
        
        void OnEnable()
        {
            visitor = new EntityIMGUIVisitor();
        }

        public override void OnInspectorGUI()
        {
            var targetProxy = (EntitySelectionProxy) target;
            if (!targetProxy.Exists)
                return;
            targetProxy.Container.PropertyBag.VisitStruct(ref targetProxy.Container, visitor);
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }
    }
}
