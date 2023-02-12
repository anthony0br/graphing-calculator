using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Handles initilisation, UI, etc
// Use Formula class to create UI elements in a list and update them when click/enter pressed?

public class Main : MonoBehaviour
{
    public GameObject LineUIPrefab;
    public GameObject LineObjectPrefab;
    public List<Color> Colours;

    public GameObject NewButton { get; private set; }
    public int TotalLineCount { get; set; }
    [HideInInspector]
    public List<UserLine> Lines;

    // Rendering bounds
    public float MinX;
    public float MaxX;
    public float Scale;
    public float ZoomSensitivity = 0.1f;
    private Vector2 offset = new Vector2();
    private float halfScaledWidth;
    private Vector3 lastMousePosition;
    private GameObject linesContainer;

    private Camera MainCamera;
    private GameObject MainCanvasObject;

    private void setBounds() {
        float viewportWidth = MainCanvasObject.GetComponent<RectTransform>().rect.width;
        halfScaledWidth = viewportWidth / (Scale * 2);
        MinX = offset.x / Scale - halfScaledWidth;
        MaxX = offset.x / Scale + halfScaledWidth;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialise total line count
        TotalLineCount = 0;
        linesContainer = GameObject.Find("MainCanvas/Lines");

        NewButton = GameObject.Find("MainCanvas/ScrollSidebar/Viewport/Content/NewButton");

        // Initialise camera and canvas
        MainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
        MainCanvasObject = GameObject.Find("MainCanvas");

        // Set MinX and MaxX
        setBounds();

        // Initialise input
        lastMousePosition = Input.mousePosition;

        // Create grid lines
        gameObject.AddComponent<GridLines>();

        // Create initial line
        gameObject.AddComponent<UserLine>();

        // Listen to NewButton clicks to create new lines
        NewButton.GetComponent<Button>().onClick.AddListener(() => {
            gameObject.AddComponent<UserLine>();
        });
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;
        float mouseScrollDelta = Input.mouseScrollDelta.y;

        // Panning and panning
        // Check if the mouse is in the window and to the right of the sidebar
        float viewportWidth = MainCanvasObject.GetComponent<RectTransform>().rect.width;
        float windowWidthRatio = viewportWidth / Screen.width;
        Vector3 view = MainCamera.ScreenToViewportPoint(lastMousePosition);
        RectTransform sidebar = GameObject.Find("ScrollSidebar").GetComponent<RectTransform>();
        bool isInSidebar = RectTransformUtility.RectangleContainsScreenPoint(sidebar, lastMousePosition, MainCamera);
        bool isOutside = view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
        if (!isOutside && !isInSidebar) {
            // Panning
            if (Input.GetMouseButton(0)) {
                Vector3 delta = mouseDelta * windowWidthRatio;
                linesContainer.transform.Translate(delta);
                // Divide by scale to convert unity engine units to number units
                offset = offset - new Vector2(delta.x, delta.y);
            }
            // Zooming: set new scale, the change in scale is proportional to the existing scale
             Scale = Scale + mouseScrollDelta * Scale * ZoomSensitivity;
        }
        
        // Update MinX and MaxX
        setBounds();

        // Set canvas scale
        linesContainer.transform.localScale = new Vector3(Scale, Scale, 1);
    }
}
