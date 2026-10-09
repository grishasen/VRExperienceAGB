using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Non-interactive hover feedback shared by desktop and controller canvas pointers.</summary>
    public sealed class ButtonHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly HashSet<int> pointers=new HashSet<int>();
        private GameObject hint;
        private UnityEngine.UI.Outline outline;
        private static ButtonHint active;
        public bool Visible => hint!=null && hint.activeSelf;
        public void OnPointerEnter(PointerEventData data)
        {
            var button=GetComponent<UnityEngine.UI.Button>();
            if(button==null || !button.IsInteractable())return;
            var view=GetComponentInParent<OneTreeExperience>();if(view==null)return;
            if(active!=null && active!=this)active.Hide();active=this;pointers.Add(data.pointerId);
            if(hint==null) {
                outline=gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.48f,.65f,.68f,.65f);outline.effectDistance=new Vector2(1,-1);
                var canvas=view.Garden.CanvasAt("ButtonHint",Vector3.zero,new Vector2(510,155),.0013f,view.transform);
                // This canvas is display-only: no raycaster or Meta surface is needed.
                foreach(var raycaster in canvas.GetComponents<UnityEngine.UI.GraphicRaycaster>())Destroy(raycaster);
                foreach(var surface in canvas.GetComponentsInChildren<Oculus.Interaction.PointableCanvas>())surface.gameObject.SetActive(false);
                var background=canvas.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.025f,.05f,.06f,.96f);background.raycastTarget=false;
                var label=view.Garden.Text(canvas.transform,"HintText","",Vector2.zero,new Vector2(475,130),26);label.richText=false;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=26;
                canvas.sortingOrder=60;hint=canvas.gameObject;
            }
            outline.enabled=true;hint.SetActive(true);
            var source=GetComponentInParent<Canvas>();var corners=new Vector3[4];
            var anchor=GetComponent<ExperienceMenuTarget>()!=null?(RectTransform)source.transform:(RectTransform)transform;
            anchor.GetWorldCorners(corners);
            var head=view.Garden.Locomotion.Head;
            var position=(corners[1]+corners[2])*.5f+source.transform.up*.16f-source.transform.forward*.015f;
            hint.transform.SetPositionAndRotation(position,Quaternion.LookRotation(position-head.position));
            string labelText=GetComponentInChildren<TMP_Text>().text;
            hint.GetComponentInChildren<TMP_Text>().text=Description(GetComponent<ExperienceMenuTarget>()?.Command,labelText)+"\nTrigger to select";
        }
        public void OnPointerExit(PointerEventData data){pointers.Remove(data.pointerId);if(pointers.Count==0)Hide();}
        private void Hide(){pointers.Clear();if(hint!=null)hint.SetActive(false);if(outline!=null)outline.enabled=false;if(active==this)active=null;}
        private void OnDisable()=>Hide();
        private void OnDestroy(){if(hint!=null)Destroy(hint);if(active==this)active=null;}
        public static string Description(string command,string label)
        {
            switch(command) {
                case "close":return "Close this menu and return to the scene. A / Tab also toggles it.";
                case "single":return "Choose a tree in the forest, then explore its branches yourself.";
                case "forest":return "View every tree in the selected model.";
                case "forest-info":return "Open the model summary in front of you. Close it with X on the panel.";
                case "profiles":return "Choose one profile and follow its route through every tree.";
                case "compare":return "Choose profiles A and B and compare their routes and results.";
                case "table":case "results-table":return "Show the whole model on the miniature table.";
                case "calculate-all":return "Skip the animation and calculate every tree immediately.";
                case "pause":return "Pause or resume the moving balls without losing progress.";
                case "position":case "recenter":return "Place the scene in front of you. B / R recenters it directly.";
                case "library":return "Open or import model JSON and profile files.";
                case "upload":return "Transfer JSON and download screenshots using your computer browser.";
                case "next-a":case "next-b":return "Cycle through the loaded profiles for this model.";
                case "start-profile":case "start-ab":return "Start the tour. A / Tab opens pause and calculation controls.";
                case "stop":return "Stop the tour and return to the forest.";
                case "restart":return "Restart the complete tour from the first tree.";
                case "speed":return "Cycle through playback speeds.";
                default:return label;
            }
        }
    }
}
