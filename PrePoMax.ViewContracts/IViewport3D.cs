using System;
using System.Drawing;
using System.Collections.Generic;
using CaeGlobals;

namespace vtkControl
{
    public interface IViewportActor
    {
        uint MemorySize { get; }

        int GetNumberOfElements();
        Color Color { get; set; }

        bool UseSecondaryHighlightColor { get; set; }
    }

    public sealed class CameraState
    {
        public double[] Position, FocalPoint, ViewUp;
        public double ParallelScale;
    }

    /// <summary>Portable viewport operations; native actor handles belong to the implementing viewport.</summary>
    public interface IViewport3D
    {
        void SetCompositeFibreActors(vtkMaxActorData[] data, CompositePlyLegendEntry[] legend = null);
        bool HasCompositeFibreActors { get; }
        event Action<double[], double[], double[][], bool, vtkSelectOperation, string[]> OnMouseLeftButtonUpSelection;
        event Action<double[]> RotationPointChanged;

        double[][][] GetStlTriangles(vtkMaxActorData data);
        bool RenderingOn { get; set; }

        bool UserPick { get; set; }

        bool ViewAnimationEnabled { get; set; }

        vtkEdgesVisibility EdgesVisibility { get; set; }

        bool QuerySelectionInProgress { get; set; }

        vtkSelectBy SelectBy { get; set; }

        vtkSelectItem SelectItem { get; set; }

        void SetSelectBy(vtkSelectBy selectBy);
        void SetAnimationAcceleration(bool animationAcceleration);
        void SetSelectableActorsFilter(string[] selectableActors);
        void SetNavigationSensitivity(double rotateSensitivity, double zoomSensitivity, bool invertZoom, bool smoothZoom);
        void SetTrackpadNavigation(bool enabled, bool useWheelFallback, bool twoFingerDragPans, double panSensitivity, bool invertX, bool invertY);
        void StartBoxZoom();
        bool IsBoxSelectionActive { get; }

        void Pick(int x1, int y1, bool boxSelection, int x2, int y2);
        int[][] GetVisualizationCellEdges(int numOfNodes);
        void GetClosestCellsOnActors(double[] point, string[] actorNames, out double[] distances, out double[][] normals);
        void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds);
        double GetSelectionPrecision();
        void SetSelectionDistance(int distanceInPixels);
        void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, out int[] pointIds, out int[] cellIds);
        void GetViewParameters(out double[] position, out double[] focalPoint, out double[] viewUp, out double parallelScale);
        void SetViewParameters(bool animate, double[] position, double[] focalPoint, double[] viewUp, double parallelScale);
        void SetFrontBackView(bool animate, bool front);
        void SetTopBottomView(bool animate, bool top);
        void SetLeftRightView(bool animate, bool left);
        void SetAxisView(bool animate, double[] axis, bool zoomToFit = true);
        void SetNormalView(bool animate);
        void SetVerticalAxisView(bool animate, double[] axis, bool zoomToFit = true);
        void SetVerticalView(bool animate, bool updateView);
        double[] GetClosestUpView();
        void SetIsometricView(bool animate, bool positive, bool zoomToFit = true);
        void SetZoomFactor(double factor);
        void AnimateZoomToFactor(double factor, int timeMs);
        void SetZoomToFit(bool animate);
        void SetBoxZoom(int[] point1, int[] point2, bool animate);
        void BoxZoomEnded();
        void SetRotationPoint(double[] rotationPoint);
        double[] GetViewPlaneNormal();
        double GetAngle(double[] v1, double[] v2);
        double[] Subtract(double[] v1, double[] v2);
        void AdjustCameraDistanceAndClipping();
        void AdjustCameraDistanceAndClippingRedraw();
        void UpdateScalarsAndRedraw();
        void UpdateScalarsAndCameraAndRedraw();
        void Rotate(double azimuthAngle, double elevationAngle, double rollAngle);
        IViewportActor AddPoints(vtkMaxActorData data);
        IViewportActor AddCells(vtkMaxActorData data);
        void AddCells(vtkMaxActorData[] data);
        void AddSphereActor(vtkMaxActorData data, double symbolSize);
        void AddCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels, double symbolSize, vtkRendererLayer layer);
        void AddCaptionActor(string name, string caption, Color color, double[] position, double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer);
        void AddLabelActor(string name, double[][] positions, int[] labels, Color color, double fontScaleFactor, vtkRendererLayer layer);
        void AddLabelActor(string name, double[][] positions, string[] labels, Color color, double fontScaleFactor, vtkRendererLayer layer);
        void AddOrientedDisplacementConstraintActor(vtkMaxActorData data, double symbolSize);
        void AddOrientedConeActors(vtkMaxActorData data, double symbolSize);
        void AddOrientedRotationalConstraintActor(vtkMaxActorData data, double symbolSize);
        void AddOrientedJointActor(vtkMaxActorData data, double symbolSize, vtkJointSymbolEnum symbol);
        void AddOrientedArrowsActor(vtkMaxActorData data, double symbolSize, bool invert, double relativeSize = 1);
        void AddOrientedDoubleArrowsActor(vtkMaxActorData data, double symbolSize);
        void AddOrientedSpringActor(vtkMaxActorData data, double symbolSize, bool invert);
        void AddOrientedThermoActor(vtkMaxActorData data, double symbolSize, bool invert);
        void AddOrientedFluxActor(vtkMaxActorData data, double symbolSize, bool center, bool invert);
        void AddActor(IViewportActor actor);
        void HighlightActor(string actorName);
        void HideActors(string[] actorNames, bool updateColorContours);
        void ShowActors(string[] actorNames, bool updateColorContours);
        void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update);
        void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor, bool elementWise, bool hatch);
        void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor, bool elementWise, bool hatch);
        void RemoveSectionView(bool releaseVolumeGrids = false);
        void PreviewExplodedView(Dictionary<string, double[]> partOffsets, bool animate, int timeMs);
        void RemovePreviewedExplodedView(string[] partNames);
        void ApplyFollowerView(string[] partNames, int[] nodeIds);
        void UpdateFollowerView();
        void UpdateFollowerViewPint();
        void UpdateFollowerViewPlane();
        void RemoveFollowerView();
        void AddSymmetry(int symmetryPlane, double[] symmetryPoint);
        void AddLinearPattern(double[] displacement, int numOfItems);
        void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems);
        void ApplyTransforms();
        void RemoveTransformedActors();
        void HideTransformedActors();
        void ShowTransformedActors();
        void SetCoorSysVisibility(bool visibility);
        void ResetCoorSysWidgetPosition();
        void SetScaleWidgetVisibility(bool visibility);
        void SetScaleWidgetUnit(string unit);
        void InitializeResultWidgetPositions();
        void SetScalarBarColorSpectrum(vtkMaxColorSpectrum colorSpectrum);
        bool ScalarBarAddMinColor { get; }

        bool ScalarBarAddMaxColor { get; }

        void SetScalarBarVisibility(bool visibility);
        bool TryGetScalarBarValueEditData(bool editMinimum, out Rectangle rectangle, out string valueText, out double minimum, out double maximum);
        void SetScalarBarNumberFormat(string numberFormat);
        void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent, string minMaxType);
        void DrawScalarBarBackground(bool drawBackground);
        void DrawScalarBarBorder(bool drawBorder);
        void InitializeColorBarWidgetPosition();
        void SetColorBarColorsAndLabels(Color[] colors, string[] labels);
        void AddColorBarColorsAndLabels(Color[] colors, string[] labels);
        void DrawColorBarBackground(bool drawBackground);
        void DrawColorBarBorder(bool drawBorder);
        void HideColorBar();
        void SetStatusBlockVisibility(bool visibility);
        void SetStatusBlockNumberFormat(string numberFormat);
        void DrawStatusBlockBackground(bool drawBackground);
        void DrawStatusBlockBorder(bool drawBorder);
        void SetStatusBlock(string name, DateTime dateTime, float analysisTimeOrFrequency, string unit, string deformationVariable, float scaleFactor, vtkMaxFieldDataType fieldType, int stepNumber, int incrementNumber, int nodalDiameter);
        void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw);
        void SetLighting(double ambient, double diffuse, bool redraw);
        void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw);
        void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor);
        void SetMouseHighlightColor(Color mouseHighlightColor);
        void SetDrawSymbolEdges(bool drawSymbolEdges);
        void AddVectorFieldActor(vtkMaxActorData data, double maxMagnitude, double maxArrowLength, double arrowSize);
        void AddTensorFieldActor(vtkMaxActorData data, double maxAbsValue, double maxGlyphLength, double arrowSize);
        bool AddAnimatedVectorFieldActor(vtkMaxActorData data, double maxMagnitude, double maxArrowLength, double arrowSize);
        bool AddAnimatedTensorFieldActor(vtkMaxActorData data, double maxAbsValue, double maxGlyphLength, double arrowSize);
        void AddScalarFieldOnCells(vtkMaxActorData data, bool update);
        bool AddAnimatedScalarFieldOnCells(vtkMaxActorData data);
        bool AddAnimatedScalarFieldOnCellsAddRemoveActors(vtkMaxActorData data);
        bool AddAnimatedScalarFieldOnCellsAllActors(vtkMaxActorData data);
        void UpdateActorScalarField(string actorName, float[] values, NodesExchangeData extremeNodes, ResultFieldExchangeData resultField, bool update);
        void UpdateActorsColorContoursVisibility(string[] actorNames, bool colorContours);
        float GetAnimationScaleFactor(int frameNumber);
        void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, int[] modeNumber, int[] nodalDiameter, float[] scale, double[] allFramesScalarRange, vtkMaxAnimationType animationType);
        void SetAnimationFrame(int frameNumber, bool scalarRangeFromAllFrames);
        bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value);
        void SaveAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps, bool scalarRangeFromAllFrames, bool swing, bool encoderOptions);
        void SaveAnimationAsImages(string fileName, int[] firstLastFrame, int step, bool scalarRangeFromAllFrames, bool swing);
        void RenderToPNG(string fileName);
        void RenderToPNG(string fileName, bool transparentBackground, int magnification);
        void Clear();
        void ClearButKeepParts();
        void ClearSelection(bool clearAllMouseSelection = true);
        void ClearCurrentMouseSelection();
        void ClearCurrentMouseSelection(bool render);
        void ClearOverlay();
        void AddMeasurementAnnotation(string name, vtkMaxActorData nodeData, vtkMaxActorData lineData, double[][] arrowCoordinates, double[][] arrowNormals, double symbolSize, bool visible);
        void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates, double[][] arrowCoordinates, double[][] arrowNormals, bool visible);
        void UpdateOverlayPointsActor(string actorName, double[][] coordinates);
        void UpdateOverlayCaptionActorPosition(string actorName, double[] position);
        void RemoveMeasurementAnnotations(string[] names);
        void RemoveOverlayActors(string[] actorNames);
        void SetWidgetInteractionEnabled(bool enabled);
        void SetWidgetHighlightLocked(bool locked);
        void SetWidgetHighlightEnabled(bool enabled);
        void ResetScaleBarPosition();
        void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible);
        void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible, string[] animationActorNames);
        void UpdateArrowWidgetText(string name, string text);
        void UpdateArrowWidgetAnimationData(string name, string text, double[] anchorPoint);
        void ArrangeVisibleArrowWidgets();
        void RemoveAllArrowWidgets();
        void RemoveArrowWidgets(string[] widgetNames);
        void HideAllArrowWidgets();
        void SwitchLights();
        void InterpolateMeshData(PartExchangeData source, ref PartExchangeData input);
        bool ContainsActor(string actorName);
        void UpdateActorModelEdgesToDraw(string actorName, PartExchangeData modelEdgesToDraw);
        void UpdateActor(string oldName, string newName, Color newColor);
        void UpdateActorColor(IViewportActor actor, Color newColor);
        bool UpdateActorsColorTables(string[] actorNames, Color[][] colorTables);
        double[] GetBoundingBox();
        double[] GetBoundingBoxSize();
        double GetBoundingBoxDiagonal();
        void CropPartWithCylinder(string partName, double r, string fileName);
        void CropPartWithCube(string partName, double x, double y, double z, double a, string fileName);
        void SmoothPart(string partName, double a, string fileName);
        vtkMaxActorData[] GetVtkMaxActorData();
        void Export(string fileName);
        void AddTetrahedronFaces(double[][] nodes, int[][] tetrahedrons, Color color, int faceId, vtkRendererLayer layer);
        void AnimateCamera(CameraState start, CameraState end, int timeMs = 500);
        void TransformAllMaxActors(double[] matrix, int numOfItems);
    }
}
