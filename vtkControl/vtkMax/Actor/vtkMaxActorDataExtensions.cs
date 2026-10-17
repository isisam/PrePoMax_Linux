namespace vtkControl
{
    public static class vtkMaxActorDataExtensions
    {
        public static double[][][] GetStlTriangles(this vtkMaxActorData data)
        {
            var actor = new vtkMaxActor(data);
            return actor.GetStlTriangles();
        }
    }
}
