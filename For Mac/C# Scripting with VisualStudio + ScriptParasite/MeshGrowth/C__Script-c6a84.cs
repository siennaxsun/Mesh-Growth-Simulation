using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Plankton;
using PlanktonGh;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_c6a84 : GH_ScriptInstance
{
  #region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
  #endregion

  #region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
  #endregion
  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  #region Runscript
  private void RunScript(bool iReset, int iSubIterationCount, bool iGrow, Mesh iStartingMesh, int iMaxVertexCount, double iCollisionDistance, double iCollisionWeight, double iEdgeLengthWeight, double iBendingResistanceWeight, bool iUseRTree, ref object oMesh)
  {
        if (iReset || meshGrowthSys == null)
    {
      if (iStartingMesh == null || !iStartingMesh.IsValid || iStartingMesh.Faces.Count == 0)
      {
        Print("Warning: Starting mesh is empty or invalid.");
        return;
      }
      meshGrowthSys = new MeshGrowthSystem(iStartingMesh, Print);
    }

    // UPDATE: Set the parameters every frame
    meshGrowthSys.Grow = iGrow;
    meshGrowthSys.MaxVertexCount = iMaxVertexCount;
    meshGrowthSys.CollisionDistance = iCollisionDistance;
    meshGrowthSys.CollisionWeight = iCollisionWeight;
    meshGrowthSys.EdgeLengthConstraintWeight = iEdgeLengthWeight;
    meshGrowthSys.BendingResistanceWeight = iBendingResistanceWeight;
    meshGrowthSys.UseRTree = iUseRTree;

    try
    {
      meshGrowthSys.Update();
      oMesh = meshGrowthSys.Display();
    }
    catch (Exception ex)
    {
      Print("Update error: " + ex.Message);
    }

  }
  #endregion
  #region Additional
  //-----------------------------------------
  // declare persistent data
  private MeshGrowthSystem meshGrowthSys;
  /// </summary>

  // --------------------------------------------
  // define the class MeshGrowthSystem
  public class MeshGrowthSystem
  {
    private PlanktonMesh ptMesh;

    public bool Grow = false;
    public double CollisionDistance;
    public double CollisionWeight;
    public int MaxVertexCount;
    public double EdgeLengthConstraintWeight;
    public double BendingResistanceWeight;
    public bool UseRTree;

    private List<Vector3d> totalWeightedMoves;
    private List<double> totalWeights;

    private Action<string> _print;

    // constructor method
    public MeshGrowthSystem(Mesh inputMesh, Action<string> printMethod)
    {
      ptMesh = inputMesh.ToPlanktonMesh();
      _print = printMethod;
    }


    //----------------------------------------------------------
    // PUBLIC methods to be invoked outside the class

    // 0. group all the vector moving behaviors together here
    public void Update()
    {

      if (Grow) ProcessEdgeSpliting();

      totalWeights = new List<double>();
      totalWeightedMoves = new List<Vector3d>();
      for (int i = 0; i < ptMesh.Vertices.Count; i++)
      {
        Vector3d move = new Vector3d(0, 0, 0);
        totalWeightedMoves.Add(move);
        totalWeights.Add(0);
      }

      // for debugging purpose
      _print("Update started. Vertices: " + ptMesh.Vertices.Count + " Faces: " + ptMesh.Faces.Count);
      _print("Parameters - Collision Distance: " + CollisionDistance + ", Collision Weight: " + CollisionWeight);
      _print("Edge Length Weight: " + EdgeLengthConstraintWeight + ", Bending Weight: " + BendingResistanceWeight);

      for (int i = 0; i < ptMesh.Vertices.Count; i++)
      {
        if (i >= ptMesh.Vertices.Count)
        {
          _print("Warning: Vertex index out of range: " + i);
          return;
        }

      }

      // invoke private methods
      if (UseRTree) ProcessCollisionUsingRtree();
      else ProcessCollision();
      ProcessEdgeLengthConstraint();
      ProcessBendResistance();


      // debug--see how much movement each vertex moves
      double maxMove = 0.0;
      double sumMove = 0.0;
      int n = ptMesh.Vertices.Count;
      for (int i = 0; i < n; i++)
      {
        double len = totalWeightedMoves[i].Length;
        sumMove += len;
        if (len > maxMove) maxMove = len;
      }
      double avgMove = (n > 0) ? (sumMove / n) : 0.0;
      _print("avg move: " + avgMove.ToString() + "  max move: " + maxMove.ToString());

      // actually update the vertices
      UpdateVertexPosition();

    }


    // 1. output or display the resulting Plankton Mesh back to rhino mesh
    public Mesh Display()
    {
      Mesh mesh = ptMesh.ToRhinoMesh();
      return mesh;
    }

    // ----------------------------------------------------------------------
    // PRIVATE methods that act on the moving vectors and to be called inside Update()

    // 0. this method is to update vertex position based on the calcualted moving vector
    private void UpdateVertexPosition()
    {
      for (int i = 0; i < ptMesh.Vertices.Count; i++)
      {
        PlanktonVertex currentVertex = ptMesh.Vertices[i];
        if (totalWeights[i] > 0)
        {
          Vector3d averageMove = totalWeightedMoves[i] / totalWeights[i];
          ptMesh.Vertices.SetVertex(i, currentVertex.X + averageMove.X, currentVertex.Y + averageMove.Y, currentVertex.Z + averageMove.Z);
        }
        else
        {
          continue;
        }
      }
    }


    // 1. calculate the moving vector based on collision distance constraint
    private void ProcessCollision()
    {
      for (int i = 0; i < ptMesh.Vertices.Count; i++)
        for (int j = i + 1; j < ptMesh.Vertices.Count; j++)
        {
          Point3d vertexA = ptMesh.Vertices[i].ToPoint3d();
          Point3d vertexB = ptMesh.Vertices[j].ToPoint3d();
          double distance = vertexA.DistanceTo(vertexB);
          Vector3d move = vertexA - vertexB; // direction from B to A
          move.Unitize();
          // if too close, repel the two vertices
          if (distance < CollisionDistance)
          {
            move = 0.5 * (CollisionDistance - distance) * move;
            totalWeightedMoves[i] += move * CollisionWeight;
            totalWeightedMoves[j] -= move * CollisionWeight;
            totalWeights[i] += CollisionWeight;
            totalWeights[j] += CollisionWeight;
          }
          else
          {
            continue;
          }
        }
    }

    // 2. calculate if the half edge shall be split into half
    private void ProcessEdgeSpliting()
    {
      for (int k = 0; k < ptMesh.Halfedges.Count; k += 2)
      {
        double edgeLength = ptMesh.Halfedges.GetLength(k);
        if (ptMesh.Vertices.Count < MaxVertexCount && edgeLength > 0.99 * CollisionDistance)
        {
          SplitEdge(k);
        }
      }
    }

    // 3. Implement the edge spliting if the half edge is determined to be split
    private void SplitEdge(int edgeIndex)
    {
      int newHalfEdgeIndex = ptMesh.Halfedges.SplitEdge(edgeIndex);

      Point3d midPt = 0.5 * (ptMesh.Vertices[ptMesh.Halfedges[edgeIndex].StartVertex].ToPoint3d() + ptMesh.Vertices[ptMesh.Halfedges[edgeIndex + 1].StartVertex].ToPoint3d());
      ptMesh.Vertices.SetVertex(ptMesh.Vertices.Count - 1, midPt);

      if (ptMesh.Halfedges[edgeIndex].AdjacentFace >= 0)
      {
        ptMesh.Faces.SplitFace(newHalfEdgeIndex, ptMesh.Halfedges[edgeIndex].PrevHalfedge);
      }

      if (ptMesh.Halfedges[edgeIndex + 1].AdjacentFace >= 0)
      {
        ptMesh.Faces.SplitFace(edgeIndex + 1, ptMesh.Halfedges[ptMesh.Halfedges[edgeIndex + 1].NextHalfedge].NextHalfedge);
      }
    }

    // 4. Check edge length
    private void ProcessEdgeLengthConstraint()
    {
      int halfedgeCount = ptMesh.Halfedges.Count;

      for (int k = 0; k < halfedgeCount; k += 2)
      {
        PlanktonHalfedge halfedge = ptMesh.Halfedges[k];
        int i = halfedge.StartVertex;
        int j = ptMesh.Halfedges[halfedge.NextHalfedge].StartVertex;

        Vector3d move = ptMesh.Vertices[j].ToPoint3d() - ptMesh.Vertices[i].ToPoint3d();
        double distance = move.Length;
        if (distance > CollisionDistance)
        {
          move *= 0.5 * (distance - CollisionDistance) / move.Length;
          totalWeightedMoves[i] += move * EdgeLengthConstraintWeight;
          totalWeightedMoves[j] -= move * EdgeLengthConstraintWeight;
          totalWeights[i] += EdgeLengthConstraintWeight;
          totalWeights[j] += EdgeLengthConstraintWeight;
        }
        else
        {
          continue;
        }
      }
    }

    // 5. Implement bending resistance
    private void ProcessBendResistance()
    {
      for (int k = 0; k < ptMesh.Halfedges.Count; k += 2)
      {
        // skip if this edge is naked
        if (ptMesh.Halfedges[k].AdjacentFace == -1 || ptMesh.Halfedges[k + 1].AdjacentFace == -1)
        {
          continue;
        }
        else
        {
          // for index out of range problem, in this case: if 12 vertices (counting from 0, 1, 2 ...11), start from 0, 2, 4, 6, 8, 10
          int i = ptMesh.Halfedges[k].StartVertex;
          int j = ptMesh.Halfedges[k + 1].StartVertex;
          int p = ptMesh.Halfedges[ptMesh.Halfedges[k].PrevHalfedge].StartVertex;
          int q = ptMesh.Halfedges[ptMesh.Halfedges[k + 1].PrevHalfedge].StartVertex;

          Point3d posI = ptMesh.Vertices[i].ToPoint3d();
          Point3d posJ = ptMesh.Vertices[j].ToPoint3d();
          Point3d posP = ptMesh.Vertices[p].ToPoint3d();
          Point3d posQ = ptMesh.Vertices[q].ToPoint3d();

          Point3d origin = (posI + posJ + posP + posQ) / 4;
          Vector3d faceNormalIJP = Vector3d.CrossProduct(posJ - posI, posP - posI);
          Vector3d faceNormalIJQ = Vector3d.CrossProduct(posQ - posI, posJ - posI);
          Vector3d planeNormal = faceNormalIJP + faceNormalIJQ;
          planeNormal.Unitize();
          Plane plane = new Plane(origin, planeNormal);

          Vector3d moveI = plane.ClosestPoint(posI) - posI;
          Vector3d moveJ = plane.ClosestPoint(posJ) - posJ;
          Vector3d moveP = plane.ClosestPoint(posP) - posP;
          Vector3d moveQ = plane.ClosestPoint(posQ) - posQ;

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

    // 6. using RTree for collision process
    private void ProcessCollisionUsingRtree()
    {
      RTree rTree = new RTree();
      for (int i = 0; i < ptMesh.Vertices.Count; i++)
      {
        rTree.Insert(ptMesh.Vertices[i].ToPoint3d(), i);
      }

      for (int i = 0; i < ptMesh.Vertices.Count; i++)
      {
        Point3d pointI = ptMesh.Vertices[i].ToPoint3d();
        Sphere searchSphere = new Sphere(pointI, CollisionDistance);
        List<int> collisionIndices = new List<int>();

        rTree.Search(
          searchSphere,
          (object sender, RTreeEventArgs args) =>
          {
          if (i < args.Id)
            collisionIndices.Add(args.Id);
          }
          );

        foreach (int j in collisionIndices)
        {
          Point3d pointJ = ptMesh.Vertices[j].ToPoint3d();
          Vector3d move = pointJ - pointI;
          double distance = pointI.DistanceTo(pointJ);
          move *= 0.5 * (distance - CollisionDistance) / distance;
          totalWeightedMoves[i] += move * CollisionWeight;
          totalWeightedMoves[j] -= move * CollisionWeight;
          totalWeights[i] += CollisionWeight;
          totalWeights[j] += CollisionWeight;
        }
      }
    }

  }
  #endregion
}