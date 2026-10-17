using CaeResults;
using System;
using CaeGlobals;
using vtkControl;
using FileInOut.Output;

namespace PrePoMax
{
    public partial class FrmMain
    {
        public void SetShowAllProperties(bool value)
        {
            DynamicTypeDescriptor.DynamicCustomTypeDescriptor.ShowAllProperties = value;
        }

        public IViewport3D Viewport
        {
            get
            {
                return _vtk;
            }
        }

        IAsyncResult IMainView.BeginInvoke(Delegate action)
        {
            return BeginInvoke(action);
        }

        IViewportActor IMainView.Add3DNodes(vtkMaxActorData data)
        {
            return Add3DNodes(data);
        }

        IViewportActor IMainView.Add3DCells(vtkMaxActorData data)
        {
            return Add3DCells(data);
        }

        void IMainView.AddActor(IViewportActor actor)
        {
            AddActor((vtkMaxActor)actor);
        }

        public void ShowTimedError(string message, int milliseconds)
        {
            UserControls.AutoClosingMessageBox.ShowError(message, milliseconds);
        }

        public void ExportThreeMF(Export3mfFileProperties properties)
        {
            ThreeMFFileWriter.Write(properties.FileName, GetVtkMaxActorData(), GetLookupTable(), properties.EdgeThicknessRatio, GetClosestUpView());
            WriteDataToOutput("Model exported to file: " + properties.FileName);
        }
    }
}
