using UnityEngine;

namespace NetLib.UI.Keyboard
{
	/// <summary>
	/// Script for opening and initializing a keyboard for a specific input object.
	/// </summary>
	public class OpenCanvasKeyboard : MonoBehaviour 
	{
        [Tooltip("Canvas to open keyboard under")]
		public GameObject canvasKeyboardObject;

		[Tooltip("Input Object to receive text")]
		public GameObject inputObject;

		[Tooltip("The prefab from which to instantiate the keyboard")]
        public GameObject keyboardPrefab;

		/// <summary>
		/// Opens a new keyboard with the values set in the inspector.
		/// </summary>
		public void OpenKeyboard() 
		{		
			CanvasKeyboard.Open(canvasKeyboardObject, keyboardPrefab, inputObject != null ? inputObject : gameObject);
		}

		/// <summary>
		/// Closes all open keyboards.
		/// </summary>
		public void CloseKeyboard() 
		{		
			CanvasKeyboard.Close();
		}
	}
}