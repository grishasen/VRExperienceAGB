using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    public sealed partial class ExperienceDirector
    {
        private void BuildMenu()
        {
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            title.text="MODEL FOREST";subtitle.text=View.ActiveModelName+"\n"+View.Model.Trees.Count+" trees · "+View.AvailableProfiles.Profiles.Count+" profiles";
            switch(MenuPage)
            {
                case "home":
                    Row("Learn · first steps","learn",0,0,true);Row("Resume scene","close",0,1,true);
                    Row("Explore the forest","forest",1,0,true);Row("About this model","model-info",1,1,true);
                    Row("Play a prepared profile","profiles",2);Row("Compare profiles A / B","compare",3);
                    Row(ResultsVisible?"Table View · results":"Table View","table",4,0,true);Row("Models / profiles","library",4,1,true);break;
                case "library":
                    title.text="MODELS AND PROFILES";subtitle.text=Library.Status;
                    Row("Saved models","models",0);Row("Saved profile files","profile-files",1);
                    Row("Import model JSON","import-model",2);Row("Import profile JSON","import-profile",3);
                    Row("From computer / Wi-Fi","upload",4,0,true);Row("Formats / folders","library-help",4,1,true);break;
                case "upload":
                    title.text="UPLOAD FROM COMPUTER";subtitle.text=Library.Status;
                    var address=Garden.Text(content,"UploadAddress",Library.UploadAddress==null?"Enable a temporary upload session.\nUse a computer on the same Wi-Fi network.":Library.UploadAddress+"\nAccess code: "+Library.UploadCode+"\nOpen this address in your computer browser.\nChoose Model JSON or Profile JSON.\nKeep this app open; session expires after 15 minutes.",new Vector2(0,70),new Vector2(970,250),30);address.richText=false;
                    Row(Library.UploadAddress==null?"Start receiving":"Stop receiving",Library.UploadAddress==null?"upload-start":"upload-stop",3);
                    Row("Models","models",4,0,true);Row("Profile files","profile-files",4,1,true);break;
                case "library-help":
                    title.text="IMPORT JSON";subtitle.text="Files stay on this device. Main menu → Compare chooses A and B.";
                    var help=Garden.Text(content,"ImportHelp","Models: normalized v1 or supported AGB tree export.\nProfiles: normalized v1, or CDH JSON / JSONL execution records.\nCDH selection uses model rule/component name from the export filename.\n\nSaved folders:\n"+Library.Storage.ModelsPath+"\n"+Library.Storage.ProfilesPath+"\n\nImport from Downloads or copy files to these folders and Refresh.\nMaximum file size: 32 MiB. Invalid files keep the current scene.",new Vector2(0,-5),new Vector2(970,445),25);help.richText=false;break;
                case "models":case "profile-files":
                    bool profileFiles=MenuPage=="profile-files";
                    title.text=profileFiles?"PROFILE FILES":"MODEL FILES";subtitle.text=Library.Status;
                    Library.List(profileFiles);int pages=Math.Max(1,(Library.ListedFiles.Length+2)/3);libraryPage=Math.Min(libraryPage,pages-1);
                    for(int i=0;i<3;i++){int index=libraryPage*3+i;if(index<Library.ListedFiles.Length)Row(VRExperienceAGB.Import.ModelFileLibrary.DisplayName(Library.ListedFiles[index]).Replace("<","‹"),"library-open:"+index,i);}
                    if(Library.ListedFiles.Length==0)Garden.Text(content,"EmptyLibrary","No saved files. Import a JSON document.",new Vector2(0,85),new Vector2(950,90),28);
                    Row("Import JSON",profileFiles?"import-profile":"import-model",3,0,true);Row("Refresh files","library-refresh",3,1,true);
                    Row("Previous · "+(libraryPage+1)+" / "+pages,"library-prev",4,0,true);Row("Next","library-next",4,1,true);break;
                case "tree":
                    title.text="TREE "+(View.Ensemble.Index+1)+" / "+View.Model.Trees.Count;
                    Row("Previous decision","tree-back",0,0,true);Row("Restart tree","tree-restart",0,1,true);
                    Row("Back to forest","return-forest",1,0,true);Row("Table View","table",1,1,true);
                    Row("Current decision","focus-current",2,0,true);Row("Seated / standing","seated",2,1,true);
                    Row("Continue exploring","close",3,0,true);Row("Go to tree","tree-jump",3,1,true);Row("Learn · first steps","learn",4,0,true);Row("Explore branches manually","manual-tree",4,1,true);break;
                case "position":
                    title.text="POSITION YOUR SCENE";subtitle.text="Place the forest in front of you. Tracking remains active.";
                    Row("Place in front of me","recenter",0);
                    Row("Closer","closer",1,0,true);Row("Farther","farther",1,1,true);
                    Row("Move left","left",2,0,true);Row("Move right","right",2,1,true);
                    Row("Turn left","turn-left",3,0,true);Row("Turn right","turn-right",3,1,true);
                    Row("Menu lower","lower",4,0,true);Row("Menu higher","higher",4,1,true);break;
                case "profiles":case "compare":
                    title.text=MenuPage=="compare"?"COMPARE TWO PROFILES":"PLAY A PREPARED PROFILE";
                    subtitle.text=View.AvailableProfiles.Profiles.Count+" profiles · "+View.Model.Trees.Count+" trees · "+View.Model.Id;
                    if(View.AvailableProfiles.Profiles.Count==0){subtitle.text=CapabilityCopy+"\nImport compatible inputs to enable playback.";Row("Models / profiles","library",1);Row("Manual branches","single",2);break;}
                    Row("A: "+View.AvailableProfiles.Profiles[SelectedA].DisplayName,"next-a",0);
                    if(MenuPage=="compare")Row("B: "+View.AvailableProfiles.Profiles[SelectedB%View.AvailableProfiles.Profiles.Count].DisplayName,"next-b",1);
                    Row("Review selected profile values","values",2);
                    Row("Start · visit every tree",MenuPage=="compare"?"start-ab":"start-profile",3);Row("Pace: "+Pace,"pace",4,0,true);Row(MenuPage=="compare"?"What-if · edit B":"Terms and calculation",MenuPage=="compare"?"what-if":"glossary",4,1,true);break;
                case "values":BuildInputs();break;
                case "playback":
                    title.text="PROFILE PLAYBACK";subtitle.text=Playback==null?"No active run":"Tree "+(Playback.TreeIndex+1)+" / "+View.Model.Trees.Count;
                    Row(Playback?.Paused==true?"Resume":"Pause","pause",0,0,true);Row("Next step","step",0,1,true);
                    Row("Previous tree","previous-tree",1,0,true);Row("Speed: "+(Playback?.Speed??1).ToString("0.#")+"x","speed",1,1,true);
                    Row("Restart all trees","restart",2,0,true);Row("Calculate whole model","calculate-all",2,1,true);Row("Continue viewing","close",3,0,true);Row("Pace: "+Pace,"pace",3,1,true);Row("End playback · forest","stop",4);break;
                case "result":
                    title.text="COMPLETE MODEL RESULT";subtitle.text="All "+View.Model.Trees.Count+" trees evaluated · source scoring unverified";
                    var result=Garden.Text(content,"Result",ResultSummary(),new Vector2(0,100),new Vector2(960,220),32);result.richText=false;result.enableAutoSizing=true;result.fontSizeMin=24;result.fontSizeMax=32;
                    Row("Forest with results","results-forest",3,0,true);Row("Table with results","results-table",3,1,true);
                    Row("Calculation details","result-details",4,0,true);Row("What-if · edit B","what-if",4,1,true);break;
                case "tools":
                    title.text="TOOLS AND SETTINGS";
                    Row("Comfort and feedback","comfort",0,0,true);Row("Performance capture","performance",0,1,true);
                    Row("Search predictors","search",1,0,true);Row("Boosting trail","trail",1,1,true);
                    Row("Recorded reviews","review",2,0,true);Row("Help","help",2,1,true);
                    Row("Table display","table-tools",3,0,true);Row("Go to tree","tree-jump",3,1,true);
                    Row("About model","model-info",4,0,true);Row("Constellation guides","stars",4,1,true);break;
                case "table-tools":
                    title.text="TABLE DISPLAY";subtitle.text="Every tree remains in the model and calculation.";
                    Row("Smaller","table-smaller",0,0,true);Row("Larger","table-larger",0,1,true);
                    Row("Turn left","table-left",1,0,true);Row("Turn right","table-right",1,1,true);
                    Row("Labels: "+(Garden.M5.Diorama.ShowAllLabels?"all":"selected / hover"),"table-labels",2);
                    Row(Garden.M5.Diorama.GroupDescription+" · change group","table-group",3);Row("Inspect selected tree","table-inspect",4,0,true);Row("Go to tree","tree-jump",4,1,true);break;
                case "help":
                    title.text="HOW TO EXPLORE";subtitle.text="A / Tab: menu · B / R: center scene · X / F12: screenshot";
                    Garden.Text(content,"Help","Point and press the trigger to select. Hover for help.\nLeft stick: walk (toggle in Comfort). Right stick: snap turn.\nX (left) / F12: save a screenshot.\nDownload: Models / profiles → From computer / Wi-Fi.\n\nChoose a tree in the forest for manual branches.\nPlay profiles to see every tree in order.\nGold = A · Purple = B\nBlue result = positive · Red = negative\n\nPosition scene places the forest in front of you.\nClick a node label to pin its details.",new Vector2(0,0),new Vector2(970,440),27);break;
                default:BuildLearningMenu();break;
            }
        }
    }
}
