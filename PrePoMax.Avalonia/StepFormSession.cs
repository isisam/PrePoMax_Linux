// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using CaeModel;
using PrePoMax.PropertyViews;

namespace PrePoMax.Avalonia;

public sealed class StepFormSession : EntityFormSession
{
    private readonly StaticStep step;
    public ViewStaticStep View { get; }
    public override string Kind => "Step";
    public override NamedClass Draft => step;
    private readonly List<ViewStepControlParameter> controls;
    public override IReadOnlyList<FormPage> Pages => new[] { new FormPage("Static step", View) }.Concat(controls.Select(v => new FormPage(v.Name, v))).ToArray();
    public static readonly string[] ControlTypes = ["Reset", "Time Incrementation", "Field", "Contact"];
    public StepFormSession(Controller c, StaticStep? source = null) : base(c, source?.Name)
    {
        step = source?.DeepClone() ?? new StaticStep(c.GetStepNames().GetNextNumberedKey("Step"));
        View = new ViewStaticStep(step);
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
        controls.Add(Wrap(p));
    }
    public void RemoveControl(int index) => controls.RemoveAt(index);
    public override void Changed(string property) => View.UpdateVisibility();
    public void FatiguePreset(int jumps)
    {
        if (jumps < 1 || jumps > int.MaxValue - 10) throw new CaeException("The number of jumps must be a positive integer with room for 10 extra increments.");
        // Only existing typed Windows fields; the CalculiX writer is unchanged.
        step.Nlgeom = false; step.SolverType = SolverTypeEnum.Spooles;
        View.IncrementationType = IncrementationTypeEnum.Direct;
        step.TimePeriod = jumps; step.InitialTimeIncrement = 1; step.MaxIncrements = jumps + 10;
        var output = step.FieldOutputs.Values.OfType<ElementFieldOutput>().FirstOrDefault();
        if (output == null) step.AddFieldOutput(new ElementFieldOutput("EF-SDV", ElementFieldVariable.SDV));
        else output.Variables |= ElementFieldVariable.SDV;
        View.UpdateVisibility();
    }
    public override void Validate()
    {
        CheckName(Controller.GetStepNames(), "step");
        step.StepControls.Parameters.Clear(); foreach (var control in controls) step.StepControls.AddParameter(control.Base);
    }
    protected override void Publish()
    {
        if (OriginalName == null) { Controller.AddStepCommand(step.DeepClone(), true); Controller.AddDefaultJobCommand(); }
        else Controller.ReplaceStepCommand(OriginalName, step.DeepClone());
    }
}
