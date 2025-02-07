using Astro;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalogInteract : MonoBehaviour
{
    /*
     * btnName options:
     * up
     * down
     * left
     * right
     * zoomIn
     * zoomOut
     */
    public string btnName;

    public SpaceCameraControllerSystem cameraController;

    public Material defaultMaterial;
    public Material pressedMaterial;

    private MeshRenderer buttonRenderer;

    /*
     * Couldn't figure out adding text to the buttons
     * 
     *   ---------------------------
     *   | ZoomIn |  Up  | ZoomOut |
     *   |--------|------|---------|
     *   |  Left  | Down |  Right  |
     *   ---------------------------
     */


    private void Start()
    {

        buttonRenderer = GetComponent<MeshRenderer>();

        if (buttonRenderer == null)
        {
            Debug.Log("MeshRenderer not found");
            return;
        }

        if (defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }
        else
        {
            Debug.Log("Default Material not found");
        }
    }

    private void OnMouseDown()
    {
        if (buttonRenderer != null && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }

        transform.position += new Vector3 (0,-.02f,.01f);

        cameraController.AnalogButtonHelper(btnName, true);
        Debug.Log("Button Down: " + btnName);

    }

    private void OnMouseUp()
    {
        if (buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.material = defaultMaterial;
        }

        transform.position += new Vector3(0,.02f,-.01f);

        cameraController.AnalogButtonHelper(btnName, false);
        Debug.Log("Button Up: " + btnName);
    }
}
