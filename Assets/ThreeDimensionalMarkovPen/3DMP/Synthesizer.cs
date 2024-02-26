using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace ThreeDimensionalMarkovPen._3DMP
{
    partial class MarkovPen
    {
        
        /**
 * @class Synthesizer
 * @brief The Synthesizer class is responsible for reconstructing the targetMapping based on the exampleMapping.
 *
 * This class is part of the MarkovPen and plays a key role in generating associations for the targetMapping
 * by utilizing the information from the exampleMapping. It calculates offsets and inflates associations to
 * reconstruct the relationship between curves.
 */
        private class Synthesizer
        {
            private Mapping _exampleMapping;

            
            /**
     * @brief Empty constructor for the Synthesizer class.
     *
     * This constructor initializes the Synthesizer with a null exampleMapping.
     */
            public Synthesizer()
            {
                _exampleMapping = null;
            }

            
            /**
     * @brief Constructor for the Synthesizer class.
     *
     * This constructor initializes the Synthesizer with the provided exampleMapping.
     *
     * @param exampleMapping The exampleMapping used for generating associations.
     */
            public Synthesizer(Mapping exampleMapping)
            {
                _exampleMapping = exampleMapping;
            }

            /**
     * @brief Reconstructs the targetMapping by generating associations based on the exampleMapping.
     *
     * This method iteratively applies offsets to the targetMapping, generating associations and inflating points.
     *
     * @param targetMapping The targetMapping to be reconstructed.
     * @return A list of associations representing the reconstructed relationship between curves.
     */
            
            public List<Tuple<UnityEngine.Vector3, UnityEngine.Vector3>> Reconstruct(Mapping targetMapping)
            {
               
                float offset = _exampleMapping.MaxOffset;
                if(targetMapping.IsEmpty()) targetMapping.SetMaxOffset(offset);
                

                int index = (targetMapping._lastIndex + 1) % _exampleMapping.GetMapping.Count;

                List<Tuple<UnityEngine.Vector3, UnityEngine.Vector3>> associations = new List<Tuple<UnityEngine.Vector3, UnityEngine.Vector3>> ();
                    
                while (true)
                {
                    Vector2 offsets = _exampleMapping.GetOffsets(index);
                 
                    if (!targetMapping.Apply(offsets, index))
                    {
                        break;
                    }
                    associations.Add(targetMapping.Inflate(targetMapping.GetAssociation(targetMapping.GetMapping.Count-1)));
                 // Increment the index in a circular manner to iterate through the example mapping
                    index = (index + 1) % _exampleMapping.GetMapping.Count;
                }


                return associations;
            }

            
            /**
 * @brief Checks if the Synthesizer is trained with an exampleMapping.
 *
 * This method determines whether the Synthesizer has been initialized with a valid exampleMapping.
 *
 * @return True if the Synthesizer is trained (exampleMapping is not null), false otherwise.
 */

            public bool IsTrained()
            {
                return _exampleMapping != null;
            }

            /**
 * @brief Clears the exampleMapping in the Synthesizer.
 *
 * This method sets the exampleMapping attribute to null, effectively clearing the training data in the Synthesizer.
 */
            public void Clear()
            {
                _exampleMapping = null;
            }
        }
    }
}