using UnityEditor;

[CustomEditor(typeof(CompassTarget))]
public class CompassTarget_CustomEditor : Editor
{
    private SerializedProperty hideWhenOutsideOfRange;
    private SerializedProperty visibilityRange;
    private SerializedProperty showDistanceToTarget;
    private SerializedProperty distanceTextRoundDecimals;

    private void OnEnable()
    {
        hideWhenOutsideOfRange = serializedObject.FindProperty("hideWhenOutsideOfRange");
        visibilityRange = serializedObject.FindProperty("visibilityRange");
        showDistanceToTarget = serializedObject.FindProperty("showDistanceToTarget");
        distanceTextRoundDecimals = serializedObject.FindProperty("distanceTextRoundDecimals");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "hideWhenOutsideOfRange", "visibilityRange", "showDistanceToTarget", "distanceTextRoundDecimals");

        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        AddField(hideWhenOutsideOfRange, indentLevel: 1);

        if (hideWhenOutsideOfRange.boolValue == true)
        {
            AddField(visibilityRange, indentLevel: 2);
            if (visibilityRange.floatValue < 0f) visibilityRange.floatValue = 0f;
        }

        AddField(showDistanceToTarget, indentLevel: 1);

        if (showDistanceToTarget.boolValue == true)
        {
            EditorGUI.indentLevel += 2;
            EditorGUILayout.LabelField("Distance text decimal precision");
            distanceTextRoundDecimals.intValue = EditorGUILayout.IntSlider(distanceTextRoundDecimals.intValue, 0, 2);
            EditorGUI.indentLevel -= 2;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddField(SerializedProperty serializedProperty, int indentLevel)
    {
        EditorGUI.indentLevel += indentLevel;
        EditorGUILayout.PropertyField(serializedProperty);
        EditorGUI.indentLevel -= indentLevel;
    }
}
