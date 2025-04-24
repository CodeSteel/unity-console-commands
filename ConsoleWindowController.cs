using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Steelbox.Console
{
    public class ConsoleWindowController : UIWindowBase
    {
        [SerializeField] private CanvasGroup _consoleView;
        [SerializeField] private TMP_InputField _commandInputField;
        [SerializeField] private TMP_Text _commandSuggestionText;
        [SerializeField] private ConsoleLogUIController _consoleLogPrefab;
        [SerializeField] private RectTransform _consoleCommandContentParent;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Toggle _showUnityDebugs;

        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;
        private string _cachedInputBeforeBrowsing = string.Empty;
        
        private List<string> _autocompleteMatches = new();
        private string _lastAutocompleteBase = "";
        
        private bool _isShowing;
        private readonly List<ConsoleLogUIController> _instancedConsoleLogs = new List<ConsoleLogUIController>();
        
        private void Start()
        {
            InputManager.ConsoleAction.performed += OnConsoleInput;
            InputManager.TabAction.performed += OnPressedTab;
            InputManager.ArrowKeysAction.performed += OnPressedArrowKeys;
            ConsoleManager.OnLogToConsole += OnConsoleLog;
            ConsoleManager.OnClear += OnConsoleClear;
            _commandInputField.onSubmit.AddListener(OnSubmitCommandInput);
            _commandInputField.onValueChanged.AddListener(OnInputValueChanged);
            _showUnityDebugs.onValueChanged.AddListener(OnShowUnityDebugsChanged);
        }

        private void OnDestroy()
        {
            InputManager.ConsoleAction.performed -= OnConsoleInput;
            InputManager.TabAction.performed -= OnPressedTab;
            InputManager.ArrowKeysAction.performed -= OnPressedArrowKeys;
            ConsoleManager.OnLogToConsole -= OnConsoleLog;
            ConsoleManager.OnClear -= OnConsoleClear;
            _commandInputField.onSubmit.RemoveListener(OnSubmitCommandInput);
            _commandInputField.onValueChanged.RemoveListener(OnInputValueChanged);
            _showUnityDebugs.onValueChanged.RemoveListener(OnShowUnityDebugsChanged);
        }

        private void OnConsoleInput(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (_isShowing)
            {
                UIManager.Instance.Hide();
            }
            else
            {
                UIManager.Instance.Show(EUIWindow.Console);
            }
        }

        private void OnConsoleLog(string log, ConsoleLogType logType)
        {
            ConsoleLogUIController consoleLog = Instantiate(_consoleLogPrefab, _consoleCommandContentParent);
            consoleLog.Initialize(DateTime.Now.ToShortTimeString(), log, logType);
            _instancedConsoleLogs.Add(consoleLog);
            ScrollBottomNextFrame();
        }

        private void OnConsoleClear()
        {
            foreach (var controller in _instancedConsoleLogs)
            {
                Destroy(controller.gameObject);
            }
            _instancedConsoleLogs.Clear();
            ScrollBottomNextFrame();
        }

        private void OnSubmitCommandInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                ScrollBottomNextFrame();
                return;
            }

            _commandHistory.Add(input);
            _historyIndex = _commandHistory.Count;
            _cachedInputBeforeBrowsing = string.Empty;

            string[] all = input.Split(" ");
            string command = all.Length > 0 ? all[0] : string.Empty;
            string[] args = all.Length > 1 ? all.Skip(1).ToArray() : Array.Empty<string>();
    
            ConsoleManager.Instance.ExecuteCommand(command, args);
            _commandInputField.text = string.Empty;
            
            _autocompleteMatches.Clear();
            _lastAutocompleteBase = "";
            _commandSuggestionText.text = "";
        }

        private void OnInputValueChanged(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                _commandSuggestionText.text = "";
                _autocompleteMatches.Clear();
                _lastAutocompleteBase = string.Empty;
                return;
            }

            string lower = input.ToLower();

            if (_lastAutocompleteBase != lower)
            {
                _lastAutocompleteBase = lower;

                _autocompleteMatches = ConsoleManager.Instance.CommandKeys
                    .Where(cmd => cmd.StartsWith(lower, StringComparison.OrdinalIgnoreCase) && cmd != lower)
                    .OrderBy(cmd => cmd)
                    .ToList();
            }

            if (_autocompleteMatches.Count > 0)
            {
                _commandSuggestionText.text = string.Join("\n", _autocompleteMatches);
            }
            else
            {
                _commandSuggestionText.text = "";
            }
        }

        private void OnPressedTab(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (_autocompleteMatches.Count == 0) return;

            string completed = _autocompleteMatches[0];

            _commandInputField.text = completed + " ";
            _commandInputField.caretPosition = _commandInputField.text.Length;
            _commandInputField.Select();
            _commandInputField.ActivateInputField();

            if (_autocompleteMatches.Count > 0)
            {
                _commandSuggestionText.text = string.Join("\n", _autocompleteMatches);
            }
            else
            {
                _commandSuggestionText.text = string.Empty;
            }
        }

        private void OnPressedArrowKeys(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            float arrowDir = context.ReadValue<float>();

            if (_commandHistory.Count == 0)
                return;

            // Arrow up
            if (arrowDir > 0f)
            {
                if (_historyIndex == _commandHistory.Count)
                {
                    _cachedInputBeforeBrowsing = _commandInputField.text;
                }

                _historyIndex = Mathf.Max(0, _historyIndex - 1);
                _commandInputField.text = _commandHistory[_historyIndex];
                StartCoroutine(DelayedResetCaret());
            }
            // Arrow down
            else if (arrowDir < 0f)
            {
                _historyIndex = Mathf.Min(_commandHistory.Count, _historyIndex + 1);
                if (_historyIndex == _commandHistory.Count)
                {
                    _commandInputField.text = _cachedInputBeforeBrowsing;
                }
                else
                {
                    _commandInputField.text = _commandHistory[_historyIndex];
                }

                _commandInputField.caretPosition = _commandInputField.text.Length;
            }

            _commandInputField.Select();
            _commandInputField.ActivateInputField();
        }

        private void OnShowUnityDebugsChanged(bool showUnityDebugs)
        {
            ConsoleManager.Instance.LogUnityDebugs = showUnityDebugs;
        }
        
        private IEnumerator DelayedResetCaret()
        {
            yield return null;
            yield return null;
            _commandInputField.caretPosition = _commandInputField.text.Length;
        }
        
        private void ScrollBottomNextFrame()
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            yield return null;
            yield return null;
            
            _scrollRect.verticalNormalizedPosition = 0f;
            _commandInputField.Select();
            _commandInputField.ActivateInputField();
        }
        
        public override void Show()
        {
            _isShowing = true;
            
            _consoleView.DOKill();
            _consoleView.gameObject.SetActive(true);
            _consoleView.DOFade(1f, 0.3f);

            _commandInputField.text = string.Empty;

            ScrollBottomNextFrame();
        }

        public override void Hide()
        {
            _isShowing = false;
            
            _consoleView.DOKill();
            _consoleView.DOFade(0f, 0.3f).OnComplete(() =>
            {
                _consoleView.gameObject.SetActive(false);
            });
        }
    }
}