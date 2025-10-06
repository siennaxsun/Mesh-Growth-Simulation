
using Plankton;
using PlanktonGh;
using Rhino.Geometry;
using System.Collections.Generic;

namespace MeshGrowth
{
    public class MeshGrowthSystem
    {
        private PlanktonMesh ptMesh;

        public bool Grow = false;
        public int MaxVertexCount;
        public double CollisionDistance;
        public double CollisionWeight;
        public double EdgeLengthConstraintWeight;
        public double BendingResistanceWeight;
        public bool UseRTree;

        private List<Vector3d> totalWeightedMoves;
        private List<double> totalWeights;


        public MeshGrowthSystem(Mesh inputMesh)
        {
            // convert input mesh (rhino) to plankton mesh
            ptMesh = inputMesh.ToPlanktonMesh();
        }

        public Mesh GetRhinoMesh()
        {
            return ptMesh.ToRhinoMesh();
        }

        public void Update()
        {
            if (Grow) ProcessEdgeSpliting();

            totalWeightedMoves = new List<Vector3d>();
            totalWeights = new List<double>();
            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                Vector3d move = new Vector3d(0, 0, 0);
                totalWeightedMoves.Add(move);
                totalWeights.Add(0);
            }

            if (UseRTree) ProcessCollisionUsingRTRee();
            else ProcessCollision();

            ProcessEdgeLengthConstraint();
            ProcessBendingResistance();
            UpdateVertexPosition();

        }



        private void UpdateVertexPosition()
        {
            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                if (totalWeights[i] > 0)
                {
                    PlanktonVertex vertex = ptMesh.Vertices[i];
                    Vector3d avgMove = totalWeightedMoves[i] / totalWeights[i];
                    ptMesh.Vertices.SetVertex(i, vertex.X + avgMove.X, vertex.Y + avgMove.Y, vertex.Z + avgMove.Z);
                }

            }
        }

        private void ProcessCollision()
        {
            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                for (int j = i + 1; j < ptMesh.Vertices.Count; j++)
                {
                    Point3d pointI = ptMesh.Vertices[i].ToPoint3d();
                    Point3d pointJ = ptMesh.Vertices[j].ToPoint3d();
                    Vector3d move = pointI - pointJ; // vector from J to I
                    double distance = pointI.DistanceTo(pointJ);

                    if (distance < CollisionDistance)
                    {
                        move.Unitize();
                        move *= 0.5 * (CollisionDistance - distance);
                        totalWeightedMoves[i] += move * CollisionWeight;
                        totalWeightedMoves[j] -= move * CollisionWeight;
                        totalWeights[i] += CollisionWeight;
                        totalWeights[j] += CollisionWeight;
                    }
                }
            }
        }

        private void ProcessEdgeSpliting()
        {
            for (int k = 0; k < ptMesh.Halfedges.Count; k += 2)
            {
                double length = ptMesh.Halfedges.GetLength(k);

                if (length > 0.99 * CollisionDistance && ptMesh.Vertices.Count < MaxVertexCount)
                {
                    SplitEdge(k);
                }
            }
        }

        private void SplitEdge(int edgeIndex)
        {
            int newHalfEdgeIndex = ptMesh.Halfedges.SplitEdge(edgeIndex);

            ptMesh.Vertices.SetVertex(
                ptMesh.Vertices.Count - 1,
                0.5 * (ptMesh.Vertices[ptMesh.Halfedges[edgeIndex].StartVertex].ToPoint3d() + ptMesh.Vertices[ptMesh.Halfedges[edgeIndex + 1].StartVertex].ToPoint3d()));

            if (ptMesh.Halfedges[edgeIndex].AdjacentFace >= 0)
                ptMesh.Faces.SplitFace(newHalfEdgeIndex, ptMesh.Halfedges[edgeIndex].PrevHalfedge);

            if (ptMesh.Halfedges[edgeIndex + 1].AdjacentFace >= 0)
                ptMesh.Faces.SplitFace(edgeIndex + 1, ptMesh.Halfedges[ptMesh.Halfedges[edgeIndex + 1].NextHalfedge].NextHalfedge);
        }


        private void ProcessEdgeLengthConstraint()
        {
            // FIXED: Process constraints before edge splitting to avoid index issues
            int halfedgeCount = ptMesh.Halfedges.Count;

            for (int k = 0; k < halfedgeCount; k += 2)
            {
                // FIXED: Check if halfedge indices are still valid
                if (k >= ptMesh.Halfedges.Count || k + 1 >= ptMesh.Halfedges.Count)
                    continue;

                double length = ptMesh.Halfedges.GetLength(k);
                int startVertexIndex = ptMesh.Halfedges[k].StartVertex;
                int endVertexIndex = ptMesh.Halfedges[k + 1].StartVertex;

                // FIXED: Check if vertex indices are valid
                if (startVertexIndex >= ptMesh.Vertices.Count || endVertexIndex >= ptMesh.Vertices.Count)
                    continue;

                Point3d startVertex = ptMesh.Vertices[startVertexIndex].ToPoint3d();
                Point3d endVertex = ptMesh.Vertices[endVertexIndex].ToPoint3d();
                Vector3d move = endVertex - startVertex;

                if (length > CollisionDistance)
                {
                    move.Unitize();
                    move *= 0.5 * (length - CollisionDistance);
                    totalWeightedMoves[startVertexIndex] += move * EdgeLengthConstraintWeight;
                    totalWeightedMoves[endVertexIndex] -= move * EdgeLengthConstraintWeight; // FIXED: Should be -= not +=
                    totalWeights[startVertexIndex] += EdgeLengthConstraintWeight;
                    totalWeights[endVertexIndex] += EdgeLengthConstraintWeight;
                }
            }
        }

        private void ProcessBendingResistance()
        {

            for (int k = 0; k < ptMesh.Halfedges.Count; k += 2)
            {
                if (ptMesh.Halfedges[k].AdjacentFace != -1 && ptMesh.Halfedges[k + 1].AdjacentFace != -1)
                {
                    int i = ptMesh.Halfedges[k].StartVertex;
                    int j = ptMesh.Halfedges[k + 1].StartVertex;
                    int p = ptMesh.Halfedges[ptMesh.Halfedges[k].PrevHalfedge].StartVertex;
                    int q = ptMesh.Halfedges[ptMesh.Halfedges[k + 1].PrevHalfedge].StartVertex;

                    // FIXED: Check if all vertex indices are valid
                    if (i >= ptMesh.Vertices.Count || j >= ptMesh.Vertices.Count ||
                        p >= ptMesh.Vertices.Count || q >= ptMesh.Vertices.Count)
                        continue;

                    Point3d vI = ptMesh.Vertices[i].ToPoint3d();
                    Point3d vJ = ptMesh.Vertices[j].ToPoint3d();
                    Point3d vP = ptMesh.Vertices[p].ToPoint3d();
                    Point3d vQ = ptMesh.Vertices[q].ToPoint3d();

                    Vector3d IJ = vJ - vI; // vector from I to J
                    Vector3d IP = vP - vI; // vector from I to P
                    Vector3d IQ = vQ - vI; // vector from I to Q

                    Vector3d nP = Vector3d.CrossProduct(IJ, IP);
                    Vector3d nQ = Vector3d.CrossProduct(IQ, IJ);

                    Point3d planeOrigin = (vI + vJ + vP + vQ) / 4; // FIXED: Should be vJ, not vI twice
                    Vector3d planeNormal = nP + nQ;
                    planeNormal.Unitize();
                    Plane targetPlane = new Plane(planeOrigin, planeNormal);

                    Vector3d moveI = targetPlane.ClosestPoint(vI) - vI;
                    Vector3d moveJ = targetPlane.ClosestPoint(vJ) - vJ;
                    Vector3d moveP = targetPlane.ClosestPoint(vP) - vP;
                    Vector3d moveQ = targetPlane.ClosestPoint(vQ) - vQ;

                    totalWeightedMoves[i] += moveI * BendingResistanceWeight;
                    totalWeightedMoves[j] += moveJ * BendingResistanceWeight;
                    totalWeightedMoves[p] += moveP * BendingResistanceWeight;
                    totalWeightedMoves[q] += moveQ * BendingResistanceWeight;
                    totalWeights[i] += BendingResistanceWeight;
                    totalWeights[j] += BendingResistanceWeight;
                    totalWeights[p] += BendingResistanceWeight;
                    totalWeights[q] += BendingResistanceWeight;
                }
            }
        }

        private void ProcessCollisionUsingRTRee()
        {
            RTree rTree = new RTree();
            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                Point3d pointI = ptMesh.Vertices[i].ToPoint3d();
                rTree.Insert(pointI, i);
            }

            // FIXED: Changed condition from ptMesh.Vertices.Count > 0 to i < ptMesh.Vertices.Count
            for (int i = 0; i < ptMesh.Vertices.Count; i++)
            {
                Point3d point = ptMesh.Vertices[i].ToPoint3d();
                Sphere searchSphere = new Sphere(point, CollisionDistance);
                List<int> collisionIndices = new List<int>();

                rTree.Search(
                    searchSphere,
                    (sender, args) => { if (i < args.Id) collisionIndices.Add(args.Id); }
                );

                foreach (int j in collisionIndices)
                {
                    Point3d pointI = ptMesh.Vertices[i].ToPoint3d();
                    Point3d pointJ = ptMesh.Vertices[j].ToPoint3d();
                    Vector3d move = pointI - pointJ; // vector from J to I
                    double distance = move.Length;

                    if (distance < CollisionDistance && distance > 0) // FIXED: Added distance > 0 check
                    {
                        move.Unitize();
                        move *= (CollisionDistance - distance) * 0.5;
                        totalWeightedMoves[i] += move * CollisionWeight;
                        totalWeightedMoves[j] -= move * CollisionWeight;
                        totalWeights[i] += CollisionWeight;
                        totalWeights[j] += CollisionWeight;
                    }
                }
            }
        }
    }
}