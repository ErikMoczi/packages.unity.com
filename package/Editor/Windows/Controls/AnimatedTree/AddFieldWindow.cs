
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Tiny
{
    internal class AddFieldWindow : TinyAnimatedTreeWindow<AddFieldWindow, TinyType>
    {
        private TinyType Type;
        private TinyField Field;
        private event Action<TinyField> OnCreateField;

        public static bool Show(Rect rect, IRegistry registry, TinyType type, TinyField field = null, Action<TinyField> onCreateField = null)
        {
            var window = GetWindow();
            if (null == window)
            {
                return false;
            }
            window.Type = type;
            window.Field = field;
            window.OnCreateField = onCreateField;
            return Show(rect, registry, true);
        }

        protected override void OnBeforePopulateMenu()
        {
            var builtin = TinyAnimatedTree.Element.MakeGroup("Built-in", "Built-in types that does not require any modules.", true);

            foreach (var type in TinyType.BuiltInTypes)
            {
                builtin.Add(TinyAnimatedTree.Element.MakeLeaf(type.Name, MakeTooltip(type), true, () => OnItemClicked(type)));
            }

            Tree.Add(builtin);
        }

        protected override IEnumerable<TinyType> GetItems(TinyModule module)
        {
            return module.Types.Deref(Registry).Where(t => t.IsStruct || t.IsPrimitive || t.IsEnum);
        }

        protected override void OnItemClicked(TinyType value)
        {
            if (null == Field)
            {
                Field = Type.CreateField(TinyId.New(), TinyUtility.GetUniqueName(Type.Fields, "NewField"), value.Ref, false);
                OnCreateField?.Invoke(Field);
            }
            else
            {
                Field.FieldType = value.Ref;
            }

            if (!TinyTypeValidation.Validate(Type.Registry.FindAllByType<TinyType>()))
            {
                // Default to int field
                Field.FieldType = TinyType.Int32.Ref;
            }
        }

        protected override string TreeName()
        {
            return "Tiny Fields";
        }
    }
}
