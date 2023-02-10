using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Line : MonoBehaviour
{
    const float OVERSHOOT_FACTOR = 0.8f;
    const float MINIMUM_OVERSHOOT = 0.4f;
    const float MAXIMUM_OVERSHOOT = 1.2f;
    const float MAX_SCALE_RATIO = 2f;

    private Main main;
    private GameObject lineObject;
    private FormulaTree formulaTree;
    private LineRenderer lineRenderer;
    private float minX;
    private float maxX;
    private float step = 2.0f; // The step, assuming 1 unit = 1 pixel
    private Color colour;
    // Ordered list of vertices (from left to right)
    private List<Vector2> vertices;
    public bool inverse = false;
    // Scale: different parts of the graph can be rendered using different step widths due to a changing scale
    // Each time the graph is updated, the greatest and smallest scale values are recorded
    // The graph is re-rendered if the ratio greatest/smallest exceeds MAX_SCALE_RATIO
    private float greatestScale;
    private float smallestScale;

    // Constructor: Create the line renderer
    // Uses awake to ensure main is initialised before other methods called
    public void Awake()
    {
        main = GameObject.Find("Main").GetComponent<Main>();
        lineObject = Instantiate(main.LineObjectPrefab);
        lineRenderer = lineObject.GetComponent<LineRenderer>();
        lineObject.transform.SetParent(GameObject.Find("MainCanvas/Lines").transform);
        vertices = new List<Vector2>();
        greatestScale = main.Scale;
        smallestScale = main.Scale;

        // Rescale and reposition the new line to undo Unity's actions
        lineObject.transform.localScale = Vector3.one;
        lineObject.transform.localPosition = new Vector3();
    }

    public void SetColour(Color newColour)
    {
        // Update Line Renderer
        lineRenderer.startColor = newColour;
        lineRenderer.endColor = newColour;

        // Update private Colour
        colour = newColour;
    }

    // Creates a new formula tree and re-renders
    public void SetFormula(string text, bool isInverse)
    {
        bool success;
        formulaTree = new FormulaTree(text, out success);
        print(success);

        // Set inverse 
        inverse = isInverse;

        // Create fresh vertices list (deleting all existing vertices)
        vertices = new List<Vector2>();

        // Call FitVerticesToViewport
        FitVerticesToViewport();
    }

    // Overloaded function to allow omission of inverse parameter
    public void SetFormula(string text)
    {
        SetFormula(text, false);
    }

    // Calculates regions to be unloaded and regions to be loaded and unloads/loads as appropriate
    public void FitVerticesToViewport()
    {
        // Sets the line renderer, creating a scaled array
        void setLineRenderer()
        {
            // Create render array
            int length = vertices.Count;
            var renderArray = new Vector3[length];
            for (int i = 0; i < length; i++) {
                renderArray[i] = vertices[i];
            }
            // Set line renderer
            lineRenderer.positionCount = length;
            lineRenderer.SetPositions(renderArray);
        }

        // Loads vertices between input coordinates start and end. The vertex at "start" will always connect to the existing line.
        void loadVertices(float start, float end) {
            // Check if formulaTree exists
            if (formulaTree == null) {
                return;
            }

            // Check whether adding to the left, right, or none
            if (start == end) {
                return;
            }
            bool addToLeft = start > end;

            // Iterate from left to right
            float left = addToLeft ? end : start;
            float right = addToLeft ? start : end;
            float currentVertexX = left;
            int index = 0;
            while (currentVertexX < right) {
                // Calculate y-value and add to temp vertices list
                float value = formulaTree.Calculate(currentVertexX);
                // Create the vertex. If it is an inverse function, invert the coordinates
                Vector2 vertex = inverse ? new Vector2(value, currentVertexX) : new Vector2(currentVertexX, value);
                // Add vertex to list
                if (addToLeft) {
                    vertices.Insert(index, vertex);
                } else {
                    vertices.Add(vertex);
                }
                // Increment X
                currentVertexX = currentVertexX + step / main.Scale;
                index++;
            }
        }

        void unloadVerticesOutsideRange(float min, float max) {
            // Remove list elements outside of range
            for (int i = 0; i < vertices.Count; i++) {
                if (vertices[i].x < min || vertices[i].x > max) {
                    vertices.RemoveAt(i);
                }
            }
        }  

        // Calculate min and max values
        float overshootAmount = (main.MaxX - main.MinX) * OVERSHOOT_FACTOR;
        minX = main.MinX - overshootAmount;
        maxX = main.MaxX + overshootAmount;
        
        // Unload all vertices outside range
        unloadVerticesOutsideRange(minX, maxX);

        // Update scale records
        if (main.Scale < smallestScale) {
            smallestScale = main.Scale;
        } else if (main.Scale > greatestScale) {
            greatestScale = main.Scale;
        }

        // If ratio exceeds max scale difference ratio, re-render entire graph, otherwise only render new bits
        if (greatestScale / smallestScale > MAX_SCALE_RATIO) {
            vertices = new List<Vector2>();
            loadVertices(minX, maxX);
        }
        // In this case, the graph has already been initially rendered and has not been scaled enough to be fully re-rendered
        else if (vertices.Count >= 2) {
            // Find actual maximum and minimum (existing boundaries of vertices)
            float actualMinimum = vertices[0].x;
            float actualMaximum = vertices[vertices.Count - 1].x;

            // If actualMinimum > minX, load between minX and actualMinimum
            if (actualMinimum > minX) {
                loadVertices(actualMinimum, minX);
            }
            // If maxValue > actualMaximum, load between actualMaximum and maxValue
            if (actualMaximum < maxX) {
                loadVertices(actualMaximum, maxX);
            }
        } else {
            // Fill from minValue to maxValue as the line has not been initialised yet
            loadVertices(minX, maxX);
        }

        // Commit the changes
        setLineRenderer();
    }

    void Update()
    {
        // Calculate the actual overshoot on either side
        float overshootFactorRight = (maxX - main.MaxX) / (main.MaxX - main.MinX);
        float overshootFactorLeft = (main.MinX - minX) / (main.MaxX - main.MinX);

        // If less than MINIMUM_OVERSHOOT or greater than MAXIMUM OVERSHOOT call FitVerticesToViewport()
        if (Math.Min(overshootFactorLeft, overshootFactorRight) < MINIMUM_OVERSHOOT || Math.Max(overshootFactorLeft, overshootFactorRight) > MAXIMUM_OVERSHOOT) {
            FitVerticesToViewport();
        }
    }
    
    // Deletes all vertices, UI objects, destroys the component, and moves all other UI objects under content up. Updates other UI objects under LineUI.
    public void OnDestroy()
    {
        // Delete line renderer and object
        Destroy(lineRenderer);
        Destroy(lineObject);
    }
}