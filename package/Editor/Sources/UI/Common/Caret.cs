using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class CaretFactory : UxmlFactory<Caret>
    {
        protected override Caret DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new Caret();
        }
    }

    internal class Caret : Label
    {
        private static Vector2 CaretSize = new Vector2(9, 16);

        public Caret()
        {
            AddToClassList("caret");
        }

        public void SetState(bool collapsed)
        {
            if (collapsed)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                transform.position = new Vector3(0, 0, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 90);
                transform.position = new Vector3((CaretSize.y / 2f) + (CaretSize.x / 2f), CaretSize.x / 2f, 0);
            }            
        }
    }
}
