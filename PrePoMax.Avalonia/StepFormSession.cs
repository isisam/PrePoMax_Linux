// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using CaeModel;
using PrePoMax.PropertyViews;

namespace PrePoMax.Avalonia;

public sealed class StepFormSession : EntityFormSession
{
    private readonly Step step;
    public ViewStep View { get; }
    public override string Kind => "Step";
    public override NamedClass Draft => step;
    private readonly List<ViewStepControlParameter> controls;
    public bool ControlsChanged { get; set; }
    public bool ControlWarningAccepted { get; set; }
    public const string ControlWarning = "Customized solution controls are not needed in most nonlinear analyses." +
        " They should only be used by those users who know what they are doing and are expert in the field.";
    public override IReadOnlyList<FormPage> Pages => new[] { new FormPage(step.GetType().Name, View) }.Concat(controls.Select(v => new FormPage(v.Name, v))).ToArray();
    public static readonly string[] ControlTypes = ["Reset", "Time Incrementation", "Field", "Contact"];
    public StepFormSession(Controller c, Step? source = null) : this(c, source, false) { }
    public static StepFormSession Create(Controller c, Step template) => new(c, template, true);
    private StepFormSession(Controller c, Step? source, bool create) : base(c, create ? null : source?.Name)
    {
        step = source?.DeepClone() ?? new StaticStep(c.GetStepNames().GetNextNumberedKey("Step"));
        View = step switch {
            StaticStep x when x.GetType() == typeof(StaticStep) => new ViewStaticStep(x),
            BoundaryDisplacementStep x => new ViewBoundaryDisplacementStep(x),
            FrequencyStep x => new ViewFrequencyStep(x), BuckleStep x => new ViewBuckleStep(x),
            ModalDynamicsStep x => new ViewModalDynamics(x), SteadyStateDynamicsStep x => new ViewSteadyStateDynamics(x),
            _ => throw new NotSupportedException(step.GetType().Name)
        };
        controls = step.StepControls.Parameters.Select(Wrap).ToList();
    }
    private static ViewStepControlParameter Wrap(StepControlParameter parameter) => parameter switch
    {
        ResetStepControlParameter p => new ViewResetStepControlParameter(p), TimeIncrementationStepControlParameter p => new ViewTimeIncrementationStepControlParameter(p),
        FieldStepControlParameter p => new ViewFieldStepControlParameter(p), ContactStepControlParameter p => new ViewContactStepControlParameter(p), _ => throw new NotSupportedException(parameter.GetType().Name)
    };
    public void AddControl(string name)
    {
        StepControlParameter p = name switch { "Reset" => new ResetStepControlParameter(), "Time Incrementation" => new TimeIncrementationStepControlParameter(),
            "Field" => new FieldStepControlParameter(), "Contact" => new ContactStepControlParameter(), _ => throw new NotSupportedException(name) };
        controls.Add(Wrap(p)); ControlsChanged = true;
    }
    public void RemoveControl(int index) { controls.RemoveAt(index); ControlsChanged = true; }
    public override void Changed(string property) { base.Changed(property); View.UpdateVisibility(); }
    public void FatiguePreset(int jumps)
    {
        if (jumps < 1 || jumps > int.MaxValue - 10) throw new CaeException("The number of jumps must be a positive integer with room for 10 extra increments.");
        if (this.step.GetType() != typeof(StaticStep)) throw new NotSupportedException("The fatigue preset requires a static step.");
        var step = (StaticStep)this.step;
        // Only existing typed Windows fields; the CalculiX writer is unchanged.
        step.Nlgeom = false; step.SolverType = SolverTypeEnum.Spooles;
        ((ViewStaticStep)View).IncrementationType = IncrementationTypeEnum.Direct;
        step.TimePeriod = jumps; step.InitialTimeIncrement = 1; step.MaxIncrements = jumps + 10;
        var output = step.FieldOutputs.Values.OfType<ElementFieldOutput>().FirstOrDefault();
        if (output == null) step.AddFieldOutput(new ElementFieldOutput("EF-SDV", ElementFieldVariable.SDV));
        else output.Variables |= ElementFieldVariable.SDV;
        View.UpdateVisibility();
    }
    public override void Validate()
    {
        CheckName(Controller.GetStepNames(), "step");
        if (step is FrequencyStep frequency)
        {
            var cyclic = Controller.Model.Constraints.Values.OfType<CyclicSymmetry>().Where(c => c.Active).ToArray();
            if (cyclic.Length > 0)
            {
                Controller.Model.CheckCyclicSymmetryCompatibility(cyclic[0], cyclic[0].Name);
                frequency.CheckCyclicSymmetryModes(cyclic[0].NumberOfSectors, cyclic.Any(Controller.CyclicSymmetryContainsAxisNodes));
            }
        }
        if (RequiresPublish) ValidateStepValues(step);
        step.StepControls.Parameters.Clear(); foreach (var control in controls) step.StepControls.AddParameter(control.Base);
    }
        private static void ValidateStepValues(Step step)
        {
            if (step is BuckleStep buckleStep)
            {
                if (!(buckleStep.Accuracy > 0) || double.IsInfinity(buckleStep.Accuracy))
                    throw new CaeException("The accuracy of the buckle step must be a positive finite number.");
            }
            else if (step is ModalDynamicsStep modalDynamicsStep)
                ValidateModalDamping(modalDynamicsStep.ModalDamping);
            else if (step is SteadyStateDynamicsStep steadyStateDynamicsStep)
            {
                if (!steadyStateDynamicsStep.Harmonic &&
                    steadyStateDynamicsStep.TimeLower > steadyStateDynamicsStep.TimeUpper)
                    throw new CaeException("The lower time " + steadyStateDynamicsStep.TimeLower + " exceeds the " +
                                           "upper time " + steadyStateDynamicsStep.TimeUpper + ".");
                ValidateModalDamping(steadyStateDynamicsStep.ModalDamping);
            }
        }
        private static void ValidateModalDamping(ModalDamping modalDamping)
        {
            if (modalDamping == null || modalDamping.DampingType != ModalDampingTypeEnum.Direct) return;
            //
            List<CaeJob.DampingRatioAndRange> ranges = modalDamping.DampingRatiosAndRanges;
            if (ranges == null || ranges.Count == 0)
                throw new CaeException("The Direct modal damping needs at least one damping ratio and mode range.");
            for (int i = 0; i < ranges.Count; i++)
            {
                if (ranges[i].LowestMode < 1)
                    throw new CaeException("Damping ratio row " + (i + 1) + ": the lowest mode must be at least 1.");
                if (ranges[i].HighestMode < ranges[i].LowestMode)
                    throw new CaeException("Damping ratio row " + (i + 1) + ": the highest mode value must be equal " +
                                           "or larger than the lowest mode value.");
            }
        }

    protected override void Publish()
    {
        if (OriginalName == null) { Controller.AddStepCommand(step.DeepClone(), !(step is BoundaryDisplacementStep)); Controller.AddDefaultJobCommand(); }
        else Controller.ReplaceStepCommand(OriginalName, step.DeepClone());
    }
}
