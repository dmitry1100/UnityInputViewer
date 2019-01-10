using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Axis 9[0]: m_Name = string:String:"Horizontal"
// Axis 9[1]: descriptiveName = string:String:""
// Axis 9[2]: descriptiveNegativeName = string:String:""
// Axis 9[3]: negativeButton = string:String:""
// Axis 9[4]: positiveButton = string:String:""
// Axis 9[5]: altNegativeButton = string:String:""
// Axis 9[6]: altPositiveButton = string:String:""
// Axis 9[7]: gravity = float:Float:0
// Axis 9[8]: dead = float:Float:0.19
// Axis 9[9]: sensitivity = float:Float:1
// Axis 9[10]: snap = bool:Boolean:False
// Axis 9[11]: invert = bool:Boolean:False
// Axis 9[12]: type = Enum:Enum:[2]"Joystick Axis"
// Axis 9[13]: axis = Enum:Enum:[0]"X axis"
// Axis 9[14]: joyNum = Enum:Enum:[0]"Get Motion from all Joysticks"


namespace Fiftytwo
{
    public class InputViewer : MonoBehaviour
    {
        [Serializable]
        public class Axis
        {
            public string Name;
            public int JoystickNumber;
            public int JoystickAxisIndex;
            public string JoystickAxisName;

            [NonSerialized]
            public float CurrentValue;
            [NonSerialized]
            public float InactiveTime;
        }

        public const int MaxJoysticksCount = 16;
        public const int MaxJoystickButtonsCount = 20;
        public const string TestAxisNamePrefix = "_fiftytwo_test_axis_";
        public const string PrefabResourcePath = "Fiftytwo.InputViewer";

        [SerializeField]
        private float _timeoutToHideInactiveAxesInput = 1.5f;

        [SerializeField]
        private Axis[] _axes;

#if FIFTYTWO_INPUT_VIEWER

        private readonly static string[] _baseKeyCodes =
        {
            "backspace",
            "delete",
            "tab",
            "clear",
            "return",
            "pause",
            "escape",
            "space",

            "[0]",
            "[1]",
            "[2]",
            "[3]",
            "[4]",
            "[5]",
            "[6]",
            "[7]",
            "[8]",
            "[9]",
            "[.]",
            "[/]",
            "[*]",
            "[-]",
            "[+]",
            "equals",
            "enter",

            "up",
            "down",
            "right",
            "left",

            "insert",
            "home",
            "end",
            "page up",
            "page down",

            "f1",
            "f2",
            "f3",
            "f4",
            "f5",
            "f6",
            "f7",
            "f8",
            "f9",
            "f10",
            "f11",
            "f12",
            "f13",
            "f14",
            "f15",

            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "-",
            "=",

            "!",
            "@",
            "#",
            "$",
            "%",
            "^",
            "&",
            "*",
            "(",
            ")",
            "_",
            "+",

            "[",
            "]",
            "`",
            "{",
            "}",
            "~",

            ";",
            "'",
            "\\",
            ":",
            "\"",
            "|",

            ",",
            ".",
            "/",
            "<",
            ">",
            "?",

            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h",
            "i",
            "j",
            "k",
            "l",
            "m",
            "n",
            "o",
            "p",
            "q",
            "r",
            "s",
            "t",
            "u",
            "v",
            "w",
            "x",
            "y",
            "z",

            "numlock",
            "caps lock",
            "scroll lock",
            "right shift",
            "left shift",
            "right ctrl",
            "left ctrl",
            "right alt",
            "left alt",
            "right cmd",
            "left cmd",
            "right super",
            "left super",
            "alt gr",

            "compose",
            "help",
            "print screen",
            "sys req",
            "break",
            "menu",
            "power",
            "euro",
            "undo",

            "mouse 0",
            "mouse 1",
            "mouse 2",
            "mouse 3",
            "mouse 4",
            "mouse 5",
            "mouse 6"
        };

        private readonly static string[] _keyCodes;

        private StringBuilder _sb;
        private Canvas _canvas;
        private GameObject _inputInfoGo;
        private Text _text;

        static InputViewer ()
        {
            _keyCodes = new string[_baseKeyCodes.Length + MaxJoysticksCount * MaxJoystickButtonsCount];

            int i;
            for( i = 0; i < _baseKeyCodes.Length; ++i )
                _keyCodes[i] = _baseKeyCodes[i];

            for( int joyIdx = 1; joyIdx <= MaxJoysticksCount; ++joyIdx )
            {
                for( int buttonIdx = 0; buttonIdx < MaxJoystickButtonsCount; ++buttonIdx, ++i )
                {
                    _keyCodes[i] = "joystick " + joyIdx + " button " + buttonIdx;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.AfterSceneLoad )]
        private static void Initizlize ()
        {
            var prefab = Resources.Load( PrefabResourcePath ) as GameObject;
            if( prefab == null )
                return;

            var go = Instantiate( prefab );
            go.name = prefab.name;
            DontDestroyOnLoad( go );
        }

        private void Start ()
        {
            _sb = new StringBuilder();

            for( int i = 0; i < _axes.Length; ++i )
            {
                _axes[i].CurrentValue = Input.GetAxis( _axes[i].Name );
                _axes[i].InactiveTime = _timeoutToHideInactiveAxesInput;
            }

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            _inputInfoGo = new GameObject( "Input Info", typeof( RectTransform ) );
            _inputInfoGo.transform.SetParent( transform, false );
            var rt = _inputInfoGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var bgImage = _inputInfoGo.AddComponent<Image>();
            bgImage.color = new Color( 0, 0, 0, 0.5f );

            var textGo = new GameObject( "Input Info Text", typeof( RectTransform ) );
            textGo.transform.SetParent( _inputInfoGo.transform, false );
            rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = new Vector2( 5, 5 );
            rt.offsetMax = new Vector2( -5, -5 );
            _text = textGo.AddComponent<Text>();
            _text.font = Font.CreateDynamicFontFromOSFont( "Helvetica", 16 );
            _text.fontSize = 16;
            _text.color = Color.white;
            _text.text = "";
            _canvas.enabled = false;
            _inputInfoGo.SetActive( false );

            Debug.Log( "Initialize InputViewer" );
        }

        private void Update ()
        {
            _sb.Length = 0;

            int i;
            for( i = 0; i < _axes.Length; ++i )
            {
                var axis = _axes[i];
                var value = Input.GetAxis( axis.Name );

                if( Mathf.Approximately( value, axis.CurrentValue ) )
                {
                    if( axis.InactiveTime < _timeoutToHideInactiveAxesInput )
                        axis.InactiveTime += Time.deltaTime;
                }
                else
                {
                    axis.CurrentValue = value;
                    axis.InactiveTime = 0.0f;
                }

                if( axis.InactiveTime < _timeoutToHideInactiveAxesInput )
                {
                    _sb.AppendLine( _axes[i].JoystickAxisName + " = " + value.ToString( "G3" ) );
                }
            }

            for( i = 0; i < _keyCodes.Length; ++i )
            {
                if( Input.GetKey( _keyCodes[i] ) )
                {
                    _sb.AppendLine( _keyCodes[i] );
                }
            }

            if( _sb.Length > 0 )
            {
                if( !_canvas.enabled )
                {
                    _canvas.enabled = true;
                    _inputInfoGo.SetActive( true );
                }
                _text.text = _sb.ToString();
            }
            else
            {
                if( _canvas.enabled )
                {
                    _canvas.enabled = false;
                    _inputInfoGo.SetActive( false );
                    _text.text = "";
                }
            }
        }

#endif
    }
}
