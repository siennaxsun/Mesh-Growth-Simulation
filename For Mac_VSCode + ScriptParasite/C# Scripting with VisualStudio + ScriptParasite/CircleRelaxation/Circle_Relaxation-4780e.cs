using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Security.Principal;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_4780e : GH_ScriptInstance
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
  private void RunScript(bool iReset, List<Point3d> iPoints, int iSubIterationCounts, double iCollisionDistance, ref object A)
  {
    if (iReset)
    {
      pointList = new List<Point3d>(iPoints);
      A = GetCircles(pointList, circleList, iCollisionDistance);
    }
    else
    {
      for (int i = 0; i < iSubIterationCounts; i++)
      {
        CircleRelaxation(pointList, iCollisionDistance);
      }
      A = GetCircles(pointList, circleList, iCollisionDistance);
    }
  }
  #endregion
  #region Additional
  // declare the persistent data
  List<Point3d> pointList;
  List<Circle> circleList;

  // function to detect collisions and move points apart
  public void CircleRelaxation(List<Point3d> pointList, double collisionDistance)
  {
    List<Vector3d> movingVectors = new List<Vector3d>();
    List<int> collisionCounts = new List<int>();

    for (int i = 0; i < pointList.Count; i++)
    {
      movingVectors.Add(new Vector3d(0, 0, 0));
      collisionCounts.Add(0);
    }

    for (int i = 0; i < pointList.Count; i++)
    {
      for (int j = i + 1; j < pointList.Count; j++)
      {
        Point3d ptA = pointList[i];
        Point3d ptB = pointList[j];
        Vector3d move = ptA - ptB; // from B to A
        double distance = move.Length;
        if (distance < collisionDistance)
        {
          move.Unitize();
          movingVectors[i] += 0.5 * (collisionDistance - distance) * move;
          movingVectors[j] -= 0.5 * (collisionDistance - distance) * move;
          collisionCounts[i] += 1;
          collisionCounts[j] += 1;
        }

      }

    }

    for (int i = 0; i < pointList.Count; i++)
      if (collisionCounts[i] > 0)
        pointList[i] += movingVectors[i] / collisionCounts[i];
  }

  public List<Circle> GetCircles(List<Point3d> pointList, List<Circle> circleList, double collisionDistance)
  {
    circleList = new List<Circle>();
    for (int i = 0; i < pointList.Count; i++)
    {
      double radius = collisionDistance / 2.0;
      Circle cirlce = new Circle(pointList[i], radius);
      circleList.Add(cirlce);
    }
    return circleList;
  }
  #endregion
}