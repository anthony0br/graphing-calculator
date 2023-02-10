#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class containing static methods that are used by the calculator
internal class Functions
{
    public static float Add(float num1, float num2)
    {
        return num1 + num2;
    }
    public static float Subtract(float num1, float num2)
    {
        return num1 - num2;
    }
    public static float Multiply(float num1, float num2)
    {
        return num1 * num2;
    }
    public static float Divide(float num1, float num2)
    {
        return num1 / num2;
    }
    public static float Pow(float num1, float num2)
    {
        return Mathf.Pow(num1, num2);
    }
}

public class FormulaTree
{
    public class Node
    {
        // Create properties
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public string? Data { get; set; }
        // Constructor
        public Node(string? data)
        {
            Data = data;
        }
    }

    // Static fields
    // Operator-priority dictionary
    private static readonly Dictionary<string, int> operatorPriority = new Dictionary<string, int>()
    {
        {"+", 1},
        {"-", 1},
        {"*", 2},
        {"/", 2},
        {"^", 3},
    };
    // Create the operator-function dictionary. This cannot be done as a constant as planned due to limitations of C#.
    private static readonly Dictionary<string, Func<float, float, float>> operatorFunctions = new Dictionary<string, Func<float, float, float>>()
    {
        {"+", Functions.Add},
        {"-", Functions.Subtract},
        {"*", Functions.Multiply},
        {"/", Functions.Divide},
        {"^", Functions.Pow},
    };

    // Define fields
    private Node? rootNode;

    // Constructor
    public FormulaTree(string textFormula, out bool success)
    {
        success = true;
        // Input sanitisation
        for (int i = 0; i < textFormula.Length; i++)
        {
            string character = textFormula[i].ToString();
            // Delete all spaces
            if (character == " ")
            {
                // textFormula = all text up to i + all text after i
                textFormula = textFormula.Substring(0, i) + textFormula.Substring(i + 1);
                // Decrement i as character at i + 1 has moved to i
                i--;
            }

            // Look for "x". Do not check the first position as there could be no coefficient in this case.
            if (i > 0 && character == "x")
            {
                // Check if there is a coefficient
                if (Char.IsDigit(textFormula[i - 1]))
                {
                    // Insert *
                    textFormula = textFormula.Insert(i, "*");
                }
            }

            // Look for a digit or "x" (may also check for constants later) in front of a "("
            if (i > 0 && character == "(")
            {
                char left = textFormula[i - 1];
                if (left.ToString() == "x" || Char.IsDigit(left))
                {
                    // Insert *
                    textFormula = textFormula.Insert(i, "*");
                }
            }

            // Look for a digit or "x" (may also check for constants later) in after of a ")"
            if (i < textFormula.Length - 1 && character == ")")
            {
                char right = textFormula[i + 1];
                if (right.ToString() == "x" || Char.IsDigit(right))
                {
                    // Insert *
                    textFormula = textFormula.Insert(i + 1, "*");
                }
            }

            // Look for opposing brackets, if enough space remaining for an opposing bracket pair to exist
            if (i < textFormula.Length - 2 && textFormula.Substring(i, 2) == ")(")
            {
                // Insert *
                textFormula = textFormula.Insert(i + 1, "*");
            }
        }

        // Create root node
        rootNode = new Node(textFormula);

        // Recursive function to divide the formula into a tree of binary operations
        void divideNode(Node node)
        {
            if (node.Data == null)
            {
                return;
            }
            // Returns true and the symbol and position of the rightmost operator of lowest priority outside of the brackets if one exists, returns false if one does not exist
            // Pass outputOperator and outputPosition by reference
            bool findLeastPriorityOperatorOutsideBrackets(string nodeDataCopy, out string outputOperator, out int outputPosition)
            {
                int bracketCount = 0;
                int? leastPriority = null;
                int? operatorRightmostPosition = null;
                string? leastPriorityOperator = null;
                bool foundOperator = false;
                for (int i = 0; i < nodeDataCopy.Length; i++)
                {
                    if (nodeDataCopy[i].ToString() == "(") // Count brackets
                    {
                        bracketCount++;
                    } else if (nodeDataCopy[i].ToString() == ")")
                    {
                        bracketCount--;
                    } else if (bracketCount == 0 && GetOperator(nodeDataCopy, i, out _)) // If outside brackets and the character at i is an operator
                    {
                        foundOperator = true;
                        string character = nodeDataCopy[i].ToString();
                        int priority = operatorPriority[character];
                        // If lower priority, override
                        if (leastPriority == null || priority < leastPriority)
                        {
                            leastPriority = priority;
                            leastPriorityOperator = character;
                            operatorRightmostPosition = i;
                        } else if (priority == leastPriority && i > operatorRightmostPosition) { // If same priority but further right
                            leastPriorityOperator = character;
                            operatorRightmostPosition = i;
                        }
                    }
                }
                outputOperator = leastPriorityOperator ?? "";
                outputPosition = operatorRightmostPosition ?? 0;
                return foundOperator;
            }

            string leastPriorityOperator = "";
            int operatorPosition = 0;
            bool operatorExists = false;
            bool unsimplified = true; // Whether redundant brackets exist or no, assuming they do at first
            while (unsimplified) {
                operatorExists = findLeastPriorityOperatorOutsideBrackets(node.Data, out leastPriorityOperator, out operatorPosition);
                // If there is no operator outside of the brackets, and there is a starting bracket, then the whole expression must be wrapped inside redundant brackets

                if (!operatorExists && node.Data.Length > 2 && (node.Data[0].ToString() == "("))
                {
                    unsimplified = true;
                    // Remove redundant brackets
                    node.Data = node.Data.Substring(1, node.Data.Length - 2);
                } else
                {
                    unsimplified = false;
                }
            }

            // Divide list in 2 and repeat recursively if the operator exists, if not, a leaf node is reached and exit
            if (operatorExists)
            {
                // Add leading 0 - placeholder operand - if the operator has no character or an operand to the left and length > 1
                if ((operatorPosition == 0 || operatorFunctions.TryGetValue(node.Data[operatorPosition - 1].ToString(), out _)) && node.Data.Length > 1)
                {
                    node.Data = node.Data.Insert(operatorPosition, "0");
                    operatorPosition++;
                }

                string left = node.Data.Substring(0, operatorPosition);
                string right = node.Data.Substring(operatorPosition + 1);
                node.Data = leastPriorityOperator;
                node.Left = new Node(left);
                node.Right = new Node(right);
                divideNode(node.Left);
                divideNode(node.Right);
            } else
            { // Stopping condition
                return;
            }
        }
        divideNode(rootNode);

        // Check the tree is valid
        Queue<Node> queue = new Queue<Node>();
        queue.Enqueue(rootNode);
        while (queue.TryPeek(out _)) {
            Node node = queue.Dequeue();
            bool isLeaf = true;
            if (node.Left != null) {
                isLeaf = false;
                queue.Enqueue(node.Left);
            }
            // If there is a right
            if (node.Right != null) {
                isLeaf = false;
                queue.Enqueue(node.Right);
            }
            // Ensure leaf nodes are operands
            if (isLeaf) {
                // Return false if it is not a valid operand
                bool isVariable = node.Data == "x" || node.Data == "-x" 
                    || node.Data == "--x" || node.Data == "+x";
                if (!float.TryParse(node.Data, out _) && !isVariable) {
                    success = false;
                }
            } // Ensure parent nodes are operators
            else if (node.Data == null || node.Data.Length != 1 || !GetOperator(node.Data, 0, out _)) {
                success = false;
            }
        }
    }

    // Checks whether a part of the string is an operator and returns the function if it is
    public static bool GetOperator(string formula, int position, out Func<float, float, float>? outputFunction)
    {
        // Assign initial output value
        outputFunction = null;

        // Check that the character exists
        if (position >= formula.Length) {
            return false;
        }

        // Check character is in operator list
        string character = formula[position].ToString();

        Func<float, float, float>? operatorFunction;
        // Pass operatorFunction by reference to save the result of the reference in operatorFunction
        bool characterIsOperator = operatorFunctions.TryGetValue(character, out operatorFunction);
        if (!characterIsOperator) {
            return false;
        }
        // Check context to see if symbol should be treated as an operator (signed numbers)
        // Applies to "-" and "+" signs where the left character is not an operand, or does not exist
        // Bypass this check if the length is 1 (in this case it is a existing node being read)
        if (formula.Length > 1)
        {
            string left = (position != 0) ? formula[position - 1].ToString() : "";
            if ((character == "-" || character == "+") && ((left == "(" || operatorFunctions.TryGetValue(left, out _)) || position == 0))
            {
                // Return null (not an operator) if left character is a bracket or operator
                // Allows (-x), +-x, *-x, /-x etc
                // Iterate right until find closing bracket or operator
                int i = position + 1;
                bool found = false;
                while (i < formula.Length && !found)
                {
                    string character_i = formula[i].ToString();
                    // If bracket or reached end
                    if (operatorFunctions.TryGetValue(character_i, out _))
                    {
                        // Operator comes first so it cannot be a leaf node e.g. -(x+1)
                        found = true;
                    }
                    else if (character_i == ")")
                    {
                        return false;
                    }
                    i++;
                }
                // If it has reached the end and no operator or bracket has been found, it is a leaf node so is an operand
                if (!found) {
                    return false;
                }
            }
        }

        outputFunction = operatorFunction;

        return true;
    }

    // Returns the result of the calculation
    public float Calculate(float value)
    {
        float calculate(Node node)
        {
            if (node.Data == null || node.Data == "")
            {
                return 0;
            } else if (node.Data == "x" || node.Data == "+x" || node.Data == "--x")
            {
                return value;
            } else if (node.Data == "-x")
            {
                return -value;
            }
            Func<float, float, float>? operatorFunction;
            bool operatorExists = GetOperator(node.Data, 0, out operatorFunction);
            if (operatorExists && operatorFunction != null)
            {
                if (node.Left != null && node.Right != null)
                {
                    // Calculate left and right nodes first, then return the result of the operation applied to them
                    float left = calculate(node.Left);
                    float right = calculate(node.Right);
                    return operatorFunction(left, right);
                } else
                {
                    return 0;
                }
            } else
            { // Leaf node (stopping condition), convert to float and return
                float number;
                bool success = float.TryParse(node.Data, out number);
                if (!success) {
                    number = 0;
                }
                return number;
            }
        }
        if (rootNode != null)
        {
            return calculate(rootNode);
        } else
        {
            return 0;
        }
    }
}