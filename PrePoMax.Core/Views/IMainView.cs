using System;
using System.Collections.Generic;
using System.Drawing;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeResults;
using CaeJob;
using UserControls;
using vtkControl;

namespace PrePoMax
{
    /// <summary>Host operations used by the controller, Python gateways and automation server.</summary>
    public interface IMainView
    {
        Controller Controller { get; set; }

        void SetCurrentView(ViewGeometryModelResults view);
        void SetCurrentEdgesVisibilities(vtkControl.vtkEdgesVisibility edgesVisibility);
        bool ScreenUpdating { get; set; }

        bool RenderingOn { get; set; }

        bool LastOpenResultsAccepted { get; }

        void Open(string fileName, Action<string, string> ActionOnFile, bool resetCamera = true, string parameters = null);
        void UpdateRecentFilesThreadSafe();
        void EnableDisableUndoRedo(string undo, string redo);
        SidePanelsState GetSidePanelsState();
        void SetPendingSidePanelsState(SidePanelsState state);
        void SetRotationPoint(double[] rotationPoint);
        void UpdateUserViewsThreadSafe(string[] viewNames);
        void SetAnnotateWithColor(AnnotateWithColorEnum annotateWithColor);
        MeshSetupItem EditMeshingParametersFromHistory(MeshSetupItem meshSetupItem);
        void ShowSplitMeshResults();
        void SaveAutomationScreenshot(string fileName, bool transparentBackground, int magnification);
        bool IsWindowHidden { get; }

        string AutomationModelFileName { get; }

        void RequestAutomationExit();
        void PrepareAutomationPythonEngine();
        void EchoAutomationExecution(string label, string source, string output, string error);
        string GetAutomationBusyReason();
        void SelectResultsUnitSystem(string parameters = null);
        void UpdateUnitSystem(UnitSystem unitSystem);
        void RunHistoryPostprocessing();
        void RunHistoryPostprocessing(bool? answerToAsk);
        void OpenAnalysisResults(string jobName, bool asynchronous = true);
        void UpdateAnalysisProgress(string jobName, JobStatus jobStatus);
        AnalysisJob GetDefaultJob();
        string GetDefaultJobName();
        void SelectionChanged(int[] ids = null);
        void SetQuerySelection(bool querySelection);
        bool GetQuerySelection();
        void SetSelectBy(vtkSelectBy selectBy);
        void SetSelectItem(vtkSelectItem selectItem);
        void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds, string[] selectionPartNames = null);
        void GetClosestCellsOnParts(double[] point, string[] partNames, out double[] distances, out double[][] normals);
        double GetSelectionPrecision();
        void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, string[] selectionPartNames, out int[] pointIds, out int[] cellIds);
        void CloseAllForms();
        void SelectOneNameInSymbolsNames(string name);
        void SetResultNames();
        void SetCurrentResults(string resultsName);
        string GetDeformationVariable();
        DeformationScaleFactorTypeEnum GetDeformationType();
        float GetDeformationFactor();
        void SetDeformationType(DeformationScaleFactorTypeEnum deformationType);
        void SetDeformationFactor(float deformationFactor);
        ComplexResultTypeEnum GetComplexResultType();
        double GetComplexAngleDeg();
        void SetStateReady(string currentText);
        bool SetStateWorking(string text, bool showCancelButton = false);
        bool IsStateOpening();
        void SetTitle(string title);
        string GetFileNameToOpen(string title);
        string GetFileNameToOpen(string title, string filter);
        string GetFileNameToImport(string title, bool onlyMaterials);
        string GetFileNameToImport(string title, string filter);
        string GetFileNameToSaveAs();
        void ClearControls();
        void ClearResults();
        void Clear3D();
        void ClearButKeepParts();
        void Clear3DSelection();
        void ClearActiveTreeSelection();
        void GetViewParameters(out double[] position, out double[] focalPoint, out double[] viewUp, out double parallelScale);
        void SetViewParameters(bool animate, double[] position, double[] focalPoint, double[] viewUp, double parallelScale);
        void SetFrontBackView(bool animate, bool front);
        void SetStandardView(string orientation, bool animate);
        void SetZoomToFit(bool animate);
        void Rotate(double azimuthAngle, double elevationAngle, double rollAngle);
        double[] GetViewPlaneNormal();
        void AdjustCameraDistanceAndClipping();
        void UpdateScalarsAndRedraw();
        void UpdateScalarsAndCameraAndRedraw();
        vtkMaxActorData[] GetVtkMaxActorData();
        double[] GetClosestUpView();
        void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor, bool elementWise, bool hatch);
        void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor, bool elementWise, bool hatch);
        void RemoveSectionView(bool releaseVolumeGrids = false);
        void PreviewExplodedView(Dictionary<string, double[]> partOffsets, bool animate, int timeMs);
        void RemovePreviewedExplodedView(string[] partNames);
        void SetExplodedViewStatus(bool status);
        void ApplyFollowerView(string[] partName, int[] nodeIds);
        void UpdateFollowerView();
        void RemoveFollowerView();
        void AddSymmetry(int symmetryPlane, double[] symmetryPoint);
        void AddLinearPattern(double[] displacement, int numOfItems);
        void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems);
        void ApplyTransformations();
        void SetTransformationsStatus(bool status);
        void HideTransformedActors();
        IViewportActor Add3DNodes(vtkMaxActorData nodeData);
        IViewportActor Add3DCells(vtkMaxActorData cellData);
        void Add3DCells(vtkMaxActorData[] cellData);
        void AddActor(IViewportActor actor);
        void AddScalarFieldOn3DCells(vtkMaxActorData actorData, bool update);
        void AddVectorFieldActor(vtkMaxActorData actorData, double maxMagnitude, double maxArrowLength, double arrowSize);
        void AddTensorFieldActor(vtkMaxActorData actorData, double maxAbsValue, double maxGlyphLength, double arrowSize);
        bool AddAnimatedVectorFieldActor(vtkMaxActorData actorData, double maxMagnitude, double maxArrowLength, double arrowSize);
        bool AddAnimatedTensorFieldActor(vtkMaxActorData actorData, double maxAbsValue, double maxGlyphLength, double arrowSize);
        bool AddAnimatedScalarFieldOn3DCells(vtkMaxActorData actorData);
        void UpdateActorSurfaceScalarField(string actorName, float[] values, NodesExchangeData extremeNodes, ResultFieldExchangeData resultField, bool update);
        void UpdateActorColorContoursVisibility(string[] actorNames, bool colorContour);
        void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update);
        void AddSphereActor(vtkMaxActorData actorData, double symbolSize);
        void AddCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels, double symbolSize, vtkRendererLayer layer);
        void AddCaptionActor(string name, string caption, Color color, double[] position, double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer);
        void AddLabelActor(string name, double[][] positions, int[] labels, Color color, double fontScaleFactor, vtkRendererLayer layer);
        void AddLabelActor(string name, double[][] positions, string[] labels, Color color, double fontScaleFactor, vtkRendererLayer layer);
        void RemoveOverlayActors(string[] actorNames);
        void AddMeasurementAnnotation(string name, vtkMaxActorData nodeData, vtkMaxActorData lineData, double[][] arrowCoordinates, double[][] arrowNormals, double symbolSize, bool visible);
        void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates, double[][] arrowCoordinates, double[][] arrowNormals, bool visible);
        void RemoveMeasurementAnnotations(string[] names);
        void UpdateOverlayPointsActor(string actorName, double[][] coordinates);
        void UpdateOverlayCaptionActorPosition(string actorName, double[] position);
        void AddOrientedDisplacementConstraintActor(vtkMaxActorData actorData, double symbolSize);
        void AddOrientedConeActors(vtkMaxActorData data, double symbolSize);
        void AddOrientedRotationalConstraintActor(vtkMaxActorData actorData, double symbolSize);
        void AddOrientedJointActor(vtkMaxActorData actorData, double symbolSize, vtkJointSymbolEnum symbol);
        void AddOrientedArrowsActor(vtkMaxActorData actorData, double symbolSize, bool invert = false, double relativeSize = 1);
        void AddOrientedDoubleArrowsActor(vtkMaxActorData actorData, double symbolSize);
        void AddOrientedSpringActor(vtkMaxActorData actorData, double symbolSize, bool invert = false);
        void AddOrientedThermosActor(vtkMaxActorData actorData, double symbolSize, bool invert = false);
        void AddOrientedFluxActor(vtkMaxActorData actorData, double symbolSize, bool center, bool invert);
        bool ContainsActor(string actorName);
        void HighlightActor(string actorName);
        void UpdateActor(string oldName, string newName, Color newColor);
        bool UpdateActorsColorTables(string[] actorNames, Color[][] colorTables);
        void UpdateActorModelEdgesToDraw(string actorName, PartExchangeData modelEdgesToDraw);
        void HideActors(string[] actorNames, bool updateColorContours);
        void ShowActors(string[] actorNames, bool updateColorContours);
        void SetCoorSysVisibility(bool visibility);
        void SetScaleWidgetVisibility(bool visibility);
        void SetWidgetHighlightEnabled(bool enabled);
        void SetSelectionDistance(int distanceInPixels);
        void InitializeResultWidgetPositions();
        void SetScalarBarColorSpectrum(vtkControl.vtkMaxColorSpectrum colorSpectrum);
        void SetScalarBarVisibility(bool visibility);
        void SetScalarBarNumberFormat(string numberFormat);
        void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent, string minMaxType);
        void DrawLegendBackground(bool drawBackground);
        void DrawLegendBorder(bool drawBorder);
        void InitializeColorBarWidgetPosition();
        void SetColorBarColorsAndLabels(Color[] colors, string[] labels);
        void AddColorBarColorsAndLabels(Color[] colors, string[] labels);
        void DrawColorBarBackground(bool drawBackground);
        void DrawColorBarBorder(bool drawBorder);
        void HideColorBar();
        void SetStatusBlockVisibility(bool draw);
        void SetStatusBlockNumberFormat(string numberFormat);
        void DrawStatusBlockBackground(bool drawBackground);
        void DrawStatusBlockBorder(bool drawBorder);
        void SetStatusBlock(string name, DateTime dateTime, float analysisTime, string unit, string deformationVariable, float scaleFactor, vtkControl.vtkMaxFieldDataType fieldType, int stepNumber, int incrementNumber, int nodalDiameter);
        void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw);
        void SetLighting(double ambient, double diffuse, bool redraw);
        void SetNavigationSensitivity(double rotateSensitivity, double zoomSensitivity, bool invertZoom, bool smoothZoom);
        void SetTrackpadNavigation(bool enabled, bool useWheelFallback, bool twoFingerDragPans, double panSensitivity, bool invertX, bool invertY);
        void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw);
        void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor);
        void SetMouseHighlightColor(Color mouseHighlightColor);
        void SetDrawSymbolEdges(bool drawSilhouettes);
        void CropPartWithCylinder(string partName, double r, string fileName);
        void CropPartWithCube(string partName, double x, double y, double z, double a, string fileName);
        void SmoothPart(string partName, double a, string fileName);
        void ActivateUserPick();
        void DeactivateUserPick();
        void SetFieldData(string name, string component);
        void SetViewResultsType(ViewResultsTypeEnum viewResultsType);
        void SetResultsUndeformedModelType(UndeformedModelTypeEnum undeformedModelType);
        void SetFieldData(string name, string component, int stepId, int stepIncrementId);
        void SelectStepAndIncrement(int stepId, int incrementId);
        void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, int[] modeNumber, int[] nodalDiameter, float[] scale, double[] allFramesScalarRange, vtkMaxAnimationType animationType);
        void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible);
        void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible, string[] animationActorNames);
        void UpdateArrowWidgetAnimationData(string name, string text, double[] anchorPoint);
        bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value);
        void ArrangeVisibleArrowWidgets();
        void RemoveArrowWidgets(string[] widgetNames);
        void RegenerateTree(bool remeshing = false);
        void AddTreeNode(ViewGeometryModelResults view, NamedClass item, string parentName);
        void UpdateTreeNode(ViewGeometryModelResults view, string oldItemName, NamedClass item, string parentName, bool updateSelection = true);
        void UpdateTreeNodeLayout(ViewGeometryModelResults view, string itemName, NamedClass item, string parentName);
        void SwapTreeNode(ViewGeometryModelResults view, string firstItemName, NamedClass firstItem, string secondItemName, NamedClass secondItem, string parentName);
        void RemoveTreeNode<T>(ViewGeometryModelResults view, string nodeName, string parentName)
            where T : NamedClass;
        bool[][] GetTreeExpandCollapseState();
        void SetTreeExpandCollapseState(bool[][] states);
        Dictionary<string, bool>[] GetStableTreeExpandCollapseState();
        void SetStableTreeExpandCollapseState(Dictionary<string, bool>[] states);
        void UpdateHighlight();
        void UpdateHighlightFromTree();
        void SelectBaseParts(string[] partNames);
        void SelectFirstComponentOfFirstFieldOutput();
        void SelectFieldOutputComponent(string fieldName, string componentName);
        void SetNumberOfModelUserKeywords(int numOfUserKeywords);
        void SetNumberOfModelModifiedKeywords(int numOfModifiedKeywords);
        void WriteDataToOutput(string data);
        void WriteErrorToOutput(string data);
        T InvokeIfRequired<T>(Func<T> func);
        void InvokeIfRequired(Action action);
        void InvokeIfRequired<T>(Action<T> action, T parameter);
        void InvokeIfRequired<T1, T2>(Action<T1, T2> action, T1 parameter1, T2 parameter2);
        void InvokeIfRequired<T1, T2, T3>(Action<T1, T2, T3> action, T1 parameter1, T2 parameter2, T3 parameter3);
        void InvokeIfRequired<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4);
        void InvokeIfRequired<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5);
        void InvokeIfRequired<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6);
        void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7);
        void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7, T8 parameter8);
        void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7, T8 parameter8, T9 parameter9);
        void ExportThreeMF(Export3mfFileProperties properties);
        void Invalidate();
        void SetShowAllProperties(bool value);
        bool IsDisposed { get; }

        bool IsHandleCreated { get; }

        IAsyncResult BeginInvoke(Delegate action);
        void ShowTimedError(string message, int milliseconds);
        IViewport3D Viewport { get; }
    }
}
