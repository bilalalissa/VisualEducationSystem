#nullable enable
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public interface IWebcamRecognitionBackend
    {
        string BackendName { get; }
        string StatusSummary { get; }
        bool IsOperational { get; }
        bool TryRecognizeFrame(Texture sourceTexture, out WebcamRecognitionFrame frame);
    }
}
