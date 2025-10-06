using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_6dadd : GH_ScriptInstance
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
  private void RunScript(bool iReset, List<Point3d> iPoints, int iIterationCount, int iMaxVertexCount, double iCollisionDistance, double iLengthConstraint, ref object oPoints, ref object oCurve)
  {

    if (iReset)
    {
      pointList = new List<Point3d>(iPoints);
    }
    else
    {
      for (int i = 0;  i < iIterationCount; i++)
      {
        if (pointList.Count < iMaxVertexCount)
        {
          DivideCurve(pointList, iLengthConstraint);
        }
        ProcessCollision(pointList, iCollisionDistance);
      }
    }
    DrawCurves(pointList, ref oCurve);
    oPoints = pointList;

  }
  #endregion
  #region Additional
  

  // ------------------------------------------------------------
  // declare the persistent variables here
  List<Point3d> pointList;
  Random randomGenerator = new Random();
  

  // ------------------------------------------------------------
  // function to insert mid points if the segment is too long
  // pointList will be updated--more points to be added
  public void DivideCurve(List<Point3d> pointList, double iLengthConstraint)
  {
    // conditional for loop
    for (int i = 0; i < pointList.Count - 1;)
    {
      Point3d ptA = pointList[i];
      Point3d ptB = pointList[i + 1];
      double distance = ptA.DistanceTo(ptB);
      if (distance > iLengthConstraint)
      {
        // strategy 1: insert mid point
        Point3d midPt = new Point3d((ptA.X + ptB.X) / 2, (ptA.Y + ptB.Y) / 2, (ptA.Z + ptB.Z) / 2);

        // strategy 2: insert random point
        double t = randomGenerator.NextDouble(); // 0 to 1
        LineCurve line = new LineCurve(ptA, ptB);
        Point3d randomPt = line.PointAt(t);
        pointList.Insert(i + 1, randomPt);
      }
      else
      {
        i++;
      }
    }
  }

  // ------------------------------------------------------------
  // detect collision
  // pointList will be updated--points will be moved
  public void ProcessCollision(List<Point3d> pointList, double collisionDistance)
  {
    List<Vector3d> moveVectors = new List<Vector3d>(new Vector3d[pointList.Count]);
    List<int> collisionCounts = new List<int>(new int[pointList.Count]);

    for (int i = 0; i < pointList.Count; i++)
    {
      for (int j = i + 1; j < pointList.Count; j++)
      {
        Point3d ptA = pointList[i];
        Point3d ptB = pointList[j];
        double distance = ptA.DistanceTo(ptB);

        if (distance < collisionDistance)
        {
          // for debugging
          Print(i.ToString() + " " + j.ToString() + " " + "true");

          Vector3d move = ptA - ptB; // from B to A
          move.Unitize();
          moveVectors[i] += 0.5 * (collisionDistance - distance) * move;
          moveVectors[j] -= 0.5 * (collisionDistance - distance) * move;
          collisionCounts[i]++;
          collisionCounts[j]++;
        }

      }
    }

    for (int i = 0; i < pointList.Count; i++)
    {
      if (collisionCounts[i] > 0)
      {
        Vector3d averageMovingDirection = moveVectors[i] / collisionCounts[i];
        pointList[i] += averageMovingDirection;
      }
    }
  }

  // ------------------------------------------------------------
  // draw curves
  public void DrawCurves(List<Point3d> pointList, ref object curve)
  {
    Polyline polyline = new Polyline(pointList);
    curve = polyline;
  }
  #endregion
}