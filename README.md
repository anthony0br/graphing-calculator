# nea
My A-Level Computer Science NEA, a graphing calculator in C#/Unity. This repository contains all files in the "Assets/Scripts/" directory of the Unity project. Note that I decided to use git version control late, so many commits are omitted.

Demonstration video: https://www.youtube.com/watch?v=_VJc8ZFs7vQ&t=8s

Future improvements/optimisations to make:
- [ ] Rewrite FormulaTree using the Shunting Yard Algorithm, rather than the current recursive algorithm - convert the infix expression to RPN using a stack and output queue and use a stack to compute the output.
- [ ] Use tokenisation and/or enums to allow the support of multi-character operators/functions and ability to quickly add new functions with a given precedence.
