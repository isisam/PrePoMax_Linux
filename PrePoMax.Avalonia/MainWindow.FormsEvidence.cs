// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PrePoMax.Avalonia;

public sealed partial class MainWindow
{
    // Invoked only by the existing opt-in evidence loop. Every edit is routed to
    // the very controls shown to a user; it cannot call adapter setters/commands.
    private object? FormsEvidenceState()
    {
        if (activeEditor is not { } editor) return null;
        object[] Describe(DescriptorPropertyGrid grid) => grid.SelectedObject == null ? [] : DescriptorPropertyGrid.Properties(grid.SelectedObject).Select(p =>
        {
            var ctx = new DescriptorContext(grid.SelectedObject, p);
            string Format(object? v) { try { return p.Converter.ConvertToString(ctx, System.Globalization.CultureInfo.CurrentCulture, v) ?? ""; } catch { return v?.ToString() ?? ""; } }
            return (object)new { p.Name, p.DisplayName, p.Category, p.Description, p.IsReadOnly,
                value = Format(p.GetValue(grid.SelectedObject)), editor = grid.Editors.GetValueOrDefault(p.Name)?.GetType().Name,
                exclusive = p.Converter.GetStandardValuesExclusive(ctx), values = p.Converter.GetStandardValuesSupported(ctx) ? DescriptorPropertyGrid.StandardValues(p, ctx).Select(Format).ToArray() : null };
        }).ToArray();
        return new { kind = editor.Session.Kind, name = editor.Session.Draft.Name, original = editor.Session.OriginalName,
            pages = editor.Session.Pages.Select(p => p.Title).ToArray(), page = editor.PageSelector.SelectedIndex, row = editor.RowSelector.SelectedIndex,
            rows = editor.RowSelector.Items.Cast<object>().Select(v => v.ToString()).ToArray(), properties = Describe(editor.Properties), data = Describe(editor.DataProperties),
            error = editor.Error.Text, errors = editor.Properties.Errors, dataErrors = editor.DataProperties.Errors,
            selection = editor.SelectionPanel?.ItemSetData.ItemIds, selectionStatus = editor.SelectionPanel?.Status.Text,
            selectBy=mainView.Controller.SelectBy.ToString(),selectAngle=mainView.Controller.SelectAngle,geometryMode=mainView.Controller.GeometrySelectMode.ToString(),highlightedFaces,
            history=mainView.Controller.Selection.Nodes.Select(n=>new {type=n.GetType().Name,mouse=n is CaeGlobals.SelectionNodeMouse m?new {by=m.SelectBy.ToString(),m.Angle,m.PickedPoint,m.PlaneParameters,m.CompletelyInside}:null}) };
    }
    private void FormsEvidenceCommand(string op, JsonElement c)
    {
        if(op=="form-camera")
        {
            double[] Read(string key)=>c.GetProperty(key).EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            Width=1400;Height=1000;viewport.Width=c.GetProperty("width").GetInt32();viewport.Height=c.GetProperty("height").GetInt32();
            viewport.SetViewParameters(false,Read("position"),Read("focal"),Read("up"),c.GetProperty("scale").GetDouble());return;
        }
        if(op=="form-membership")
        {
            var mesh=mainView.Controller.Model.Mesh;
            File.WriteAllText(c.GetProperty("path").GetString()!,JsonSerializer.Serialize(new {
                nodes=mesh.NodeSets.Values.Where(s=>s.Name.StartsWith("L3Select")).Select(s=>new{s.Name,ids=s.Labels.Order(),history=s.CreationData?.Nodes.Select(n=>n.GetType().Name)}),
                elements=mesh.ElementSets.Values.Where(s=>s.Name.StartsWith("L3Select")).Select(s=>new{s.Name,ids=s.Labels.Order(),history=s.CreationData?.Nodes.Select(n=>n.GetType().Name)}),
                surfaces=mesh.Surfaces.Values.Where(s=>s.Name.StartsWith("L3Select")).Select(s=>new{s.Name,ids=s.FaceIds?.Order(),s.Area,history=s.CreationData?.Nodes.Select(n=>n.GetType().Name)})
            },new JsonSerializerOptions{WriteIndented=true}),new System.Text.UTF8Encoding(false));return;
        }
        if (op == "form-export") { ExportInpTo(c.GetProperty("path").GetString()!); return; }
        if (op == "form-audit")
        {
            var model = mainView.Controller.Model;
            var audit = new {
                materials = model.Materials.Values.Select(m => new { m.Name, properties = m.Properties.Select(p => new {
                    type = p.GetType().Name, constants = p is CaeModel.UserMaterial um ? um.Constants.Select(v => v.Value.Value).ToArray() : null }) }),
                sections = model.Sections.Values.Select(s => new { s.Name, s.MaterialName, region = s.RegionName, s.RegionType, s.Active }),
                layups = model.CompositeLayups.Values.Select(l => new { l.Name, l.Active, region = l.RegionName }),
                steps = mainView.Controller.GetStepNames(),
                keywords = model.CalculixUserKeywords.Select(k => new { path = k.Key, text = k.Value.Data, suppressed = k.Value.IsSuppressed }),
                overrides = model.CalculixKeywordOverrides.Select(k => new { path = k.Indices, text = k.Data, k.Disabled, suppressed = k.IsSuppressed }),
                plyWarning = this.model?.PlyWarning
            };
            File.WriteAllText(c.GetProperty("path").GetString()!, JsonSerializer.Serialize(audit, new JsonSerializerOptions { WriteIndented = true })); return;
        }
        var editor = activeEditor ?? throw new InvalidOperationException("No form is open");
        if(op=="form-select-mode"){editor.SelectionPanel!.Mode.SelectedItem=c.GetProperty("value").GetString();return;}
        if(op=="form-geometry-mode"){editor.SelectionPanel!.GeometryMode.SelectedIndex=c.GetProperty("index").GetInt32();return;}
        if(op=="form-angle"){var angle=editor.SelectionPanel!.Angle;angle.Text=c.GetProperty("value").GetString();angle.RaiseEvent(new KeyEventArgs{RoutedEvent=InputElement.KeyDownEvent,Key=Key.Enter});return;}
        if(op=="form-position")
        {
            editor.WindowStartupLocation=WindowStartupLocation.Manual;editor.Position=new global::Avalonia.PixelPoint(c.GetProperty("x").GetInt32(),c.GetProperty("y").GetInt32());return;
        }
        if (op == "form-page") { editor.PageSelector.SelectedIndex = c.GetProperty("index").GetInt32(); return; }
        if (op == "form-row") { editor.RowSelector.SelectedIndex = c.GetProperty("index").GetInt32(); return; }
        if (op == "form-material-type") { editor.MaterialType.SelectedItem = c.GetProperty("value").GetString(); return; }
        if (op == "form-temperature") { editor.MaterialTemperature.IsChecked = c.GetProperty("value").GetBoolean(); return; }
        if (op == "form-click") { editor.Buttons[c.GetProperty("button").GetString()!].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); return; }
        if (op == "form-ids") { editor.SelectionPanel!.IdInput.Text = c.GetProperty("value").GetString(); return; }
        if (op == "form-selection") { editor.SelectionPanel!.Buttons[c.GetProperty("button").GetString()!].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); return; }
        if (op == "form-name") { editor.MaterialName.Text = c.GetProperty("value").GetString(); return; }
        if (op == "form-set")
        {
            var grid = c.TryGetProperty("data", out var data) && data.GetBoolean() ? editor.DataProperties : editor.Properties;
            var control = grid.Editors[c.GetProperty("property").GetString()!]; string value = c.GetProperty("value").GetString()!;
            if (control is StackPanel panel)
            {
                var dropdown = panel.Children.OfType<ComboBox>().Single();
                control = dropdown.Items.Cast<object>().Any(v => v.ToString() == value) ? dropdown : panel.Children.OfType<TextBox>().Single();
            }
            if (control is TextBox text) { text.Text = value; text.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter }); }
            else if (control is ComboBox combo) { int index = Array.IndexOf(combo.Items.Cast<object>().Select(v => v.ToString()).ToArray(), value); if (index < 0) throw new ArgumentException("No dropdown value " + value); combo.SelectedIndex = index; }
            else throw new NotSupportedException("Evidence editor " + control.GetType().Name);
        }
    }
}
