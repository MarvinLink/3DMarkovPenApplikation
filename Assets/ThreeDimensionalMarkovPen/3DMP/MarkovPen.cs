using System;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

namespace ThreeDimensionalMarkovPen._3DMP
{
    //@class MarkovPen
    //@brief The Markov Pen is a technique for generating character styles."
    /*
     *This class serves as the base for other partial classes. It is an GameObject in Unity and needs therefore derive from MonoBehaviour.
    */
    public partial class MarkovPen : MonoBehaviour

    {
        private Mapping _exampleMapping;
        private Mapping _targetMapping;
        private Synthesizer _synthesizer;
        private Curve _exampleStyleCurve;
        private Curve _targetStyleCurve;
        private BaseCurve _exampleBaseCurve;
        private BaseCurve _targetBaseCurve;


        /**
 * @brief Initializes the MarkovPen base class.
 * 
 * This method is responsible for setting up the MarkovPen by providing it with an example mapping, which represents the relation
 * between arc length positions and offsets from style to base curve.
 * 
 * @param exampleMapping A Mapping computed from the example curves, containing the association between arc length positions and offsets.
 */
        public void Initialize(Mapping exampleMapping)
        {
            _exampleMapping = exampleMapping;
            Debug.Log("mapping " + _exampleMapping.IsEmpty());
            _synthesizer = new Synthesizer(_exampleMapping);
        }

        /**
 * @brief Reconstructs the targetMapping using the Synthesizer class.
 * 
 * This method calls the Synthesizer class to reconstruct the targetMapping in the same way as the exampleMapping. 
 * It takes a targetMapping, which represents the association between arc length positions and offsets for a growing targetBaseCurve 
 * and an empty targetStyleCurve.
 * 
 * @param targetMapping A Mapping representing the growing targetBaseCurve and an empty targetStyleCurve.
 * @return A list of Tuple<Vector3, Vector3> representing the reconstructed points on the target curve.
 */
        public List<Tuple<Vector3, Vector3>> Reconstruct(Mapping targetMapping)
        {
            
                List<Tuple<Vector3, Vector3>> result = _synthesizer.Reconstruct(targetMapping);
                return result;
            
           
        }
        /**
 * @brief Checks if the MarkovPen is trained.
 * 
 * This method returns a boolean value indicating whether the MarkovPen is considered trained. 
 * It checks if the _exampleMapping is not null.
 * 
 * @return True if the MarkovPen is trained (exampleMapping is not null), false otherwise.
 */
        public bool IsTrained()
        {
            return _exampleMapping != null;
        }
        
         
        /**
 * @brief Clears the MarkovPen by setting the synthesizer and exampleMapping to null.
 * 
 * This method resets the MarkovPen by nullifying both the synthesizer and exampleMapping attributes.
 * It is used to clean up the MarkovPen's state, making it ready for new training or operations.
 */

        public void Clear()
        {
            _synthesizer = null;
            _exampleMapping = null;
        }
    }
}