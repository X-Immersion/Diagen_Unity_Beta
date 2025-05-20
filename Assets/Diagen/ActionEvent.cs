using System.Reflection;
using UnityEngine;

namespace DiagenCommonTypes
{
    [System.Serializable]
    public class ActionEvent
    {
        [SerializeField] public GameObject scriptObject; // Reference to the GameObject

        public void Execute()
        {
            if (scriptObject == null)
            {
                Debug.LogError("❌ ActionEvent.Execute(): scriptObject is NULL!");
                return;
            }

            // Ensure we're using the in-scene instance instead of a prefab reference
            GameObject sceneObject = GameObject.Find(scriptObject.name);

            if (sceneObject != null)
            {
                scriptObject = sceneObject; // Replace with the in-scene instance
            }
            else
            {
                Debug.LogError($"❌ No GameObject found in the scene with the name '{scriptObject.name}'!");
                return;
            }

            // 🔹 Find any MonoBehaviour on the object that has a "Run" method
            MonoBehaviour[] scripts = scriptObject.GetComponents<MonoBehaviour>();
            MethodInfo runMethod = null;
            MonoBehaviour targetScript = null;

            foreach (var script in scripts)
            {
                runMethod = script.GetType().GetMethod("Run", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (runMethod != null)
                {
                    targetScript = script;
                    break; // Stop at the first valid script with "Run"
                }
            }

            if (targetScript == null || runMethod == null)
            {
                Debug.LogError($"❌ No script with a 'Run()' method found on '{scriptObject.name}'! Available components:");
                foreach (var comp in scriptObject.GetComponents<Component>())
                {
                    Debug.Log($"📌 Component: {comp.GetType().Name}");
                }
                return;
            }

            // 🔹 Invoke "Run" dynamically
            runMethod.Invoke(targetScript, null);
            Debug.Log($"✅ Successfully invoked 'Run()' on '{scriptObject.name}' using '{targetScript.GetType().Name}'.");
        }
    }
}
