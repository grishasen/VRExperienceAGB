using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    // Historical component suites retain their original fixture and UI before Start runs.
    // ScenarioRevisionTests exercises the new default experience without these overrides.
    internal static class LegacyTeachingScene
    {
        public static IEnumerator Load()
        {
            UnityAction<Scene,LoadSceneMode> configure=(scene,mode)=> {
                if(scene.name!="OneTreeLearning")return;
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var view in root.GetComponentsInChildren<OneTreeExperience>(true)) {
                        view.useLocalSample=false;view.useUnifiedInterface=false;
                    }
            };
            SceneManager.sceneLoaded+=configure;
            try { yield return SceneManager.LoadSceneAsync("OneTreeLearning",LoadSceneMode.Single); }
            finally { SceneManager.sceneLoaded-=configure; }
        }
    }
}
