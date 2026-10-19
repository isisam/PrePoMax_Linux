using System;
using System.Drawing;
using Kitware.VTK;

namespace vtkControl
{
    public partial class vtkControl
    {
        private readonly System.Collections.Generic.List<vtkMaxActor> _fibreActors = new System.Collections.Generic.List<vtkMaxActor>();
        public bool HasCompositeFibreActors { get { return _fibreActors.Count > 0; } }
        private readonly System.Collections.Generic.Dictionary<vtkProperty, double> _fibreOpacity = new System.Collections.Generic.Dictionary<vtkProperty, double>();

        private void ApplyCompositeFibreOpacity()
        {
            foreach (var item in _fibreOpacity) item.Key.SetOpacity(Math.Min(item.Value, .16));
        }

        // Transient world-space actors, deliberately outside model/selection actors.
        // Preserve the original opacity, including user-defined transparency.
        public void SetCompositeFibreActors(vtkMaxActorData[] data)
        {
            foreach (var actor in _fibreActors)
            {
                _renderer.RemoveActor(actor.Geometry);
                ReleaseActorGraphicsResources(actor);
            }
            _fibreActors.Clear();
            foreach (var item in _fibreOpacity) item.Key.SetOpacity(item.Value);
            _fibreOpacity.Clear();
            if (data != null && data.Length > 0)
            {
                foreach (var actor in _actors.Values)
                {
                    foreach (var prop in new[] { actor.GeometryProperty, actor.ElementEdgesProperty, actor.ModelEdgesProperty })
                    {
                        if (prop == null || _fibreOpacity.ContainsKey(prop)) continue;
                        _fibreOpacity.Add(prop, prop.GetOpacity());
                        prop.SetOpacity(Math.Min(prop.GetOpacity(), .16));
                    }
                }
                foreach (var item in data)
                {
                    var actor = new vtkMaxActor(item);
                    actor.Geometry.PickableOff();
                    _renderer.AddActor(actor.Geometry);
                    _fibreActors.Add(actor);
                }
            }
            RenderScene();
        }
        public double[][][] GetStlTriangles(vtkMaxActorData data)
        {
            return data.GetStlTriangles();
        }

        IViewportActor IViewport3D.AddPoints(vtkMaxActorData data)
        {
            return AddPoints(data);
        }

        IViewportActor IViewport3D.AddCells(vtkMaxActorData data)
        {
            return AddCells(data);
        }

        void IViewport3D.AddActor(IViewportActor actor)
        {
            AddActor((vtkMaxActor)actor);
        }

        void IViewport3D.UpdateActorColor(IViewportActor actor, Color color)
        {
            UpdateActorColor((vtkMaxActor)actor, color);
        }

        public void AnimateCamera(CameraState start, CameraState end, int timeMs = 500)
        {
            using (var a = vtkCamera.New())
            using (var b = vtkCamera.New())
            {
                a.SetPosition(start.Position[0], start.Position[1], start.Position[2]);
                a.SetFocalPoint(start.FocalPoint[0], start.FocalPoint[1], start.FocalPoint[2]);
                a.SetViewUp(start.ViewUp[0], start.ViewUp[1], start.ViewUp[2]);
                a.SetParallelScale(start.ParallelScale);
                b.SetPosition(end.Position[0], end.Position[1], end.Position[2]);
                b.SetFocalPoint(end.FocalPoint[0], end.FocalPoint[1], end.FocalPoint[2]);
                b.SetViewUp(end.ViewUp[0], end.ViewUp[1], end.ViewUp[2]);
                b.SetParallelScale(end.ParallelScale);
                AnimateCamera(a, b, _renderer.GetActiveCamera(), timeMs);
            }
        }

        public void TransformAllMaxActors(double[] matrix, int numOfItems)
        {
            if (matrix == null || matrix.Length != 16)
                throw new ArgumentException("Expected a 4x4 matrix.", nameof(matrix));
            using (var transform = vtkTransform.New())
            using (var nativeMatrix = vtkMatrix4x4.New())
            {
                for (int row = 0; row < 4; row++)
                    for (int col = 0; col < 4; col++)
                        nativeMatrix.SetElement(row, col, matrix[4 * row + col]);
                transform.SetMatrix(nativeMatrix);
                TransformAllMaxActors(transform, numOfItems);
            }
        }
    }
}
