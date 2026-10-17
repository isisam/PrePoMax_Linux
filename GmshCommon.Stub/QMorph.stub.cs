// Hand-written API-compatible stub of GmshCommon/QMorph.h (Q-Morph quad mesher wrapper; native lib ThirdParty/QuadMesher).
// QMorphMesh() in GmshAPI.cs is flagged "not offered yet" upstream, so this stays a stub until that feature ships.
#pragma warning disable CS1591
using System;
namespace GmshCommon
{
    public class QMorphFaceReport
    {
        public int Tag { get; set; }
        public bool QMorph { get; set; }
        public string Reason { get; set; }
        public int Background { get; set; }
        public int Triangles { get; set; }
        public int Quads { get; set; }
        public double P95Condition { get; set; }
        public double MaxCondition { get; set; }
        public double MinAngle { get; set; }
        public double MaxAngle { get; set; }
        public double Seconds { get; set; }
        public bool Periodic { get; set; }
        public bool Singular { get; set; }
    }
    public static class QMorph
    {
        public static int MakeFaceNodeCountsEven(int[] surfaceTags) { throw new NotSupportedException("GmshCommon stub: QMorph"); }
        public static void SetBackgroundAlgorithms(int[] surfaceTags) { throw new NotSupportedException("GmshCommon stub: QMorph"); }
        public static QMorphFaceReport[] QuadMeshSurfaces(int[] surfaceTags) { throw new NotSupportedException("GmshCommon stub: QMorph"); }
        public static QMorphFaceReport[] QuadMeshSurfaces(int[] surfaceTags, bool cleanup, int[] alternatives) { throw new NotSupportedException("GmshCommon stub: QMorph"); }
    }
}
