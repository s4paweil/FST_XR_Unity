using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class Touch : MonoBehaviour, IMixedRealityTouchHandler
{
    private Color initColor;
    
    // Start is called before the first frame update
    void Start()
    {
        initColor = gameObject.transform.GetComponent<MeshRenderer>().material.color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.cyan;
    }

    public void OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameObject.transform.GetComponent<MeshRenderer>().material.color = initColor;
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        
    }
}
