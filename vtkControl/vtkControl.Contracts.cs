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
        private readonly System.Collections.Generic.Dictionary<vtkProperty, vtkProperty> _fibreProperties = new System.Collections.Generic.Dictionary<vtkProperty, vtkProperty>();
        private CompositePlyLegendEntry[] _fibreLegend;
        private string _fibreLegendTitle = "Ply fibre directions";
        private string _fibreMirrorText = "Mirrors share colours";
        private vtkActor2D _fibreLegendActor;
        private vtkImageData _fibreLegendImage;
        private vtkImageMapper _fibreLegendMapper;
        private vtkCoordinate _fibreLegendAnchor;

        public void SetCompositeFibreLegendText(string title, string mirrorText)
        {
            _fibreLegendTitle = title;
            _fibreMirrorText = mirrorText;
            UpdateCompositeFibreLegend();
            if (_fibreLegend != null) RenderScene();
        }

        private void ClearCompositeFibreLegend()
        {
            if (_fibreLegendActor != null) { _overlayRenderer.RemoveActor2D(_fibreLegendActor); _fibreLegendActor.Dispose(); }
            _fibreLegendActor = null;
            if (_fibreLegendMapper != null) _fibreLegendMapper.Dispose();
            _fibreLegendMapper = null;
            if (_fibreLegendImage != null) _fibreLegendImage.Dispose();
            _fibreLegendImage = null;
            if (_fibreLegendAnchor != null) _fibreLegendAnchor.Dispose();
            _fibreLegendAnchor = null;
        }

        private void UpdateCompositeFibreLegend()
        {
            ClearCompositeFibreLegend();
            if (_fibreLegend == null || _fibreLegend.Length == 0) return;
            int rows = Math.Min(12, _fibreLegend.Length), columns = (_fibreLegend.Length + rows - 1) / rows;
            int width = 246 * columns, height = 36 + rows * 23 + (_fibreLegend[0].Symmetric ? 24 : 0);
            // GDI renders Unicode (including zh-TW, minus and degree signs) into
            // native VTK image data. The legend is inside the viewport framebuffer.
            using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bitmap))
            using (var normal = new Font("Microsoft JhengHei UI", 10, FontStyle.Regular))
            using (var bold = new Font(normal, FontStyle.Bold))
            {
                g.Clear(Color.FromArgb(250, 252, 253));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawRectangle(Pens.LightSlateGray, 0, 0, width - 1, height - 1);
                g.DrawString(_fibreLegendTitle, bold, Brushes.Black, 10, 7);
                for (int i = 0; i < _fibreLegend.Length; i++)
                {
                    var entry = _fibreLegend[i];
                    int x = 10 + (i / rows) * 246, y = 34 + (i % rows) * 23;
                    if (entry.Selected) g.FillRectangle(Brushes.LightGray, x - 3, y - 1, 228, 23);
                    g.DrawString(entry.Label, entry.Selected ? bold : normal, Brushes.Black, x, y);
                    using (var brush = new SolidBrush(entry.Color)) g.FillRectangle(brush, x + 200, y + 3, 15, 15);
                }
                if (_fibreLegend[0].Symmetric) g.DrawString(_fibreMirrorText, normal, Brushes.DimGray, 10, height - 23);
                _fibreLegendImage = vtkImageData.New();
                _fibreLegendImage.SetDimensions(width, height, 1);
                _fibreLegendImage.SetScalarTypeToUnsignedChar();
                _fibreLegendImage.SetNumberOfScalarComponents(4);
                _fibreLegendImage.AllocateScalars();
                var bits = bitmap.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                           System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    byte[] pixels = new byte[width * height * 4], row = new byte[width * 4];
                    for (int y = 0; y < height; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(bits.Scan0 + y * bits.Stride, row, 0, row.Length);
                        int offset = (height - 1 - y) * row.Length;
                        for (int x = 0; x < width; x++)
                        {
                            pixels[offset + x * 4] = row[x * 4 + 2];
                            pixels[offset + x * 4 + 1] = row[x * 4 + 1];
                            pixels[offset + x * 4 + 2] = row[x * 4];
                            pixels[offset + x * 4 + 3] = row[x * 4 + 3];
                        }
                    }
                    System.Runtime.InteropServices.Marshal.Copy(pixels, 0, _fibreLegendImage.GetScalarPointer(), pixels.Length);
                }
                finally { bitmap.UnlockBits(bits); }
            }
            _fibreLegendMapper = vtkImageMapper.New();
            _fibreLegendMapper.SetInput(_fibreLegendImage);
            _fibreLegendMapper.SetColorWindow(255); _fibreLegendMapper.SetColorLevel(127.5);
            _fibreLegendActor = vtkActor2D.New();
            _fibreLegendActor.SetMapper(_fibreLegendMapper);
            _fibreLegendActor.PickableOff();
            _fibreLegendAnchor = vtkCoordinate.New();
            _fibreLegendAnchor.SetCoordinateSystemToNormalizedViewport();
            _fibreLegendAnchor.SetValue(1, 1);
            var position = _fibreLegendActor.GetPositionCoordinate();
            position.SetCoordinateSystemToDisplay(); position.SetReferenceCoordinate(_fibreLegendAnchor);
            position.SetValue(-width - 14, -height - 14);
            _overlayRenderer.AddActor2D(_fibreLegendActor);
        }

        private void ApplyCompositeFibreOpacity()
        {
            foreach (var item in _fibreOpacity)
            {
                item.Key.SetOpacity(Math.Min(item.Value, .12));
                item.Key.SetColor(.65, .65, .65);
                item.Key.SetRepresentationToWireframe();
                item.Key.SetLineWidth(1);
            }
        }

        // Transient world-space actors, deliberately outside model/selection actors.
        // Preserve the original opacity, including user-defined transparency.
        public void SetCompositeFibreActors(vtkMaxActorData[] data, CompositePlyLegendEntry[] legend = null)
        {
            _fibreLegend = data == null ? null : legend;
            UpdateCompositeFibreLegend();
            foreach (var actor in _fibreActors)
            {
                _renderer.RemoveActor(actor.Geometry);
                ReleaseActorGraphicsResources(actor);
            }
            _fibreActors.Clear();
            foreach (var item in _fibreProperties) { item.Key.DeepCopy(item.Value); item.Value.Dispose(); }
            _fibreProperties.Clear();
            _fibreOpacity.Clear();
            if (data != null && data.Length > 0)
            {
                foreach (var actor in _actors.Values)
                {
                    foreach (var prop in new[] { actor.GeometryProperty, actor.ElementEdgesProperty, actor.ModelEdgesProperty })
                    {
                        if (prop == null || _fibreOpacity.ContainsKey(prop)) continue;
                        _fibreOpacity.Add(prop, prop.GetOpacity());
                        var saved = vtkProperty.New(); saved.DeepCopy(prop); _fibreProperties.Add(prop, saved);
                    }
                }
                ApplyCompositeFibreOpacity();
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
