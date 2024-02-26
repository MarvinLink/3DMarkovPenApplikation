using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThreeDimensionalMarkovPen.Curves.New;

using ThreeDimensionalMarkovPen.Version2._0;

//using ThreeDimensionalMarkovPen.Version2._0.ThreeDimensionalMarkovPen.New;
//using ThreeDimensionalMarkovPen.Version2._0.ThreeDimensionalMarkovPen.New;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;
using LineSketchObject = CENTIS.UnitySketchingKernel.SketchObjectManagement.LineSketchObject;
using UnityEngine;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.UIElements.MouseButton;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace ThreeDimensionalMarkovPen.Control
{
    public class InputController : MonoBehaviour
    {
        

        //Sketching
        public SketchWorld sketchWorld;
        public DefaultReferences defaults;
        public PointAddingHandler pointAddingHandler;

        //Performance
        public LineSketchObjectFactory lineSketchObjectFactory;

        //MarkovPen
        private _3DMP.MarkovPen.Mapping _targetMapping;
        private VRSketchingGeometry.SketchObjectManagement.LineSketchObject _previousLineSketchObject;
        public _3DMP.MarkovPen markovPen;
        public _3DMP.MarkovPen.Curve _exampleStyleCurve = new _3DMP.MarkovPen.Curve();
        public _3DMP.MarkovPen.BaseCurve _exampleBaseCurve = new _3DMP.MarkovPen.BaseCurve();
        public _3DMP.MarkovPen.BaseCurve _targetBaseCurve = new _3DMP.MarkovPen.BaseCurve();
        public _3DMP.MarkovPen.Curve _targetStyleCurve = new _3DMP.MarkovPen.Curve(1);
        
        //Control
        public InputActionProperty inputActionPropertyTrigger;
        public InputActionProperty inputActionPropertyGrip;
    


        private void Start()
        {
            sketchWorld = Instantiate(defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
            sketchWorld.gameObject.SetActive(false);
            lineSketchObjectFactory = GameObject.Find("Performance").GetComponent<LineSketchObjectFactory>();
            lineSketchObjectFactory.Initialize(defaults);

            pointAddingHandler = GameObject.Find("LineSketchUtilities").GetComponent<PointAddingHandler>();
            markovPen = GameObject.Find("MarkovPen").GetComponent<_3DMP.MarkovPen>();


          
            inputActionPropertyTrigger.action.performed += OnInputActionPerformed;
            inputActionPropertyTrigger.action.canceled += OnInputActionReleased;
            inputActionPropertyGrip.action.started += OnInputGripPerformed;
            _exampleStyleCurve = new _3DMP.MarkovPen.Curve();
            _exampleBaseCurve = new _3DMP.MarkovPen.BaseCurve();
            _targetBaseCurve = new _3DMP.MarkovPen.BaseCurve();
            _targetStyleCurve = new _3DMP.MarkovPen.Curve(1);
        }


        private void OnInputActionPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("Trigger Performed");
            if (inputActionPropertyTrigger.action.triggered)
            {
                pointAddingHandler.activeLineSketchObject = lineSketchObjectFactory.GetLineSketchObjectFromPool();

                if (_exampleBaseCurve.IsEmpty() && _exampleStyleCurve.IsEmpty() &&
                    !markovPen.IsTrained())
                {
                    Debug.Log("active Curve ExampleBaseCurve");
                    pointAddingHandler.activeCurve = _exampleBaseCurve;
                }

                else if (_exampleBaseCurve.IsFinished() && _exampleStyleCurve.IsEmpty() &&
                         !markovPen.IsTrained())
                {
                    Debug.Log("active Curve Examplestylecurve");
                    pointAddingHandler.activeCurve = _exampleStyleCurve;
                }

                else if (_exampleBaseCurve.IsFinished() && _exampleStyleCurve.IsFinished() &&
                         markovPen.IsTrained() && _targetMapping == null)
                {
                    pointAddingHandler.activeCurve = _targetBaseCurve;
                    
                    pointAddingHandler.activeLineSketchObject.gameObject.SetActive(false);
                    _targetMapping = new _3DMP.MarkovPen.Mapping(_targetStyleCurve, _targetBaseCurve);
                    pointAddingHandler.activeMapping = _targetMapping;
                    VRSketchingGeometry.SketchObjectManagement.LineSketchObject line =  lineSketchObjectFactory.GetLineSketchObjectFromPool();
                    line.SetSmoohtness(4);
                    pointAddingHandler.activeTargetStyleLineSketchObject = line;

                }
            }
        }


        private void OnInputActionReleased(InputAction.CallbackContext context)
        {
            Debug.Log("TriggerReleased");
            if (!inputActionPropertyTrigger.action.triggered)
            {
            
                    pointAddingHandler.EndLine();


                    if (_exampleBaseCurve.IsFinished() && _exampleStyleCurve.IsFinished() &&
                        !markovPen.IsTrained())
                    {
                        
                        Debug.Log("Initialize Mapping");
                        XRInteractorLineVisual lineRenderer = GameObject.Find("DrawHand").GetComponent<XRInteractorLineVisual>();
                        lineRenderer.enabled = false;
                        
                    
                        _3DMP.MarkovPen.Mapping exampleMapping =
                            new _3DMP.MarkovPen.Mapping(_exampleStyleCurve, _exampleBaseCurve);

                        Debug.Log(exampleMapping.GetMapping.Count);
                 
                        if(!exampleMapping.IsEmpty())markovPen.Initialize(exampleMapping);
                        pointAddingHandler.activeMarkovPen = markovPen;
                    }
                    else if (markovPen.IsTrained())
                    {
                        _targetStyleCurve = new _3DMP.MarkovPen.Curve(1);
                        _targetBaseCurve = new _3DMP.MarkovPen.BaseCurve();
                        _targetMapping = null;
                    }

                  
                
            }
        }


        void OnInputGripPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("Grip Performed");
            bool gripPressed = context.started;
            if (gripPressed && _targetBaseCurve != null && (pointAddingHandler.storedLines.Count > 0) && pointAddingHandler.storedLines != null && pointAddingHandler.storedLines.Last().Item1 != null && pointAddingHandler.storedLines.Last().Item2.ControlPoints != null )
            {
                
                if (( _exampleBaseCurve != null && _exampleBaseCurve.ControlPoints != null && pointAddingHandler.storedLines.Last() != null && pointAddingHandler.storedLines.Last().Item2.ControlPoints.SequenceEqual(_exampleBaseCurve.ControlPoints)))
                {
                    _exampleBaseCurve = new _3DMP.MarkovPen.BaseCurve();
                }
                else if (_exampleStyleCurve != null && _exampleStyleCurve.ControlPoints != null && pointAddingHandler.storedLines.Last().Item2.ControlPoints.SequenceEqual(_exampleStyleCurve.ControlPoints))
                {
                    _exampleStyleCurve = new _3DMP.MarkovPen.Curve();
                    markovPen.Clear();
                    _targetMapping = null;
                    pointAddingHandler.SetInactive();
                    XRInteractorLineVisual lineRenderer = GameObject.Find("DrawHand").GetComponent<XRInteractorLineVisual>();
                    lineRenderer.enabled = true;
                }
                else if (_targetStyleCurve != null && _targetStyleCurve.ControlPoints != null && pointAddingHandler.storedLines.Last().Item2.ControlPoints.SequenceEqual(_targetStyleCurve.ControlPoints))
                {
                   
                    _targetStyleCurve = new _3DMP.MarkovPen.Curve(1);
                 
                }
                
                

                pointAddingHandler.RemoveLastLine();
            }else if (gripPressed && pointAddingHandler.storedLines.Count > 0) {
                pointAddingHandler.RemoveLastLine();
            }
        }

       
    }
}