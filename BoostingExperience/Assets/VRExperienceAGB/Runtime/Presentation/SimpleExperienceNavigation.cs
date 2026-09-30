using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    public enum NavigationPage { Home, TreeList, Forest, Tree, Diorama }

    /// <summary>Case selection and return destinations; never evaluates or edits model data.</summary>
    public sealed class SimpleExperienceNavigation
    {
        private readonly ForestGardenView garden;
        private OneTreeExperience View => garden.Experience;
        private readonly Canvas home, list, forest;
        private readonly TMP_Text pageLabel;
        private readonly List<GameObject> rows = new List<GameObject>();
        private readonly GameObject previous, next;
        private int listPage;
        public bool TreeMenuOpen { get; private set; }
        private bool pausedBeforeTreeMenu;
        public bool ExplicitlyPaused => TreeMenuOpen ? pausedBeforeTreeMenu : View.Session.State.Paused;
        public void ToggleExplicitPause()
        {
            if(TreeMenuOpen) pausedBeforeTreeMenu=!pausedBeforeTreeMenu;
            else View.Session.SetPaused(!View.Session.State.Paused);
        }
        private const int PageSize = 6;
        public NavigationPage Page { get; private set; } = NavigationPage.Home;
        public NavigationPage ReturnPage { get; private set; } = NavigationPage.TreeList;
        public long Revision { get; private set; }

        public SimpleExperienceNavigation(ForestGardenView garden, Transform root)
        {
            this.garden=garden;
            home=Panel("CaseMenu",new Vector2(1000,470),root);
            garden.Text(home.transform,"Title","EXPLORE THE MODEL",new Vector2(0,174),new Vector2(940,60),36);
            garden.Text(home.transform,"Model","Mobile Click-Through Rate · "+garden.PlotCount+" trees",new Vector2(0,109),new Vector2(940,55),26);
            garden.Button(home,"M3 · One tree",new Vector2(0,28),new Vector2(850,83),GardenCommand.SingleTree,0,32);
            garden.Button(home,"M4 · Whole forest",new Vector2(0,-76),new Vector2(850,83),GardenCommand.Forest,0,32);
            garden.Text(home.transform,"Hint","Point at a button and press the trigger.",new Vector2(0,-174),new Vector2(940,55),25);

            list=Panel("TreeSelection",new Vector2(1100,690),root);
            garden.Text(list.transform,"Title","M3 · CHOOSE A TREE",new Vector2(0,290),new Vector2(1040,60),34);
            for(int i=0;i<PageSize;i++)
                rows.Add(garden.Button(list,"",new Vector2(0,207-i*68),new Vector2(1000,59),GardenCommand.SelectTree,i,27));
            previous=garden.Button(list,"Previous",new Vector2(-370,-241),new Vector2(230,57),GardenCommand.ListPrevious,0,25);
            next=garden.Button(list,"Next",new Vector2(370,-241),new Vector2(230,57),GardenCommand.ListNext,0,25);
            pageLabel=garden.Text(list.transform,"Page","",new Vector2(0,-241),new Vector2(410,55),25);
            garden.Button(list,"Back to cases",new Vector2(0,-309),new Vector2(370,52),GardenCommand.Home,0,25);

            forest=Panel("ForestNavigation",new Vector2(600,210),root);
            garden.Button(forest,"Menu · A / Tab",new Vector2(0,60),new Vector2(350,64),GardenCommand.Home,0,27);
            garden.Text(forest.transform,"Hint","Point at a crown · Trigger to enter\nLeft stick: walk · Right stick: turn\nPath markers: teleport",new Vector2(0,-36),new Vector2(565,105),25);
            Refresh();Place();
        }
        private Canvas Panel(string name,Vector2 size,Transform root)
        {
            var canvas=garden.CanvasAt(name,Vector3.zero,size,.001f,root);
            var image=canvas.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.025f,.065f,.085f,.97f);image.raycastTarget=true;
            return canvas;
        }
        public void ShowHome() => Show(NavigationPage.Home);
        public void ShowDiorama() => Show(NavigationPage.Diorama);
        public void ShowForest() => Show(NavigationPage.Forest);
        public void InvalidatePointerPresses() { Revision++; }
        public void ReturnFromTree() => Show(ReturnPage);
        private void Show(NavigationPage page)
        {
            if(TreeMenuOpen) { TreeMenuOpen=false; View.Session.SetPaused(pausedBeforeTreeMenu); }
            Page=page;Revision++;
            View.ShowNavigationOverview();
            Refresh();Place();
        }
        public void OpenTree(int index)
        {
            if(index<0||index>=garden.PlotCount || (Page!=NavigationPage.Forest && Page!=NavigationPage.TreeList && Page!=NavigationPage.Diorama))return;
            ReturnPage=Page;
            View.SelectGardenTree(index);
            Page=NavigationPage.Tree;Revision++;
            View.EnterNavigationTree();
            Refresh();
        }
        public bool Activate(GardenCommand command,int index)
        {
            switch(command)
            {
                case GardenCommand.Home:case GardenCommand.FieldGuide:ShowHome();return true;
                case GardenCommand.SingleTree:Show(NavigationPage.TreeList);return true;
                case GardenCommand.Forest:Show(NavigationPage.Forest);return true;
                case GardenCommand.SelectTree:OpenTree(index);return true;
                case GardenCommand.ListPrevious:listPage=Mathf.Max(0,listPage-1);Revision++;Refresh();return true;
                case GardenCommand.ListNext:listPage=Mathf.Min((garden.PlotCount-1)/PageSize,listPage+1);Revision++;Refresh();return true;
                case GardenCommand.Path:
                    if(Page==NavigationPage.Forest && index>=0&&index<garden.PlotCount){garden.Locomotion.Teleport(garden.StandingPoint(index));Place();Revision++;}return true;
                case GardenCommand.Waypoint:
                    if(Page==NavigationPage.Forest && index>=0&&index*10<garden.PlotCount){garden.Locomotion.Teleport(new Vector3(0,0,4.5f+index*4.8f));Place();Revision++;}return true;
                default:return false;
            }
        }
        public void Refresh()
        {
            home.gameObject.SetActive(Page==NavigationPage.Home);
            list.gameObject.SetActive(Page==NavigationPage.TreeList);
            forest.gameObject.SetActive(Page==NavigationPage.Forest);
            for(int i=0;i<rows.Count;i++)
            {
                int index=listPage*PageSize+i;rows[i].SetActive(index<garden.PlotCount);
                if(index>=garden.PlotCount)continue;
                var metric=garden.Metrics[index];rows[i].GetComponent<GardenPointerTarget>().index=index;
                rows[i].GetComponentInChildren<TMP_Text>().text="Tree "+(index+1)+"    ·    Depth "+metric.MaximumDepth+"    ·    "+metric.LeafCount+" leaves";
            }
            previous.GetComponent<UnityEngine.UI.Button>().interactable=listPage>0;
            next.GetComponent<UnityEngine.UI.Button>().interactable=(listPage+1)*PageSize<garden.PlotCount;
            pageLabel.text=(listPage*PageSize+1)+"–"+Mathf.Min((listPage+1)*PageSize,garden.PlotCount)+" of "+garden.PlotCount;
            if(Page==NavigationPage.Tree)SimplifyTreeControls();
        }
        public void ToggleTreeMenu()
        {
            if(Page!=NavigationPage.Tree)return;
            if(!TreeMenuOpen) { pausedBeforeTreeMenu=View.Session.State.Paused; View.Session.SetPaused(true); }
            else View.Session.SetPaused(pausedBeforeTreeMenu);
            TreeMenuOpen=!TreeMenuOpen;Revision++;View.NameTooltip?.Hide();View.RefreshNavigationTree();
        }
        public void RestartTree()
        {
            View.Session.Restart();pausedBeforeTreeMenu=false;
            if(TreeMenuOpen)View.Session.SetPaused(true);
            View.RefreshNavigationTree();
        }
        private void SimplifyTreeControls()
        {
            // Lower the diagram instead of moving tracking. Keep the table's base at floor level in either posture.
            View.presentationRoot.localPosition=new Vector3(0,View.IsSeated?-.4f:0,0);
            var console=View.explanation.transform.parent;
            console.localPosition=new Vector3(0,.85f,3.25f);
            var stone=View.presentationRoot.Find("ConsoleStone");
            if(stone!=null) { stone.localPosition=new Vector3(0,View.IsSeated?.75f:.55f,3.93f);stone.localScale=new Vector3(3.3f,View.IsSeated?.618f:1.03f,.52f); }
            bool showBranches=!TreeMenuOpen&&!View.Session.State.AtLeaf;
            foreach(var name in new[]{"TrueChoiceStone","FalseChoiceStone"})
            {
                var choice=View.presentationRoot.Find(name);
                if(choice==null)continue;
                choice.gameObject.SetActive(showBranches);
                choice.localPosition=new Vector3(name=="TrueChoiceStone"?-.93f:.93f,1.1f,3.52f);
            }
            foreach(var control in View.controls)
            {
                bool branch=control.action==TreeAction.TrueBranch||control.action==TreeAction.FalseBranch;
                bool show=TreeMenuOpen ? control.action==TreeAction.Back||control.action==TreeAction.Restart||control.action==TreeAction.Seated||control.action==TreeAction.Overview||control.action==TreeAction.Menu : branch&&showBranches;
                control.gameObject.SetActive(show);
                if(!show)continue;
                control.GetComponent<UnityEngine.UI.Image>().color=branch?Color.clear:new Color(.045f,.17f,.21f,.98f);
                var rect=(RectTransform)control.transform;
                rect.sizeDelta=branch?new Vector2(210,76):new Vector2(280,54);
                control.label.enableAutoSizing=true;control.label.fontSizeMin=16;control.label.fontSizeMax=21;
                control.label.rectTransform.sizeDelta=rect.sizeDelta-new Vector2(16,8);
                switch(control.action)
                {
                    case TreeAction.TrueBranch:rect.anchoredPosition=new Vector2(-370,100);control.label.text="TRUE";break;
                    case TreeAction.FalseBranch:rect.anchoredPosition=new Vector2(370,100);control.label.text="FALSE";break;
                    case TreeAction.Back:rect.anchoredPosition=new Vector2(-160,25);control.label.text="Back";break;
                    case TreeAction.Menu:rect.anchoredPosition=new Vector2(0,-128);control.label.text="Close menu";break;
                    case TreeAction.Restart:rect.anchoredPosition=new Vector2(160,25);control.label.text="Restart tree";break;
                    case TreeAction.Seated:rect.anchoredPosition=new Vector2(-160,-52);control.label.text=View.IsSeated?"Use standing layout":"Use seated layout";break;
                    case TreeAction.Overview:rect.anchoredPosition=new Vector2(160,-52);control.label.text=ReturnPage==NavigationPage.Forest?"Back to forest":ReturnPage==NavigationPage.Diorama?"Back to tabletop":"Back to tree list";break;
                }
                if(!branch)control.GetComponent<UnityEngine.UI.Button>().interactable=true;
            }
            View.status.text="";View.feedback.gameObject.SetActive(false);
            if(View.menuBackdrop!=null)
            {
                View.menuBackdrop.SetActive(TreeMenuOpen);
                View.menuBackdrop.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                var rect=(RectTransform)View.menuBackdrop.transform;rect.anchoredPosition=new Vector2(0,-25);rect.sizeDelta=new Vector2(720,350);
            }
            View.explanation.gameObject.SetActive(true);
            View.explanation.rectTransform.anchoredPosition=new Vector2(0,TreeMenuOpen?110:-5);
            View.explanation.rectTransform.sizeDelta=new Vector2(740,72);
            View.explanation.enableAutoSizing=true;View.explanation.fontSizeMin=18;View.explanation.fontSizeMax=24;
            View.explanation.text=TreeMenuOpen ? "TREE MENU · "+(View.IsSeated?"Seated":"Standing") :
                View.Session.CurrentNode is VRExperienceAGB.Domain.SplitNode split ? View.Condition(split,true)+"?" : "Leaf reached";
            View.explanation.raycastTarget=!TreeMenuOpen;
            View.score.gameObject.SetActive(!TreeMenuOpen);
            View.score.rectTransform.anchoredPosition=new Vector2(0,-118);View.score.rectTransform.sizeDelta=new Vector2(740,50);View.score.fontSize=18;
            View.score.text="Tree "+(View.Ensemble.Index+1)+" / "+garden.PlotCount+" · "+(View.Session.State.AtLeaf?"Leaf score "+View.Session.State.Contribution.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture):"Point at a label for its full condition")+"\nA: Tree menu · No profile probability";
            View.NameTooltip?.UseCompactLayout();
        }
        public void Place()
        {
            Vector3 forward=garden.Locomotion.Head.forward;forward.y=0;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;forward.Normalize();
            foreach(var panel in new[]{home,list,forest})
            {
                panel.transform.position=garden.Locomotion.Head.position+forward*1.5f-Vector3.up*(panel==forest?.5f:.13f);
                panel.transform.rotation=Quaternion.LookRotation(forward);
                if(panel==forest)panel.transform.position-=panel.transform.right*.62f;
            }
        }
    }
}
