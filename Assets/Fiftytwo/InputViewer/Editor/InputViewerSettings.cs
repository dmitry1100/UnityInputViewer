using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace Fiftytwo
{
    public class InputViewerSettings : SettingsProvider
    {
        private const string compileSymbol = "FIFTYTWO_INPUT_VIEWER";

        private SerializedObject _inputManagerSo;
        private SerializedProperty _axesProp;
        private InputViewer _inputViewer;
        private SerializedObject _inputViewerSo;
        private SerializedProperty _timeoutToHideInactiveAxesInputProp;
        private SerializedProperty _inputViewerAxesProp;
        private bool _isActive;

        private InputViewer.Axis[] _axes;

        public InputViewerSettings( string path, SettingsScope scopes = SettingsScope.Project, IEnumerable<string> keywords = null )
            : base( path, scopes, keywords )
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new InputViewerSettings( "Fiftytwo/Input Viewer", SettingsScope.Project );
            return provider;
        }

        public override void OnActivate( string searchContext, VisualElement rootElement )
        {
            var inputManager = AssetDatabase.LoadMainAssetAtPath( "ProjectSettings/InputManager.asset" );
            _inputManagerSo = new SerializedObject( inputManager );
            _axesProp = _inputManagerSo.FindProperty( "m_Axes" );

            InitializeInputManagerNames();
            InitiallizeIsActive();
        }

        private void InitializeInputManagerNames()
        {
            var isEmpty = _axesProp.arraySize == 0;
            if( isEmpty )
                _axesProp.InsertArrayElementAtIndex( 0 );

            var axis = _axesProp.GetArrayElementAtIndex( 0 );
            var joystickAxis = axis.FindPropertyRelative( "axis" );
            var enumNames = joystickAxis.enumNames;
            for( int i = 0; i < enumNames.Length; ++i )
            {
                var enumName = enumNames[i];
                var bracketIdx = enumName.IndexOf( '(' );
                if( bracketIdx > 0 )
                    enumNames[i] = enumName.Remove( bracketIdx - 1 );
            }

            if( isEmpty )
                _axesProp.DeleteArrayElementAtIndex( 0 );

            _axes = new InputViewer.Axis[enumNames.Length * InputViewer.MaxJoysticksCount];
            int axesIdx = 0;
            for( int joyIdx = 1; joyIdx <= InputViewer.MaxJoysticksCount; joyIdx++ )
            {
                for( int enumNamesIdx = 0; enumNamesIdx < enumNames.Length; ++enumNamesIdx, ++axesIdx )
                {
                    _axes[axesIdx] = new InputViewer.Axis
                    {
                        Name = InputViewer.TestAxisNamePrefix + joyIdx + "_" + ( enumNamesIdx + 1 ),
                        JoystickNumber = joyIdx,
                        JoystickAxisIndex = enumNamesIdx,
                        JoystickAxisName = "joystick " + joyIdx + " " + enumNames[enumNamesIdx]
                    };
                }
            }
        }

        private void InitiallizeIsActive ()
        {
            if( !GetSymbols().Contains( compileSymbol ) )
            {
                _isActive = false;
                Deactivate();
                return;
            }

            var namesSet = new HashSet<string>( _axes.Select( n => n.Name ) );

            int axesCount = _axesProp.arraySize;
            for( int i = 0; i < axesCount; ++i )
            {
                var axisProp = _axesProp.GetArrayElementAtIndex( i );
                var name = axisProp.FindPropertyRelative( "m_Name" ).stringValue;
                if( name.StartsWith( InputViewer.TestAxisNamePrefix ) )
                {
                    if( !namesSet.Remove( name ) )
                    {
                        _isActive = false;
                        Deactivate();
                        return;
                    }
                }
            }

            if( namesSet.Count == 0 )
            {
                _isActive = true;
                CreateInputViewerPrefab();
                SetCompileSymbol();
                return;
            }

            _isActive = false;
            if( namesSet.Count != _axes.Length )
                Deactivate();
            else
            {
                DestroyInputViewerPrefab();
                RemoveCompileSymbol();
            }
        }

        public override void OnGUI( string searchContext )
        {
            EditorGUI.BeginChangeCheck();
            _isActive = EditorGUILayout.ToggleLeft( "Active", _isActive );
            if( EditorGUI.EndChangeCheck() )
            {
                if( _isActive )
                    Activate();
                else
                    Deactivate();
            }

            if( _isActive )
            {
                EditorGUI.BeginChangeCheck();
                _timeoutToHideInactiveAxesInputProp.floatValue =
                    EditorGUILayout.FloatField( _timeoutToHideInactiveAxesInputProp.displayName,
                        _timeoutToHideInactiveAxesInputProp.floatValue );
                if( EditorGUI.EndChangeCheck() )
                    _inputViewerSo.ApplyModifiedProperties();
            }
        }

        private void Activate ()
        {
            CreateInputViewerPrefab();
            _inputViewerAxesProp.arraySize = _axes.Length;

            for( int i = 0, insertIdx = _axesProp.arraySize; i < _axes.Length; ++i, ++insertIdx )
            {
                _axesProp.InsertArrayElementAtIndex( insertIdx );
                var axisProp = _axesProp.GetArrayElementAtIndex( insertIdx );
                axisProp.FindPropertyRelative( "m_Name" ).stringValue = _axes[i].Name;
                axisProp.FindPropertyRelative( "descriptiveName" ).stringValue = "";
                axisProp.FindPropertyRelative( "descriptiveNegativeName" ).stringValue = "";
                axisProp.FindPropertyRelative( "positiveButton" ).stringValue = "";
                axisProp.FindPropertyRelative( "altNegativeButton" ).stringValue = "";
                axisProp.FindPropertyRelative( "altPositiveButton" ).stringValue = "";
                axisProp.FindPropertyRelative( "gravity" ).floatValue = 0.0f;
                axisProp.FindPropertyRelative( "dead" ).floatValue = 0.19f;
                axisProp.FindPropertyRelative( "sensitivity" ).floatValue = 1.0f;
                axisProp.FindPropertyRelative( "snap" ).boolValue = false;
                axisProp.FindPropertyRelative( "invert" ).boolValue = false;
                axisProp.FindPropertyRelative( "type" ).enumValueIndex = 2; // Joystick Axis
                axisProp.FindPropertyRelative( "axis" ).enumValueIndex = _axes[i].JoystickAxisIndex;
                axisProp.FindPropertyRelative( "joyNum" ).enumValueIndex = _axes[i].JoystickNumber;

                var prefabAxisProp = _inputViewerAxesProp.GetArrayElementAtIndex( i );
                prefabAxisProp.FindPropertyRelative( "Name" ).stringValue = _axes[i].Name;
                prefabAxisProp.FindPropertyRelative( "JoystickNumber" ).intValue = _axes[i].JoystickNumber;
                prefabAxisProp.FindPropertyRelative( "JoystickAxisIndex" ).intValue = _axes[i].JoystickAxisIndex;
                prefabAxisProp.FindPropertyRelative( "JoystickAxisName" ).stringValue = _axes[i].JoystickAxisName;
            }

            _inputManagerSo.ApplyModifiedProperties();
            _inputViewerSo.ApplyModifiedProperties();

            SetCompileSymbol();
        }

        private void CreateInputViewerPrefab ()
        {
            var go = new GameObject( InputViewer.PrefabResourcePath );
            var viewer = go.AddComponent<InputViewer>();
            var script = MonoScript.FromMonoBehaviour( viewer );
            var scriptPath = AssetDatabase.GetAssetPath( script );
            var scriptDirPath = Path.GetDirectoryName( scriptPath );
            var resourcesDirPath = scriptDirPath + "/Resources";
            if( !Directory.Exists( resourcesDirPath ) )
                AssetDatabase.CreateFolder( scriptDirPath, "Resources" );
            var prefabPath = resourcesDirPath + "/" + InputViewer.PrefabResourcePath + ".prefab";
            if( !File.Exists( prefabPath ) )
                PrefabUtility.SaveAsPrefabAsset( go, prefabPath );
            Object.DestroyImmediate( go );

            _inputViewer = AssetDatabase.LoadAssetAtPath<InputViewer>( prefabPath );
            _inputViewerSo = new SerializedObject( _inputViewer );
            _timeoutToHideInactiveAxesInputProp =
                _inputViewerSo.FindProperty( "_timeoutToHideInactiveAxesInput" );
            _inputViewerAxesProp = _inputViewerSo.FindProperty( "_axes" );
        }

        private void Deactivate ()
        {
            for( int i = _axesProp.arraySize; --i >= 0; )
            {
                var axisProp = _axesProp.GetArrayElementAtIndex( i );
                if( axisProp.FindPropertyRelative( "m_Name" ).stringValue.StartsWith( InputViewer.TestAxisNamePrefix ) )
                {
                    _axesProp.DeleteArrayElementAtIndex( i );
                }
            }

            _inputManagerSo.ApplyModifiedProperties();

            DestroyInputViewerPrefab();

            RemoveCompileSymbol();
        }

        private void DestroyInputViewerPrefab ()
        {
            _timeoutToHideInactiveAxesInputProp = null;
            _inputViewerSo = null;
            _inputViewer = null;

            var go = new GameObject( InputViewer.PrefabResourcePath );
            var viewer = go.AddComponent<InputViewer>();
            var script = MonoScript.FromMonoBehaviour( viewer );
            var scriptPath = AssetDatabase.GetAssetPath( script );
            var scriptDirPath = Path.GetDirectoryName( scriptPath );
            var resourcesDirPath = scriptDirPath + "/Resources";
            if( Directory.Exists( resourcesDirPath ) )
                AssetDatabase.DeleteAsset( resourcesDirPath );
            Object.DestroyImmediate( go );
        }

        private void SetCompileSymbol ()
        {
            var symbols = GetSymbols();
            if( symbols.Contains( compileSymbol ) )
                return;

            symbols.Add( compileSymbol );
            SetSymbols( symbols );
        }

        private void RemoveCompileSymbol ()
        {
            var symbols = GetSymbols();
            bool changed = false;
            while( symbols.Remove( compileSymbol ) )
                changed = true;
            if( changed )
                SetSymbols( symbols );
        }

        private List<string> GetSymbols ()
        {
            var symbolsStr = PlayerSettings.GetScriptingDefineSymbolsForGroup( GetBuildTargetGroup() );
            return symbolsStr.Split( new [] { ';' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
        }

        private void SetSymbols ( IEnumerable<string> symbols )
        {
            var symbolsStr = string.Join( ";", symbols );
            PlayerSettings.SetScriptingDefineSymbolsForGroup( GetBuildTargetGroup(), symbolsStr );
        }

        private BuildTargetGroup GetBuildTargetGroup ()
        {
            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            return BuildPipeline.GetBuildTargetGroup( currentBuildTarget );
        }
    }
}
