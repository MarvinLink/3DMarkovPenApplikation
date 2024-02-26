using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;

namespace ThreeDimensionalMarkovPen.Curves.New
{
    public class LineSketchObjectFactory : MonoBehaviour
    {
        public DefaultReferences defaults;
        public Queue<LineSketchObject> lineObjectPool = new Queue<LineSketchObject>();
        private float _lineDiameter = 0.025f;
        

        public void Initialize(DefaultReferences defaultReferences)
        {
            defaults = defaultReferences;
            InitializeLineObjectPool(1000);
        }

        private void InitializeLineObjectPool(int size)
        {
            for (int i = 0; i < size; i++)
            {
                LineSketchObject lineObject = CreateNewLineSketchObject();
                MeshCollider meshCollider = lineObject.GetComponent<MeshCollider>();
                meshCollider.enabled = false;
                lineObject.SetLineDiameter(0.025f);
                lineObject.gameObject.SetActive(true);
                ReturnLineObjectToPool(lineObject);
            }
        }

        public LineSketchObject GetLineSketchObjectFromPool()
        {
            if (lineObjectPool.Count > 0)
            {
                LineSketchObject lineSketchObject = lineObjectPool.Dequeue();
                lineSketchObject.SetLineDiameter(_lineDiameter);
                lineSketchObject.gameObject.SetActive(true);
                return lineSketchObject;
            }

            return CreateNewLineSketchObject();
        }

        public void ReturnLineObjectToPool(LineSketchObject lineObject)
        {
            lineObject.gameObject.SetActive(false);
            lineObjectPool.Enqueue(lineObject);
        }

        private LineSketchObject CreateNewLineSketchObject()
        {
            
            if (defaults != null && defaults.LineSketchObjectPrefab != null)
            {
                LineSketchObject newLineObject = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
                newLineObject.GameObject().SetActive(false);
               
                return newLineObject;
                
            }
            else
            {
                Debug.LogError("Defaults or LineSketchObjectPrefab is null.");
                return null;
            }
        }
    }
}