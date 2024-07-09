using System;
using System.Linq;
using NetLib.Script;
using NetLib.UI.Keyboard;
using UnityEngine;

namespace NetLib.UI
{
    /// <summary>
    /// Provides functionality for the connection menu prefab.
    /// </summary>
    public class ConnectionMenu : MonoBehaviour
    {
        [Tooltip("The NetworkManager to be ")]
        public NetworkManager networkManager;

        [Tooltip("The default port value to be used for setting up a connection. " +
                 "This will be used when no port is entered in the menu on runtime.")]
        public ushort defaultPort = 1337;

        [Tooltip("The default ip value to be used for setting up a connection. " + 
                 "This will be used when no port is entered in the menu on runtime.")]
        public string defaultIp = "127.0.0.1";

        [Tooltip("Setting this to true will display a debug console in the menu which captures Unity logs")]
        public bool showDebugLog;

        [Tooltip("Set this to true if you do not wish to show the 3D keyboard when clicking on an input field.")]
        public bool useNativeKeyboard;

        // ------------------------------------------------------------------------------------------------------------
        [Header("Internal Settings - Do not modify")]

        [Tooltip("The ToggleGroup which manages the server/client options")]
        public UnityEngine.UI.ToggleGroup toggleGroup;
        [Tooltip("The InputField for the server ip")]
        public UnityEngine.UI.InputField ipInput;
        [Tooltip("The InputField for the connection port")]
        public UnityEngine.UI.InputField portInput;
        [Tooltip("The object to activate when a menu related error message is to be displayed")]
        public GameObject errorLabel;
        [Tooltip("The component to write menu related error messages to")]
        public UnityEngine.UI.Text errorText;
        [Tooltip("The GameObject to treat as the output console")]
        public GameObject logConsole;
        [Tooltip("The component to redirect Unity logs to")]
        public UnityEngine.UI.Text logConsoleText;
        
        // updated with debug log messages
        // used since we cannot directly write to a Text component from outside the main unity thread,
        // but log messages may be dispatched in other threads
        private string logTextThreadSafe;
        private bool logTextChanged;


        /// <summary>
        /// Quits the application.
        /// </summary>
        public static void QuitApplication() =>
            Application.Quit();

        /// <summary>
        /// Starts the NetworkManager with the options set in the menu.
        /// </summary>
        public void Connect()
        {
            ResetErrors();

            if (!toggleGroup.ActiveToggles().Any())
            {
                DisplayError("<color=red>Error:</color> No start option selected");
                return;
            }

            var activeToggle = toggleGroup.ActiveToggles().First();
            switch (activeToggle.gameObject.name)
            {
                case "ServerToggle":
                    StartServer();
                    break;
                case "ClientToggle":
                    StartClient();
                    break;
            }
        }

        public void OpenKeyboard(OpenCanvasKeyboard k)
        {
            if (!useNativeKeyboard)
                k.OpenKeyboard();
        }

        private void Awake()
        {
            // if no NetworkManager is set in the Inspector, try to find one in the scene
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<NetworkManager>();
            }

            if (networkManager == null)
            {
                Debug.LogError("Could not find NetworkManager");
            }

            ipInput.placeholder.GetComponent<UnityEngine.UI.Text>().text = defaultIp;
            portInput.placeholder.GetComponent<UnityEngine.UI.Text>().text = defaultPort.ToString();

            logTextThreadSafe = "";

            errorLabel.SetActive(false);
            logConsole.SetActive(false);
        }

        private void Start()
        {
            // in this case, we don't need a menu
            if (networkManager.autoStartOption != NetworkManager.AutoStartOption.None)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            // workaround for when Debug.Log is called outside the main thread
            if (showDebugLog && logTextChanged)
            {
                logTextChanged = false;
                logConsoleText.text = logTextThreadSafe;
                logConsole.SetActive(true);
                logConsole.GetComponent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition = 0;
            }

            // hide when networkManager is initialized
            if (gameObject.activeSelf && networkManager.IsRunning)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (showDebugLog)
                Application.logMessageReceivedThreaded += HandleLog;
        }

        private void OnDisable()
        {
            if (showDebugLog)
                Application.logMessageReceivedThreaded -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (logTextThreadSafe.Length != 0)
                logTextThreadSafe += "\n";

            logTextThreadSafe += message;
            logTextChanged = true;
        }

        private void StartServer()
        {
            if (!ParsePort(out ushort port))
                return;

            // TODO: Improve connection failure logic
            networkManager.Stop();
            networkManager.StartServer(port);
            DisplayError("Starting server...");
        }

        private void StartClient()
        {
            if (!ParsePort(out ushort port))
                return;
            if (!ParseIp(out string ip))
                return;

            // TODO: Improve connection failure logic
            networkManager.Stop();
            networkManager.StartClient(ip, port);
            DisplayError("Connecting...");
        }

        // return true if the port is a valid and writes the port to result
        private bool ParsePort(out ushort result)
        {
            bool success;
            if (string.IsNullOrEmpty(portInput.textComponent.text))
            {
                result = defaultPort;
                success = true;
            }
            else
            {
                success = ushort.TryParse(portInput.textComponent.text, out result);
            }

            if (success) 
                return true;

            DisplayError("<color=red>Error:</color> Invalid input for port");
            return false;
        }

        // return true if the ip is a valid and writes the ip to result
        private bool ParseIp(out string result)
        {
            result = string.IsNullOrEmpty(ipInput.textComponent.text) ? defaultIp : ipInput.textComponent.text;

            if (result.ToLower().Trim() == "localhost")
            {
                result = result.ToLower().Trim();
                return true;
            }

            try
            {
                System.Net.IPAddress.Parse(result);
            }
            catch (FormatException)
            {
                DisplayError("<color=red>Error:</color> Invalid input for ip address");
                return false;
            }

            return true;
        }

        private void DisplayError(string text)
        {
            errorText.text = text;
            errorLabel.SetActive(true);
        }

        /// <summary>
        /// Resets the menu error field.
        /// </summary>
        public void ResetErrors()
        {
            errorText.text = string.Empty;
            errorLabel.SetActive(false);
        }
    }
}
