using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input.Custom;

namespace Rhx.Commands.Roof
{
    public class GableRoofCommand : Command
    {
        ///<summary>The only instance of this command.</summary>
        public static GableRoofCommand Instance { get; private set; }

        internal static double DefaultHeight = 0;

        public GableRoofCommand() { Instance = this; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "GableRoof";

        protected override string CommandContextHelpUrl {
            get {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Doc", "GableRoofCommand.html");
            }
        }

        double m_tolerance;

        ObjRef m_objref;
        Plane m_plane;
        Brep m_brep;
        ComponentIndex m_ridgepole;

        Point3d m_basePoint;
        double m_height = 1;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            m_tolerance = doc.ModelAbsoluteTolerance;

            if (!SelectFace())
                return Result.Cancel;

            if (!SelectDirection())
                return Result.Cancel;

            if (!Split())
                return Result.Cancel;

            m_basePoint = new Point3d(m_brep.Edges[m_ridgepole.Index].PointAtNormalizedLength(0.5));

            if(!SelectHeight())
                return Result.Cancel;

            MoveRidgepole();

            doc.Objects.UnselectAll();
            doc.Objects.Delete(m_objref.Object());

            var ruid = doc.Objects.AddBrep(m_brep);
            doc.Objects.FindId(ruid).SelectSubObject(m_ridgepole, true, true, true);

            return Result.Success;
        }
        
        private bool SelectFace()
        {
            using (var cmd = new GetObject())
            {
                cmd.SetCommandPrompt("Select the face");
                cmd.GeometryFilter = ObjectType.Surface;
                cmd.SubObjectSelect = true;
                cmd.Get();

                if (cmd.CommandResult() != Result.Success)
                    return false;

                if (cmd.ObjectCount == 0)
                    return false;

                m_objref = cmd.Object(0);

                return true;
            }
        }
        
        bool SelectDirection()
        {
            using (var cmd = new GetLine())
            {
                cmd.AcceptZeroLengthLine = false;
                
                if (cmd.Get(out Line line) != Result.Success)
                    return false;

                if (line.Direction == Vector3d.ZAxis)
                    return false;

                m_plane = new Plane(line.From, new Vector3d(line.To - line.From), Vector3d.ZAxis);

                return true;
            }
        }

        bool Split()
        {
            Intersection.BrepPlane(m_objref.Surface().ToBrep(), m_plane, m_tolerance, out var curves, out var pts);
            if (curves.Length == 0)
                return false;

            var index = m_objref.GeometryComponentIndex.Index;
            if (index == -1)
                index = 0;

            m_brep = m_objref.Brep();
            var faceCount = m_brep.Faces.Count;

            m_brep = m_brep.Faces[index].Split(curves, m_tolerance);

            if (m_brep.Faces.Count != faceCount + 1) //TODO: Case m_brep.Faces.Count > faceCount + 1
                return false;

            foreach(var e1 in m_brep.Faces[index].AdjacentEdges())
            {
                foreach(var e2 in m_brep.Faces[faceCount].AdjacentEdges())
                {
                    if (e1 != e2)
                        continue;

                    m_ridgepole = m_brep.Edges[e1].ComponentIndex();

                    return true;
                }
            }
            
            return false;
        }
        
        bool SelectHeight()
        {
            using (var cmd = new GetPoint())
            {
                cmd.SetCommandPrompt("Select the Height");
                cmd.AcceptNumber(true, true);
                cmd.SetDefaultNumber(DefaultHeight);

                var r = cmd.Get();
                switch (r)
                {
                    case Rhino.Input.GetResult.Number:
                        m_height = cmd.Number();
                        DefaultHeight = m_height;
                        break;

                    case Rhino.Input.GetResult.Point:
                        m_height = cmd.Point().Z - m_basePoint.Z;
                        break;

                    default:
                        return false;
                }

                return true;
            }
        }
        
        void MoveRidgepole()
        {
            var components = new List<ComponentIndex> { m_ridgepole };
            m_brep.TransformComponent(components, Transform.Translation(0, 0, m_height), m_tolerance, 10, true);
        }
    }
}
