namespace UnityEngine.Experimental.Input.Processors
{
    public class InvertVector3Processor : InputProcessor<Vector3>
    {
        public bool invertX = true;
        public bool invertY = true;
        public bool invertZ = true;

        public override Vector3 Process(Vector3 value, InputControl<Vector3> control)
        {
            if (invertX)
                value.x *= -1;
            if (invertY)
                value.y *= -1;
            if (invertZ)
                value.z *= -1;
            return value;
        }
    }
}
