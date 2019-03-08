using System;
using UnityEngine;

namespace UnityEngine.XR.MagicLeap
{
    public interface IWarning
    {
        string Message { get; }
        bool Triggered { get; }

        void Reset();
    }

    public class MLWarning : IWarning
    {
        public string Message { get; private set; }
        public bool Triggered { get; private set; }

        public MLWarning(string msg)
        {
            Message = msg;
            Reset();
        }

        public void Reset()
        {
            Triggered = false;
        }

        public void Trigger()
        {
            if (!Triggered)
            {
                Debug.LogWarning(Message);
                Triggered = true;
            }
        }
    }

    public class MLFormattedWarning : IWarning
    {
        public string Message { get; private set; }
        public bool Triggered { get; private set; }

        public MLFormattedWarning(string msg)
        {
            Message = msg;
            Reset();
        }

        public void Reset()
        {
            Triggered = false;
        }

        public void Trigger(params object[] args)
        {
            if (!Triggered)
            {
                var msg = string.Format(Message, args);
                Debug.LogWarning(msg);
                Triggered = true;
            }
        }
    }

    public class MLConditionalWarning : IWarning
    {
        public bool Flag { get; set; }
        public string Message { get { return _MsgFunc(Flag); } }
        public bool Triggered { get; private set; }

        private Func<bool, string> _MsgFunc;

        public MLConditionalWarning(Func<bool, string> msgFunc)
        {
            _MsgFunc = msgFunc;
            Flag = false;
            Reset();
        }

        public void Reset()
        {
            Triggered = false;
        }

        public void Trigger(bool flag, params object[] args)
        {
            if (!Triggered)
            {
                Flag = flag;
                var msg = string.Format(Message, args);
                Debug.LogWarning(msg);
                Triggered = true;
            }
        }
    }

    public static class MLWarnings
    {
        private const string _WarnedAboutClearColorMsg = @"
Camera not cleared to transparent black. Please correct for improved performance.";
        private const string _WarnedAboutFarClippingPlaneMsg = @"
ML: Far clip distance {0} meters is greater than {1} meter maximum. Setting to maximum.";
        private const string _WarnedAboutNearClippingPlaneMsg = @"
ML: Near clip distance {0} meters is smaller than {1} meter minimum. Setting to minimum.";
        private const string _WarnedAboutNonUniformScaleMsg = @"
ML: The main camera's parent in the game object hierarchy has a non-uniform scale:
x, y and z scale values are not all the same. Any anscestor of the camera can change the scale
so go up the game object hierarchy and verify that no anscestor is unexpectedly changing
the scale differently for x, y, or z. Falling back on average scale for graphics. This may result
in unexpected results.";
        private const string _WarnedAboutStereoConvergenceMsg = @"
Stereo Convergence set as {0}, which is closer than the {1} near clip distance. Clamping to near clip distance.";
        private const string _WarnedAboutStereoConvergencePointMsg = @"
Stereo Convergence Point set at {0}, which is closer than the {1} near clip distance. Clamping to near clip distance.";

        public static readonly MLWarning WarnedAboutClearColor = new MLWarning(_WarnedAboutClearColorMsg);
        public static readonly MLFormattedWarning WarnedAboutFarClippingPlane = new MLFormattedWarning(_WarnedAboutFarClippingPlaneMsg);
        public static readonly MLFormattedWarning WarnedAboutNearClippingPlane = new MLFormattedWarning(_WarnedAboutNearClippingPlaneMsg);
        public static readonly MLWarning WarnedAboutNonUniformScale = new MLWarning(_WarnedAboutNonUniformScaleMsg);

        public static readonly MLConditionalWarning WarnedAboutSteroConvergence =
            new MLConditionalWarning((point) => point
                ? _WarnedAboutStereoConvergencePointMsg
                : _WarnedAboutStereoConvergenceMsg );
    }

    // TODO
    /*
    WarningStringMsg(
            "To use Magic Leap Zero Iteration mode, the editor must be running under OpenGL.\n"
            " 1) Go to Edit -> Project Settings -> Player, and select the first tab there (standalone settings)...\n"
            " 2) Open the 'Other Settings' section, and uncheck 'Auto Graphics API for %s' An API list will appear.\n"
            " 3) Use the plus button to add 'OpenGLCore' to the API list.\n"
            " 4) Drag it to the top of the list. The editor will now switch to using OpenGL.\n",
            platformName);

    WarningStringMsg(
            "To use Magic Leap Zero Iteration mode in the editor, the SDK path must be set in the Lumin build settings.\n"
            " 1) Use the Magic Leap Package Manager to locate your Lumin SDK. With the SDK highlighted, click `Open Folder` to see where it is...\n"
            " 2) Go to File -> Build Settings, and select to the 'Lumin OS' platform.\n"
            " 3) Set 'Lumin SDK Location' to the SDK folder.\n"
            );

    WarningStringMsg(
            "To use Magic Leap Zero Iteration mode in the editor, you must start the Magic Leap Remote Server...\n"
            " 1) Locate your Lumin SDK (according to the build settings, it is currently at '%s').\n"
            " 2) In the VirtualDevice folder, run MLRemote.\n"
            " 3) Using the Magic Leap Remote Server window, start the server (either simulator or device).\n",
            sdkPath.c_str());
     */
}