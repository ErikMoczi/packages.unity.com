using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Core2D.Camera2D
         },
         optionalGuids: new []
         {
             CoreGuids.Core2D.Camera2DAxisSort,
             CoreGuids.Core2D.Camera2DClippingPlanes
         }),
     UsedImplicitly]
    internal class CameraFamily : ComponentFamily
    {
        public override string Name => "Camera";
        public CameraFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext)
        {
        }
    }
}