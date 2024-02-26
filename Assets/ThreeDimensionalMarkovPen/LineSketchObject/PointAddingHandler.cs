using System;
using System.Collections.Generic;
using System.Linq;
using CENTIS.UnitySketchingKernel.SketchObjectManagement;
using ThreeDimensionalMarkovPen.Control;
using ThreeDimensionalMarkovPen.Curves.New;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;
using LineSketchObject = VRSketchingGeometry.SketchObjectManagement.LineSketchObject;

namespace ThreeDimensionalMarkovPen.Version2._0
{
    public class PointAddingHandler : MonoBehaviour
    {
        public CommandInvoker Invoker;
 
        public LineSketchObject activeLineSketchObject;
        public _3DMP.MarkovPen.Curve activeCurve;
        public _3DMP.MarkovPen.Mapping activeMapping;
        public _3DMP.MarkovPen activeMarkovPen;
        public LineSketchObject activeTargetStyleLineSketchObject;

        
        private List<Vector3> cursorWorldPositions = new List<Vector3>();
        
        


        public Transform drawHand;
        

        public LayerMask layerMask;

        private List<Tuple<VRSketchingGeometry.SketchObjectManagement.LineSketchObject, _3DMP.MarkovPen.Curve>>
            _storedLines =
                new List<Tuple<LineSketchObject, _3DMP.MarkovPen.Curve>>();

        public List<Tuple<LineSketchObject, _3DMP.MarkovPen.Curve>> storedLines
        {
            get => _storedLines;
            set => _storedLines = value;
        }

        private void Start()
        {
            Invoker = new CommandInvoker();
            
            drawHand = GameObject.Find("DrawHand").GetComponent<Transform>();
            
            layerMask = LayerMask.GetMask("Surface");
        }

        private void Update()
        {
            if (activeLineSketchObject != null && activeCurve != null)
            {
                AddControlPoint(activeLineSketchObject);
            }
        }

        public void AddControlPoint(LineSketchObject line)
        {
            Vector3 controlPoint = Vector3.zero;

            Vector3 cursorWorldPosition = drawHand.position;

            //MarkovPenReconstruct
            if (!cursorWorldPositions.Contains(cursorWorldPosition))
            {
                cursorWorldPositions.Add(cursorWorldPosition);
                controlPoint = cursorWorldPosition;


                List<Tuple<Vector3, Vector3>> associations = new List<Tuple<Vector3, Vector3>>();

                if (activeMapping != null && activeMarkovPen != null && activeTargetStyleLineSketchObject != null)
                {
                  
                    associations = activeMarkovPen.Reconstruct(activeMapping);
                    foreach (var association in associations)
                    {
                        Invoker.ExecuteCommand(new AddControlPointCommand(activeTargetStyleLineSketchObject,
                            association.Item2));
                        activeLineSketchObject.gameObject.SetActive(false);
                    }
                }
            }


            //MarkovPenExample
            if (activeMarkovPen == null)
            {
                RaycastHit hit;
                List<Vector3> examplePositions = new List<Vector3>();
                Vector3 projectedPoint = Vector3.zero;
                foreach (var position in cursorWorldPositions)
                {
                    if (Physics.Raycast(position, drawHand.forward, out hit, layerMask))
                    {
                        if (!examplePositions.Contains(hit.point))
                        {
                            projectedPoint = hit.point;
                            examplePositions.Add(projectedPoint);
                        }
                    }
                }

                controlPoint = projectedPoint;

                cursorWorldPositions = examplePositions;
            }

            
            //Sketching
            if (activeCurve != null && controlPoint != Vector3.zero)
            {
                activeCurve.AddControlPoint(controlPoint, drawHand.up);

                if (cursorWorldPositions.Count == 3)
                {
                    line.SetControlPoints(cursorWorldPositions);
                }
                else if (cursorWorldPositions.Count > 3)
                {
                    if (controlPoint != Vector3.zero)
                    {
           
                        Invoker.ExecuteCommand(new AddControlPointCommand(line, controlPoint));
           
                    }
                }
            }
        }

       

        public void EndLine()
        {
            Debug.Log("EndLine");
         
            if(activeCurve != null) activeCurve.Finish();
           
            List<Tuple<Vector3, Vector3>> associations = new List<Tuple<Vector3, Vector3>>();

            if (activeMapping != null && activeMarkovPen != null && activeTargetStyleLineSketchObject != null)
            {
                  
                associations = activeMarkovPen.Reconstruct(activeMapping);
                foreach (var association in associations)
                {
                  
                    Invoker.ExecuteCommand(new AddControlPointCommand(activeTargetStyleLineSketchObject,
                        association.Item2));
                    activeLineSketchObject.gameObject.SetActive(false);
                }
            }
            if (activeTargetStyleLineSketchObject == null)
                _storedLines.Add(Tuple.Create(activeLineSketchObject, activeCurve));
            if (activeTargetStyleLineSketchObject != null)
            {
                _storedLines.Add(Tuple.Create(activeTargetStyleLineSketchObject, new _3DMP.MarkovPen.Curve()));
                activeTargetStyleLineSketchObject = null;
            }

            activeLineSketchObject = null;
            cursorWorldPositions.Clear();
        }


        public void RemoveLastLine()
        {
            

            if (_storedLines.Count > 0)
            {
                LineSketchObject lastLine = _storedLines.Last().Item1;
                _storedLines.RemoveAt(_storedLines.Count - 1);
                if(lastLine != null) lastLine.gameObject.SetActive(false);
            }

           
        }

        public void SetInactive()
        {
            activeMarkovPen = null;
            activeCurve = null;
        }
    }
}