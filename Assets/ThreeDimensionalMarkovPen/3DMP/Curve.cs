using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ThreeDimensionalMarkovPen._3DMP
{
    public partial class MarkovPen
    {
        /**
        * @class Curve
        * @brief That partial class is representative for the style curve of the MarkovPen
        *
         * The Curve class provides basic spline functionalities, adding from control points, computing form arc length positions and interpolation on the curve
        */
        public class Curve
        {
            protected List<float> _arcLengthPositions;
            protected List<Vector3> _controlPoints = new List<Vector3>();

            public float tension = 0f;
            public float continuity = 0f;
            public float bias = 0f;

            private Vector3? _lastInput = null;
            private const float RESPONSIVENESS = 1f;
            private float _responsiveness = RESPONSIVENESS;

            /*
            * @brief Constructor for the style curve sets the first arc length position for KochanekBartelsSpline point doubling
            * @param responsiveness Parameter to set the grade of smoothing between 0 and 1 per default at 1/2
            * 
            */
            public Curve(float responsiveness = RESPONSIVENESS)
            {
                _responsiveness = responsiveness;
                _arcLengthPositions = new List<float> { 0f };
            }

            /**
 * @brief Add a control point to the curve and update related information.
 * 
 * This public virtual method adds a control point to the curve and performs necessary updates.
 * If the curve is empty, the control point is directly added. If it's the first control point,
 * it is stored as the last input for future interpolation. For subsequent points, the method uses
 * cubic Hermite interpolation (elasticurve implementation) to add a new point to the curve.
 * The method also updates arc length information when the curve has at least three control points.
 * 
 * @param controlPoint The new control point to be added.
 */
            public virtual void AddControlPoint(Vector3 controlPoint, Vector3 upVector)
            {
                
                if (_controlPoints.Count == 0)
                {
                    _controlPoints.Add(controlPoint);
                    return;
                }
                if (_lastInput == null)
                {
                    _lastInput = controlPoint;
                    return;
                }
                
                
                //elasticurve implementation
                _controlPoints.Add(Interpolate(_controlPoints[_controlPoints.Count > 1 ? ^2 : ^1], _controlPoints[^1], (Vector3)_lastInput,
                    controlPoint, 0, 0, 0, _responsiveness));
                _lastInput = controlPoint;

                if (_controlPoints.Count >= 3)
                {
                    _arcLengthPositions.Add(_arcLengthPositions.Last() + ComputeArcLength(_controlPoints.Count - 2));
                }
            }

            /**
 * @brief Calculate the tangent vector at a given point using cubic Hermite interpolation factors.
 * 
 * This protected static method computes the tangent vector at a specified point
 * using cubic Hermite interpolation factors (tension, continuity, and bias).
 * 
 * @param point1 The previous control point.
 * @param point2 The current control point.
 * @param point3 The next control point.
 * @param tension The tension factor for interpolation.
 * @param continuity The continuity factor for interpolation.
 * @param bias The bias factor for interpolation.
 * @return The computed tangent vector at the given point.
 */
            protected static Vector3 ComputeTangent(Vector3 point1, Vector3 point2, Vector3 point3,
                float tension, float continuity, float bias)
            {
                var factor1 = (1 - tension) * (1 + continuity) * (1 + bias) / 2;
                var factor2 = (1 - tension) * (1 - continuity) * (1 - bias) / 2;

                return factor1 * (point2 - point1) + factor2 * (point3 - point2);
            }

            
            /**
 * @brief Get an array of Vector3 positions based on the provided index.
 * 
 * This helper method returns an array of length 4 containing Vector3 positions. 
 * It takes an index 'i' and retrieves the stored control points at positions (i - 1), i, (i + 1), and (i + 2).
 * 
 * @param i The index used to retrieve control points.
 * @return An array of Vector3 positions.
 */
            protected Vector3[] GetSegmentPositions(int index)
            {
                
                Vector3 p1 = _controlPoints[index == 0 ? index :index-1];
                Vector3 p2 = _controlPoints[index];
                Vector3 p3 = _controlPoints[index + 1];
                Vector3 p4 = _controlPoints[index == _controlPoints.Count -2 ? index+ 1: index + 2];

                return new[] { p1, p2, p3, p4 };
            }

            
            /**
 * @brief Append a control point to the curve and update related information.
 * 
 * This public method appends a control point to the curve and performs necessary updates.
 * If the curve is empty, the control point is directly added. For subsequent points, the method
 * adds the point to the curve and updates arc length information when the curve has at least four control points.
 * 
 * @param point The control point to be appended to the curve.
 */
            public void Append(Vector3 point)
            {
                if (_controlPoints.Count == 0)
                {
                    _controlPoints.Add(point);
                }

                _controlPoints.Add(point);
                if (_controlPoints.Count > 3)
                {
                    ComputeArcLength(_controlPoints.Count - 2);
                }
            }
            /**
 * @brief Calculates the position along the Bezier curve at a given arc length parameter.
 * 
 * This method computes the position on the Bezier curve corresponding to the specified arc length parameter.
 * If the given arc length parameter is less than 0, it returns the position at the start of the curve.
 * If the arc length parameter is greater than or equal to the total arc length of the curve, it returns
 * the position at the end of the curve.
 * 
 * @param l The arc length parameter at which to calculate the position.
 * 
 * @return The Vector3 position on the Bezier curve at the specified arc length parameter.
 */
            public Vector3 PositionAt(float l)
            {
                if (l < 0)
                {
                    //throw new Exception("(l <= 0)");
                    Vector3 tangent = ComputeTangent(_controlPoints[0], _controlPoints[0], _controlPoints[1], 0, 0, 0)
                        .normalized;
                    return _controlPoints[0] + l * tangent;
                }

                if (l >= _arcLengthPositions.Last())
                {
                    //throw new Exception("l >= _arcLengthPositions.Last()");
                    Vector3 tangent = ComputeTangent(_controlPoints[^2], _controlPoints[^1], _controlPoints[^1], 0, 0, 0)
                        .normalized;
                    return _controlPoints[^2] + (l - _arcLengthPositions.Last()) * tangent;
                }

                float t = TimeAt(l);
                Vector3[] segment = GetSegmentPositions(SegmentIndex(t));

                return Interpolate(segment[0], segment[1], segment[2], segment[3], 0, 0, 0, SegmentT(t));
            }

            /**
 * @brief Computes the local parameter within a curve segment based on the given time parameter.
 * 
 * This method calculates the local parameter within a curve segment, which is a value between 0 and 1,
 * based on the given time parameter. The local parameter represents the relative position within a segment
 * and is obtained by taking the modulus of the given time parameter.
 * 
 * @param t The time parameter for the curve segment.
 * 
 * @return The local parameter within the curve segment (value between 0 and 1).
 */
            protected float SegmentT(float t)
            {
                return t % 1;
            }

            
            /**
 * @brief Round down the given time parameter 't' and return the corresponding integer.
 *
 * This protected helper method takes a float 't' representing a time parameter,
 * rounds it down to the nearest integer, and returns the resulting integer value,
 * effectively removing the decimal part.
 *
 * @param t The input time parameter as a float.
 * @return The rounded down integer value of 't'.
 */
            protected int SegmentIndex(float t)
            {
                return Mathf.FloorToInt(t);
            }

            /**
 * @brief Calculate the time parameter 't' based on the given arc length.
 * 
 * This protected helper method computes the time parameter 't' as a float,
 * taking an input float 'length'. Since each spline segment has a time of "0" at the beginning
 * and "1" at the end, it iterates through the arc length positions until the length 'l'
 * is no longer greater than or equal to the arc length at index 'i + 1'.
 * Subsequently, it calculates the decimal part of the time parameter using the formula
 * for calculating time on a spline segment.
 * 
 * @param l The desired arc length.
 * @return The calculated time parameter 't'.
 */
            protected float TimeAt(float l)
            {
                int i = 0;

                while (i < _arcLengthPositions.Count - 2 && l >= _arcLengthPositions[i + 1]) i++;

                float t = (l - _arcLengthPositions[i]) / (_arcLengthPositions[i + 1] - _arcLengthPositions[i]);
                return i + t;
            }

            
            /**
 * @brief Perform cubic Hermite interpolation to calculate the position on the curve.
 * 
 * This public method conducts cubic Hermite interpolation to determine the position on the curve
 * for a given parameter value 't'. The interpolation takes into account tension, continuity, 
 * and bias factors, which are set to default values of 1 if not specified.
 * 
 * @param positions An array of four Vector3 positions used for interpolation.
 * @param t The parameter value for interpolation.
 * @param tension The tension factor for interpolation (default: 1).
 * @param continuity The continuity factor for interpolation (default: 1).
 * @param bias The bias factor for interpolation (default: 1).
 * @return The interpolated Vector3 position on the curve.
 */
            public Vector3 Interpolate(Vector3 point1, Vector3 point2, Vector3 point3,
                Vector3 point4,
                float tension, float continuity, float bias, float t)
            {
                if (t <= 0f)
                {
                    return point2;
                }
                else if (t >= 1f)
                {
                    return point3;
                }


                var tangent2 = ComputeTangent(point1, point2, point3, tension, continuity, bias);

                var tangent3 = ComputeTangent(point2, point3, point4, tension, continuity, bias);


                float h1 = (float)(2 * Math.Pow(t, 3) - 3 * Math.Pow(t, 2) + 1);
                float h2 = (float)((-2) * Math.Pow(t, 3) + 3 * Math.Pow(t, 2));
                float h3 = (float)(Math.Pow(t, 3) - 2 * Math.Pow(t, 2) + t);
                float h4 = (float)(Math.Pow(t, 3) - Math.Pow(t, 2));

                Vector3 newPoint = h1 * point2 + h2 * point3 + h3 * tangent2 + h4 * tangent3;


                return newPoint;
            }

            /**
 * @brief Calculate the arc length between two points on the interpolated curve.
 * 
 * This public method computes the arc length between two points on the curve,
 * where the points are obtained by interpolating four control points (p1, p2, p3, p4)
 * with specified interpolation factors. If the distance between the interpolated points
 * exceeds a specified threshold, the calculation is performed recursively for subsegments
 * until the threshold is met.
 * 
 * @param p1 The first control point.
 * @param p2 The second control point.
 * @param p3 The third control point.
 * @param p4 The fourth control point.
 * @param t1 The parameter value for the first interpolated point.
 * @param t2 The parameter value for the second interpolated point.
 * @param threshold The maximum distance threshold for recursive calculation (default: 0.1).
 * @return The computed arc length between the two interpolated points.
 */
            public float ArcLength(Vector3 p1, Vector3 p2, Vector3 p3,
                Vector3 p4, float t1, float t2, float threshold = 0.1f)
            {
                Vector3 interpolatedPoint1 =
                    Interpolate(p1, p2, p3, p4, tension, continuity, bias, t1);

                Vector3 interpolatedPoint2 =
                    Interpolate(p1, p2, p3, p4, tension, continuity, bias, t2);

                float distance = Vector3.Distance(interpolatedPoint1, interpolatedPoint2);

                if (distance < threshold)
                {
                    return distance;
                }
                else
                {
                    float tMid = t1 + (t2 - t1) / 2;
                    return ArcLength(p1, p2, p3, p4, t1, tMid, threshold) +
                           ArcLength(p1, p2, p3, p4, tMid, t2, threshold);
                }
            }

         
            /**
 * @brief Computes the arc length of a cubic Bezier curve segment at a specific parameter value.
 *
 * This function calculates the arc length of a cubic Bezier curve segment defined by its
 * control points at the specified parameter value.
 *
 * @param i The index of the curve segment.
 *
 * @return The arc length of the cubic Bezier curve segment at the given parameter value.
 */
            private float ComputeArcLength(int i)
            {
                Vector3[] segment = GetSegmentPositions(i);

                return ArcLength(segment[0], segment[1], segment[2], segment[3], 0f, 1f);
            }

            /**
 * @brief Retrieve the total arc length of the entire curve.
 * 
 * This public virtual method returns the total arc length of the entire curve,
 * which is obtained by accessing the last element of the '_arcLengthPositions' array.
 * 
 * @return The total arc length of the curve.
 */
            public virtual float ArcLength()
            {
                return _arcLengthPositions.Last();
            }

            /**
 * @brief Get the list of control points for the curve.
 * 
 * This public property provides read-only access to the list of control points
 * used in the curve. It returns the '_controlPoints' list.
 * 
 * @return The list of control points.
 */
            
            public List<Vector3> ControlPoints => _controlPoints;


            /**
 * @brief Check if the curve has any control points.
 * 
 * This public method returns a boolean indicating whether the curve
 * has any control points. It checks if the count of control points
 * in the '_controlPoints' list is equal to zero.
 * 
 * @return True if the curve has no control points, otherwise false.
 */
            public bool IsEmpty()
            {
                return _controlPoints.Count == 0;
            }

            /**
 * @brief Finalize the curve, updating arc length information.
 * 
 * This public virtual method finalizes the curve by updating the arc length information.
 * If the curve has less than two control points, no action is taken. Otherwise, the method
 * adds the computed arc length for the last segment to the '_arcLengthPositions'.
 * 
 * @remarks The method assumes that '_arcLengthPositions' and '_controlPoints' are appropriately initialized.
 */
            public virtual void Finish()
            {
                
                if (_controlPoints.Count < 2) return;
                _arcLengthPositions.Add(_arcLengthPositions.Last() + ComputeArcLength(_controlPoints.Count - 2));
                
            }

            
            /**
 * @brief Check if the curve has been fully processed and finalized.
 * 
 * This public method returns true if the curve has been fully processed and finalized,
 * and false otherwise. It checks if the number of control points is greater than three
 * and if the count of '_arcLengthPositions' is equal to the count of control points.
 * 
 * @return True if the curve is finished, otherwise false.
 */
            public bool IsFinished()
            {
                if(this is BaseCurve) Debug.Log("BaseCurve is finished"+ (_controlPoints.Count >= 2 && _arcLengthPositions.Count == _controlPoints.Count));
                Debug.Log(  "Curve is finished"+
                          (_controlPoints.Count >= 2 && _arcLengthPositions.Count == _controlPoints.Count) + "Differenz: " +
                              (_arcLengthPositions.Count - _controlPoints.Count));
                return _controlPoints.Count >= 2  && _arcLengthPositions.Count == _controlPoints.Count;
            }

         
            
        }
    }
}