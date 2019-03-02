using Rhino;
using Rhino.FileIO;

namespace Rhx.Commands.Roof
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        ///<summary>Gets the only instance of the PlugIn plug-in.</summary>
        public static PlugIn Instance { get; private set; }

        public PlugIn() { Instance = this; }

        protected override bool ShouldCallWriteDocument(FileWriteOptions options) =>true;

        protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
        {
            archive.WriteDouble(GableRoofCommand.DefaultHeight);
        }

        protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
        {
            GableRoofCommand.DefaultHeight = archive.ReadDouble();
        }
    }
}