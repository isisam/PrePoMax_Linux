using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using CaeGlobals;
using CaeModel;
using CaeResults;

// L1a model-only PMX adapter. The ordinary Windows PMX container is retained; UI-only
// records are projected into inert data during reading and never executed or written back.
internal static class PmxModelFile
{
    public static FeModel Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        string header = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(32)).TrimEnd('\0');
        if (!header.StartsWith("PrePoMax v3.0.0")) throw new InvalidDataException("L1a supports PMX 3.0.0; use Windows to convert earlier files.");
        int length = reader.ReadInt32();
        if (length <= 0 || length > stream.Length - stream.Position) throw new InvalidDataException("Invalid PMX compressed-block length.");
        using var compressed = new MemoryStream(reader.ReadBytes(length));
        using var block = Tools.Decompress(compressed);
        using var binary = new BinaryReader(block);
        long objectLength = binary.ReadInt64();
        if (objectLength <= 0 || objectLength > block.Length - block.Position || objectLength > int.MaxValue)
            throw new InvalidDataException("Invalid PMX object-block length.");
        using var objects = new MemoryStream(binary.ReadBytes((int)objectLength));
        var formatter = LegacyFormatter.Create();
        formatter.Binder = new ProjectionBinder();
        var data = (object[])formatter.Deserialize(objects);
        var controller = data[0] as UiRecord ?? throw new SerializationException("Missing PMX controller.");
        var model = controller.Values["_model"] as FeModel ?? throw new SerializationException("Missing PMX model.");
        FeModel.ReadFromBinaryReader(model, binary, 3_000_000);
        Console.Error.WriteLine("PMX model loaded; UI history, annotations, jobs and results are outside the L1a CLI model scope.");
        return model;
    }
    public static void Save(string path, FeModel model)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = File.Create(temporary))
            {
                Tools.WriteStringToFileStream(stream, "PrePoMax v3.0.0 RC2", 32);
                using var block = new MemoryStream();
                using (var binary = new BinaryWriter(block, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    new object[] { new ControllerShell(model), null, null, null }.DumpToStream(binary);
                    FeModel.WriteToBinaryWriter(model, binary);
                    binary.Flush();
                }
                block.Position = 0;
                byte[] bytes = Tools.Compress(block, CompressionLevel.Optimal);
                Tools.WriteIntToFileStream(stream, bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
                ResultsCollection.WriteToFileStream(new ResultsCollection(), stream, CompressionLevel.Optimal);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    [Serializable]
    private sealed class ControllerShell : ISerializable
    {
        private readonly FeModel model;
        public ControllerShell(FeModel model) { this.model = model; }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.FullTypeName = "PrePoMax.Controller";
            info.AssemblyName = "PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null";
            info.AddValue("_model", model);
            info.AddValue("_allResults", new ResultsCollection());
            info.AddValue("_results", null);
            info.AddValue("_annotations", null);
            info.AddValue("_commands", null);
        }
    }
    [Serializable]
    private sealed class UiRecord : ISerializable
    {
        public readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        private UiRecord(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info) Values[entry.Name] = entry.Value;
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            throw new NotSupportedException("UI projections cannot be serialized.");
    }
    private sealed class ProjectionBinder : SerializationBinder
    {
        private static bool IsUi(string assembly) => assembly == "PrePoMax" || assembly == "UserControls" || assembly == "vtkControl";
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (typeName.StartsWith("System.DelegateSerializationHolder") ||
                typeName.StartsWith("System.Reflection.MemberInfoSerializationHolder")) return typeof(UiRecord);
            if (IsUi(new AssemblyName(assemblyName).Name) && !typeName.Contains("[")) return typeof(UiRecord);
            var renamed = RenamedTypeSerializationBinder.Instance.BindToType(assemblyName, typeName);
            if (renamed != null) return renamed;
            return Type.GetType(typeName + ", " + assemblyName,
                a => IsUi(a.Name) ? typeof(UiRecord).Assembly : Assembly.Load(a),
                (a, name, ignoreCase) =>
                    name.StartsWith("PrePoMax.") || name.StartsWith("UserControls.") || name.StartsWith("vtkControl.") ||
                    name.StartsWith("System.DelegateSerializationHolder")
                    ? typeof(UiRecord) : a?.GetType(name, throwOnError: true, ignoreCase: ignoreCase),
                throwOnError: true);
        }
    }
}
