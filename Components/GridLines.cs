using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridLines : MonoBehaviour
{
    private Color AXIS_COLOUR = new Color(0.9f, 0.9f, 0.9f, 1);
    private Line yAxis;
    private Line xAxis;

    // Start is called before the first frame update
    void Start()
    {
        // Create axes lines
        yAxis = gameObject.AddComponent<Line>();
        xAxis = gameObject.AddComponent<Line>();
        yAxis.SetFormula("0", true);
        xAxis.SetFormula("0");
        yAxis.SetColour(AXIS_COLOUR);
        xAxis.SetColour(AXIS_COLOUR);
    }
}
