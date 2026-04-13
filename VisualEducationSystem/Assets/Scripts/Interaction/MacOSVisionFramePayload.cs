#nullable enable
using System;

namespace VisualEducationSystem.Interaction
{
    [Serializable]
    public sealed class MacOSVisionFramePayload
    {
        public bool hasUser;
        public int userId;
        public float userConfidence;
        public float userViewportX;
        public float userViewportY;
        public float userViewportWidth;
        public float userViewportHeight;
        public bool leftTracked;
        public float leftConfidence;
        public float leftViewportX;
        public float leftViewportY;
        public string leftGesture = string.Empty;
        public bool rightTracked;
        public float rightConfidence;
        public float rightViewportX;
        public float rightViewportY;
        public string rightGesture = string.Empty;
        public double timestamp;
    }
}
