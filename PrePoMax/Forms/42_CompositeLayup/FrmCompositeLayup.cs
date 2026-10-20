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
        private readonly Action<int[]> _highlightPly;
        private FeModel _model;
        private string _oldName;
        private CompositeLayupEditorData _data;
        private ViewCompositeLayup _view;
        private readonly DataGridView _grid;
        private readonly TextBox _summary;
        private readonly StackPlot _plot;
        private readonly Panel _content;
        private readonly CheckBox _showFibres;
        private readonly TrackBar _fibreSize;
        private readonly CheckBox _allPlies;
        private readonly TrackBar _plyGap;
        private readonly ComboBox _viewportPly;
        private bool _syncViewportPly;
        private List<ExpandedCompositePly> _expanded;
        private bool _loading;
        private bool _invalidCell;
        private List<string> _errors = new List<string>();

        // Passing null permits constructing, filling and reading the form in a headless STA test.
        // No control in this constructor shows a window or constructs the main VTK form.
        public FrmCompositeLayup(Controller controller) : this(controller, null) { }
        public FrmCompositeLayup(Controller controller, Action<int[]> highlightPly)
        {
            _controller = controller;
            _highlightPly = highlightPly ?? (controller == null ? (Action<int[]>)null : controller.HighlightCompositePly);
            SuspendLayout();
            // The inherited designer opts out of scaling. This editor uses one DPI
            // baseline for the entire tree, including its inherited dialog buttons.
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Composite Layup";
            ClientSize = new Size(1180, 1000);
            MinimumSize = new Size(960, 760);
            MaximizeBox = true;
            var content = _content = new Panel { Name = "layupContent", Dock = DockStyle.Fill, AutoScroll = true };
            var body = new TableLayoutPanel { Name = "layupBody", Location = Point.Empty,
                Size = new Size(1156, 938), MinimumSize = new Size(880, 650),
                ColumnCount = 1, RowCount = 3 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.Controls.Add(body);
            content.Layout += (s, e) => {
                body.Size = new Size(Math.Max(body.MinimumSize.Width, content.ClientSize.Width),
                                     Math.Max(body.MinimumSize.Height, content.ClientSize.Height));
                // Scale(SizeF) visits descendants after resizing their parent.
                // Reflow the table even when the final viewport size is unchanged;
                // otherwise its scaled children can retain stale dock bounds.
                ReflowContainers(body);
            };
            Controls.Add(content);
            Padding = new Padding(12);
            gbProperties.Parent = body;
            gbProperties.Dock = DockStyle.Fill;
            gbProperties.Text = "Layup properties";
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.AccessibleName = "Composite layup properties";
            propertyGrid.AccessibleDescription = "Name, region, rectangular coordinate system, stacking axis, symmetry and boundary tolerance.";
            var middle = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            middle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            body.Controls.Add(middle, 0, 1);
            var plyGroup = new GroupBox { Text = "Plies (bottom to top; Elements is an optional mesh-band count)", Dock = DockStyle.Fill };
            middle.Controls.Add(plyGroup, 0, 0);
            var plyBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            plyBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            plyBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            plyBody.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            plyGroup.Controls.Add(plyBody);
            _grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                Name = "plyGrid", AccessibleName = "Composite plies, bottom to top",
                AccessibleDescription = "Edit ply name, material, thickness with length units, angle with angle units, and optional positive integer mesh-band count.",
                AllowUserToOrderColumns = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders,
                RowHeadersWidth = 32, BackgroundColor = SystemColors.Window };
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
            // Let FlowLayout measure every wrapped row, including translated text
            // and scaled fonts; a fixed-height row clips Reverse at minimum size.
            var tools = new FlowLayoutPanel { Name = "plyToolbar", Dock = DockStyle.Fill,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty, WrapContents = true };
            plyBody.Controls.Add(tools, 0, 1);
            _showFibres = new CheckBox { Name = "showFibreDirections", Text = "Show fibre directions", AutoSize = true, Checked = true };
            _fibreSize = new TrackBar { Name = "fibreGlyphSize", Minimum = 10, Maximum = 200, Value = 100,
                TickFrequency = 50, Width = 110, Height = 30, AutoSize = false,
                AccessibleName = "Fibre glyph size" };
            var fibreTools = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true };
            fibreTools.Controls.Add(_showFibres);
            fibreTools.Controls.Add(new Label { Text = "Glyph size", AutoSize = true, Margin = new Padding(3, 7, 3, 0) });
            fibreTools.Controls.Add(_fibreSize);
            _allPlies = new CheckBox { Name = "allPlyColours", Text = "All plies", AutoSize = true };
            fibreTools.Controls.Add(_allPlies);
            _viewportPly = new ComboBox { Name = "viewportPly", DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 145, AccessibleName = "Viewport ply" };
            fibreTools.Controls.Add(new Label { Text = "Viewport ply", AutoSize = true, Margin = new Padding(3, 7, 3, 0) });
            fibreTools.Controls.Add(_viewportPly);
            _plyGap = new TrackBar { Name = "explodedPlyGap", Minimum = 0, Maximum = 200, Value = 100,
                TickFrequency = 50, Width = 110, Height = 30, AutoSize = false, Enabled = false,
                AccessibleName = "Ply gap" };
            fibreTools.Controls.Add(new Label { Text = "Ply gap", AutoSize = true, Margin = new Padding(3, 7, 3, 0) });
            fibreTools.Controls.Add(_plyGap);
            _allPlies.CheckedChanged += (s, e) => { _plyGap.Enabled = _allPlies.Checked; Highlight(); };
            _plyGap.ValueChanged += (s, e) => Highlight();
            _viewportPly.SelectedIndexChanged += (s, e) => {
                if (!_syncViewportPly) { _allPlies.Checked = false; Highlight(); }
            };
            plyGroup.Controls.Add(fibreTools);
            _showFibres.CheckedChanged += (s, e) => Highlight();
            _fibreSize.ValueChanged += (s, e) => Highlight();
            foreach (string action in new[] { "Add", "Insert", "Delete", "Move up", "Move down", "Duplicate", "Reverse" })
            {
                var button = new Button { Name = "btnPly" + action.Replace(" ", ""), Text = action,
                    AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6, 0, 6, 0),
                    UseCompatibleTextRendering = true, MinimumSize = new Size(65, 25), CausesValidation = false };
                button.Click += (s, e) => EditTable(action);
                tools.Controls.Add(button);
            }
            var plotGroup = new GroupBox { Text = "3D ply orientation", Dock = DockStyle.Fill };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _plot = new StackPlot { MinimumSize = new Size(440, 0), Size = new Size(440, 700) };
            scroll.Controls.Add(_plot);
            scroll.Resize += (s, e) => ResizePlot();
            plotGroup.Controls.Add(scroll);
            middle.Controls.Add(plotGroup, 1, 0);
            var validation = new GroupBox { Text = "Validation (updated after every edit)", Dock = DockStyle.Fill };
            _summary = new TextBox { Name = "LayupValidation", Tag = "L10n", Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
            validation.Controls.Add(_summary);
            body.Controls.Add(validation, 0, 2);
            // Keep the actions outside the scrolling content and reserve their
            // preferred height on every layout pass, rather than anchoring offsets.
            var actions = new FlowLayoutPanel { Name = "layupActions", Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = true,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 6, 0, 0) };
            foreach (var button in new[] { btnCancel, btnOK, btnOkAddNew })
            {
                button.Anchor = AnchorStyles.None;
                button.AutoSize = true;
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.MinimumSize = new Size(75, 25);
                actions.Controls.Add(button);
            }
            Controls.Add(actions);
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
            _grid.CurrentCellChanged += (s, e) => {
                if (!_loading) { SelectViewportPly(_grid.CurrentRow?.Index ?? 0); _allPlies.Checked = false; }
                Highlight();
            };
            _grid.CellClick += (s, e) => {
                if (e.RowIndex >= 0) { SelectViewportPly(e.RowIndex); _allPlies.Checked = false; Highlight(); }
            };
            _grid.DataError += (s, e) => {
                _invalidCell = true;
                _controller?.ShowCompositeFibreDirections(null, null, -1, 1);
                e.ThrowException = false;
                _summary.Text = "The table contains an invalid value: " + e.Exception.Message;
                btnOK.Enabled = btnOkAddNew.Enabled = false;
            };
            ResumeLayout(true);
        }

        private static void ReflowContainers(Control parent)
        {
            parent.PerformLayout();
            foreach (Control child in parent.Controls)
                if (child is Panel || child is GroupBox) ReflowContainers(child);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _controller?.ShowCompositeFibreDirections(null, null, -1, 1);
            base.Dispose(disposing);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            // A form-scale/font pass can finish without resizing the viewport.
            // Always invalidate its nested docking calculations at that boundary.
            _content?.PerformLayout();
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
            _invalidCell = false;
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
            SelectViewportPly(_grid.CurrentRow?.Index ?? 0);
            _allPlies.Checked = false;
            Highlight();
        }
        private void ValidateCell(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_loading || e.RowIndex < 0) return;
            var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            try
            {
                _data.SetCell(e.RowIndex, e.ColumnIndex, Convert.ToString(e.FormattedValue));
                _invalidCell = false;
                cell.ErrorText = "";
                cell.Style.BackColor = SystemColors.Window;
                _propertyItemChanged = true;
                // EndEdit may precede the focus-change validation. Refresh after parsing the
                // committed value too, otherwise the summary/OK state lags behind by one edit.
                RefreshValidation();
            }
            catch (Exception ex)
            {
                _invalidCell = true;
                _controller?.ShowCompositeFibreDirections(null, null, -1, 1);
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
            int count = _data?.Layup.GetExpandedPlies().Count ?? 0;
            _plot.Size = new Size(Math.Max(440, _plot.Parent.ClientSize.Width - 20), StackPlot.ContentHeight(count));
            _plot.Invalidate();
        }
        private void SelectViewportPly(int index)
        {
            _syncViewportPly = true;
            try { if (index >= 0 && index < _viewportPly.Items.Count) _viewportPly.SelectedIndex = index; }
            finally { _syncViewportPly = false; }
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
            int viewportIndex = _viewportPly.SelectedIndex;
            _syncViewportPly = true;
            try {
                _viewportPly.Items.Clear();
                var physicalPlies = _data.Layup.GetExpandedPlies();
                for (int i = 0; i < physicalPlies.Count; i++)
                    _viewportPly.Items.Add(vtkControl.CompositePlyPalette.Label(i, physicalPlies[i].AngleDeg));
                if (physicalPlies.Count > 0) _viewportPly.SelectedIndex = Math.Max(0, Math.Min(viewportIndex, physicalPlies.Count - 1));
            }
            finally { _syncViewportPly = false; }
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
            _controller?.ShowCompositeFibreDirections(null, null, -1, 1);
            ItemSetDataEditor.SelectionForm?.Hide();
            _controller?.SetSelectByToOff();
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
            // Keep the schematic selection live even in controller-free form previews.
            int physical = _viewportPly.SelectedIndex;
            _plot.SelectedPly = _allPlies.Checked ? -1 : physical >= 0 && _data != null
                ? vtkControl.CompositePlyPalette.Source(physical, _data.Layup.Plies.Count, _data.Layup.Symmetric)
                : _grid.CurrentRow?.Index ?? -1;
            _plot.Invalidate();
            if (_loading || !Visible || _closing || _controller == null) return;
            _controller.ClearSelectionHistory();
            // An invalid edited stack must also erase the previous valid ply highlight.
            if (_expanded == null) _highlightPly(new int[0]);
            int index = _plot.SelectedPly;
            if (_expanded != null && index >= 0 && index < _data.Layup.Plies.Count)
            {
                var ids = _expanded[index].ElementIds.AsEnumerable();
                if (_data.Layup.Symmetric) ids = ids.Concat(_expanded[_expanded.Count - 1 - index].ElementIds);
                _highlightPly(ids.Distinct().ToArray());
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
            _controller.ShowCompositeFibreDirections(_showFibres.Checked && !_invalidCell ? _data?.Layup : null, _expanded, _allPlies.Checked ? -1 : Math.Max(0, _viewportPly.SelectedIndex), _fibreSize.Value / 100.0, _plyGap.Value / 100.0);
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
            public int SelectedPly = -1;
            private const int Pitch = 180;
            public static int ContentHeight(int count) { return StackStart(count) + count * Pitch + 58; }
            private static int StackStart(int count) { return 412 + count * 24; }
            public StackPlot()
            {
                DoubleBuffered = true; BackColor = SystemColors.Window;
                AccessibleName = "3D ply orientation";
                AccessibleDescription = "Plan view overlays every ply; exploded plates show signed angles from the local zero axis. Red marks the selected ply and its symmetric partner. Spacing is not to scale.";
            }
            // u/v are the cyclic local axes used by CompositeLayupExpansion.
            private static PointF Project(float x, float y, float scale, float cx, float cy)
            { return new PointF(cx + scale * (.80f * x + .48f * y), cy + scale * (-.18f * x + .30f * y)); }
            // Viewed from +stack toward the origin: +u right, +v up; positive angles CCW.
            private static PointF Plan(float x, float y, float scale, float cx, float cy)
            { return new PointF(cx + scale * x, cy - scale * y); }
            private bool Selected(int i, int count)
            { return i == SelectedPly || (Layup.Symmetric && SelectedPly >= 0 && i == count - 1 - SelectedPly); }
            private static string Angle(double angle)
            { return angle.ToString("+0.####;-0.####;0", System.Globalization.CultureInfo.InvariantCulture) + "°"; }
            private Color PlyColor(int index)
            {
                return vtkControl.CompositePlyPalette.Display(
                    vtkControl.CompositePlyPalette.Source(index, Layup.Plies.Count, Layup.Symmetric),
                    SelectedPly >= 0 && !Selected(index, Layup.Plies.Count * (Layup.Symmetric ? 2 : 1)));
            }
            private static Color Shade(Color c) { return Color.FromArgb(c.R * 3 / 4, c.G * 3 / 4, c.B * 3 / 4); }
            private void DrawText(Graphics g, string text, RectangleF box, Color color, bool bold = false, bool center = false, bool badge = false)
            {
                // Pixel fonts keep the same logical geometry at 96 and 192 DPI.
                using (var font = new Font(Font.FontFamily, Font.SizeInPoints * 96 / 72, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(color))
                using (var format = new StringFormat { Alignment = center ? StringAlignment.Center : StringAlignment.Near,
                    LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    if (badge) g.FillRectangle(Brushes.White, box);
                    g.DrawString(text, font, brush, box, format);
                }
            }
            private static double Sweep(double angle)
            { double a = angle % 360; return a == 0 && angle != 0 ? Math.Sign(angle) * 360 : a; }
            private void Arc(Graphics g, double angle, float radius, Func<float, float, PointF> project, Color color)
            {
                double sweep = Sweep(angle);
                var points = new List<PointF>();
                int steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / 3));
                for (int n = 0; n <= steps; n++)
                {
                    double a = sweep * n / steps * Math.PI / 180;
                    points.Add(project(radius * (float)Math.Cos(a), radius * (float)Math.Sin(a)));
                }
                if (sweep == 0)
                { var p = points[0]; g.FillEllipse(Brushes.DarkSlateGray, p.X - 3, p.Y - 3, 6, 6); return; }
                using (var halo = new Pen(Color.White, 6)) g.DrawLines(halo, points.ToArray());
                using (var pen = new Pen(color, 2.8f) { EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor })
                    g.DrawLines(pen, points.ToArray());
            }
            // Render the geometry and text afresh at 2x; never enlarge a 1x screenshot.
            public Bitmap RenderPreview(int scale)
            {
                if (scale < 1 || scale > 4) throw new ArgumentOutOfRangeException(nameof(scale));
                var bitmap = new Bitmap(Width * scale, Height * scale);
                bitmap.SetResolution(96 * scale, 96 * scale);
                using (var g = Graphics.FromImage(bitmap))
                { g.Clear(BackColor); g.ScaleTransform(scale, scale); DrawPreview(g); }
                return bitmap;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (var bitmap = RenderPreview(2))
                    e.Graphics.DrawImage(bitmap, new Rectangle(0, 0, Width, Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel);
            }
            private void DrawPreview(Graphics g)
            {
                if (Layup == null || Layup.Plies.Count == 0) return;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                var plies = Layup.GetExpandedPlies();
                string[] axes = { "X", "Y", "Z" };
                int stack = Math.Max(1, Math.Min(3, Layup.StackingAxis));
                string cs = Layup.CoordinateSystemName ?? PrePoMax.L10n.Translator.Translate("Global");
                DrawText(g, string.Format(PrePoMax.L10n.Translator.Translate("Reference: {0}"), cs), new RectangleF(10, 3, Width - 20, 23), Color.DarkSlateGray, true);
                DrawText(g, "0° = " + axes[stack % 3] + "    90° = " + axes[(stack + 1) % 3] + "    +" + axes[stack - 1], new RectangleF(10, 26, Width - 20, 22), Color.DarkSlateGray);
                DrawText(g, string.Format(PrePoMax.L10n.Translator.Translate("Plan view — looking down +{0}"), axes[stack - 1]), new RectangleF(10, 53, Width - 20, 24), Color.Black, true);
                float cx = Width / 2f, cy = 221, side = 224;
                Func<float, float, PointF> plan = (x, y) => Plan(x, y, side, cx, cy);
                var square = new RectangleF(cx - side / 2, cy - side / 2, side, side);
                g.FillRectangle(Brushes.AliceBlue, square);
                using (var edge = new Pen(Color.SlateGray, 1.3f)) g.DrawRectangle(edge, square.X, square.Y, square.Width, square.Height);
                using (var zero = new Pen(Color.DarkSlateGray, 2) { EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor })
                { g.DrawLine(zero, cx - side / 2 - 16, cy, cx + side / 2 + 52, cy); g.DrawLine(zero, cx, cy + side / 2 + 12, cx, cy - side / 2 - 18); }
                DrawText(g, "0° " + axes[stack % 3], new RectangleF(cx + side / 2 + 14, cy + 6, 75, 22), Color.DarkSlateGray, true);
                DrawText(g, "90° " + axes[(stack + 1) % 3], new RectangleF(cx + 8, cy - side / 2 - 28, 90, 22), Color.DarkSlateGray);
                for (int i = 0; i < plies.Count; i++)
                {
                    double a = plies[i].AngleDeg * Math.PI / 180;
                    float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                    // Same-angle mirrors share a diameter using alternating coloured dashes.
                    Func<int, bool> same = j => Math.Abs(Math.IEEERemainder(plies[j].AngleDeg - plies[i].AngleDeg, 180)) < 1e-7;
                    int duplicates = Enumerable.Range(0, plies.Count).Count(same);
                    int ordinal = Enumerable.Range(0, i).Count(same);
                    float extent = .5f / Math.Max(Math.Abs(dx), Math.Abs(dy));
                    using (var fibre = new Pen(PlyColor(i), Selected(i, plies.Count) ? 5.5f : 3.8f))
                    {
                        if (duplicates > 1) { fibre.DashPattern = new[] { 3f, 3f * (duplicates - 1) }; fibre.DashOffset = -3 * ordinal; }
                        g.DrawLine(fibre, plan(-dx * extent, -dy * extent), plan(dx * extent, dy * extent));
                    }
                    // Separate radii keep positive, negative and repeated-angle arcs legible.
                    float radius = .14f + .031f * (i % 8);
                    Arc(g, plies[i].AngleDeg, radius, plan, PlyColor(i));
                    double half = Sweep(plies[i].AngleDeg) / 2 * Math.PI / 180;
                    PointF label = plan((radius + .24f) * (float)Math.Cos(half), (radius + .24f) * (float)Math.Sin(half));
                    // Labels for coincident angles belong in the colour-keyed legend below.
                    if (duplicates == 1)
                        DrawText(g, Angle(plies[i].AngleDeg), new RectangleF(label.X - 27, label.Y - 10, 54, 20), PlyColor(i), true, true, true);
                    float y = 351 + i * 24;
                    if (Selected(i, plies.Count))
                        using (var mark = new Pen(Controller.CompositePlyHighlightColor, 2)) g.DrawRectangle(mark, 10, y, Width - 20, 23);
                    using (var swatch = new Pen(PlyColor(i), 4)) g.DrawLine(swatch, 22, y + 11, 56, y + 11);
                    DrawText(g, (i + 1) + ". " + plies[i].Name + "   " + Angle(plies[i].AngleDeg), new RectangleF(67, y, Width - 85, 23), PlyColor(i), true);
                }
                DrawText(g, PrePoMax.L10n.Translator.Translate("Red outline = selected ply (and symmetric partner)"), new RectangleF(10, 354 + plies.Count * 24, Width - 20, 24), Color.DarkRed);
                int start = StackStart(plies.Count);
                DrawText(g, PrePoMax.L10n.Translator.Translate("3D ply orientation"), new RectangleF(10, start - 25, Width - 20, 24), Color.Black, true);
                float scale = Math.Min(Width * .60f, 260);
                for (int i = 0; i < plies.Count; i++)
                {
                    bool selected = Selected(i, plies.Count);
                    Color color = Color.FromArgb(242, 246, 248);
                    float plateY = start + 52 + (plies.Count - 1 - i) * Pitch;
                    Func<float, float, PointF> project = (x, y) => Project(x, y, scale, cx, plateY);
                    PointF[] top = { project(-.5f, -.5f), project(.5f, -.5f), project(.5f, .5f), project(-.5f, .5f) };
                    PointF[] face = { top[1], top[2], top[3], new PointF(top[3].X, top[3].Y + 6), new PointF(top[2].X, top[2].Y + 6), new PointF(top[1].X, top[1].Y + 6) };
                    using (var brush = new SolidBrush(Shade(color))) g.FillPolygon(brush, face);
                    using (var brush = new SolidBrush(color)) g.FillPolygon(brush, top);
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddPolygon(top); var state = g.Save();
                        try
                        {
                            g.SetClip(path);
                            double a = (plies[i].AngleDeg % 360) * Math.PI / 180;
                            float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                            using (var fibre = new Pen(PlyColor(i), selected ? 4.2f : 2.8f))
                            {
                                if (Layup.Symmetric && i >= Layup.Plies.Count) fibre.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                for (int line = -7; line <= 7; line++)
                                {
                                    float offset = line * .12f;
                                    g.DrawLine(fibre, project(-dx * 2 - dy * offset, -dy * 2 + dx * offset), project(dx * 2 - dy * offset, dy * 2 + dx * offset));
                                }
                            }
                        }
                        finally { g.Restore(state); }
                    }
                    using (var edge = new Pen(selected ? Controller.CompositePlyHighlightColor : Color.SlateGray, selected ? 3 : 1))
                    { g.DrawPolygon(edge, top); g.DrawPolygon(edge, face); }
                    // The baseline, radius and sampled arc use the SAME projection as the fibres.
                    using (var halo = new Pen(Color.White, 5)) g.DrawLine(halo, project(0, 0), project(.53f, 0));
                    using (var zero = new Pen(Color.DarkSlateGray, 1.7f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash, EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor })
                        g.DrawLine(zero, project(0, 0), project(.53f, 0));
                    double rad = Sweep(plies[i].AngleDeg) * Math.PI / 180;
                    using (var ray = new Pen(Color.DarkSlateGray, 1.5f))
                        g.DrawLine(ray, project(0, 0), project(.36f * (float)Math.Cos(rad), .36f * (float)Math.Sin(rad)));
                    Arc(g, plies[i].AngleDeg, .32f, project, Color.MidnightBlue);
                    double mid = Sweep(plies[i].AngleDeg) / 2 * Math.PI / 180;
                    PointF arcPoint = project(.32f * (float)Math.Cos(mid), .32f * (float)Math.Sin(mid));
                    float labelX = cx + scale * .43f, labelY = plateY + 33;
                    using (var leader = new Pen(Color.MidnightBlue, 1)) g.DrawLine(leader, arcPoint, new PointF(labelX, labelY + 10));
                    DrawText(g, Angle(plies[i].AngleDeg), new RectangleF(labelX, labelY, 70, 22), Color.MidnightBlue, true, true, true);
                    PointF origin = project(.54f, 0);
                    DrawText(g, "0°", new RectangleF(origin.X, origin.Y - 22, 38, 20), Color.DarkSlateGray, false, true, true);
                    DrawText(g, (i + 1) + ". " + plies[i].Name + "   " + Angle(plies[i].AngleDeg), new RectangleF(10, plateY + scale * .24f + 9, Width - 20, 23), PlyColor(i), true, true);
                    if (Layup.Symmetric && i == Layup.Plies.Count - 1)
                        using (var pen = new Pen(Color.DarkRed, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            g.DrawLine(pen, 10, plateY - Pitch / 2 + 15, Width - 10, plateY - Pitch / 2 + 15);
                }
                DrawText(g, PrePoMax.L10n.Translator.Translate(Layup.Symmetric ? "Symmetric — dashed midplane" : "Bottom → top"), new RectangleF(0, Height - 48, Width, 22), Color.DarkSlateGray, false, true);
                DrawText(g, PrePoMax.L10n.Translator.Translate("Spacing not to scale"), new RectangleF(0, Height - 26, Width, 22), Color.DarkSlateGray, false, true);
            }
        }
    }
}
