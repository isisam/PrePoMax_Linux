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
    public class NullMainView : IMainView
    {
        private SidePanelsState sidePanels = new SidePanelsState();
        private bool[][] treeState = Array.Empty<bool[]>();
        private Dictionary<string, bool>[] stableTreeState = Array.Empty<Dictionary<string, bool>>();
        public Controller Controller { get; set; }

        public virtual void SetCurrentView(ViewGeometryModelResults view)
        {
        }

        public virtual void SetCurrentEdgesVisibilities(vtkControl.vtkEdgesVisibility edgesVisibility)
        {
        }

        public bool ScreenUpdating { get; set; }
        public bool RenderingOn { get; set; }
        public bool LastOpenResultsAccepted { get; }

        public virtual void Open(string fileName, Action<string, string> ActionOnFile, bool resetCamera = true, string parameters = null)
        {
            ActionOnFile(fileName, parameters);
        }

        public virtual void UpdateRecentFilesThreadSafe()
        {
        }

        public virtual void EnableDisableUndoRedo(string undo, string redo)
        {
        }

        public virtual SidePanelsState GetSidePanelsState()
        {
            return sidePanels;
        }

        public virtual void SetPendingSidePanelsState(SidePanelsState state)
        {
            sidePanels = state ?? new SidePanelsState();
        }

        public virtual void SetRotationPoint(double[] rotationPoint)
        {
        }

        public virtual void UpdateUserViewsThreadSafe(string[] viewNames)
        {
        }

        public virtual void SetAnnotateWithColor(AnnotateWithColorEnum annotateWithColor)
        {
        }

        public virtual MeshSetupItem EditMeshingParametersFromHistory(MeshSetupItem meshSetupItem)
        {
            throw new NotSupportedException("This operation requires a graphical host.");
        }

        public virtual void ShowSplitMeshResults()
        {
            throw new NotSupportedException("This operation requires a graphical host.");
        }

        public virtual void SaveAutomationScreenshot(string fileName, bool transparentBackground, int magnification)
        {
            throw new NotSupportedException("This operation requires a graphical host.");
        }

        public bool IsWindowHidden { get; }
        public string AutomationModelFileName { get; }

        public virtual void RequestAutomationExit()
        {
        }

        public virtual void PrepareAutomationPythonEngine()
        {
        }

        public virtual void EchoAutomationExecution(string label, string source, string output, string error)
        {
        }

        public virtual string GetAutomationBusyReason()
        {
            return default(string);
        }

        public virtual void SelectResultsUnitSystem(string parameters = null)
        {
        }

        public virtual void UpdateUnitSystem(UnitSystem unitSystem)
        {
        }

        public virtual void RunHistoryPostprocessing()
        {
        }

        public virtual void RunHistoryPostprocessing(bool? answerToAsk)
        {
        }

        public virtual void OpenAnalysisResults(string jobName, bool asynchronous = true)
        {
        }

        public virtual void UpdateAnalysisProgress(string jobName, JobStatus jobStatus)
        {
        }

        public virtual AnalysisJob GetDefaultJob()
        {
            return default(AnalysisJob);
        }

        public virtual string GetDefaultJobName()
        {
            return default(string);
        }

        public virtual void SelectionChanged(int[] ids = null)
        {
        }

        public virtual void SetQuerySelection(bool querySelection)
        {
        }

        public virtual bool GetQuerySelection()
        {
            return default(bool);
        }

        public virtual void SetSelectBy(vtkSelectBy selectBy)
        {
        }

        public virtual void SetSelectItem(vtkSelectItem selectItem)
        {
        }

        public virtual void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds, string[] selectionPartNames = null)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void GetClosestCellsOnParts(double[] point, string[] partNames, out double[] distances, out double[][] normals)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual double GetSelectionPrecision()
        {
            return default(double);
        }

        public virtual void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, string[] selectionPartNames, out int[] pointIds, out int[] cellIds)
        {
            throw new NotSupportedException("This operation requires a viewport backend.");
        }

        public virtual void CloseAllForms()
        {
        }

        public virtual void SelectOneNameInSymbolsNames(string name)
        {
        }

        public virtual void SetResultNames()
        {
        }

        public virtual void SetCurrentResults(string resultsName)
        {
        }

        public virtual string GetDeformationVariable()
        {
            return default(string);
        }

        public virtual DeformationScaleFactorTypeEnum GetDeformationType()
        {
            return default(DeformationScaleFactorTypeEnum);
        }

        public virtual float GetDeformationFactor()
        {
            return 1;
        }

        public virtual void SetDeformationType(DeformationScaleFactorTypeEnum deformationType)
        {
        }

        public virtual void SetDeformationFactor(float deformationFactor)
        {
        }

        public virtual ComplexResultTypeEnum GetComplexResultType()
        {
            return default(ComplexResultTypeEnum);
        }

        public virtual double GetComplexAngleDeg()
        {
            return default(double);
        }

        public virtual void SetStateReady(string currentText)
        {
        }

        public virtual bool SetStateWorking(string text, bool showCancelButton = false)
        {
            return default(bool);
        }

        public virtual bool IsStateOpening()
        {
            return default(bool);
        }

        public virtual void SetTitle(string title)
        {
        }

        public virtual string GetFileNameToOpen(string title)
        {
            return default(string);
        }

        public virtual string GetFileNameToOpen(string title, string filter)
        {
            return default(string);
        }

        public virtual string GetFileNameToImport(string title, bool onlyMaterials)
        {
            return default(string);
        }

        public virtual string GetFileNameToImport(string title, string filter)
        {
            return default(string);
        }

        public virtual string GetFileNameToSaveAs()
        {
            return default(string);
        }

        public virtual void ClearControls()
        {
        }

        public virtual void ClearResults()
        {
        }

        public virtual void Clear3D()
        {
        }

        public virtual void ClearButKeepParts()
        {
        }

        public virtual void Clear3DSelection()
        {
        }

        public virtual void ClearActiveTreeSelection()
        {
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

        public virtual void SetStandardView(string orientation, bool animate)
        {
        }

        public virtual void SetZoomToFit(bool animate)
        {
        }

        public virtual void Rotate(double azimuthAngle, double elevationAngle, double rollAngle)
        {
        }

        public virtual double[] GetViewPlaneNormal()
        {
            return new double[]
            {
                0,
                0,
                1
            };
        }

        public virtual void AdjustCameraDistanceAndClipping()
        {
        }

        public virtual void UpdateScalarsAndRedraw()
        {
        }

        public virtual void UpdateScalarsAndCameraAndRedraw()
        {
        }

        public virtual vtkMaxActorData[] GetVtkMaxActorData()
        {
            return default(vtkMaxActorData[]);
        }

        public virtual double[] GetClosestUpView()
        {
            return new double[]
            {
                0,
                0,
                1
            };
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

        public virtual void SetExplodedViewStatus(bool status)
        {
        }

        public virtual void ApplyFollowerView(string[] partName, int[] nodeIds)
        {
        }

        public virtual void UpdateFollowerView()
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

        public virtual void ApplyTransformations()
        {
        }

        public virtual void SetTransformationsStatus(bool status)
        {
        }

        public virtual void HideTransformedActors()
        {
        }

        public virtual IViewportActor Add3DNodes(vtkMaxActorData nodeData)
        {
            return new NullViewportActor();
        }

        public virtual IViewportActor Add3DCells(vtkMaxActorData cellData)
        {
            return new NullViewportActor();
        }

        public virtual void Add3DCells(vtkMaxActorData[] cellData)
        {
        }

        public virtual void AddActor(IViewportActor actor)
        {
        }

        public virtual void AddScalarFieldOn3DCells(vtkMaxActorData actorData, bool update)
        {
        }

        public virtual void AddVectorFieldActor(vtkMaxActorData actorData, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
        }

        public virtual void AddTensorFieldActor(vtkMaxActorData actorData, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
        }

        public virtual bool AddAnimatedVectorFieldActor(vtkMaxActorData actorData, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
            return default(bool);
        }

        public virtual bool AddAnimatedTensorFieldActor(vtkMaxActorData actorData, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
            return default(bool);
        }

        public virtual bool AddAnimatedScalarFieldOn3DCells(vtkMaxActorData actorData)
        {
            return default(bool);
        }

        public virtual void UpdateActorSurfaceScalarField(string actorName, float[] values, NodesExchangeData extremeNodes, ResultFieldExchangeData resultField, bool update)
        {
        }

        public virtual void UpdateActorColorContoursVisibility(string[] actorNames, bool colorContour)
        {
        }

        public virtual void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update)
        {
        }

        public virtual void AddSphereActor(vtkMaxActorData actorData, double symbolSize)
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

        public virtual void RemoveOverlayActors(string[] actorNames)
        {
        }

        public virtual void AddMeasurementAnnotation(string name, vtkMaxActorData nodeData, vtkMaxActorData lineData, double[][] arrowCoordinates, double[][] arrowNormals, double symbolSize, bool visible)
        {
        }

        public virtual void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates, double[][] arrowCoordinates, double[][] arrowNormals, bool visible)
        {
        }

        public virtual void RemoveMeasurementAnnotations(string[] names)
        {
        }

        public virtual void UpdateOverlayPointsActor(string actorName, double[][] coordinates)
        {
        }

        public virtual void UpdateOverlayCaptionActorPosition(string actorName, double[] position)
        {
        }

        public virtual void AddOrientedDisplacementConstraintActor(vtkMaxActorData actorData, double symbolSize)
        {
        }

        public virtual void AddOrientedConeActors(vtkMaxActorData data, double symbolSize)
        {
        }

        public virtual void AddOrientedRotationalConstraintActor(vtkMaxActorData actorData, double symbolSize)
        {
        }

        public virtual void AddOrientedJointActor(vtkMaxActorData actorData, double symbolSize, vtkJointSymbolEnum symbol)
        {
        }

        public virtual void AddOrientedArrowsActor(vtkMaxActorData actorData, double symbolSize, bool invert = false, double relativeSize = 1)
        {
        }

        public virtual void AddOrientedDoubleArrowsActor(vtkMaxActorData actorData, double symbolSize)
        {
        }

        public virtual void AddOrientedSpringActor(vtkMaxActorData actorData, double symbolSize, bool invert = false)
        {
        }

        public virtual void AddOrientedThermosActor(vtkMaxActorData actorData, double symbolSize, bool invert = false)
        {
        }

        public virtual void AddOrientedFluxActor(vtkMaxActorData actorData, double symbolSize, bool center, bool invert)
        {
        }

        public virtual bool ContainsActor(string actorName)
        {
            return default(bool);
        }

        public virtual void HighlightActor(string actorName)
        {
        }

        public virtual void UpdateActor(string oldName, string newName, Color newColor)
        {
        }

        public virtual bool UpdateActorsColorTables(string[] actorNames, Color[][] colorTables)
        {
            return default(bool);
        }

        public virtual void UpdateActorModelEdgesToDraw(string actorName, PartExchangeData modelEdgesToDraw)
        {
        }

        public virtual void HideActors(string[] actorNames, bool updateColorContours)
        {
        }

        public virtual void ShowActors(string[] actorNames, bool updateColorContours)
        {
        }

        public virtual void SetCoorSysVisibility(bool visibility)
        {
        }

        public virtual void SetScaleWidgetVisibility(bool visibility)
        {
        }

        public virtual void SetWidgetHighlightEnabled(bool enabled)
        {
        }

        public virtual void SetSelectionDistance(int distanceInPixels)
        {
        }

        public virtual void InitializeResultWidgetPositions()
        {
        }

        public virtual void SetScalarBarColorSpectrum(vtkControl.vtkMaxColorSpectrum colorSpectrum)
        {
        }

        public virtual void SetScalarBarVisibility(bool visibility)
        {
        }

        public virtual void SetScalarBarNumberFormat(string numberFormat)
        {
        }

        public virtual void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent, string minMaxType)
        {
        }

        public virtual void DrawLegendBackground(bool drawBackground)
        {
        }

        public virtual void DrawLegendBorder(bool drawBorder)
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

        public virtual void SetStatusBlockVisibility(bool draw)
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

        public virtual void SetStatusBlock(string name, DateTime dateTime, float analysisTime, string unit, string deformationVariable, float scaleFactor, vtkControl.vtkMaxFieldDataType fieldType, int stepNumber, int incrementNumber, int nodalDiameter)
        {
        }

        public virtual void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw)
        {
        }

        public virtual void SetLighting(double ambient, double diffuse, bool redraw)
        {
        }

        public virtual void SetNavigationSensitivity(double rotateSensitivity, double zoomSensitivity, bool invertZoom, bool smoothZoom)
        {
        }

        public virtual void SetTrackpadNavigation(bool enabled, bool useWheelFallback, bool twoFingerDragPans, double panSensitivity, bool invertX, bool invertY)
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

        public virtual void SetDrawSymbolEdges(bool drawSilhouettes)
        {
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

        public virtual void ActivateUserPick()
        {
        }

        public virtual void DeactivateUserPick()
        {
        }

        public virtual void SetFieldData(string name, string component)
        {
        }

        public virtual void SetViewResultsType(ViewResultsTypeEnum viewResultsType)
        {
        }

        public virtual void SetResultsUndeformedModelType(UndeformedModelTypeEnum undeformedModelType)
        {
        }

        public virtual void SetFieldData(string name, string component, int stepId, int stepIncrementId)
        {
        }

        public virtual void SelectStepAndIncrement(int stepId, int incrementId)
        {
        }

        public virtual void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, int[] modeNumber, int[] nodalDiameter, float[] scale, double[] allFramesScalarRange, vtkMaxAnimationType animationType)
        {
        }

        public virtual void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible)
        {
        }

        public virtual void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint, bool drawBackground, bool drawBorder, bool visible, string[] animationActorNames)
        {
        }

        public virtual void UpdateArrowWidgetAnimationData(string name, string text, double[] anchorPoint)
        {
        }

        public virtual bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value)
        {
            coordinates = default(double[]);
            value = default(float);
            return default(bool);
        }

        public virtual void ArrangeVisibleArrowWidgets()
        {
        }

        public virtual void RemoveArrowWidgets(string[] widgetNames)
        {
        }

        public virtual void RegenerateTree(bool remeshing = false)
        {
        }

        public virtual void AddTreeNode(ViewGeometryModelResults view, NamedClass item, string parentName)
        {
        }

        public virtual void UpdateTreeNode(ViewGeometryModelResults view, string oldItemName, NamedClass item, string parentName, bool updateSelection = true)
        {
        }

        public virtual void UpdateTreeNodeLayout(ViewGeometryModelResults view, string itemName, NamedClass item, string parentName)
        {
        }

        public virtual void SwapTreeNode(ViewGeometryModelResults view, string firstItemName, NamedClass firstItem, string secondItemName, NamedClass secondItem, string parentName)
        {
        }

        public virtual void RemoveTreeNode<T>(ViewGeometryModelResults view, string nodeName, string parentName)
            where T : NamedClass
        {
        }

        public virtual bool[][] GetTreeExpandCollapseState()
        {
            return treeState;
        }

        public virtual void SetTreeExpandCollapseState(bool[][] states)
        {
            treeState = states ?? Array.Empty<bool[]>();
        }

        public virtual Dictionary<string, bool>[] GetStableTreeExpandCollapseState()
        {
            return stableTreeState;
        }

        public virtual void SetStableTreeExpandCollapseState(Dictionary<string, bool>[] states)
        {
            stableTreeState = states ?? Array.Empty<Dictionary<string, bool>>();
        }

        public virtual void UpdateHighlight()
        {
        }

        public virtual void UpdateHighlightFromTree()
        {
        }

        public virtual void SelectBaseParts(string[] partNames)
        {
        }

        public virtual void SelectFirstComponentOfFirstFieldOutput()
        {
        }

        public virtual void SelectFieldOutputComponent(string fieldName, string componentName)
        {
        }

        public virtual void SetNumberOfModelUserKeywords(int numOfUserKeywords)
        {
        }

        public virtual void SetNumberOfModelModifiedKeywords(int numOfModifiedKeywords)
        {
        }

        public virtual void WriteDataToOutput(string data)
        {
            Console.WriteLine(data);
        }

        public virtual void WriteErrorToOutput(string data)
        {
            Console.Error.WriteLine(data);
        }

        public virtual T InvokeIfRequired<T>(Func<T> func)
        {
            return func();
        }

        public virtual void InvokeIfRequired(Action action)
        {
            action();
        }

        public virtual void InvokeIfRequired<T>(Action<T> action, T parameter)
        {
            action(parameter);
        }

        public virtual void InvokeIfRequired<T1, T2>(Action<T1, T2> action, T1 parameter1, T2 parameter2)
        {
            action(parameter1, parameter2);
        }

        public virtual void InvokeIfRequired<T1, T2, T3>(Action<T1, T2, T3> action, T1 parameter1, T2 parameter2, T3 parameter3)
        {
            action(parameter1, parameter2, parameter3);
        }

        public virtual void InvokeIfRequired<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
        {
            action(parameter1, parameter2, parameter3, parameter4);
        }

        public virtual void InvokeIfRequired<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5)
        {
            action(parameter1, parameter2, parameter3, parameter4, parameter5);
        }

        public virtual void InvokeIfRequired<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6)
        {
            action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
        }

        public virtual void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7)
        {
            action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);
        }

        public virtual void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7, T8 parameter8)
        {
            action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8);
        }

        public virtual void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7, T8 parameter8, T9 parameter9)
        {
            action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8, parameter9);
        }

        public virtual void ExportThreeMF(Export3mfFileProperties properties)
        {
            throw new NotSupportedException("This operation requires a graphical host.");
        }

        public virtual void Invalidate()
        {
        }

        public virtual void SetShowAllProperties(bool value)
        {
        }

        public bool IsDisposed { get; }
        public bool IsHandleCreated { get; } = true;

        public virtual IAsyncResult BeginInvoke(Delegate action)
        {
            return System.Threading.Tasks.Task.Run(() => action.DynamicInvoke());
        }

        public virtual void ShowTimedError(string message, int milliseconds)
        {
        }

        public IViewport3D Viewport { get; } = new NullViewport3D();
    }
}
