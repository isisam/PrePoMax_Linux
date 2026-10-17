using System;
using System.Drawing;
using System.Collections.Generic;
using CaeGlobals;

namespace vtkControl
{
    public class NullViewport3D : IViewport3D
    {
        public virtual double[][][] GetStlTriangles(vtkMaxActorData data)
        {
            throw new NotSupportedException("STL triangulation requires a viewport backend.");
        }

        public bool RenderingOn { get; set; }
        public bool UserPick { get; set; }
        public bool ViewAnimationEnabled { get; set; }
        public vtkEdgesVisibility EdgesVisibility { get; set; }
        public bool QuerySelectionInProgress { get; set; }
        public vtkSelectBy SelectBy { get; set; }
        public vtkSelectItem SelectItem { get; set; }

        public virtual void SetSelectBy(vtkSelectBy selectBy)
        {
        }

        public virtual void SetAnimationAcceleration(bool animationAcceleration)
        {
        }

        public virtual void SetSelectableActorsFilter(string[] selectableActors)
        {
        }

        public virtual void SetNavigationSensitivity(double rotateSensitivity, double zoomSensitivity, bool invertZoom, bool smoothZoom)
        {
        }

        public virtual void SetTrackpadNavigation(bool enabled, bool useWheelFallback, bool twoFingerDragPans, double panSensitivity, bool invertX, bool invertY)
        {
        }

        public virtual void StartBoxZoom()
        {
        }

        public bool IsBoxSelectionActive { get; }

        public virtual void Pick(int x1, int y1, bool boxSelection, int x2, int y2)
        {
        }

        public virtual int[][] GetVisualizationCellEdges(int numOfNodes)
        {
            return default(int[][]);
        }

        public virtual void GetClosestCellsOnActors(double[] point, string[] actorNames, out double[] distances, out double[][] normals)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual double GetSelectionPrecision()
        {
            return default(double);
        }

        public virtual void SetSelectionDistance(int distanceInPixels)
        {
        }

        public virtual void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, out int[] pointIds, out int[] cellIds)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void GetViewParameters(out double[] position, out double[] focalPoint, out double[] viewUp, out double parallelScale)
        {
            position = default(double[]);
            focalPoint = default(double[]);
            viewUp = default(double[]);
            parallelScale = default(double);
        }

        public virtual void SetViewParameters(bool animate, double[] position, double[] focalPoint, double[] viewUp, double parallelScale)
        {
        }

        public virtual void SetFrontBackView(bool animate, bool front)
        {
        }

        public virtual void SetTopBottomView(bool animate, bool top)
        {
        }

        public virtual void SetLeftRightView(bool animate, bool left)
        {
        }

        public virtual void SetAxisView(bool animate, double[] axis, bool zoomToFit = true)
        {
        }

        public virtual void SetNormalView(bool animate)
        {
        }

        public virtual void SetVerticalAxisView(bool animate, double[] axis, bool zoomToFit = true)
        {
        }

        public virtual void SetVerticalView(bool animate, bool updateView)
        {
        }

        public virtual double[] GetClosestUpView()
        {
            return default(double[]);
        }

        public virtual void SetIsometricView(bool animate, bool positive, bool zoomToFit = true)
        {
        }

        public virtual void SetZoomFactor(double factor)
        {
        }

        public virtual void AnimateZoomToFactor(double factor, int timeMs)
        {
        }

        public virtual void SetZoomToFit(bool animate)
        {
        }

        public virtual void SetBoxZoom(int[] point1, int[] point2, bool animate)
        {
        }

        public virtual void BoxZoomEnded()
        {
        }

        public virtual void SetRotationPoint(double[] rotationPoint)
        {
        }

        public virtual double[] GetViewPlaneNormal()
        {
            return default(double[]);
        }

        public virtual double GetAngle(double[] v1, double[] v2)
        {
            return default(double);
        }

        public virtual double[] Subtract(double[] v1, double[] v2)
        {
            return default(double[]);
        }

        public virtual void AdjustCameraDistanceAndClipping()
        {
        }

        public virtual void AdjustCameraDistanceAndClippingRedraw()
        {
        }

        public virtual void UpdateScalarsAndRedraw()
        {
        }

        public virtual void UpdateScalarsAndCameraAndRedraw()
        {
        }

        public virtual void Rotate(double azimuthAngle, double elevationAngle, double rollAngle)
        {
        }

        public virtual IViewportActor AddPoints(vtkMaxActorData data)
        {
            return default(IViewportActor);
        }

        public virtual IViewportActor AddCells(vtkMaxActorData data)
        {
            return default(IViewportActor);
        }

        public virtual void AddCells(vtkMaxActorData[] data)
        {
        }

        public virtual void AddSphereActor(vtkMaxActorData data, double symbolSize)
        {
        }

        public virtual void AddCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels, double symbolSize, vtkRendererLayer layer)
        {
        }

        public virtual void AddCaptionActor(string name, string caption, Color color, double[] position, double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer)
        {
        }

        public virtual void AddLabelActor(string name, double[][] positions, int[] labels, Color color, double fontScaleFactor, vtkRendererLayer layer)
        {
        }

        public virtual void AddLabelActor(string name, double[][] positions, string[] labels, Color color, double fontScaleFactor, vtkRendererLayer layer)
        {
        }

        public virtual void AddOrientedDisplacementConstraintActor(vtkMaxActorData data, double symbolSize)
        {
        }

        public virtual void AddOrientedConeActors(vtkMaxActorData data, double symbolSize)
        {
        }

        public virtual void AddOrientedRotationalConstraintActor(vtkMaxActorData data, double symbolSize)
        {
        }

        public virtual void AddOrientedJointActor(vtkMaxActorData data, double symbolSize, vtkJointSymbolEnum symbol)
        {
        }

        public virtual void AddOrientedArrowsActor(vtkMaxActorData data, double symbolSize, bool invert, double relativeSize = 1)
        {
        }

        public virtual void AddOrientedDoubleArrowsActor(vtkMaxActorData data, double symbolSize)
        {
        }

        public virtual void AddOrientedSpringActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
        }

        public virtual void AddOrientedThermoActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
        }

        public virtual void AddOrientedFluxActor(vtkMaxActorData data, double symbolSize, bool center, bool invert)
        {
        }

        public virtual void AddActor(IViewportActor actor)
        {
        }

        public virtual void HighlightActor(string actorName)
        {
        }

        public virtual void HideActors(string[] actorNames, bool updateColorContours)
        {
        }

        public virtual void ShowActors(string[] actorNames, bool updateColorContours)
        {
        }

        public virtual void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update)
        {
        }

        public virtual void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor, bool elementWise, bool hatch)
        {
        }

        public virtual void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor, bool elementWise, bool hatch)
        {
        }

        public virtual void RemoveSectionView(bool releaseVolumeGrids = false)
        {
        }

        public virtual void PreviewExplodedView(Dictionary<string, double[]> partOffsets, bool animate, int timeMs)
        {
        }

        public virtual void RemovePreviewedExplodedView(string[] partNames)
        {
        }

        public virtual void ApplyFollowerView(string[] partNames, int[] nodeIds)
        {
        }

        public virtual void UpdateFollowerView()
        {
        }

        public virtual void UpdateFollowerViewPint()
        {
        }

        public virtual void UpdateFollowerViewPlane()
        {
        }

        public virtual void RemoveFollowerView()
        {
        }

        public virtual void AddSymmetry(int symmetryPlane, double[] symmetryPoint)
        {
        }

        public virtual void AddLinearPattern(double[] displacement, int numOfItems)
        {
        }

        public virtual void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems)
        {
        }

        public virtual void ApplyTransforms()
        {
        }

        public virtual void RemoveTransformedActors()
        {
        }

        public virtual void HideTransformedActors()
        {
        }

        public virtual void ShowTransformedActors()
        {
        }

        public virtual void SetCoorSysVisibility(bool visibility)
        {
        }

        public virtual void ResetCoorSysWidgetPosition()
        {
        }

        public virtual void SetScaleWidgetVisibility(bool visibility)
        {
        }

        public virtual void SetScaleWidgetUnit(string unit)
        {
        }

        public virtual void InitializeResultWidgetPositions()
        {
        }

        public virtual void SetScalarBarColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
        {
        }

        public bool ScalarBarAddMinColor { get; }
        public bool ScalarBarAddMaxColor { get; }

        public virtual void SetScalarBarVisibility(bool visibility)
        {
        }

        public virtual bool TryGetScalarBarValueEditData(bool editMinimum, out Rectangle rectangle, out string valueText, out double minimum, out double maximum)
        {
            rectangle = default(Rectangle);
            valueText = default(string);
            minimum = default(double);
            maximum = default(double);
            return default(bool);
        }

        public virtual void SetScalarBarNumberFormat(string numberFormat)
        {
        }

        public virtual void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent, string minMaxType)
        {
        }

        public virtual void DrawScalarBarBackground(bool drawBackground)
        {
        }

        public virtual void DrawScalarBarBorder(bool drawBorder)
        {
        }

        public virtual void InitializeColorBarWidgetPosition()
        {
        }

        public virtual void SetColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
        }

        public virtual void AddColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
        }

        public virtual void DrawColorBarBackground(bool drawBackground)
        {
        }

        public virtual void DrawColorBarBorder(bool drawBorder)
        {
        }

        public virtual void HideColorBar()
        {
        }

        public virtual void SetStatusBlockVisibility(bool visibility)
        {
        }

        public virtual void SetStatusBlockNumberFormat(string numberFormat)
        {
        }

        public virtual void DrawStatusBlockBackground(bool drawBackground)
        {
        }

        public virtual void DrawStatusBlockBorder(bool drawBorder)
        {
        }

        public virtual void SetStatusBlock(string name, DateTime dateTime, float analysisTimeOrFrequency, string unit, string deformationVariable, float scaleFactor, vtkMaxFieldDataType fieldType, int stepNumber, int incrementNumber, int nodalDiameter)
        {
        }

        public virtual void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw)
        {
        }

        public virtual void SetLighting(double ambient, double diffuse, bool redraw)
        {
        }

        public virtual void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw)
        {
        }

        public virtual void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor)
        {
        }

        public virtual void SetMouseHighlightColor(Color mouseHighlightColor)
        {
        }

        public virtual void SetDrawSymbolEdges(bool drawSymbolEdges)
        {
        }

        public virtual void AddVectorFieldActor(vtkMaxActorData data, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
        }

        public virtual void AddTensorFieldActor(vtkMaxActorData data, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
        }

        public virtual bool AddAnimatedVectorFieldActor(vtkMaxActorData data, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
            return default(bool);
        }

        public virtual bool AddAnimatedTensorFieldActor(vtkMaxActorData data, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
            return default(bool);
        }

        public virtual void AddScalarFieldOnCells(vtkMaxActorData data, bool update)
        {
        }

        public virtual bool AddAnimatedScalarFieldOnCells(vtkMaxActorData data)
        {
            return default(bool);
        }

        public virtual bool AddAnimatedScalarFieldOnCellsAddRemoveActors(vtkMaxActorData data)
        {
            return default(bool);
        }

        public virtual bool AddAnimatedScalarFieldOnCellsAllActors(vtkMaxActorData data)
        {
            return default(bool);
        }

        public virtual void UpdateActorScalarField(string actorName, float[] values, NodesExchangeData extremeNodes, ResultFieldExchangeData resultField, bool update)
        {
        }

        public virtual void UpdateActorsColorContoursVisibility(string[] actorNames, bool colorContours)
        {
        }

        public virtual float GetAnimationScaleFactor(int frameNumber)
        {
            return default(float);
        }

        public virtual void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, int[] modeNumber, int[] nodalDiameter, float[] scale, double[] allFramesScalarRange, vtkMaxAnimationType animationType)
        {
        }

        public virtual void SetAnimationFrame(int frameNumber, bool scalarRangeFromAllFrames)
        {
        }

        public virtual bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value)
        {
            coordinates = default(double[]);
            value = default(float);
            return default(bool);
        }

        public virtual void SaveAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps, bool scalarRangeFromAllFrames, bool swing, bool encoderOptions)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void SaveAnimationAsImages(string fileName, int[] firstLastFrame, int step, bool scalarRangeFromAllFrames, bool swing)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void RenderToPNG(string fileName)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void RenderToPNG(string fileName, bool transparentBackground, int magnification)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void Clear()
        {
        }

        public virtual void ClearButKeepParts()
        {
        }

        public virtual void ClearSelection(bool clearAllMouseSelection = true)
        {
        }

        public virtual void ClearCurrentMouseSelection()
        {
        }

        public virtual void ClearCurrentMouseSelection(bool render)
        {
        }

        public virtual void ClearOverlay()
        {
        }

        public virtual void AddMeasurementAnnotation(string name, vtkMaxActorData nodeData, vtkMaxActorData lineData, double[][] arrowCoordinates, double[][] arrowNormals, double symbolSize, bool visible)
        {
        }

        public virtual void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates, double[][] arrowCoordinates, double[][] arrowNormals, bool visible)
        {
        }

        public virtual void UpdateOverlayPointsActor(string actorName, double[][] coordinates)
        {
        }

        public virtual void UpdateOverlayCaptionActorPosition(string actorName, double[] position)
        {
        }

        public virtual void RemoveMeasurementAnnotations(string[] names)
        {
        }

        public virtual void RemoveOverlayActors(string[] actorNames)
        {
        }

        public virtual void SetWidgetInteractionEnabled(bool enabled)
        {
        }

        public virtual void SetWidgetHighlightLocked(bool locked)
        {
        }

        public virtual void SetWidgetHighlightEnabled(bool enabled)
        {
        }

        public virtual void ResetScaleBarPosition()
        {
        }

        public virtual void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible)
        {
        }

        public virtual void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible, string[] animationActorNames)
        {
        }

        public virtual void UpdateArrowWidgetText(string name, string text)
        {
        }

        public virtual void UpdateArrowWidgetAnimationData(string name, string text, double[] anchorPoint)
        {
        }

        public virtual void ArrangeVisibleArrowWidgets()
        {
        }

        public virtual void RemoveAllArrowWidgets()
        {
        }

        public virtual void RemoveArrowWidgets(string[] widgetNames)
        {
        }

        public virtual void HideAllArrowWidgets()
        {
        }

        public virtual void SwitchLights()
        {
        }

        public virtual void InterpolateMeshData(PartExchangeData source, ref PartExchangeData input)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual bool ContainsActor(string actorName)
        {
            return default(bool);
        }

        public virtual void UpdateActorModelEdgesToDraw(string actorName, PartExchangeData modelEdgesToDraw)
        {
        }

        public virtual void UpdateActor(string oldName, string newName, Color newColor)
        {
        }

        public virtual void UpdateActorColor(IViewportActor actor, Color newColor)
        {
        }

        public virtual bool UpdateActorsColorTables(string[] actorNames, Color[][] colorTables)
        {
            return default(bool);
        }

        public virtual double[] GetBoundingBox()
        {
            return default(double[]);
        }

        public virtual double[] GetBoundingBoxSize()
        {
            return default(double[]);
        }

        public virtual double GetBoundingBoxDiagonal()
        {
            return default(double);
        }

        public virtual void CropPartWithCylinder(string partName, double r, string fileName)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void CropPartWithCube(string partName, double x, double y, double z, double a, string fileName)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void SmoothPart(string partName, double a, string fileName)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual vtkMaxActorData[] GetVtkMaxActorData()
        {
            return default(vtkMaxActorData[]);
        }

        public virtual void Export(string fileName)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void AddTetrahedronFaces(double[][] nodes, int[][] tetrahedrons, Color color, int faceId, vtkRendererLayer layer)
        {
        }

        public virtual void AnimateCamera(CameraState start, CameraState end, int timeMs = 500)
        {
        }

        public virtual void TransformAllMaxActors(double[] matrix, int numOfItems)
        {
        }
    }
}
