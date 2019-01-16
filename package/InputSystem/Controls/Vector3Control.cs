using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A floating-point 3D vector control composed of three <see cref="AxisControl">AxisControls</see>.
    /// </summary>
    public class Vector3Control : InputControl<Vector3>
    {
        [InputControl(offset = 0, displayName = "X")]
        public AxisControl x { get; private set; }
        [InputControl(offset = 4, displayName = "Y")]
        public AxisControl y { get; private set; }
        [InputControl(offset = 8, displayName = "Z")]
        public AxisControl z { get; private set; }

        public Vector3Control()
        {
            m_StateBlock.format = InputStateBlock.kTypeVector3;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            x = builder.GetControl<AxisControl>(this, "x");
            y = builder.GetControl<AxisControl>(this, "y");
            z = builder.GetControl<AxisControl>(this, "z");
            base.FinishSetup(builder);
        }

        public override unsafe Vector3 ReadUnprocessedValueFromState(void* statePtr)
        {
            return new Vector3(x.ReadValueFromState(statePtr), y.ReadValueFromState(statePtr), z.ReadValueFromState(statePtr));
        }

        public override unsafe void WriteValueIntoState(Vector3 value, void* statePtr)
        {
            x.WriteValueIntoState(value.x, statePtr);
            y.WriteValueIntoState(value.y, statePtr);
            z.WriteValueIntoState(value.z, statePtr);
        }

        public override unsafe float EvaluateMagnitude(void* statePtr)
        {
            ////REVIEW: this can go beyond 1; that okay?
            return ReadValueFromState(statePtr).magnitude;
        }
    }
}
