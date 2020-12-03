using System;
using System.Collections;
using System.Linq;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class DiophantineEquationsScript : MonoBehaviour
{
    #region Public Fields

    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable[] Buttons;
    public TextMesh[] ButtonTexts;
    public TextMesh ScreenText;
    public Renderer ScreenRenderer;

    #endregion

    #region Private Fields

    // For serial number checks
    private static readonly char[] Numbers = "0123456789".ToCharArray();
    private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private int moduleId;
    private static int moduleIdCounter;

    // For logging purposes
    private static readonly char[] VarNames = new char[] { 'X', 'Y', 'Z', 'W' };
    private static readonly string[] Ordinals = new string[] { "1st", "2nd", "3rd", "4th" };

    // Answer Generation
    private readonly int[] answer = new int[4]; // X, Y, Z, W
    private readonly int[] coef = new int[4]; // A, B, C, D
    private int N;
    private bool solutionExists;

    // Player Submission
    private string[] submission = new string[4] { "", "", "", "" };
    private int currentIndex = 0;

    private bool solved;
    #endregion

    /// <summary>
    /// On Module Activation
    /// </summary>
    private void Start()
    {
        moduleId = ++moduleIdCounter;

        ScreenText.text = "";
        for (int i = 0; i < Buttons.Length; i++)
        {
            int j = i;
            Buttons[j].OnInteract += delegate ()
            {
                HandlePress(j);
                return false;
            };
        }

        Generate();
        ScreenText.text = DisplayNumber(coef[0], true) + "x" + GetSign(coef[1]) + DisplayNumber(coef[1], false) + "y" + GetSign(coef[2]) + DisplayNumber(coef[2], false) + "z" + GetSign(coef[3]) + DisplayNumber(coef[3], false) + "w=" + N;
    }

    #region Button Methods

    /// <summary>
    /// Button Press Event.
    /// </summary>
    /// <param name="btn">Button id</param>
    private void HandlePress(int btn)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[btn].transform);
        Buttons[btn].AddInteractionPunch();

        if (solved)
            return;

        string text = ButtonTexts[btn].text;

        switch (text)
        {
            case "S": // Pushing the SUBMIT button
                PressSubmit(btn);
                break;
            case "C": // Pushing CLEAR button
                PressClear();
                break;
            case "-": // Pushing MINUS button
                PressMinus();
                break;
            default: // Pushing number button
                PressNumber(btn);
                break;
        }
    }

    /// <summary>
    /// On pressing SUBMIT.
    /// </summary>
    /// <param name="btn">Just for sound support</param>
    private void PressSubmit(int btn)
    {
        if (currentIndex == 0 && submission[0] == "" && !solutionExists) // No solutions is correct answer, mosule solved
        {
            Debug.LogFormat("[Diophantine Equations #{0}]: Player pressed SUBMIT, implying there are no solutions.", moduleId);
            Debug.LogFormat("[Diophantine Equations #{0}]: It worked, module solved!", moduleId);

            StartCoroutine(AnimateScreenPass());
            solved = true;

            Module.HandlePass();
            Audio.PlaySoundAtTransform("solve", Buttons[btn].transform);
        }
        else if (currentIndex == 0 && submission[0] == "" && solutionExists) // No solutions is wrong answer, module striked
        {
            Debug.LogFormat("[Diophantine Equations #{0}]: Player pressed SUBMIT, implying there are no solutions.", moduleId);
            Debug.LogFormat("[Diophantine Equations #{0}]: That didn't work well, strike!", moduleId);

            StartCoroutine(AnimateScreenStrike());
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("strike", Buttons[btn].transform);
        }
        else if (submission[currentIndex] != "") // Submitting an answer
        {
            if (int.Parse(submission[currentIndex]) == answer[currentIndex]) // Answer is correct
            {
                Debug.LogFormat("[Diophantine Equations #{0}]: Player's submission for {1} is {2}. That was correct!", moduleId, VarNames[currentIndex], submission[currentIndex]);
                // Iterating index
                currentIndex++;

                if (currentIndex < 4) // Displaying next submission prompt
                    ScreenText.text = VarNames[currentIndex] + " = " + submission[currentIndex];
                else // All answers were correct, module solved
                {
                    Debug.LogFormat("[Diophantine Equations #{0}]: All answers were correct, module solved!", moduleId);

                    StartCoroutine(AnimateScreenPass());
                    solved = true;

                    Module.HandlePass();
                    Audio.PlaySoundAtTransform("solve", Buttons[btn].transform);
                }
            }
            else // Answer is incorrect, submission reset
            {
                Debug.LogFormat("[Diophantine Equations #{0}]: Player's submission for {1} is {2}. The correct answer is {3}. Strike! Submission reset.", moduleId, VarNames[currentIndex], submission[currentIndex], answer[currentIndex]);

                StartCoroutine(AnimateScreenStrike());
                Module.HandleStrike();
                Audio.PlaySoundAtTransform("strike", Buttons[btn].transform);

                submission = new string[4] { "", "", "", "" };
                currentIndex = 0;
            }
        }
    }

    /// <summary>
    /// On pressing CLEAR.
    /// </summary>
    private void PressClear()
    {
        // Truncating the last digit
        if (submission[currentIndex] != "")
        {
            submission[currentIndex] = submission[currentIndex].Substring(0, submission[currentIndex].Length - 1);
            ScreenText.text = VarNames[currentIndex] + " = " + submission[currentIndex];


        }

        if (currentIndex == 0 && submission[currentIndex] == "")
            ScreenText.text = DisplayNumber(coef[0], true) + "x" + GetSign(coef[1]) + DisplayNumber(coef[1], false) + "y" + GetSign(coef[2]) + DisplayNumber(coef[2], false) + "z" + GetSign(coef[3]) + DisplayNumber(coef[3], false) + "w=" + N;

    }

    /// <summary>
    /// On pressing MINUS.
    /// </summary>
    private void PressMinus()
    {
        // Adding minus at the beginning of the submission
        if (submission[currentIndex] != "")
        {
            if (submission[currentIndex][0] != '-')
                submission[currentIndex] = '-' + submission[currentIndex];
            else
                submission[currentIndex] = submission[currentIndex].Substring(1);
        }
        else
            submission[currentIndex] = "-";

        // Displaying submission on the display
        ScreenText.text = VarNames[currentIndex] + " = " + submission[currentIndex];
    }

    /// <summary>
    /// On pressing the number button.
    /// </summary>
    /// <param name="btn">Button Id</param>
    private void PressNumber(int btn)
    {
        if (submission[currentIndex].Length < 4)
        {
            // Adding the digit to a submission
            submission[currentIndex] += ButtonTexts[btn].text;

            // Displaying submission on the display
            ScreenText.text = VarNames[currentIndex] + " = " + submission[currentIndex];
        }
    }

    #endregion

    #region Display Equations
    /// <summary>
    /// Returns + if number is positive else returns -.
    /// </summary>
    /// <param name="num">Number</param>
    /// <returns></returns>
    private string GetSign(int num)
    {
        return num >= 0 ? "+" : "-";
    }

    /// <summary>
    /// Displays an absolute value of a number if it's not equal to 1 with or without sign.
    /// </summary>
    /// <param name="num">Number</param>
    /// <param name="sign">Show sign</param>
    /// <returns></returns>
    private string DisplayNumber(int num, bool sign)
    {
        if (!sign)
            return Math.Abs(num) == 1 ? "" : Math.Abs(num).ToString();
        else
            return Math.Abs(num) == 1 ? (num == -1 ? "-" : "") : num.ToString();
    }
    #endregion

    #region Answer Generation

    /// <summary>
    /// Generation of the problem and the answer.
    /// </summary>
    private void Generate()
    {
        // Variable defenitions
        int m = 0, k = 0, mi = 0, ki = 0, q = 0, l = 0, li = 0, index = 0;
        int[] t;
        int[,] matrix;
        char ch;
        string debug;

        // Variable initializations
        t = new int[4];

        // Problem generation
        for (int i = 0; i < coef.Length; i++)
            coef[i] = GetRandomValue();
        N = GetRandomValue();

        // Matrix composition 
        matrix = new int[5, 4] { { coef[0], coef[1], coef[2], coef[3] }, { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };

        Debug.LogFormat("[Diophantine Equations #{0}]: Initial matrix looks like this: ", moduleId);
        OutputMatrix(matrix);

        // Algorithm runs until only one non-zero number remains in the top row
        while (!OnlyOneNonZero(matrix))
        {
            GetAbsMin(matrix, ref m, ref mi);
            GetNonZero(matrix, mi, ref k, ref ki);
            q = GetQ(m, k);
            Subtract(ref matrix, mi, ki, q);

            // Logging
            Debug.LogFormat("[Diophantine Equations #{0}]: Value of M = {1} in column number {2}", moduleId, m, mi + 1);
            Debug.LogFormat("[Diophantine Equations #{0}]: Value of K = {1} in column number {2}", moduleId, k, ki + 1);
            Debug.LogFormat("[Diophantine Equations #{0}]: {1} = {2} * {3} + {4}, so value of Q = {3}", moduleId, k, m, q, k - m * q);
            Debug.LogFormat("[Diophantine Equations #{0}]: Subtracting column {1} multiplied by {2} from column {3}", moduleId, mi + 1, q, ki + 1);
            OutputMatrix(matrix);
        }

        // If L divides N then solution exists
        GetL(matrix, ref l, ref li);
        solutionExists = (N % l == 0);

        Debug.LogFormat("[Diophantine Equations #{0}]: Value of L = {1} in column number {2}", moduleId, l, li + 1);

        // Solution generation
        if (solutionExists)
        {
            Debug.LogFormat("[Diophantine Equations #{0}]: {1} divides {2} so solutions exist", moduleId, l, N);

            for (int i = 0; i < answer.Length; i++)
            {
                ch = Info.GetSerialNumber().ToArray()[i];

                if (i == li)
                {
                    t[i] = N / l;
                    Debug.LogFormat("[Diophantine Equations #{0}]: {1} column contains L so t[{2}] = {3}", moduleId, Ordinals[i], i + 1, t[i]);
                }
                else if (Numbers.Contains(ch))
                {
                    t[i] = int.Parse(ch.ToString());
                    Debug.LogFormat("[Diophantine Equations #{0}]: {1} symbol in serial number is a number {2} so t[{3}] = {4}", moduleId, Ordinals[i], ch, i + 1, t[i]);
                }
                else
                {
                    index = Array.IndexOf(Letters, ch) + 1;
                    t[i] = index % 10;
                    Debug.LogFormat("[Diophantine Equations #{0}]: {1} symbol in serial number is a letter {2} so t[{3}] = {4} mod 10 = {5}", moduleId, Ordinals[i], ch, i + 1, index, t[i]);
                }
            }

            // Solution logging
            for (int i = 0; i < answer.Length; i++)
            {
                debug = VarNames[i] + " = ";
                for (int j = 0; j < answer.Length; j++)
                {
                    answer[i] += t[j] * matrix[i + 1, j];
                    debug += t[j] + " * " + matrix[i + 1, j] + (j < answer.Length - 1 ? " + " : (" = " + answer[i]));
                }

                Debug.LogFormat("[Diophantine Equations #{0}]: " + debug, moduleId);
            }
        }
        else
            Debug.LogFormat("[Diophantine Equations #{0}]: {1} doen't divide {2} so solutions don't exist", moduleId, l, N);
    }

    /// <summary>
    /// Checks if top row of the matrix contains only one non-zero element.
    /// </summary>
    /// <param name="matrix">Matrix</param>
    /// <returns>Returns true if yes.</returns>
    private bool OnlyOneNonZero(int[,] matrix)
    {
        int nonzeros = 0;

        for (int i = 0; i < matrix.GetLength(1); i++)
            if (matrix[0, i] != 0)
                nonzeros++;

        return nonzeros == 1;
    }

    /// <summary>
    /// Gets the minimum by absolute value element in the top row of the matrix and it's column index.
    /// </summary>
    /// <param name="matrix">Matrix</param>
    /// <param name="m">Minimum value</param>
    /// <param name="mi">Column index</param>
    private void GetAbsMin(int[,] matrix, ref int m, ref int mi)
    {
        int j = 0, absmin;
        while (matrix[0, j] == 0)
            j++;

        absmin = Math.Abs(matrix[0, j]);
        m = matrix[0, j];
        mi = j;

        for (int i = j + 1; i < matrix.GetLength(1); i++)
            if (Math.Abs(matrix[0, i]) < absmin && matrix[0, i] != 0)
            {
                absmin = Math.Abs(matrix[0, i]);
                m = matrix[0, i];
                mi = i;
            }
    }

    /// <summary>
    /// Gets First non-zero element in the top row of the matrix ignoring column 'mi'.
    /// </summary>
    /// <param name="matrix">Matrix</param>
    /// <param name="mi">Index of column that should be ignored</param>
    /// <param name="k">First non-zero</param>
    /// <param name="ki">Column index</param>
    private void GetNonZero(int[,] matrix, int mi, ref int k, ref int ki)
    {
        for (int i = 0; i < matrix.GetLength(1); i++)
            if (mi != i && matrix[0, i] != 0)
            {
                k = matrix[0, i];
                ki = i;
                break;
            }
    }

    /// <summary>
    /// Gets number q such that k = m * q + r, where r is strictly positive remainder. 
    /// </summary>
    /// <param name="m">Number m</param>
    /// <param name="k">Number k</param>
    /// <returns>Number q</returns>
    private int GetQ(int m, int k)
    {
        int r = k % m;

        if (r < 0)
            r += Math.Abs(m);

        return (k - r) / m;
    }

    /// <summary>
    /// Subtracts column 'mi' multiplied by 'q' from column 'ki'.
    /// </summary>
    /// <param name="matrix">Matrix</param>
    /// <param name="mi">Column index of m</param>
    /// <param name="ki">Column index of k</param>
    /// <param name="q">Number q</param>
    private void Subtract(ref int[,] matrix, int mi, int ki, int q)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
            matrix[i, ki] -= q * matrix[i, mi];
    }

    /// <summary>
    /// Gets the number left in the top row after all steps of the algorithm.
    /// </summary>
    /// <param name="matrix">Matrix</param>
    /// <param name="L">Number left</param>
    /// <param name="Li">Column index</param>
    private void GetL(int[,] matrix, ref int L, ref int Li)
    {
        for (int i = 0; i < matrix.GetLength(1); i++)
            if (matrix[0, i] != 0)
            {
                L = matrix[0, i];
                Li = i;
            }
    }

    /// <summary>
    /// Generates values for coefficients and the right hand side of the equation, 0 is excluded.
    /// </summary>
    /// <returns>Random value excluding 0.</returns>
    private int GetRandomValue()
    {
        return Rnd.Range(0, 2) == 0 ? Rnd.Range(-10, 0) : Rnd.Range(1, 11);
    }
    #endregion

    #region Animation
    /// <summary>
    /// Screen Animation when solved.
    /// </summary>
    /// <returns>...</returns>
    private IEnumerator AnimateScreenPass()
    {
        ScreenText.text = "Nice!";
        ScreenText.color = new Color32(255, 255, 255, 255);

        for (int i = 0; i < 64; i++)
        {
            ScreenText.fontSize += (i % 5) / 2;
            ScreenRenderer.material.color = new Color32(18, (byte)(2 * i), 18, 255);

            yield return new WaitForSecondsRealtime(0.0005f);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        for (int i = 0; i < 64; i++)
        {
            ScreenText.color = new Color32((byte)(252 - 4 * i), (byte)(252 - 4 * i), (byte)(252 - 4 * i), 255);
            ScreenRenderer.material.color = new Color32(0, (byte)(126 - 2 * i), 0, 255);

            yield return new WaitForSecondsRealtime(0.0005f);
        }
    }

    /// <summary>
    /// Screen Animation when striked.
    /// </summary>
    /// <returns>...</returns>
    private IEnumerator AnimateScreenStrike()
    {
        ScreenText.text = "Wrong!";

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 25; j++)
            {
                ScreenText.fontSize += 5 * ((j % 4) / 3);
                ScreenRenderer.material.color = new Color32((byte)(255 / 25 * j), 18, 18, 255);

                yield return new WaitForSecondsRealtime(0.01f);
            }

            for (int j = 25; j >= 0; j--)
            {
                ScreenText.fontSize -= 5 * (((25 - j) % 4) / 3);
                ScreenRenderer.material.color = new Color32((byte)(255 / 25 * j), 18, 18, 255);

                yield return new WaitForSecondsRealtime(0.01f);
            }

            ScreenRenderer.material.color = new Color32(18, 18, 18, 255);
        }

        ScreenText.fontSize = 25;
        ScreenText.text = DisplayNumber(coef[0], true) + "x" + GetSign(coef[1]) + DisplayNumber(coef[1], false) + "y" + GetSign(coef[2]) + DisplayNumber(coef[2], false) + "z" + GetSign(coef[3]) + DisplayNumber(coef[3], false) + "w=" + N;
    }
    #endregion

    /// <summary>
    /// Logs the matrix
    /// </summary>
    /// <param name="matrix">Matrix</param>
    private void OutputMatrix(int[,] matrix)
    {
        string debug;

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            debug = "";
            for (int j = 0; j < matrix.GetLength(1); j++)
                debug += matrix[i, j] + " ";
            Debug.LogFormat("[Diophantine Equations #{0}]: {1}", moduleId, debug);
        }

        
    }

    #region TP Support

    private const string TwitchHelpMessage = @"!{0} submit x y z w (Example: !{0} submit 5 45 7 -6) To submit that there are no solutions type just !{0} submit";

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split();
        int dummy = 0;

        if(split.Length == 5)
        {
            if (split[0] == "submit" && int.TryParse(split[1], out dummy) && int.TryParse(split[2], out dummy) && int.TryParse(split[3], out dummy) && int.TryParse(split[4], out dummy))
            {
                yield return null;

                for (int i = 1; i < 5; i++)
                {
                    for (int j = 0; j < split[i].Length; j++)
                    {
                        Buttons[GetButtonId(split[i][j])].OnInteract();
                        yield return new WaitForSecondsRealtime(0.2f);
                    }
                    Buttons[7].OnInteract();
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }
            else if (split[0] != "submit")
                yield return string.Format("sendtochaterror {0} is not a valid command", split[0]);
            else
                yield return "sendtochaterror One of the inputs was not a number";
        }
        else if(split.Length == 1)
        {
            if (split[0] == "submit")
            {
                yield return null;

                Buttons[7].OnInteract();
                yield return new WaitForSecondsRealtime(0.2f);
            }
            else
                yield return string.Format("sendtochaterror {0} is not a valid command", split[0]);
        }
        else
            yield return "sendtochaterror Not enough parameters";


    }

    private int GetButtonId(char ch)
    {
        switch(ch)
        {
            case '0':
                return 11;
            case '1':
                return 8;
            case '2':
                return 9;
            case '3':
                return 10;
            case '4':
                return 4;
            case '5':
                return 5;
            case '6':
                return 6;
            case '7':
                return 0;
            case '8':
                return 1;
            case '9':
                return 2;
            case '-':
                return 12;
            default:
                return -1;

        }
    }

    #endregion
}
