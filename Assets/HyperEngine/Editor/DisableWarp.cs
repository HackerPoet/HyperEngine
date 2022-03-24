#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

//Ensure class initializer is called whenever scripts recompile
[InitializeOnLoadAttribute]
public static class DisableWarp {
    //Register an event handler when the class is initialized
    static DisableWarp() {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state) {
        //Activate script AFTER play-mode exits
        if (state == PlayModeStateChange.EnteredEditMode) {
            //Unity editor should show things in Euclidean coordinates when game is not running
            Shader.SetGlobalFloat("_Enable", 0.0f);
            Shader.SetGlobalFloat("_DualLight", 0.0f);
            Shader.SetGlobalVector("_WarpParams", Vector4.zero);

            //Re-enable culling for the scene view camera
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv) sv.camera.ResetCullingMatrix();
        } else if (state == PlayModeStateChange.EnteredPlayMode) {
            //Disable culling for the scene view camera during gameplay
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv) sv.camera.cullingMatrix = DisableCameraCull.hugeBounds;
        }
    }
}
#endif
