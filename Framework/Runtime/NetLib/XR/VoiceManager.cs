using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_WSA
using UnityEngine.Windows.Speech;
#endif

namespace NetLib.XR
{
    /// <summary>
    /// Implements voice commands.
    /// Keywords and actions to be invoked can be set in the inspector.
    /// </summary>
    public class VoiceManager : MonoBehaviour
    {
        [Tooltip("Collection of keyword and the actions to be invoked when the keyword is recognized")]
        public Dictionary<string, Action> keywordCollection;

#if UNITY_WSA
        private KeywordRecognizer keywordRecognizer;
#endif

#if UNITY_WSA
        private void Awake()
        {
            keywordCollection = new Dictionary<string, Action>();
        }

        private void Start()
        {
            // KeywordRecognizer needs at least one keyword
            if (keywordCollection.Count == 0)
                keywordCollection.Add("", () => { });

            keywordRecognizer = new KeywordRecognizer(keywordCollection.Keys.ToArray());

            // Define generic event handler for keyword recognition
            keywordRecognizer.OnPhraseRecognized += (args) =>
            {
                if (keywordCollection.TryGetValue(args.text, out var keywordAction))
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
}
