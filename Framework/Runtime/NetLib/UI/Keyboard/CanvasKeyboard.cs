using UnityEngine;
using System.Reflection;

namespace NetLib.UI.Keyboard
{
    /// <summary>
    /// A generic 2d ui keyboard.
    /// An implementation can set the input by calling SendKeyString.
    /// </summary>
	public class CanvasKeyboard : MonoBehaviour 
	{
        [Tooltip("The object on which to write the keyboard input")]
        public GameObject inputObject;

        /// <summary>
        /// Opens a new keyboard.
        /// </summary>
        /// <param name="parent">The GameObject to act as the parent container for the keyboard GameObject</param>
        /// <param name="keyboardPrefab">The prefab from which to initialize the keyboard object</param>
        /// <param name="inputObject">The GameObject to redirect user input to</param>
		public static void Open(GameObject parent, GameObject keyboardPrefab, GameObject inputObject = null)
		{
			// Don't open the keyboard if it is already open for the current input object
			var keyboard = FindObjectOfType<CanvasKeyboard>();
            if (keyboard != null && keyboard.inputObject == inputObject) 
                return;

            Close();

            var keyboardObject = Instantiate(keyboardPrefab);
            keyboard = keyboardObject.GetComponent<CanvasKeyboard>();
            keyboard.transform.SetParent(parent.transform, false);
            keyboard.inputObject = inputObject;
        }
		
        /// <summary>
        /// Closes all keyboards in the scene.
        /// </summary>
		public static void Close()
		{
            foreach (var kb in FindObjectsOfType<CanvasKeyboard>())
			{
				kb.CloseKeyboard();
			}
		}
		
        /// <summary>
        /// Returns true if any keyboard is open in the scene.
        /// </summary>
		public static bool IsOpen => FindObjectsOfType<CanvasKeyboard>().Length != 0;

        /// <summary>
        /// The current text that was entered by the user.
        /// </summary>
        public string Text
        {
            get
            {
                if (inputObject == null) 
                    return "";

                var components = inputObject.GetComponents(typeof(Component));
                foreach (var component in components)
                {
                    var prop = GetTextProperty(component);
                    if (prop != null)
                    {
                        return prop.GetValue(component, null) as string;
                    }
                }
                return inputObject.name;
            }

            private set
            {
                if (inputObject == null) 
                    return;

                var components = inputObject.GetComponents(typeof(Component));
                foreach (var component in components)
                {
                    var prop = GetTextProperty(component);
                    if (prop != null)
                    {
                        prop.SetValue(component, value, null);
                        return;
                    }
                }
                inputObject.name = value;
            }
        }

        /// <summary>
        /// Appends a string of text to the user input. Supports the ascii backspace character.
        /// </summary>
        /// <param name="keyString"></param>
        public void SendKeyString(string keyString)
		{
			if (keyString.Length == 1 && keyString[0] == 8/*ASCII.Backspace*/)
			{
				if (Text.Length > 0)
				{
					Text = Text.Remove(Text.Length - 1); 
				}
			}
			else
			{
				Text += keyString;
			}
        }

        /// <summary>
        /// Closes this keyboard by destroying the GameObject.
        /// </summary>
		public void CloseKeyboard()
		{
			Destroy(gameObject);
		}


        private static PropertyInfo GetTextProperty(Component component) =>
            component.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
    }
}