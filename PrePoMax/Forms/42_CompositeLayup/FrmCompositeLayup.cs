using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CaeGlobals;
using CaeModel;
using CaeMesh;

namespace PrePoMax.Forms
{
    public class FrmCompositeLayup : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        private readonly Controller _controller;
        private FeModel _model;
        private string _oldName;
        private CompositeLayupEditorData _data;
        private ViewCompositeLayup _view;
        private readonly DataGridView _grid;
        private readonly TextBox _summary;
        private readonly StackPlot _plot;
        private List<ExpandedCompositePly> _expanded;
        private bool _loading;
        private List<string> _errors = new List<string>();

        // Passing null permits constructing, filling and reading the form in a headless STA test.
        // No control in this constructor shows a window or constructs the main VTK form.
        public FrmCompositeLayup(Controller controller)
        {
            _controller = controller;
            Text = "Composite Layup";
            ClientSize = new Size(980, 780);
            MinimumSize = new Size(960, 760);
            MaximizeBox = true;
            var body = new TableLayoutPanel { Location = new Point(12, 12), Size = new Size(956, 718),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 1, RowCount = 3 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 255));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            Controls.Add(body);
            gbProperties.Parent = body;
            gbProperties.Dock = DockStyle.Fill;
            gbProperties.Text = "Layup properties";
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.AccessibleName = "Composite layup properties";
            propertyGrid.AccessibleDescription = "Name, region, rectangular coordinate system, stacking axis, symmetry and boundary tolerance.";
            var middle = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73));
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            body.Controls.Add(middle, 0, 1);
            var plyGroup = new GroupBox { Text = "Plies (bottom to top; Elements is an optional mesh-band count)", Dock = DockStyle.Fill };
            middle.Controls.Add(plyGroup, 0, 0);
            var plyBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            plyBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            plyBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            plyGroup.Controls.Add(plyBody);
            _grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                Name = "plyGrid", AccessibleName = "Composite plies, bottom to top",
                AccessibleDescription = "Edit ply name, material, thickness with length units, angle with angle units, and optional positive integer mesh-band count.",
                AllowUserToOrderColumns = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersWidth = 32, BackgroundColor = SystemColors.Window };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlyName", HeaderText = "Name", FillWeight = 115 });
            _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Material", HeaderText = "Material", FillWeight = 130,
                FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Thickness", HeaderText = "Thickness", FillWeight = 100,
                Tag = CompositeLayupEditorData.Converter(2) });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Angle", HeaderText = "Angle [deg]", FillWeight = 85,
                Tag = CompositeLayupEditorData.Converter(3) });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Elements", HeaderText = "Elements", FillWeight = 65 });
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                string headerText = column.HeaderText;
                column.HeaderCell = new PlyHeaderCell(column.Name == "Thickness" ? "Positive thickness; accepts model length units, for example 0.05 cm."
                    : column.Name == "Angle" ? "Ply angle; accepts degrees or radians."
                    : column.Name == "Elements" ? "Optional positive integer number of existing mesh bands; does not generate elements."
                    : column.Name == "Material" ? "Choose an existing model material." : "Unique ply name.");
                column.HeaderCell.Value = headerText;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
            }
            _grid.EditingControlShowing += (s, e) => {
                e.Control.AccessibleName = _grid.CurrentCell.OwningColumn.HeaderText + " for ply " + (_grid.CurrentCell.RowIndex + 1);
                e.Control.AccessibleDescription = _grid.CurrentCell.OwningColumn.HeaderCell.AccessibilityObject.Description;
            };
            plyBody.Controls.Add(_grid, 0, 0);
            var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            plyBody.Controls.Add(tools, 0, 1);
            foreach (string action in new[] { "Add", "Insert", "Delete", "Move up", "Move down", "Duplicate", "Reverse" })
            {
                var button = new Button { Name = "btnPly" + action.Replace(" ", ""), Text = action,
                    AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6, 0, 6, 0),
                    UseCompatibleTextRendering = true, MinimumSize = new Size(65, 25), CausesValidation = false };
                button.Click += (s, e) => EditTable(action);
                tools.Controls.Add(button);
            }
            var plotGroup = new GroupBox { Text = "Ply stack (+ axis upward)", Dock = DockStyle.Fill };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _plot = new StackPlot { Width = 200, Height = 200 };
            scroll.Controls.Add(_plot);
            scroll.Resize += (s, e) => { _plot.Width = Math.Max(100, scroll.ClientSize.Width - 20); ResizePlot(); };
            plotGroup.Controls.Add(scroll);
            middle.Controls.Add(plotGroup, 1, 0);
            var validation = new GroupBox { Text = "Validation (updated after every edit)", Dock = DockStyle.Fill };
            _summary = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
            validation.Controls.Add(_summary);
            body.Controls.Add(validation, 0, 2);
            btnCancel.Location = new Point(893, 744);
            btnOK.Location = new Point(812, 744);
            btnOkAddNew.Location = new Point(731, 744);
            btnCancel.CausesValidation = false;
            _grid.CellValidating += ValidateCell;
            _grid.CellEndEdit += (s, e) => { if (!_loading) RefreshValidation(); };
            _grid.CurrentCellDirtyStateChanged += (s, e) => {
                if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewComboBoxCell)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += (s, e) => {
                if (!_loading && e.RowIndex >= 0 && e.ColumnIndex == 1)
                {
                    _data.SetCell(e.RowIndex, 1, Convert.ToString(_grid.Rows[e.RowIndex].Cells[1].Value));
                    _propertyItemChanged = true;
                    RefreshValidation();
                }
            };
            _grid.SelectionChanged += (s, e) => Highlight();
            _grid.DataError += (s, e) => {
                e.ThrowException = false;
                _summary.Text = "The table contains an invalid value: " + e.Exception.Message;
                btnOK.Enabled = btnOkAddNew.Enabled = false;
            };
        }

        public void LoadFromModel(FeModel model, CompositeLayup value, string oldName)
        {
            _model = model;
            _oldName = oldName;
            _data = new CompositeLayupEditorData(value);
            _view = new ViewCompositeLayup(_data.Layup);
            _view.Populate(model);
            propertyGrid.SelectedObject = _view;
            btnOkAddNew.Visible = oldName == null;
            _propertyItemChanged = false;
            FillRows(0);
        }
        public CompositeLayup ReadBack()
        {
            if (!_grid.EndEdit()) throw new CaeException("Correct the invalid ply cell before continuing.");
            return _data.ReadBack();
        }
        protected override bool OnPrepareForm(string stepName, string name)
        {
            if (_controller.Model.Mesh == null) throw new CaeException("Import or create a solid mesh first.");
            CompositeLayup value;
            if (name != null) value = _controller.Model.CompositeLayups[name];
            else
            {
                string part = _controller.GetModelPartNames().FirstOrDefault();
                value = new CompositeLayup(_controller.GetCompositeLayupNames().GetNextNumberedKey("Layup"), part, RegionTypeEnum.PartName);
                value.Plies.Add(new CompositePly("Ply-1", _controller.GetMaterialNames().FirstOrDefault(), 1, 0));
            }
            LoadFromModel(_controller.Model, value, name);
            return true;
        }
        private void FillRows(int selected)
        {
            _loading = true;
            try
            {
                _grid.Rows.Clear();
                var column = (DataGridViewComboBoxColumn)_grid.Columns[1];
                column.Items.Clear();
                column.Items.AddRange(_model.Materials.Keys.Concat(_data.Layup.Plies.Select(p => p.MaterialName ?? ""))
                    .Distinct(StringComparer.OrdinalIgnoreCase).Cast<object>().ToArray());
                _grid.Columns[2].HeaderText = "Thickness [" + _model.UnitSystem.LengthUnitAbbreviation + "]";
                foreach (var ply in _data.Layup.Plies)
                    _grid.Rows.Add(ply.Name, ply.MaterialName ?? "", CompositeLayupEditorData.Converter(2).ConvertToString(ply.Thickness),
                        CompositeLayupEditorData.Converter(3).ConvertToString(ply.AngleDeg), ply.ElementsThroughThickness?.ToString() ?? "");
                if (_grid.Rows.Count > 0) _grid.CurrentCell = _grid.Rows[Math.Max(0, Math.Min(selected, _grid.Rows.Count - 1))].Cells[0];
            }
            finally { _loading = false; }
            RefreshValidation();
        }
        private void ValidateCell(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_loading || e.RowIndex < 0) return;
            var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            try
            {
                _data.SetCell(e.RowIndex, e.ColumnIndex, Convert.ToString(e.FormattedValue));
                cell.ErrorText = "";
                cell.Style.BackColor = SystemColors.Window;
                _propertyItemChanged = true;
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                cell.ErrorText = ex.Message;
                cell.Style.BackColor = Color.MistyRose;
                _summary.Text = ex.Message;
                btnOK.Enabled = btnOkAddNew.Enabled = false;
            }
        }
        private void EditTable(string action)
        {
            if (_data == null || !_grid.EndEdit()) return;
            int index = _grid.CurrentRow?.Index ?? -1;
            if (action == "Add") index = _data.Add(_data.Layup.Plies.Count, _model.Materials.Keys.FirstOrDefault());
            else if (action == "Insert") index = _data.Add(Math.Max(index, 0), _model.Materials.Keys.FirstOrDefault());
            else if (action == "Reverse") { _data.Reverse(); index = _data.Layup.Plies.Count - 1 - index; }
            else if (index >= 0)
            {
                if (action == "Delete") _data.Delete(index);
                else if (action == "Duplicate") index = _data.Duplicate(index);
                else index = _data.Move(index, action == "Move up" ? -1 : 1);
            }
            _propertyItemChanged = true;
            FillRows(index);
        }
        private void ResizePlot()
        {
            if (_plot == null || _plot.Parent == null) return;
            _plot.Height = Math.Max(_plot.Parent.ClientSize.Height - 2, (_data?.Layup.GetExpandedPlies().Count ?? 0) * 24 + 50);
            _plot.Invalidate();
        }
        private void RefreshValidation()
        {
            if (_loading || _data == null) return;
            _errors = _data.Validate(_model, _oldName, out _expanded);
            _summary.Text = _errors.Count == 0
                ? "Valid: " + _expanded.Count + " expanded plies, " + _expanded.Sum(p => p.ElementIds.Length) + " elements. Ply boundaries coincide with element layers."
                : string.Join(Environment.NewLine, _errors);
            _summary.ForeColor = _errors.Count == 0 ? Color.DarkGreen : Color.DarkRed;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var ply = _data.Layup.Plies[row.Index];
                row.DefaultCellStyle.BackColor = _errors.Any(e => e.Contains("'" + ply.Name + "'")) ? Color.MistyRose : SystemColors.Window;
            }
            btnOK.Enabled = btnOkAddNew.Enabled = _errors.Count == 0;
            _plot.Layup = _data.Layup;
            ResizePlot();
            Highlight();
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            // Switching away from retained selection must stop owning the old internal region.
            if (_data.Layup.RegionType != RegionTypeEnum.Selection)
            {
                _data.Layup.CreationData = null;
                _data.Layup.CreationIds = null;
            }
            base.OnPropertyGridPropertyValueChanged();
            ShowHideSelection();
            RefreshValidation();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            ReadBack();
            RefreshValidation();
            if (_errors.Count > 0) throw new CaeException(string.Join(Environment.NewLine, _errors));
            if (_oldName == null) _controller.AddCompositeLayupCommand(_data.ReadBack());
            else if (_propertyItemChanged) _controller.ReplaceCompositeLayupCommand(_oldName, _data.ReadBack());
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm?.Hide();
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { ShowHideSelection(); RefreshValidation(); }
        }
        protected override void OnHideOrClose()
        {
            ItemSetDataEditor.SelectionForm?.Hide();
            base.OnHideOrClose();
        }
        private void ShowHideSelection()
        {
            if (_controller == null || !Visible) return;
            if (_data.Layup.RegionType == RegionTypeEnum.Selection)
            {
                if (_data.Layup.CreationData?.SelectItem == vtkSelectItem.Part) _controller.SetSelectItemToPart();
                else _controller.SetSelectItemToElement();
                ItemSetDataEditor.SelectionForm?.ShowIfHidden(Owner);
            }
            else { ItemSetDataEditor.SelectionForm?.Hide(); _controller.SetSelectByToOff(); }
        }
        public void SelectionChanged(int[] ids)
        {
            if (_data?.Layup.RegionType != RegionTypeEnum.Selection) return;
            _data.Layup.CreationIds = (int[])ids.Clone();
            _data.Layup.CreationData = _controller.Selection.DeepCopy(true, false);
            propertyGrid.Refresh();
            _propertyItemChanged = true;
            RefreshValidation();
        }
        public void Highlight()
        {
            if (_loading || !Visible || _closing || _controller == null) return;
            _controller.ClearSelectionHistory();
            int index = _grid.CurrentRow?.Index ?? -1;
            if (_expanded != null && index >= 0 && index < _data.Layup.Plies.Count)
            {
                var ids = _expanded[index].ElementIds.AsEnumerable();
                if (_data.Layup.Symmetric) ids = ids.Concat(_expanded[_expanded.Count - 1 - index].ElementIds);
                _controller.HighlightCompositePly(ids.Distinct().ToArray());
            }
            else if (_data?.Layup.RegionType == RegionTypeEnum.Selection && _data.Layup.CreationData != null)
            {
                _controller.Selection = _data.Layup.CreationData.DeepCopy(true, false);
                _controller.HighlightSelection(true, true);
            }
            // Ply highlighting clears the renderer selection. Keep the retained picking history
            // so the next add/subtract pick extends the region instead of starting from nothing.
            if (_data?.Layup.RegionType == RegionTypeEnum.Selection && _data.Layup.CreationData != null)
                _controller.Selection = _data.Layup.CreationData.DeepCopy(true, false);
        }
        public bool IsSelectionGeometryBased() { return _data?.Layup.CreationData?.IsGeometryBased() ?? true; }
        public bool IsGeometrySelectionIdBased()
        {
            bool mode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            return IsSelectionGeometryBased() && _data.Layup.CreationData != null ? _data.Layup.CreationData.IsGeometryIdBased(mode) : mode;
        }

        private sealed class PlyHeaderCell : DataGridViewColumnHeaderCell
        {
            private readonly string _description;
            public PlyHeaderCell(string description) { _description = description; }
            public override object Clone() { return new PlyHeaderCell(_description); }
            protected override AccessibleObject CreateAccessibilityInstance() { return new HeaderAccessibleObject(this); }
            private sealed class HeaderAccessibleObject : DataGridViewColumnHeaderCellAccessibleObject
            {
                private readonly PlyHeaderCell _header;
                public HeaderAccessibleObject(PlyHeaderCell header) : base(header) { _header = header; }
                public override string Name { get { return _header.OwningColumn?.HeaderText; } set { base.Name = value; } }
                public override string Description { get { return _header._description; } }
            }
        }

        private class StackPlot : Control
        {
            public CompositeLayup Layup;
            public StackPlot() { DoubleBuffered = true; BackColor = SystemColors.Window; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Layup == null || Layup.Plies.Count == 0) return;
                var plies = Layup.GetExpandedPlies();
                var weights = plies.Select(p => double.IsNaN(p.Thickness) || double.IsInfinity(p.Thickness) || p.Thickness <= 0 ? 0 : p.Thickness).ToArray();
                double max = weights.Max();
                double sum = max > 0 ? weights.Sum(w => w / max) : 0;
                float available = Math.Max(0, Height - 45 - 24 * plies.Count);
                float bottom = Height - 25;
                for (int i = 0; i < plies.Count; i++)
                {
                    float h = 24 + (sum > 0 ? (float)(available * (weights[i] / max) / sum) : 0);
                    var rect = new RectangleF(8, bottom - h, Width - 16, h);
                    using (var brush = new SolidBrush(i % 2 == 0 ? Color.LightSteelBlue : Color.LightCyan)) e.Graphics.FillRectangle(brush, rect);
                    e.Graphics.DrawRectangle(Pens.SlateGray, rect.X, rect.Y, rect.Width, rect.Height);
                    TextRenderer.DrawText(e.Graphics, plies[i].Name + " " + plies[i].AngleDeg.ToString("G5") + "°", Font,
                        Rectangle.Round(rect), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    bottom -= h;
                    if (Layup.Symmetric && i == Layup.Plies.Count - 1)
                    {
                        using (var pen = new Pen(Color.DarkRed, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            e.Graphics.DrawLine(pen, 0, bottom, Width, bottom);
                    }
                }
                TextRenderer.DrawText(e.Graphics, Layup.Symmetric ? "Symmetric — dashed midplane" : "Bottom → top", Font,
                    new Rectangle(0, Height - 23, Width, 22), Color.DarkSlateGray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
