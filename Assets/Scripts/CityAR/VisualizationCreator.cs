using DefaultNamespace;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using TMPro;

namespace CityAR
{
    public class VisualizationCreator : MonoBehaviour
    {

        public GameObject districtPrefab;
        public GameObject buildingPrefab;
        private DataObject _dataObject;
        private GameObject _platform;
        private Data _data;
        public ToolTip toolTipPrefab;
        public TextMeshPro currentMetricText;
        public PinchSlider slider;
        
        private Metric _currentMetric;
        
        private enum Metric
        {
            LinesOfCode,
            NumberOfInterfaces,
            NumberOfMethods,
            NumberOfAbstractClasses,
        }

        private void Start()
        {
            _platform = GameObject.Find("Platform");
            _data = _platform.GetComponent<Data>();
            _dataObject = _data.ParseData();
            _currentMetric = Metric.LinesOfCode;
            BuildCity(_dataObject);
            
            _platform.GetComponent<BoundsControl>().UpdateBounds();
        }

        
        private void BuildCity(DataObject p)
        {
            if (p.project.files.Count > 0)
            {
                p.project.w = 1;
                p.project.h = 1;
                p.project.deepth = 1;
                BuildDistrict(p.project, false);
            }
        }
        

        /*
         * entry: Single entry from the data set. This can be either a folder or a single file.
         * splitHorizontal: Specifies whether the subsequent children should be split horizontally or vertically along the parent
         */
        private void BuildDistrict(Entry entry, bool splitHorizontal)
        {
            if (entry.type.Equals("File"))
            {
                //TODO if entry is from type File, create building
                //Nothing to see here!
            }
            else
            {
                float x = entry.x;
                float z = entry.z;

                float dirLocs;
                switch (_currentMetric)
                {
                    case Metric.NumberOfInterfaces:
                        dirLocs = entry.numberOfInterfaces;
                        break;
                    case Metric.NumberOfMethods:
                        dirLocs = entry.numberOfMethods;
                        break;
                    case Metric.NumberOfAbstractClasses:
                        dirLocs = entry.numberOfAbstractClasses;
                        break;
                    default: //Lines Of Code
                        dirLocs = entry.numberOfLines;
                        break;
                }
                
                entry.color = GetColorForDepth(entry.deepth);

                BuildDistrictBlock(entry, false);

                foreach (Entry subEntry in entry.files) {
                    subEntry.x = x;
                    subEntry.z = z;
                    
                    if (subEntry.type.Equals("Dir"))
                    {
                        float ratio;
                        switch (_currentMetric)
                        {
                            case Metric.NumberOfInterfaces:
                                ratio = subEntry.numberOfInterfaces / dirLocs;
                                break;
                            case Metric.NumberOfMethods:
                                ratio = subEntry.numberOfMethods / dirLocs;
                                break;
                            case Metric.NumberOfAbstractClasses:
                                ratio = subEntry.numberOfAbstractClasses / dirLocs;
                                break;
                            default: //Lines of Code
                                ratio = subEntry.numberOfLines / dirLocs;
                                break;
                        } 
                        
                        subEntry.deepth = entry.deepth + 1;

                        if (splitHorizontal) {
                            subEntry.w = ratio * entry.w; // split along horizontal axis
                            subEntry.h = entry.h;
                            x += subEntry.w;
                        } else {
                            subEntry.w = entry.w;
                            subEntry.h = ratio * entry.h; // split along vertical axis
                            z += subEntry.h;
                        }
                    }
                    else
                    {
                        subEntry.parentEntry = entry;
                    }
                    BuildDistrict(subEntry, !splitHorizontal);
                }

                if (!splitHorizontal)
                {
                    entry.x = x;
                    entry.z = z;
                    if (ContainsDirs(entry))
                    {
                        entry.h = 1f - z;
                    }
                    entry.deepth += 1;
                    BuildDistrictBlock(entry, true);
                }
                else
                {
                    entry.x = -x;
                    entry.z = z;
                    if (ContainsDirs(entry))
                    {
                        entry.w = 1f - x;
                    }
                    entry.deepth += 1;
                    BuildDistrictBlock(entry, true);
                }
            }
        }

        /*
         * entry: Single entry from the data set. This can be either a folder or a single file.
         * isBase: If true, the entry has no further subfolders. Buildings must be placed on top of the entry
         */
        private void BuildDistrictBlock(Entry entry, bool isBase)
        {
            if (entry == null)
            {
                return;
            }
            
            float w = entry.w; // w -> x coordinate
            float h = entry.h; // h -> z coordinate
            
            if (w * h > 0)
            {
                GameObject prefabInstance = Instantiate(districtPrefab, _platform.transform, true);

                if (!isBase)
                {
                    prefabInstance.name = entry.name;
                    prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = entry.color;
                    prefabInstance.transform.localScale = new Vector3(entry.w, 1f,entry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.x, entry.deepth, entry.z);
                }
                else
                {
                    prefabInstance.name = entry.name+"Base";
                    prefabInstance.transform.GetChild(0).rotation = Quaternion.Euler(90,0,0);
                    prefabInstance.transform.localScale = new Vector3(entry.w, 1,entry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.x, entry.deepth+0.001f, entry.z);
                    
                    prefabInstance.transform.GetChild(0).gameObject.transform.localPosition = new Vector3(-0.5f,0,0.5f);
                    
                    // Dont show green Space holder
                    prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                }
                
                Vector3 scale = prefabInstance.transform.localScale;
                float scaleX = scale.x - (entry.deepth * 0.005f);
                float scaleZ = scale.z - (entry.deepth * 0.005f);
                float shiftX = (scale.x - scaleX) / 2f;
                float shiftZ = (scale.z - scaleZ) / 2f;
                prefabInstance.transform.localScale = new Vector3(scaleX, scale.y, scaleZ);
                Vector3 position = prefabInstance.transform.localPosition;
                prefabInstance.transform.localPosition = new Vector3(position.x - shiftX, position.y, position.z + shiftZ);

                
                // Creating GridObjectCollection and building Buildings
                if (isBase)
                {
                    float size = 0.02f; // Größe Grundflaeche der Buildings
                    float clearedScaledWidth = size / prefabInstance.transform.localScale.x; // Bereinigte Skalierung der Eltern
                    float clearedScaledHeight = size / prefabInstance.transform.localScale.z; // Bereinigte Skalierung der Eltern
                    float gridOffset = 0.1f;
                    
                    
                    
                    // Create and manipulate GridObjectCollection 
                    prefabInstance.transform.GetChild(0).gameObject.AddComponent<GridObjectCollection>();
                    entry.goc = prefabInstance.transform.GetChild(0).GetComponent<GridObjectCollection>();
                    
                    entry.goc.CellWidth = clearedScaledWidth + 0.01f;

                    // Ausrichtung der GridObjectCollection
                    if (h > w) // Falls die Base höher als weiter ist
                    {
                        entry.goc.Layout = LayoutOrder.ColumnThenRow;
                        entry.goc.CellWidth = clearedScaledHeight + 0.01f;
                        entry.goc.CellHeight = clearedScaledHeight + 0.01f;
                        if (3 * (clearedScaledWidth + gridOffset) > 1)
                        {
                            if(2 * (clearedScaledWidth + gridOffset) > 1)
                            {
                                entry.goc.Columns = 1;
                            }
                            else
                            {
                                entry.goc.Columns = 2;
                            }
                        }
                    }
                    else
                    {
                        entry.goc.Layout = LayoutOrder.RowThenColumn;
                        entry.goc.CellHeight = clearedScaledHeight + gridOffset;
                        entry.goc.CellWidth = clearedScaledWidth + 0.01f;
                        if (3 * (clearedScaledHeight + gridOffset) > 1)         
                        {
                            if(2 * (clearedScaledHeight + gridOffset) > 1)
                            {
                                entry.goc.Rows = 1;
                            }
                            else
                            {
                                entry.goc.Rows = 2;
                            }
                        }
                    }

                    // Construction Area: Buildings are being build
                    foreach (Entry file in entry.files)
                    {
                        if (file.type.Equals("File"))
                        {
                            BuildBuilding(file,size);
                        }
                    }
                    
                    entry.goc.UpdateCollection(); // Update GridObjectCollection
                }
            }
        }
        
        private void BuildBuilding(Entry entry, float size)
        {
            if (entry == null)
            {
                return;
            }

            // Baue Building aus Prefab
            GameObject prefabInstance = Instantiate(buildingPrefab, entry.parentEntry.goc.transform, true);
            prefabInstance.name = entry.name;

            String currentMetricString;
            
            float height;
            int metricValue;
            switch (_currentMetric)
            {
                case Metric.NumberOfInterfaces:
                    height = entry.numberOfInterfaces * 20f;
                    metricValue = entry.numberOfInterfaces;
                    break;
                case Metric.NumberOfMethods:
                    height = entry.numberOfMethods * 1.136f;
                    metricValue = entry.numberOfMethods;
                    break;
                case Metric.NumberOfAbstractClasses:
                    height = entry.numberOfAbstractClasses * 33.333f;
                    metricValue = entry.numberOfAbstractClasses;
                    break;
                default: //Lines Of Code
                    height = entry.numberOfLines * 0.0604f;
                    metricValue = entry.numberOfLines;
                    break;
            }

            Transform parent = prefabInstance.transform.parent.parent;

            if (height == 0) height = 0.000001f;

            prefabInstance.transform.localScale = new Vector3(size/parent.localScale.x, height, size/parent.localScale.z);
            
            prefabInstance.transform.GetChild(0).gameObject.transform.localPosition = new Vector3(0, 0.5f, 0);

            prefabInstance.GetComponent<ScaleData>().originalHeight = prefabInstance.transform.localScale.y;

            //Create ToolTip
            toolTipPrefab = Instantiate(toolTipPrefab, prefabInstance.transform.GetChild(0), false);
            toolTipPrefab.name = "ToolTip_" + entry.name;

            toolTipPrefab.transform.localScale = new Vector3(toolTipPrefab.transform.localScale.x,100f / prefabInstance.transform.localScale.y, toolTipPrefab.transform.localScale.z);

            toolTipPrefab.transform.position = new Vector3(toolTipPrefab.transform.position.x,0 + toolTipPrefab.transform.parent.parent.localScale.y * 0.01f, toolTipPrefab.transform.position.z);
            
      
            toolTipPrefab.ToolTipText = "Name: " + entry.name + "\nMetrik: " + _currentMetric + "\nValue: " + metricValue;

            toolTipPrefab.gameObject.SetActive(false);

        }

        private bool ContainsDirs(Entry entry)
        {
            foreach (Entry e in entry.files)
            {
                if (e.type.Equals("Dir"))
                {
                    return true;
                }
            }

            return false;
        }
        
        private Color GetColorForDepth(int depth)
        {
            Color color;
            switch (depth)
            {
                case 1:
                    color = new Color(179f / 255f, 209f / 255f, 255f / 255f);
                    break;
                case 2:
                    color = new Color(128f / 255f, 179f / 255f, 255f / 255f);
                    break;
                case 3:
                    color = new Color(77f / 255f, 148f / 255f, 255f / 255f);
                    break;
                case 4:
                    color = new Color(26f / 255f, 117f / 255f, 255f / 255f);
                    break;
                case 5:
                    color = new Color(0f / 255f, 92f / 255f, 230f / 255f);
                    break;
                default:
                    color = new Color(0f / 255f, 71f / 255f, 179f / 255f);
                    break;
            }

            return color;
        }
        
        public void ChangeMetric(String metric)
        {
            slider.SliderValue = 0.5f;
            currentMetricText.text = metric;
            switch (metric)
            {
                case "NumberOfMethods":
                    _currentMetric = Metric.NumberOfMethods;
                    break;
                case "NumberOfAbstractClasses":
                    _currentMetric = Metric.NumberOfAbstractClasses;
                    break;
                case "LinesOfCode":
                    _currentMetric = Metric.LinesOfCode;
                    break;
                case "NumberOfInterfaces":
                    _currentMetric = Metric.NumberOfInterfaces;
                    break;
            }

            //text.text = _current.ToString();
            RebuildCity();
        }

        void RebuildCity()
        {
            Quaternion rotation = _platform.transform.rotation;
            _platform.transform.rotation = Quaternion.Euler(0,0,0);
            _platform.GetComponent<BoundsControl>().enabled = false; // Sonst verschwindet es...

            for (int i = 1; i < _platform.transform.childCount; i++)
            {
                Destroy(_platform.transform.GetChild(i).gameObject);
            }
            
            BuildCity(_data.ParseData());

            _platform.transform.rotation = rotation;
            
            _platform.GetComponent<BoundsControl>().enabled = true; // Und wieder aktivieren..
            _platform.GetComponent<BoundsControl>().UpdateBounds();
        }

        public void SliderScale(SliderEventData scalingValue)
        {
            float scale = scalingValue.NewValue;
            Rescale(_platform, scale * 2f);
        }

        void Rescale(GameObject parent, float scale)
        {
            if (parent == null) return;
            for(int i = 1; i < parent.transform.childCount; i++)
            {
                if (parent.transform.GetChild(i).name.Contains("Base"))
                {
                    for (int j = 0; j < parent.transform.GetChild(i).GetChild(0).childCount; j++)
                    {
                        var toScale = parent.transform.GetChild(i).GetChild(0).GetChild(j);
                        toScale.transform.localScale = new Vector3(toScale.transform.localScale.x,
                            toScale.transform.GetComponent<ScaleData>().originalHeight * scale, toScale.transform.localScale.z);
                    }
                    _platform.GetComponent<BoundsControl>().UpdateBounds();
                }
                else
                {
                    Rescale(parent.transform.GetChild(i).gameObject, scale);
                }
            }
        }
        
    }
    
    
}