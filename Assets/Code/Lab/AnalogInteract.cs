using Astro;
using FieldDay;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using System.Collections;

public enum buttons
{
    up,
    down,
    left,
    right,
    zoomIn,
    zoomOut,
    none
}

public class AnalogInteract : MonoBehaviour
{
    public buttons btnName;

    public Material defaultMaterial;
    public Material pressedMaterial;

    public Vector3 pushOffset;

    private MeshRenderer buttonRenderer;

    private Vector3 startPos;

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
        Find.State<SpaceCameraState>().buttonHeld = buttons.none;
        startPos = transform.position;

        buttonRenderer = GetComponent<MeshRenderer>();

        if (buttonRenderer == null)
        {
            Debug.Log("MeshRenderer not found");
            return;
        }

        if (defaultMaterial != null)
        {
            buttonRenderer.sharedMaterial = defaultMaterial;
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
            buttonRenderer.sharedMaterial = pressedMaterial;
        }

        Routine.Stop("pressUp");
        Routine.Start(pressDown(transform, (startPos + pushOffset)));

        Find.State<SpaceCameraState>().buttonHeld = btnName;

    }

    private void OnMouseUp()
    {
        if (buttonRenderer != null && defaultMaterial != null)
        {
            buttonRenderer.sharedMaterial = defaultMaterial;
        }

        Routine.Stop("pressDown");
        Routine.Start(pressDown(transform, startPos));

        Find.State<SpaceCameraState>().buttonHeld = buttons.none;
    }

    private static IEnumerator pressDown(Transform doc, Vector3 target)
    {
        yield return doc.MoveTo((target), 0.3f).Ease(Curve.CubeOut);
        yield return null;
    }

    private static IEnumerator pressUp(Transform doc, Vector3 target)
    {
        yield return doc.MoveTo((target), 0.3f).Ease(Curve.CubeIn);
        yield return null;
    }

}
