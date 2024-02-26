using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace ThreeDimensionalMarkovPen._3DMP
{
    public partial class MarkovPen
    {
        /**
 * @class BaseCurve
 * @brief Represents the base curve of the MarkovPen.
 *
 * The BaseCurve class extends the functionality of the Curve class and provides
 * additional features such as projection, spline functionalities, and smoothing functionalities.
 */
        public class BaseCurve : Curve
        {
            public BaseCurve() : base(0.25f)
            {
            }

            private float _tap = 0f;
            private List<Vector3> _upVectors = new List<Vector3>();

            private List<Vector3> _smoothNormals = new List<Vector3>();

            /**
     * @brief Set the tap value for smoothing.
     *
     * This method sets the tap value used for smoothing. A non-zero tap initiates
     * the smoothing process during the addition of control points.
     *
     * @param tap The tap value for smoothing.
     */
            public void SetTap(float tap)
            {
                _tap = tap;
            }
            
            /**
     * @brief Add a control point to the base curve and update related information.
     *
     * This method extends the base class method to incorporate smoothing functionalities
     * based on the tap value. It computes normals, smooth tangents, and smooth normals
     * as part of the smoothing process.
     *
     * @param controlPoint The control point to be added to the base curve.
     */
            public override void AddControlPoint(Vector3 controlPoint, Vector3 upVector)
            {
                base.AddControlPoint(controlPoint, upVector);
                
                if (_tap == 0) return;


                _upVectors.Add(_upVectors.Count > 0
                    ? (0.25f * upVector + 0.75f * _upVectors.Last()).normalized
                    : upVector);


                if (_controlPoints.Count < 4) return;
                while (_tap <= _arcLengthPositions.Last() - _arcLengthPositions[_smoothNormals.Count])
                {
                    int index = _smoothNormals.Count;
                    _smoothNormals.Add(ComputeSmoothNormal(_upVectors[index],
                        ComputeSmoothTangent(_arcLengthPositions[index])));
                    if (_smoothNormals.Count > 1 && Vector3.Dot(_smoothNormals[^1], _smoothNormals[^2]) < 0)
                        _smoothNormals[^1] *= -1;
                    
                }
            }

            /**
 * @brief Computes a smoothed tangent vector based on the specified center position.
 *
 * This function calculates a smoothed tangent vector by averaging the normalized first
 * derivatives at positions within a window around the given center position.
 *
 * @param center The center position around which the smoothed tangent is calculated.
 *
 * @return A vector representing the computed smoothed tangent.
 */
            private Vector3 ComputeSmoothTangent(float center)
            {
                float windowSize = 2 * _tap + 1;
                Vector3 smoothTangent = Vector3.zero;
                for (float l = center - _tap;
                     l <= center + _tap;
                     l += (1.0f / windowSize))
                {
                    smoothTangent += FirstDerivativeAt(l).normalized;
                }

                return smoothTangent / windowSize;
            }


            /**
 * @brief Computes a smoothed normal vector based on the provided up vector and smooth tangent.
 *
 * This function calculates a smoothed normal vector by projecting the up vector onto
 * the smooth tangent and subtracting it from the up vector. The result is then normalized.
 *
 * @param upVector The original up vector to be smoothed.
 * @param smoothTangent The smooth tangent vector to influence the smoothing.
 *
 * @return A normalized vector representing the computed smoothed normal.
 */
            private Vector3 ComputeSmoothNormal(Vector3 upVector, Vector3 smoothTangent)
            {
                upVector = upVector.normalized * 100;
                smoothTangent = smoothTangent.normalized;
                float projection = Vector3.Dot(upVector, smoothTangent);

                return (upVector - smoothTangent * projection).normalized;
            }

            /**
 * @brief Evaluate the first derivative of a cubic Hermite spline at a specified parameter t.
 *
 * This function calculates the first derivative of a cubic Hermite spline at a given parameter t
 * based on four control points and specified tension, continuity, and bias parameters.
 *
 * @param point1 The first control point of the spline.
 * @param point2 The second control point of the spline.
 * @param point3 The third control point of the spline.
 * @param point4 The fourth control point of the spline.
 * @param tension Tension parameter affecting the shape of the spline.
 * @param continuity Continuity parameter affecting the smoothness of the spline.
 * @param bias Bias parameter affecting the directionality of the spline.
 * @param t The parameter at which to evaluate the first derivative (should be in the range [0, 1]).
 *
 * @return A Vector3 representing the first derivative of the spline at the specified parameter t.
 *         If t is less than or equal to 0, the derivative at the start point (point2) is returned.
 *         If t is greater than or equal to 1, the derivative at the end point (point3) is returned.
 */
            public static Vector3 EvaluateFirstDerivative(Vector3 point1, Vector3 point2, Vector3 point3,
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


                float h1 = (float)(6 * Math.Pow(t, 2) - 6 * Math.Pow(t, 1));
                float h2 = (float)((-6) * Math.Pow(t, 2) + 6 * Math.Pow(t, 1));
                float h3 = (float)(3 * Math.Pow(t, 2) - 4 * Math.Pow(t, 1) + 1);
                float h4 = (float)(3 * Math.Pow(t, 2) - 2 * Math.Pow(t, 1));

                Vector3 newPoint = h1 * point2 + h2 * point3 + h3 * tangent2 + h4 * tangent3;


                return newPoint;
            }
        

            /**
 * @brief Retrieve the smoothed normal vector at a specified arc length along the curve.
 * 
 * This method retrieves the smoothed normal vector at a specified arc length 'l' along the curve.
 * It utilizes cubic Hermite interpolation to calculate the smoothed normal based on the
 * corresponding time parameter and the neighboring smoothed normals.
 * 
 * @param l The arc length at which to retrieve the smoothed normal vector.
 * @return The computed smoothed normal vector at the specified arc length.
 */
            public Vector3 SmoothNormalAt(float l)
            {
                if (_tap == 0)
                {
                    Vector3 line = Vector3.Normalize(_controlPoints.Last() - _controlPoints.First());
                    return new Vector3(-line.y, line.x, 0f);
                }

                Vector3 normal;

                if (l <= 0)
                    return _smoothNormals[0];
                else if (l >= _arcLengthPositions.Last())
                    return _smoothNormals.Last();
                else
                {
                    float t = TimeAt(l);

                    int index = SegmentIndex(t);

//                    Debug.Log("sn: "+ _smoothNormals.Count);
                    Debug.Log("index: " + (_controlPoints.Count - index));
                    if (index >= 0 && index + 2 <= _smoothNormals.Count)
                    {
                        normal = Interpolate(index == 0 ? _smoothNormals[0] : _smoothNormals[index - 1].normalized,
                            _smoothNormals[index].normalized,
                            _smoothNormals[index + 1].normalized,
                            index == _smoothNormals.Count - 2
                                ? _smoothNormals[index + 1].normalized
                                : _smoothNormals[index + 2].normalized, 0, 0, 0, SegmentT(t));
                    }
                    else if (index < 0)
                    {
                        throw new ArgumentOutOfRangeException("The Argument is out of Range, due index to small.");
                    }
                    else if (index + 2 > _smoothNormals.Count)
                    {
                        throw new ArgumentOutOfRangeException("The Argument is out of Range, due index to big.");
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("The Argument is out of Range, but its not clear why.");
                    }
                }


                return normal.normalized;
            }

            /**
 * @brief Compute the total arc length of the curve.
 * 
 * This method computes the total arc length of the curve based on the smoothed normals.
 * The arc length is calculated as the last value in the precomputed arc length positions.
 * 
 * @return The total arc length of the curve.
 */
            public override float ArcLength()
            {
                if (_upVectors.Count < 4) return 0;

                return _arcLengthPositions[Math.Max(_smoothNormals.Count - 1, 0)];
            }


            /**
 * @brief Retrieve the tangent vector at a specified arc length along the curve.
 * 
 * This method retrieves the tangent vector at a specified arc length 'l' along the curve.
 * It uses cubic Hermite interpolation to calculate the tangent based on the corresponding time
 * parameter and the control points of the curve segment.
 * 
 * @param l The arc length at which to retrieve the tangent vector.
 * @return The computed tangent vector at the specified arc length.
 */
            public Vector3 FirstDerivativeAt(float l)
            {
                Vector3 firstDerivative;
                if (l <= 0)
                    firstDerivative = ComputeTangent(_controlPoints[0], _controlPoints[0], _controlPoints[1], 0, 0, 0);
                else if (l >= _arcLengthPositions.Last())
                    firstDerivative = ComputeTangent(_controlPoints[_controlPoints.Count - 2],
                        _controlPoints[_controlPoints.Count - 1],
                        _controlPoints[_controlPoints.Count - 1], 0, 0, 0);
                else
                {
                    float t = TimeAt(l);

                    Vector3[] segment = GetSegmentPositions(SegmentIndex(t));


                    firstDerivative = EvaluateFirstDerivative(segment[0], segment[1], segment[2], segment[3], 0, 0, 0,
                        SegmentT(t));
                }

                return firstDerivative;
            }


            /**
 * @brief Project a point onto the curve and return the arc length positions of the projections.
 * 
 * This method projects a given point 'toProject' onto the curve and returns a list of arc length
 * positions corresponding to the projections. It uses the normalized tangent vectors and normal
 * vectors of each curve segment for projection.
 * 
 * @param toProject The point to be projected onto the curve.
 * @return A list of arc length positions corresponding to the projections.
 */
            public List<float> Project(Vector3 toProject)
            {
                List<float> projections = new List<float>();
                Vector3 line = Vector3.Normalize(_controlPoints.Last() - _controlPoints.First());
                Vector3 normal = new Vector3(-line.y, line.x, 0f);
                for (int i = 1; i < _arcLengthPositions.Count; ++i)
                {
                    Project(toProject, _arcLengthPositions[i - 1], _arcLengthPositions[i], projections, normal);
                }

                if (projections.Count == 0)
                {
                    if (Vector3.Distance(toProject, _controlPoints[0]) <
                        Vector3.Distance(toProject, _controlPoints[^1]))
                    {
                        Vector3 toPoint = toProject - _controlPoints[0];
                        Vector3 tangentFirst =
                            -ComputeTangent(_controlPoints[0], _controlPoints[0], _controlPoints[1], 0, 0, 0)
                                .normalized;
                        float l1 = Vector3.Dot(toPoint, tangentFirst);
                        projections.Add(-l1);
                    }
                    else
                    {
                        Vector3 toPoint = toProject - _controlPoints[^2];
                        Vector3 tangentLast =
                            ComputeTangent(_controlPoints[^2], _controlPoints[^1], _controlPoints[^1], 0, 0, 0)
                                .normalized;
                        float l2 = Vector3.Dot(toPoint, tangentLast);
                        projections.Add(_arcLengthPositions.Last() + l2);
                    }
                }

                return projections;
            }

            /**
 * @brief Recursively project a point onto a curve segment and update the arc length positions.
 * 
 * This method recursively projects a given point 'point' onto a curve segment defined by the
 * arc length positions 'l1' and 'l2'. It updates the list of arc length positions 'projections'
 * based on the recursive projection process. The projection is performed using a shooting method
 * with the given normal vector.
 * 
 * @param point The point to be projected onto the curve segment.
 * @param l1 The starting arc length position of the curve segment.
 * @param l2 The ending arc length position of the curve segment.
 * @param projections The list to store the resulting arc length positions.
 * @param normal The normal vector used for the shooting method.
 */
            private void Project(Vector3 point, float l1, float l2, List<float> projections, Vector3 normal)
            {
                double d1 = Shoot(point, normal, l1);
                double d2 = Shoot(point, normal, l2);


                if (d1 < 0.0 && d2 < 0.0)
                    return;

                if (d1 > 0.0 && d2 > 0.0)
                    return;

                float middle = l1 + (l2 - l1) / 2.0f;

                if (Math.Abs(d1) < 0.25 && Math.Abs(d2) < 0.25)
                {
                    projections.Add(middle);
                    return;
                }

                Project(point, l1, middle, projections, normal);
                Project(point, middle, l2, projections, normal);
            }

            /**
 * @brief Perform a shooting method to calculate the signed distance from a point to the curve.
 * 
 * This method performs a shooting method to calculate the signed distance from a given point
 * to the curve at a specified arc length position 'l'. It uses the normal vector of the curve
 * to perform the shooting and determine the closest point on the curve.
 * 
 * @param point The point from which to calculate the distance to the curve.
 * @param normal The normal vector used for the shooting method.
 * @param l The arc length position on the curve.
 * @return The signed distance from the point to the curve.
 */
            private float Shoot(Vector3 point, Vector3 normal, float l)
            {
                Vector3 basePoint = PositionAt(l);
                Vector3 toPoint = point - basePoint;

                normal.Normalize();

                float projection = Vector3.Dot(normal, toPoint);
                Vector3 offset = normal * projection;
                Vector3 closestPoint = basePoint + offset;

                float dist = Vector3.Distance(point, closestPoint);

                double determinant = toPoint.x * normal.y - toPoint.y * normal.x;
                if (determinant < 0)
                {
                    dist *= -1;
                }

                return dist;
            }

            /**
 * @brief Finalize the curve by computing smoothed tangents and normals for the remaining control points.
 * 
 * This method finalizes the curve by computing smoothed tangents and normals for the remaining
 * control points. It ensures that the curve is prepared for further operations, and the smoothing
 * process is completed for all control points beyond the initially smoothed range.
 */
            public override void Finish()
            {
                base.Finish();
                if (_controlPoints.Count < 2 || _tap == 0) return;
                for (int i = _smoothNormals.Count; i < _controlPoints.Count; i++)
                {
                    _smoothNormals.Add(ComputeSmoothNormal(_upVectors[i],
                        ComputeSmoothTangent(_arcLengthPositions[i])));
                }
            }
        }
    }
}