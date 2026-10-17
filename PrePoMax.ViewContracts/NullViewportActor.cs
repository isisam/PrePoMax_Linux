using System.Drawing;

namespace vtkControl
{
    public sealed class NullViewportActor : IViewportActor
    {
        public uint MemorySize => 0;

        public int GetNumberOfElements() => 0;
        public Color Color { get; set; }
        public bool UseSecondaryHighlightColor { get; set; }
    }
}
