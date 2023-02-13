using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserLine : MonoBehaviour
{
    const int INITIAL_Y_POS = -45;
    const int INITIAL_BUTTON_Y = -130;
    const int FRAME_HEIGHT = 90;
    private Color INPUT_FIELD_COLOUR_VALID = new Color(1f, 1f, 1f, 230f/255f); // Colors cannot be constants
    private Color INPUT_FIELD_COLOUR_INVALID = new Color(1f, 0.36f, 0.26f, 0.95f);
    public GameObject LineUI;
    private Main main;
    private GameObject lineUIParent;
    private TMP_InputField TextInputField;
    private Line line;

    void Awake()
    {
        // Create line
        line = gameObject.AddComponent<Line>();

        // Variables
        main = GameObject.Find("Main").GetComponent<Main>();
        lineUIParent = GameObject.Find("MainCanvas/ScrollSidebar/Viewport/Content");
        int numLines = main.Lines.Count;

        // Create/initialise GUI
        LineUI = Instantiate(main.LineUIPrefab);
        {
            RectTransform rect = LineUI.GetComponent<RectTransform>();
            TextInputField = LineUI.transform.Find("InputField").GetComponent<TMP_InputField>();
            LineUI.transform.SetParent(lineUIParent.transform);
            rect.localPosition = new Vector3(0, INITIAL_Y_POS - numLines * FRAME_HEIGHT, 0);
            rect.offsetMin = new Vector2(0, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0, rect.offsetMax.y);
        }

        // Update button position and vertical size of parent so scroll bar adjusts and set the button position
        {
            RectTransform newButtonRect = main.NewButton.GetComponent<RectTransform>();
            newButtonRect.localPosition = new Vector3(newButtonRect.localPosition.x, INITIAL_BUTTON_Y - numLines * FRAME_HEIGHT, newButtonRect.localPosition.z);
            RectTransform rect = lineUIParent.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (numLines + 1) * FRAME_HEIGHT - (INITIAL_Y_POS / 2));
        }

        // Set a colour based on constant-preset colour list
        int colourIndex = main.TotalLineCount % main.Colours.Count;
        SetColour(main.Colours[colourIndex]);
        main.TotalLineCount += 1;

        // On text updated (call set formula)
        TextInputField.onSubmit.AddListener(onTextUpdate);
        
        // Listen to delete button input
        {
            Button button = LineUI.transform.Find("DeleteButton").GetComponent<Button>();
            button.onClick.AddListener(() => {
                Destroy(this);
            });
        }

        // Add to Lines list
        main.Lines.Add(this);
    }

    private void onTextUpdate(string newText) {
        // Attempt to set the formula
        bool success = line.SetFormula(newText);
        
        // Display feedback to user
        Image inputFieldImage = LineUI.transform.Find("InputField").GetComponent<Image>();
        if (success) {
            inputFieldImage.color = INPUT_FIELD_COLOUR_VALID;
        } else {
            inputFieldImage.color = INPUT_FIELD_COLOUR_INVALID;
        }
    }

    public void SetColour(Color newColour) {
        // Set line colour
        line.SetColour(newColour);

        // Update UI
        Image image = LineUI.GetComponent<Image>();
        image.color = newColour;
    }

    public void OnDestroy() {
        // Remove from list and find new length
        int index = main.Lines.IndexOf(this);
        main.Lines.RemoveAt(index);
        int length = main.Lines.Count;

        // Move succeeding lines up
        for (int i = index; i < length; i++) {
            RectTransform rect = main.Lines[i].LineUI.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(rect.localPosition.x, INITIAL_Y_POS - i * FRAME_HEIGHT, rect.localPosition.z);
        }

        // Move new button up
        {
            RectTransform newButtonRect = main.NewButton.GetComponent<RectTransform>();
            newButtonRect.localPosition = new Vector3(newButtonRect.localPosition.x, INITIAL_BUTTON_Y - (length  - 1) * FRAME_HEIGHT, newButtonRect.localPosition.z);
        }

        // Destroy UI object
        Destroy(LineUI);
        
        // Destroy Line
        Destroy(line);
    }
}
