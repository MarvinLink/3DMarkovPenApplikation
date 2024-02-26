﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRSketchingGeometry.Splines;

namespace ThreeDimensionalMarkovPen._3DMP
{
    partial class MarkovPen
    {
        /**
 * @brief Represents a mapping between arc length positions and offsets from style to base curve.
 * 
 * The Mapping class establishes a relation between arc length positions and offsets from a style curve
 * to a base curve. It samples the style curve, projects the samples onto the base curve, and computes
 * the mapping along with maximum offsets.
 */
        public class Mapping
        {
            //Curves
            public BaseCurve BaseCurve { get; private set; }
            private Curve _styleCurve;

            //SamplingInterval
            private const float SAMPLINGINTERVAL = 0.025f;
            private float _samplingInterval = SAMPLINGINTERVAL;

            //Mapping
            private List<Vector2> _mapping = new List<Vector2>();

            //Offsets
            private List<float> _offsetsAlongCurve;
            private float _maxOffset = 0f;
            public int _lastIndex { get; private set; }
            

            
            /**
     * @brief Constructor for the Mapping Class.
     * @param styleCurve The style curve for the mapping.
     * @param baseCurve The base curve for the mapping.
     * @throws NullReferenceException if styleCurve or baseCurve is null.
     */
            public Mapping(Curve styleCurve,
                BaseCurve baseCurve)
            {
                Debug.Log("ENter");
                _lastIndex = -1;
                if (styleCurve == null) throw new NullReferenceException("StyleCurve must not be null");
                if (baseCurve == null) throw new NullReferenceException("BaseCurve must not be null");
                Debug.Log("ENter2");
                this.BaseCurve = baseCurve;
                this._styleCurve = styleCurve;
                if (_styleCurve.ArcLength() < _samplingInterval) return;
                Debug.Log("ENter3");
                //compute sampling interval
                float samplingInterval = ComputeSamplingInterval();
                //sample style curve
                List<Vector3> samples = SampleStyleCurveUniformly(samplingInterval);
                //project samples to base curve
                List<float> projections = Project(samples);
                //compute mapping
                ComputeMapping(samples, projections);
                //compute maximum offset
                ComputeMaxOffset();
                //compute offsets
                ComputeOffsets();

            }

            /**
 * @brief Sample the style curve uniformly based on the given sampling interval.
 * 
 * This method computes samples along the example style curve using the specified sampling interval,
 * aligning the samples to evenly cover the arc length of the curve.
 * 
 * @param samplingInterval The interval representing arc length for sampling.
 * @return A list of Vector3 representing the sampled points on the style curve.
 */
            public List<Vector3> SampleStyleCurveUniformly(float samplingInterval)
            {
                List<Vector3> samples = new List<Vector3>();
                int numSamples = Mathf.RoundToInt(_styleCurve.ArcLength() / _samplingInterval) + 1;
                for (int i = 0; i < numSamples; i++)
                {
                    samples.Add(_styleCurve.PositionAt(i * samplingInterval));
                }

                return samples;
            }

            /**
 * @brief Projects a list of 3D samples onto the base curve and returns a list of projections.
 *
 * This function takes a list of 3D samples and projects each sample onto the base curve,
 * returning a list of the resulting projections.
 *
 * @param samples A list of 3D vectors representing the samples to be projected.
 *
 * @return A list of float values representing the projections of the samples onto the base curve.
 */
            public List<float> Project(List<Vector3> samples)
            {
                List<float> projections = new List<float>();
                foreach (var sample in samples)
                {
                    float projection = BaseCurve.Project(sample)[0];
                    projections.Add(projection);
                }
                return projections;
            }

            /**
 * @brief Compute the sampling interval to evenly sample the arc length of the style curve.
 * 
 * The goal is to adjust the sampling interval so that the style curve is sampled without overshooting,
 * and the first sample on the curve is at the very beginning, and the last sample is at the very end.
 * 
 * @return The computed sampling interval.
 */
            public float ComputeSamplingInterval()
            {
                int numSamples = Mathf.RoundToInt(_styleCurve.ArcLength() / _samplingInterval);
                return _styleCurve.ArcLength() / numSamples;
            }

            /**
 * @brief Associate arc length positions of projected points to the corresponding offsets.
 * 
 * In this method, the association of arc length positions of the projected points to their corresponding offsets happens.
 * 
 * @param samples A list of Vector3 representing the samples aligned along the style curve.
 * @param projections A list of float representing the projected point samples in arc length positions aligned along the base curve.
 */
            private void ComputeMapping(List<Vector3> samples, List<float> projections)
            {
                _mapping = new List<Vector2>();
                for (int i = 0; i < samples.Count; ++i)
                {
                    Vector3 basePoint = BaseCurve.PositionAt(projections[i]);
                    Vector3 smoothNormal = BaseCurve.SmoothNormalAt(projections[i]);
                    Vector3 toSample = samples[i] - basePoint;
                    float offsetAlongNormal = Vector3.Distance(basePoint, samples[i]);
                    if (Vector3.Dot(smoothNormal, toSample) < 0) offsetAlongNormal *= -1;
//                    Debug.Log("association: " + new Vector2(projections[i], offset));
                    _mapping.Add(new Vector2(projections[i], offsetAlongNormal));
                }
            }

            /**
 * @brief Compute the maximal offset from the associations in the mapping.
 * 
 * This method iterates through the associations in the mapping and computes the maximal offset.
 */
            private void ComputeMaxOffset()
            {
                _maxOffset = 0;
                foreach (var association in _mapping)
                {
                    _maxOffset = Math.Max(_maxOffset, Math.Abs(association.y));
                }
            }

            
            /**
 * @brief Compute the offsets by iterating through the mapping and provide repetition by removing the last offset and mapping after every loop.
 * 
 * This method synchronizes the start and end points in the mapping, computes offsets, and ensures repetition by removing the last offset and mapping after every loop.
 */
            private void ComputeOffsets()
            {
                // Synchronize start and end point in mapping
                _offsetsAlongCurve = new List<float>(_mapping.Count);

                for (int i = 1; i < _mapping.Count; ++i)
                {
                    _offsetsAlongCurve.Add(_mapping[i].x - _mapping[i - 1].x);
                }

                if (IsRepetitive())
                {
                    _offsetsAlongCurve.Insert(0, _offsetsAlongCurve.Last());
                    _offsetsAlongCurve.RemoveAt(_offsetsAlongCurve.Count - 1);
                    _mapping.RemoveAt(_mapping.Count - 1);
                }
            }

            /** @brief Sets the tap of the BaseCurve to the computed max offset. 
             */
            public void SetMaxOffset(float offset)
            {
                if (IsEmpty()) ;
                BaseCurve.SetTap(offset);
                //_maxOffset = offset;
                
            }

            /**
 * @brief Check if the mapping is configured as repetitive.
 * 
 * This method returns a boolean indicating whether the mapping is considered repetitive.
 * 
 * @note The current implementation always returns true. Consider configuring this based on user preferences in the future.
 * @return true if the mapping is considered repetitive, false otherwise.
 */
            public bool IsRepetitive()
            {
                return true;
            }

            /**
 * @brief Inflate an association of the mapping to obtain a Tuple of 3D points.
 * 
 * This method takes a 2D association from the mapping and inflates it to generate a Tuple of 3D points. 
 * The first point is the base point obtained from the BaseCurve at the specified arc length position, 
 * and the second point is obtained by adding the inflated vector in the direction of the smoothed normal.
 * 
 * @param association The 2D association from the mapping containing arc length position and offset.
 * @return A Tuple of two Vector3 points representing the inflated segment.
 */
            public Tuple<Vector3, Vector3> Inflate(Vector2 association)
            {
                Vector3 basePoint = BaseCurve.PositionAt(association.x);
                Vector3 normal = BaseCurve.SmoothNormalAt(association.x);
                Vector3 toPoint = Vector3.Scale(normal.normalized,
                    new Vector3(association.y, association.y, association.y));

                return new Tuple<Vector3, Vector3>(basePoint, basePoint + toPoint);
            }

            
            /**
 * @brief Get the offsets for a given index in the mapping.
 * 
 * This method returns a Vector2 containing the offset values for the specified index in the mapping. 
 * The x-component of the Vector2 represents the offset from the _offsets list, 
 * and the y-component represents the offset from the _mapping list.
 * 
 * @param index The index for which offsets are requested.
 * @return A Vector2 containing the offset values.
 */
            public Vector2 GetOffsets(int index)
            {
                return new Vector2(_offsetsAlongCurve[index], _mapping[index].y);
            }

            /**
 * @brief Apply offsets to the mapping at a specific index and update the style curve.
 * 
 * This method applies the given offsets to the mapping at the specified index. It calculates the current arcLength based on the 
 * last entry in the mapping and the x-component of the provided offsets. If the calculated arcLength exceeds the arcLength of the 
 * base curve, the method returns false, indicating that the application of offsets is not successful. Otherwise, it adds a new entry 
 * to the mapping with the updated arcLength and the y-component of the provided offsets. Additionally, it updates the style curve by 
 * appending the inflated point corresponding to the new mapping entry.
 * 
 * @param offsets A Vector2 containing the offsets to be applied. The x-component is added to the current arcLength, and the y-component 
 * is used to inflate the style curve.
 * @param index The index at which the offsets are applied.
 * @return True if the offsets are successfully applied, false otherwise.
 */
            public bool Apply(Vector2 offsets, int index)
            {
                // Calculate the current arcLength
                float l = IsEmpty() ? 0 : _mapping.Last().x + offsets.x;

                if (l >= BaseCurve.ArcLength())
                {
                    // Check if the current arcLength exceeds the arcLength of the base curve
                    return false;
                }
                // Update _mapping by adding a point out of current arcLength and its offset
                _mapping.Add(new Vector2(l, offsets.y));
                
                _styleCurve.Append(Inflate(_mapping.Last()).Item2);
                _lastIndex = index;
                return true;
            }

            /**
 * @brief Check if the mapping is empty.
 * 
 * This method checks if the mapping is empty by examining the count of entries in the mapping list. 
 * 
 * @return True if the mapping is empty, false otherwise.
 */
            public bool IsEmpty()
            {
                return _mapping.Count == 0;
            }

            /**
 * @brief Clears attributes related to the mapping.
 * 
 * This method clears various attributes associated with the mapping, including the base curve, style curve, mapping list, offsets list,
 * maximum offset, and sampling interval. After calling this method, the mapping will be in a cleared state.
 */
            public void Clear()
            {
                BaseCurve = null;
                _styleCurve = null;
                _mapping.Clear();
                _offsetsAlongCurve.Clear();
                _maxOffset = 0.0f;
                _samplingInterval = SAMPLINGINTERVAL;
            }
            
            /**
 * @brief Gets the mapping associated with the instance.
 * 
 * This property provides access to the mapping, which represents the association of arc length positions to offsets 
 * from the style to the base curve.
 * 
 * @return A list of Vector2 containing the mapping entries, where x represents arc length position and y represents offset.
 */
            public List<Vector2> GetMapping => _mapping;

            /**
 * @brief Gets the maximum offset associated with the mapping instance.
 * 
 * This property provides access to the maximum offset computed from the associations in the mapping. The maximum offset
 * represents the highest offset value among all associations in the mapping.
 * 
 * @return A float value representing the maximum offset.
 */
            public float MaxOffset => _maxOffset;


            /**
 * @brief Gets the association at the specified index.
 * 
 * This method provides access to the association at a given index in the mapping. The association consists of arc length
 * position (x) and offset (y) from the style to the base curve.
 * 
 * @param index The index of the association to retrieve.
 * @return A Vector2 representing the association, where x is the arc length position and y is the offset.
 */
            public Vector2 GetAssociation(int index)
            {
                return _mapping[index];
            }
            
            
        }
    }
}