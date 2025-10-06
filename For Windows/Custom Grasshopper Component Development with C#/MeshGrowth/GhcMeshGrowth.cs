using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MeshGrowth
{
    public class GhcMeshGrowth : GH_Component
    {
        // define a persistent variable
        private MeshGrowthSystem myMeshGrowthSystem;


        public GhcMeshGrowth()
            : base(
                "MeshGrowth",
                "MeshGrowth",
                "Expand a mesh based on subdivition and avoiding self-collision",
                "Workshop",
                "MeshGrowth")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item);
            pManager.AddMeshParameter("Starting Mesh", "StartingMesh", "StartingMesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Subiteration Count", "Subiteration Count", "Subiteration Count", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grow", "Grow", "Grow", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Max. Vertex Count", "Max. Vertex Count", "Max. Vertex Count", GH_ParamAccess.item);
            pManager.AddNumberParameter("Edge Length Constraint Weight", "Edge Length Constraint Weight", "Edge Length Constraint Weight", GH_ParamAccess.item);
            pManager.AddNumberParameter("Collision Distance", "Collision Distance", "Collision Distance", GH_ParamAccess.item);
            pManager.AddNumberParameter("Collision Weight", "Collision Weight", "Collision Weight", GH_ParamAccess.item);
            pManager.AddNumberParameter("Bending Resistance Weight", "Bending Resistance Weight", "Bending Resistance Weight", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Use R-Tree", "Use R-Tree", "Use R-Tree", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iReset = true;
            Mesh iStartingMesh = null;
            int iSubiterationCount = 0;
            bool iGrow = false;
            int iMaxVertexCount = 0;
            double iEdgeLengthConstrainWeight = 0.0;
            double iCollisionDistance = 0.0;
            double iCollisionWeight = 0.0;
            double iBendingResistanceWeight = 0.0;
            bool iUseRTree = false;

            DA.GetData("Reset", ref iReset);
            DA.GetData("Starting Mesh", ref iStartingMesh);
            DA.GetData("Subiteration Count", ref iSubiterationCount);
            DA.GetData("Grow", ref iGrow);
            DA.GetData("Max. Vertex Count", ref iMaxVertexCount);
            DA.GetData("Edge Length Constraint Weight", ref iEdgeLengthConstrainWeight);
            DA.GetData("Collision Distance", ref iCollisionDistance);
            DA.GetData("Collision Weight", ref iCollisionWeight);
            DA.GetData("Bending Resistance Weight", ref iBendingResistanceWeight);
            DA.GetData("Use R-Tree", ref iUseRTree);


            //=============================================================================================


            if (iReset || myMeshGrowthSystem == null)
            {
                myMeshGrowthSystem = new MeshGrowthSystem(iStartingMesh);
            }
            else
            {
                // passing input values to the parameters that used to define the class
                // if you dont pass these values, when run the simulation, these parameters' value will be zero, so the mesh will not change
                myMeshGrowthSystem.CollisionDistance = iCollisionDistance;
                myMeshGrowthSystem.CollisionWeight = iCollisionWeight;
                myMeshGrowthSystem.EdgeLengthConstraintWeight = iEdgeLengthConstrainWeight;
                myMeshGrowthSystem.Grow = iGrow;
                myMeshGrowthSystem.BendingResistanceWeight = iBendingResistanceWeight;
                myMeshGrowthSystem.MaxVertexCount = iMaxVertexCount;
                myMeshGrowthSystem.UseRTree = iUseRTree;

                // Run the simulation for the specified number of sub-iterations
                for (int i = 0; i < iSubiterationCount; i++)
                {
                    myMeshGrowthSystem.Update();
                }

            }

            Mesh oMesh = myMeshGrowthSystem.GetRhinoMesh();

            //=============================================================================================
            DA.SetData("Mesh", oMesh);
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("4494b8e5-292e-4d29-a0f2-9935f9e48254"); }
        }
    }
}
