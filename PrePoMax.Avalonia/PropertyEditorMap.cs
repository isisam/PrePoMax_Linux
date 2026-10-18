// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System;
using System.Collections.Generic;

namespace PrePoMax.Avalonia;

// String editor identities are metadata, never requests to load a Windows assembly.
// The form host can refuse an unsupported editor instead of losing the property.
public enum PropertyEditorKind { Default, Color, Flags, File, Folder, Region, Point, Damping, Environment, Unsupported }
public static class PropertyEditorMap
{
    public static IReadOnlyDictionary<string, PropertyEditorKind> Entries { get; } =
        new Dictionary<string, PropertyEditorKind>(StringComparer.Ordinal) {
            ["UserControls.ColorEditorEx"] = PropertyEditorKind.Color,
            ["UserControls.FlagsEnumEditorUIEditor"] = PropertyEditorKind.Flags,
            ["DynamicTypeDescriptor.StandardValueEditor"] = PropertyEditorKind.Flags,
            ["PrePoMax.CreateFileNameEditor"] = PropertyEditorKind.File,
            ["CaeGlobals.FilteredFileNameEditor"] = PropertyEditorKind.File,
            ["System.Windows.Forms.Design.FileNameEditor"] = PropertyEditorKind.File,
            ["System.Windows.Forms.Design.FolderNameEditor"] = PropertyEditorKind.Folder,
            ["PrePoMax.ItemSetDataEditor"] = PropertyEditorKind.Region,
            ["PrePoMax.SinglePointDataEditor"] = PropertyEditorKind.Point,
            ["UserControls.DampingRatiosAndValuesUIEditor"] = PropertyEditorKind.Damping,
            ["PrePoMax.Forms.EnvVarsUIEditor"] = PropertyEditorKind.Environment
        };
    public static PropertyEditorKind Resolve(EditorAttribute? editor)
    {
        if (editor is null) return PropertyEditorKind.Default;
        string name = editor.EditorTypeName.Split(',')[0].Trim();
        return Entries.TryGetValue(name, out var kind) ? kind : PropertyEditorKind.Unsupported;
    }
}
