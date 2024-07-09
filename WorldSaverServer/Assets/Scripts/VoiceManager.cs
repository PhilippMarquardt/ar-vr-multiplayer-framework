using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using NetLib.XR;

#if NETLIB_CLIENT_AR
using Speech = UnityEngine.Windows.Speech;
#endif

public class VoiceManager : MonoBehaviour
{
#if NETLIB_CLIENT_AR
    private Speech.KeywordRecognizer keywordRecognizer;
    private delegate void KeywordAction();
    private Dictionary<string, KeywordAction> keywordCollection;
    private GestureManager gestureManager;
    private GazeCursor gazeCursor;

    private void Start()
    {
        gestureManager = GetComponent<GestureManager>();
        gazeCursor = GameObject.Find("Cursor").GetComponent<GazeCursor>();

        // Define specific event handlers for specific keywords
        keywordCollection = new Dictionary<string, KeywordAction>
        {
            { "Start", () => 
                {
                    //gestureManager.StopManipulation();
                    GameObject.Find("World").GetComponent<World>().StartGame();
                } 
            },
            { "Reset", () => 
                { 
                    SceneManager.LoadScene("Menu");
                } 
            },
            { "Menu", () =>
                {
                    SceneManager.LoadScene("Menu");
                }
            },
            {"Connect", () =>
                {
                    
                }
            },
            {"Disconnect", () =>
                {
                    
                }
            },
            {"Lock", () =>
                {
                    if (gazeCursor.FocusedObject != null)
                        gazeCursor.FocusedObject.SendMessage("LockAnchor");
                } 
            },
            {"Unlock", () =>
                {
                    if (gazeCursor.FocusedObject != null)
                        gazeCursor.FocusedObject.SendMessage("UnlockAnchor");
                }
            },
            #region XrAnchorDebug
            {"locate", () => 
                {
                    Debug.Log("XRAnchor: is located: " + GameObject.Find("EarthAnchor").GetComponent<Anchor>().IsLocated);
                }
            },
            {"save", () =>
                {
                    GameObject.Find("EarthAnchor").GetComponent<Anchor>().SaveToStore();
                } 
            },
            {"load", () => 
                {
                    GameObject.Find("EarthAnchor").GetComponent<Anchor>().LoadFromStore(); 
                }
            },
            {"export", () =>
                {
                    AnchorManager.Instance.ExportAnchors();
                }
            },
            {"close", () =>
                {
                    Application.Quit();
                }
            },
            #endregion
        };

        keywordRecognizer = new Speech.KeywordRecognizer(keywordCollection.Keys.ToArray());

        // Define generic event handler for keyword recognition
        keywordRecognizer.OnPhraseRecognized += (args) =>
        {
            if (keywordCollection.TryGetValue(args.text, out KeywordAction keywordAction))
                keywordAction.Invoke();
        };

        keywordRecognizer.Start();
    }

    private void OnDestroy()
    {
        keywordRecognizer.Stop();
        keywordRecognizer.Dispose();
    }
#endif
}
