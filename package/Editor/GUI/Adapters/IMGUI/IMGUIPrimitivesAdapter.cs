
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny
{
    internal sealed class IMGUIPrimitivesAdapter :
        IVisitValueAdapter<bool>
        , IVisitValueAdapter<sbyte>
        , IVisitValueAdapter<byte>
        , IVisitValueAdapter<short>
        , IVisitValueAdapter<ushort>
        , IVisitValueAdapter<int>
        , IVisitValueAdapter<uint>
        , IVisitValueAdapter<long>
        , IVisitValueAdapter<ulong>
        , IVisitValueAdapter<float>
        , IVisitValueAdapter<double>
        , IVisitValueAdapter<string>
    {
        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<bool> context)
            where TContainer : class, IPropertyContainer
                => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<sbyte> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<byte> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<short> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<ushort> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<int> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<uint> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<long> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<ulong> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<float> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<double> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<string> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<bool> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<sbyte> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<byte> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<short> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<ushort> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<int> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<uint> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<long> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<ulong> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<float> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<double> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<string> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);
    }
}
