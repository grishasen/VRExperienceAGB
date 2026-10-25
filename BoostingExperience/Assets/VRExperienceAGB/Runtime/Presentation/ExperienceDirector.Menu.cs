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
            title.text="Model forest";subtitle.text=View.ActiveModelName+"\n"+View.Model.Trees.Count+" trees · "+View.AvailableProfiles.Profiles.Count+" profiles";
            menuDetails = View.ActiveModelName + "\nModel ID: " + View.Model.Id + "\n" + CapabilityCopy;
            switch(MenuPage)
            {
                case "home":
                    Row("Learn","learn",0,0,true);Row("Resume scene","close",0,1,true);
                    Row("Explore the forest","forest",1,0,true);Row("Search predictors","search",1,1,true);
                    Row("Play a prepared profile","profiles",2);Row("Compare profiles A / B","compare",3);
                    Row(ResultsVisible?"Table View · results":"Table View","table",4,0,true);Row("Models / profiles","library",4,1,true);break;
                case "library":
                    title.text="MODELS AND PROFILES";subtitle.text=Library.Status;
                    Row("Saved models","models",0);Row("Saved profile files","profile-files",1);
                    Row("Import model / ZIP","import-model",2);Row("Import profile JSON","import-profile",3);
                    Row("From computer / Wi-Fi","upload",4,0,true);Row("Import details","library-help",4,1,true);break;
                case "upload":
                    title.text="UPLOAD FROM COMPUTER";subtitle.text=Library.Status;
                    var address=Garden.Text(content,"UploadAddress",Library.UploadAddress==null?"Enable a temporary upload session.\nUse a computer on the same Wi-Fi network.":Library.UploadAddress+"\nAccess code: "+Library.UploadCode+"\nOpen this address in your computer browser.\nChoose Model + profiles ZIP, Model JSON or Profile JSON.\nKeep this app open; session expires after 15 minutes.",new Vector2(0,70),new Vector2(970,250),30);address.richText=false;
                    Row(Library.UploadAddress==null?"Start receiving":"Stop receiving",Library.UploadAddress==null?"upload-start":"upload-stop",3);
                    Row("Models","models",4,0,true);Row("Profile files","profile-files",4,1,true);break;
                case "library-help":
                    title.text="Import details";subtitle.text="Files stay on this device.";
                    readingText=ReadingBlock("Models: normalized v1 or supported AGB tree export.\nProfiles: normalized v1, or CDH JSON / JSONL execution records.\nZIP: exactly one model and its profiles. Package membership binds inputs to that model; other packages stay separate. Loose CDH files use the export model name.\n\nSaved folders:\n"+Library.Storage.ModelsPath+"\n"+Library.Storage.ProfilesPath+"\n"+Library.Storage.PackagesPath+"\n\nImport from Downloads or copy files to these folders and Refresh.\nMaximum upload: 32 MiB. ZIP expanded total: 64 MiB. Invalid files keep the current scene.",new Vector2(0,100),new Vector2(940,232));
                    readingText.overflowMode=TextOverflowModes.Page;readingText.ForceMeshUpdate();readingPage=Mathf.Clamp(readingPage,1,Math.Max(1,readingText.textInfo.pageCount));readingText.pageToDisplay=readingPage;
                    subtitle.text="Page "+readingPage+" / "+Math.Max(1,readingText.textInfo.pageCount);
                    Row("Previous","reading-prev",3,0,true);Row("Next","reading-next",3,1,true);Row("Back","library",4);break;
                case "models":case "profile-files":
                    bool profileFiles=MenuPage=="profile-files";
                    title.text=profileFiles?"PROFILE FILES":"MODEL FILES";subtitle.text=Library.Status;
                    Library.List(profileFiles);int pages=Math.Max(1,(Library.ListedFiles.Length+2)/3);libraryPage=Math.Min(libraryPage,pages-1);
                    for(int i=0;i<3;i++){int index=libraryPage*3+i;if(index<Library.ListedFiles.Length)Row(Library.Storage.Label(Library.ListedFiles[index]).Replace("<","‹"),"library-open:"+index,i);}
                    if(Library.ListedFiles.Length==0)Garden.Text(content,"EmptyLibrary","No saved files. Import a ZIP package or JSON document.",new Vector2(0,85),new Vector2(950,90),28);
                    Row("Import JSON / ZIP",profileFiles?"import-profile":"import-model",3,0,true);Row("Refresh files","library-refresh",3,1,true);
                    Row("Previous · "+(libraryPage+1)+" / "+pages,"library-prev",4,0,true);Row("Next","library-next",4,1,true);break;
                case "tree":
                    title.text="TREE "+(View.Ensemble.Index+1)+" / "+View.Model.Trees.Count;
                    Row("Previous decision","tree-back",0,0,true);Row("Restart tree","tree-restart",0,1,true);
                    Row("Back to forest","return-forest",1,0,true);Row("Table View","table",1,1,true);
                    Row("Current decision","focus-current",2,0,true);Row("Seated / standing","seated",2,1,true);
                    Row("Continue exploring","close",3,0,true);Row("Go to tree","tree-jump",3,1,true);Row("Learn","learn",4,0,true);Row("Explore branches manually","manual-tree",4,1,true);break;
                case "position":
                    title.text="POSITION YOUR SCENE";subtitle.text="Adjust the scene placement.";
                    Row("Place in front of me","recenter",0);
                    Row("Closer","closer",1,0,true);Row("Farther","farther",1,1,true);
                    Row("Move left","left",2,0,true);Row("Move right","right",2,1,true);
                    Row("Turn left","turn-left",3,0,true);Row("Turn right","turn-right",3,1,true);
                    Row("Menu lower","lower",4,0,true);Row("Menu higher","higher",4,1,true);break;
                case "profiles":case "compare":
                    title.text=MenuPage=="compare"?"COMPARE TWO PROFILES":"PLAY A PREPARED PROFILE";
                    subtitle.text=View.AvailableProfiles.Profiles.Count+" profiles · "+View.Model.Trees.Count+" trees";
                    if(View.AvailableProfiles.Profiles.Count==0){subtitle.text=CapabilityCopy+"\nImport compatible inputs to enable playback.";Row("Models / profiles","library",1);Row("Manual branches","single",2);break;}
                    Row("A: "+View.AvailableProfiles.Profiles[SelectedA].DisplayName,"next-a",0);
                    if(MenuPage=="compare")Row("B: "+View.AvailableProfiles.Profiles[SelectedB%View.AvailableProfiles.Profiles.Count].DisplayName,"next-b",1);
                    Row("Review inputs","values",2);
                    Row("Start playback",MenuPage=="compare"?"start-ab":"start-profile",3);Row("Pace: "+Pace,"pace",4,0,true);Row(MenuPage=="compare"?"What-if · edit B":"Details",MenuPage=="compare"?"what-if":"glossary",4,1,true);break;
                case "values":BuildInputs();break;
                case "playback":
                    title.text="PROFILE PLAYBACK";subtitle.text=Playback==null?"No active run":"Tree "+(Playback.TreeIndex+1)+" / "+View.Model.Trees.Count;
                    menuDetails=PlaybackDetailsCopy()+"\n\n"+menuDetails;
                    Row(Playback?.Paused==true?"Resume":"Pause","pause",0,0,true);Row("Next step","step",0,1,true);
                    Row("Previous tree","previous-tree",1,0,true);Row("Speed: "+(Playback?.Speed??1).ToString("0.#")+"x","speed",1,1,true);
                    Row("Restart all trees","restart",2,0,true);Row("Calculate whole model","calculate-all",2,1,true);Row("Continue viewing","close",3,0,true);Row("Pace: "+Pace,"pace",3,1,true);Row("End playback · forest","stop",4);break;
                case "result":
                    title.text="COMPLETE MODEL RESULT";subtitle.text="All "+View.Model.Trees.Count+" trees evaluated · source scoring unverified";
                    var result=Garden.Text(content,"Result",ResultSummary(),new Vector2(0,100),new Vector2(960,220),32);result.richText=false;result.enableAutoSizing=false;result.fontSize=28;
                    Row("Forest with results","results-forest",3,0,true);Row("Table with results","results-table",3,1,true);
                    Row("Details","result-details",4,0,true);Row("What-if · edit B","what-if",4,1,true);break;
                case "tools":
                    title.text="TOOLS AND SETTINGS";
                    Row("Comfort and feedback","comfort",0,0,true);Row("Performance capture","performance",0,1,true);
                    Row("Boosting trail","trail",1);
                    Row("Recorded reviews","review",2,0,true);Row("Help","help",2,1,true);
                    Row("Table display","table-tools",3,0,true);Row("Go to tree","tree-jump",3,1,true);
                    Row("About model","model-info",4,0,true);Row("Constellation guides","stars",4,1,true);break;
                case "table-tools":
                    title.text="TABLE DISPLAY";subtitle.text="Left stick: sideways to rotate · forward/back to move closer/farther.";
                    Row("Smaller","table-smaller",0,0,true);Row("Larger","table-larger",0,1,true);
                    Row("Turn left","table-left",1,0,true);Row("Turn right","table-right",1,1,true);
                    Row("Labels: "+(Garden.M5.Diorama.ShowAllLabels?"all":"selected / hover"),"table-labels",2);
                    Row(Garden.M5.Diorama.GroupDescription+" · change group","table-group",3);Row("Inspect selected tree","table-inspect",4,0,true);Row("Go to tree","tree-jump",4,1,true);break;
                case "help":
                    if (View.GetComponent<DesktopExperienceInput>()?.enabled == true) {
                        title.text="HOW TO EXPLORE";subtitle.text="Esc: menu · R: center scene · F12: screenshot";
                        string controls="Click: select · Right-drag: look\nForest: W A S D to walk\nTable: A / D rotate · W / S distance · Q / E size\n\nTree → Explore tree or Details\nNode label → summary · Click to pin\n\nGold: profile A · Purple: profile B";
                        menuDetails += "\n\n"+controls+"\n\nModels / profiles: import a JSON file or ZIP package.\nF1: show or hide keyboard hints.\nCmd+Q: quit the application.";
                        ReadingBlock(controls,new Vector2(0,35),new Vector2(940,350));break;
                    }
                    title.text="HOW TO EXPLORE";subtitle.text="A / Tab: menu · B / R: center scene · X / F12: screenshot";
                    menuDetails += "\n\n" + "Point and press the trigger to select.\nLeft stick: walk in the forest; rotate and move closer/farther in Table View. Right stick: snap turn.\nX (left) / F12: save a screenshot.\nDownload: Models / profiles → From computer / Wi-Fi.\n\nClick a tree, then choose Explore tree or Details.\nPlay profiles to see every tree in order.\nGold = A · Purple = B\nBlue result = positive · Red = negative\n\nPoint at a node label for a summary; click to pin.\nModel details opens its full condition and evidence.";
                    ReadingBlock("Point + trigger: select\nForest: left stick to walk · Right stick to turn\nTable: left stick to rotate / move closer\n\nTree → Explore tree or Details\nNode label → summary · Click to pin\n\nGold: profile A · Purple: profile B",new Vector2(0,35),new Vector2(940,350));break;
                default:BuildLearningMenu();break;
            }
            foreach(var text in menu.GetComponentsInChildren<TMP_Text>()) TreeCardTypography.SetFont(text,text==title);
            menu.transform.Find("page-details").gameObject.SetActive(MenuPage!="menu-details");
        }
    }
}
