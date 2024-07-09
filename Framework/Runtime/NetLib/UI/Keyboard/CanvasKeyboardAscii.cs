using UnityEngine;

namespace NetLib.UI.Keyboard
{
    /// <summary>
    /// A keyboard with support for ascii characters.
    /// </summary>
    public class CanvasKeyboardAscii : MonoBehaviour
    {
        [Tooltip("The parent keyboard container")]
        public CanvasKeyboard canvasKeyboard;

        [Tooltip("The keyboard object for the unshifted variant")]
        public GameObject alphaBoardUnshifted;

        [Tooltip("The keyboard object for the shifted variant")]
        public GameObject alphaBoardShifted;

        [Tooltip("The keyboard object for the unshifted number variant")]
        public GameObject numberBoardUnshifted;

        [Tooltip("The keyboard object for the shifted number variant")]
        public GameObject numberBoardShifted;

        private bool shiftDown;
        private bool altDown;

        /// <summary>
        /// Called when a key button is pressed.
        /// </summary>
        /// <param name="kb">The key which was pressed</param>
        public void OnKeyDown(GameObject kb)
        {
            switch (kb.name)
            {
                case "DONE":
                    if (canvasKeyboard != null)
                        canvasKeyboard.CloseKeyboard();
                    break;

                case "ALT":
                    altDown = !altDown;
                    shiftDown = false;
                    Refresh();
                    break;

                case "SHIFT":
                    shiftDown = !shiftDown;
                    Refresh();
                    break;

                default:
                    if (canvasKeyboard != null)
                        canvasKeyboard.SendKeyString(kb.name == "BACKSPACE" ? "\x08" : kb.name);
                    break;
            }
        }

        private void Awake()
        {
            Refresh();
        }

        private void Refresh()
        {
            // Show the current board
            alphaBoardUnshifted.SetActive(!altDown && !shiftDown);
            alphaBoardShifted.SetActive(!altDown && shiftDown);
            numberBoardUnshifted.SetActive(altDown && !shiftDown);
            numberBoardShifted.SetActive(altDown && shiftDown);
        }
    }
}