#if UNITY_EDITOR
using Elroi.DailyMissions;
using Elroi.DailyMissions.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DailyMissionSetup
{
    const string L="Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/";

    [MenuItem("Tools/Skate Runner/Setup Daily Mission Systems")]
    public static void Run()
    {
        GameObject page=Find("MissionPage"), background=Path("StartScreenCanvas/Background");
        if(!page||!background){Debug.LogError("Open SkateRunnerStartScreen before setup.");return;}
        TMP_Text sample=Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        TMP_FontAsset font=sample?sample.font:null;
        TMP_FontAsset mission32=MissionFont(32)??font;
        TMP_FontAsset mission40=MissionFont(40)??mission32;
        TMP_FontAsset mission54=MissionFont(54)??mission40;
        TMP_FontAsset mission72=MissionFont(72)??mission54;
        HomeUIBinder binder=Object.FindFirstObjectByType<HomeUIBinder>(FindObjectsInactive.Include);
        RewardGrantedPopup reward=RewardPopup(background.transform,font);
        Mission(page,reward,binder,mission32,mission40,mission54,mission72);
        FreeCash(background.transform,reward,binder,mission32,mission40,mission54);
        AddButtons();
        EditorSceneManager.MarkSceneDirty(page.scene);
        EditorSceneManager.SaveScene(page.scene);
        Debug.Log("Daily Mission systems scene setup complete.");
    }

    static void Mission(GameObject page,RewardGrantedPopup reward,HomeUIBinder binder,
        TMP_FontAsset font32,TMP_FontAsset font40,TMP_FontAsset font54,TMP_FontAsset font72)
    {
        Transform daily=page.transform.Find("Group_Left/DailyMissionScrollRect");
        Transform story=page.transform.Find("Group_Left/LevelScrollRect");
        Transform mafia=page.transform.Find("Group_Left/MafiaBoardScrollRect");
        Transform content=daily.Find("Content");
        for(int i=content.childCount-1;i>=0;i--)Object.DestroyImmediate(content.GetChild(i).gameObject);
        VerticalLayoutGroup v=Get<VerticalLayoutGroup>(content.gameObject);v.spacing=16;v.padding=new RectOffset(14,14,14,14);
        v.childControlHeight=true;v.childControlWidth=true;v.childForceExpandHeight=false;
        ContentSizeFitter contentFitter=Get<ContentSizeFitter>(content.gameObject);
        contentFitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect dailyScroll=Get<ScrollRect>(daily.gameObject);dailyScroll.vertical=true;dailyScroll.horizontal=false;
        RectTransform contentRect=(RectTransform)content;
        contentRect.anchorMin=new Vector2(0f,1f);contentRect.anchorMax=new Vector2(1f,1f);contentRect.pivot=new Vector2(.5f,1f);
        contentRect.anchoredPosition=Vector2.zero;contentRect.sizeDelta=new Vector2(0f,0f);dailyScroll.content=contentRect;
        Image contentImage=content.GetComponent<Image>();if(contentImage)contentImage.raycastTarget=false;
        Dirty(v);Dirty(contentFitter);Dirty(dailyScroll);Dirty(contentRect);if(contentImage)Dirty(contentImage);
        float viewportHeight=((RectTransform)daily).rect.height;
        float rowHeight=Mathf.Max(200f,(viewportHeight-v.padding.top-v.padding.bottom-v.spacing*3f)/4f);
        Sprite frame=S(L+"Frame/ListFrame02_Single_Navy.png"), green=S(L+"Button/Button01_145_Green.Png");
        Sprite bar=S(L+"Slider/Slider_Basic04_Bg.png"),fill=S(L+"Slider/Slider_Basic04_Fill_Green.png");
        Sprite cash=S("Assets/Prefabs/UI/Cash 1.png");
        Sprite gems=S(L+"UI_Etc/ResourceBar_Icon_Gem_Blue.png");
        Sprite pouch=S(L+"IconMisc/Icon_ImageIcon_GoldPouch.png");
        Sprite trophy=S(L+"IconMisc/Icon_ImageIcon_Trophy_l.png");
        Sprite chest=S(L+"Icon_ShopItems/ShopItem_SpecialChest_Blue.png");
        Sprite ad=S("Assets/Prefabs/Images/Icon_ImageIcon_Ad_00_l.png");
        DailyMissionRowUI[] rows={
            Row(content,"MissionRow_CollectCash",DailyMissionId.CollectCash,pouch,cash,frame,green,bar,fill,font40,font32,rowHeight),
            Row(content,"MissionRow_CollectGems",DailyMissionId.CollectGems,gems,gems,frame,green,bar,fill,font40,font32,rowHeight),
            Row(content,"MissionRow_CompleteLevels",DailyMissionId.CompleteLevels,trophy,chest,frame,green,bar,fill,font40,font32,rowHeight),
            Row(content,"MissionRow_WatchAds",DailyMissionId.WatchRewardedAds,ad,chest,frame,green,bar,fill,font40,font32,rowHeight)};
        DailyMissionPageController controller=Get<DailyMissionPageController>(page);
        Arr(controller,"rows",rows);Obj(controller,"rewardPopup",reward);Obj(controller,"homeUIBinder",binder);
        Obj(controller,"cashIcon",cash);Obj(controller,"gemIcon",gems);
        Definitions(controller,new[]{
            new object[]{DailyMissionId.CollectCash,"COLLECT CASH","Collect 5,000 Cash",5000,500,0,pouch,cash},
            new object[]{DailyMissionId.CollectGems,"COLLECT GEMS","Collect 30 Gems",30,0,5,gems,gems},
            new object[]{DailyMissionId.CompleteLevels,"CROSS 10 LEVELS","Complete 10 Levels",10,2000,5,trophy,chest},
            new object[]{DailyMissionId.WatchRewardedAds,"AD BREAK","Watch 5 Ads",5,2500,5,ad,chest}});

        Button db=In<Button>(page.transform,"DailyMissionButton"),sb=In<Button>(page.transform,"StoryMissionsButton"),mb=In<Button>(page.transform,"MafiaBoardButton");
        foreach(Button b in new[]{db,sb,mb})if(b){UIClickToggle t=b.GetComponent<UIClickToggle>();if(t)Object.DestroyImmediate(t);}
        MissionPageTabController tabs=Get<MissionPageTabController>(page);
        Obj(tabs,"dailyScrollRect",daily.gameObject);Obj(tabs,"storyScrollRect",story.gameObject);Obj(tabs,"mafiaScrollRect",mafia.gameObject);
        Obj(tabs,"dailyButton",db);Obj(tabs,"storyButton",sb);Obj(tabs,"mafiaButton",mb);
        Obj(tabs,"dailyText",db?db.GetComponentInChildren<TMP_Text>(true):null);
        Obj(tabs,"storyText",sb?sb.GetComponentInChildren<TMP_Text>(true):null);
        Obj(tabs,"mafiaText",mb?mb.GetComponentInChildren<TMP_Text>(true):null);
        foreach(Image image in page.GetComponentsInChildren<Image>(true))
            if(image.name=="BackGlow"){image.raycastTarget=false;Dirty(image);}
        Overlay(story,"ComingSoonOverlay_Story",font54);Overlay(mafia,"ComingSoonOverlay_Mafia",font54);
        ApplyMissionFonts(page.transform,font32,font40,font54,font72);
    }

    static DailyMissionRowUI Row(Transform parent,string name,DailyMissionId id,Sprite icon,Sprite reward,
        Sprite frame,Sprite green,Sprite bar,Sprite fill,TMP_FontAsset titleFont,TMP_FontAsset bodyFont,float rowHeight)
    {
        GameObject go=GO(name,parent);Get<LayoutElement>(go).preferredHeight=rowHeight;
        Image normal=Img(go.transform,"Background_Normal",frame,Color.white,R(0,0,1,1));
        Image complete=Img(go.transform,"Background_Completed",frame,Color.white,R(0,0,1,1));complete.gameObject.SetActive(false);
        Image mi=Img(go.transform,"Icon_Mission",icon,Color.white,R(.02f,.12f,.13f,.88f));
        TMP_Text title=Txt(go.transform,"Text_Title","TITLE",titleFont,30,R(.16f,.57f,.53f,.93f),TextAlignmentOptions.Left);
        TMP_Text desc=Txt(go.transform,"Text_Description","DESCRIPTION",bodyFont,21,R(.16f,.28f,.53f,.61f),TextAlignmentOptions.Left);
        Image bg=Img(go.transform,"Progress_Background",bar,new Color(.08f,.34f,.67f,1f),R(.16f,.07f,.54f,.27f));
        Image pf=Img(bg.transform,"Progress_Fill",fill,Color.white,R(0,0,1,1),7);pf.type=Image.Type.Filled;pf.fillMethod=Image.FillMethod.Horizontal;
        TMP_Text pt=Txt(bg.transform,"Text_Progress","0 / 0",bodyFont,17,R(0,0,1,1),TextAlignmentOptions.Center);
        pt.color=new Color(.015f,.07f,.09f,1f);
        TMP_Text timer=Txt(go.transform,"Text_Timer","ENDS IN 00:00:00",bodyFont,17,R(.53f,.07f,.74f,.32f),TextAlignmentOptions.Center);
        Image ri=Img(go.transform,"Icon_Reward",reward,Color.white,R(.56f,.36f,.66f,.88f));
        TMP_Text rt=Txt(go.transform,"Text_Reward","REWARD",bodyFont,18,R(.66f,.36f,.79f,.86f),TextAlignmentOptions.Center);
        Button claim=Btn(go.transform,"Button_Claim",green,R(.80f,.22f,.98f,.79f),titleFont,"CLAIM");
        TMP_Text ct=claim.GetComponentInChildren<TMP_Text>(true);
        GameObject claimed=Txt(go.transform,"ClaimedIndicator","CLAIMED",titleFont,22,R(.80f,.22f,.98f,.79f),TextAlignmentOptions.Center).gameObject;claimed.SetActive(false);
        DailyMissionRowUI row=go.AddComponent<DailyMissionRowUI>();row.Configure(id);
        Obj(row,"missionIcon",mi);Obj(row,"titleText",title);Obj(row,"descriptionText",desc);Obj(row,"progressFill",pf);Obj(row,"progressText",pt);
        Obj(row,"timerText",timer);Obj(row,"rewardIcon",ri);Obj(row,"rewardText",rt);Obj(row,"claimButton",claim);Obj(row,"claimButtonText",ct);
        Obj(row,"normalBackground",normal);Obj(row,"completedBackground",complete);Obj(row,"claimedIndicator",claimed);return row;
    }

    static TMP_FontAsset MissionFont(int size)
    {
        string path=$"Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_{size} SDF.asset";
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }

    static void ApplyMissionFonts(Transform root,TMP_FontAsset font32,TMP_FontAsset font40,TMP_FontAsset font54,TMP_FontAsset font72)
    {
        foreach(TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            float size=text.fontSize;
            text.font=size<=36?font32:size<=48?font40:size<=60?font54:font72;
            Dirty(text);
        }
    }

    static RewardGrantedPopup RewardPopup(Transform parent,TMP_FontAsset font)
    {
        Kill(parent.Find("RewardGrantedPopup"));
        GameObject root=GO("RewardGrantedPopup",parent);Apply(root.GetComponent<RectTransform>(),R(0,0,1,1));
        Image blocker=Img(root.transform,"ScreenBlocker",null,new Color(0,0,0,.75f),R(0,0,1,1));blocker.raycastTarget=true;
        Image panel=Img(root.transform,"DialogPanel",S(L+"Popup/Popup01_Single_Navy.png"),Color.white,R(.28f,.20f,.72f,.80f));
        TMP_Text title=Txt(panel.transform,"Text_Title","REWARD CLAIMED!",font,42,R(.08f,.73f,.92f,.94f),TextAlignmentOptions.Center);
        Image a=Img(panel.transform,"Icon_Primary",null,Color.white,R(.25f,.39f,.47f,.72f));
        Image b=Img(panel.transform,"Icon_Secondary",null,Color.white,R(.53f,.39f,.75f,.72f));
        TMP_Text amount=Txt(panel.transform,"Text_Reward","REWARD",font,31,R(.12f,.20f,.88f,.43f),TextAlignmentOptions.Center);
        Button ok=Btn(panel.transform,"Button_OK",S(L+"Button/Button01_145_Green.Png"),R(.35f,.03f,.65f,.20f),font,"OK");
        RewardGrantedPopup pop=root.AddComponent<RewardGrantedPopup>();Obj(pop,"titleText",title);Obj(pop,"rewardText",amount);Obj(pop,"primaryIcon",a);Obj(pop,"secondaryIcon",b);Obj(pop,"okButton",ok);
        root.SetActive(false);return pop;
    }

    static void FreeCash(Transform parent,RewardGrantedPopup reward,HomeUIBinder binder,
        TMP_FontAsset font32,TMP_FontAsset font40,TMP_FontAsset font54)
    {
        Kill(parent.Find("FreeCashDailyPopup"));GameObject root=GO("FreeCashDailyPopup",parent);Apply(root.GetComponent<RectTransform>(),R(0,0,1,1));
        Image blocker=Img(root.transform,"ScreenBlocker",null,new Color(0,0,0,.75f),R(0,0,1,1));blocker.raycastTarget=true;
        Image panel=Img(root.transform,"DialogPanel",S(L+"Popup/Popup01_Single_Navy.png"),Color.white,R(.20f,.04f,.80f,.96f));
        Txt(panel.transform,"Text_Title","DAILY FREE CASH",font54,46,R(.10f,.865f,.88f,.97f),TextAlignmentOptions.Center);
        TMP_Text timer=Txt(panel.transform,"Text_ResetTimer","RESETS IN 00:00:00",font32,20,R(.18f,.79f,.82f,.855f),TextAlignmentOptions.Center);
        Sprite frame=S(L+"Frame/ListFrame02_Single_Navy.png"),cash=S("Assets/Prefabs/UI/Cash 1.png"),gem=S(L+"UI_Etc/ResourceBar_Icon_Gem_Blue.png");
        Sprite ad=S("Assets/Prefabs/Images/Icon_ImageIcon_Ad_00_l.png"),green=S(L+"Button/Button01_145_Green.Png");
        FreeCashRewardRowUI[] rows=new FreeCashRewardRowUI[5];
        for(int i=0;i<5;i++)
        {
            float top=.765f-i*.139f;Image row=Img(panel.transform,"RewardRow_"+(i+1),frame,Color.white,R(.065f,top-.119f,.935f,top));
            CanvasGroup cg=row.gameObject.AddComponent<CanvasGroup>();Img(row.transform,"Icon_Cash",cash,Color.white,R(.025f,.10f,.145f,.90f));
            TMP_Text amount=Txt(row.transform,"Text_Reward","REWARD",font32,23,R(.16f,.08f,.56f,.92f),TextAlignmentOptions.Left);
            GameObject adGo=Img(row.transform,"Icon_Ad",ad,Color.white,R(.57f,.18f,.65f,.82f)).gameObject;
            Button claim=Btn(row.transform,"Button_Claim",green,R(.67f,.12f,.96f,.88f),font40,i==0?"FREE":"WATCH");
            GameObject locked=Txt(row.transform,"LockIndicator","LOCKED",font40,21,R(.67f,.12f,.96f,.88f),TextAlignmentOptions.Center).gameObject;
            GameObject claimed=Txt(row.transform,"ClaimedIndicator","CLAIMED",font40,21,R(.67f,.12f,.96f,.88f),TextAlignmentOptions.Center).gameObject;locked.SetActive(false);claimed.SetActive(false);
            FreeCashRewardRowUI ui=row.gameObject.AddComponent<FreeCashRewardRowUI>();Obj(ui,"claimButton",claim);Obj(ui,"buttonText",claim.GetComponentInChildren<TMP_Text>(true));Obj(ui,"rewardText",amount);
            Obj(ui,"lockIndicator",locked);Obj(ui,"claimedIndicator",claimed);Obj(ui,"adIcon",adGo);Obj(ui,"canvasGroup",cg);rows[i]=ui;
        }
        Image closeImage=Img(panel.transform,"Button_Close",S(L+"Button/Button01_145_Red.Png"),Color.white,R(.915f,.89f,.99f,.985f));
        closeImage.raycastTarget=true;Button close=closeImage.gameObject.AddComponent<Button>();close.targetGraphic=closeImage;
        Img(closeImage.transform,"Icon_Close",S(L+"IconMisc/Icon_PictoIcon_Close.png"),Color.white,R(.24f,.18f,.76f,.78f));
        FreeCashDailyPopup pop=root.AddComponent<FreeCashDailyPopup>();Arr(pop,"rows",rows);Obj(pop,"resetTimerText",timer);Obj(pop,"closeButton",close);Obj(pop,"rewardPopup",reward);Obj(pop,"homeUIBinder",binder);Obj(pop,"cashIcon",cash);Obj(pop,"gemIcon",gem);
        Button source=Find("FreeCashButton").GetComponent<Button>();source.onClick.RemoveAllListeners();UnityEditor.Events.UnityEventTools.AddPersistentListener(source.onClick,pop.Open);root.SetActive(false);
    }

    static void AddButtons()
    {
        HomeFullscreenPopupManager manager=Object.FindFirstObjectByType<HomeFullscreenPopupManager>(FindObjectsInactive.Include);
        GameObject shop=Find("ShopPage");Transform currency=Inside(shop.transform,"Text_CurrencyPack");
        UIClickToggle toggle=currency?currency.GetComponent<UIClickToggle>():null;
        foreach(string p in new[]{"StartScreenCanvas/Background/StatusBar_Group/Stats_Cash/Button_Add","StartScreenCanvas/Background/StatusBar_Group/Stats_Gem/Button_Add"})
        {
            GameObject go=Path(p);if(!go)continue;OpenCurrencyPackButton op=Get<OpenCurrencyPackButton>(go);Obj(op,"button",go.GetComponent<Button>());Obj(op,"popupManager",manager);Obj(op,"currencyPackToggle",toggle);
        }
    }

    static void Overlay(Transform parent,string name,TMP_FontAsset font)
    {
        Kill(parent.Find(name));Image image=Img(parent,name,S(L+"Popup/Popup_Slide01_Single_Navy.png"),new Color(.08f,.12f,.25f,.96f),R(0,0,1,1));
        image.raycastTarget=true;Txt(image.transform,"Text_ComingSoon","COMING SOON",font,58,R(0,0,1,1),TextAlignmentOptions.Center);image.transform.SetAsLastSibling();
    }
    static GameObject GO(string name,Transform parent){GameObject g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(parent,false);return g;}
    static Image Img(Transform p,string n,Sprite s,Color c,Rect r,float pad=0){GameObject g=GO(n,p);Apply(g.GetComponent<RectTransform>(),r,pad);Image i=g.AddComponent<Image>();i.sprite=s;i.color=c;i.raycastTarget=false;i.preserveAspect=n.Contains("Icon");if(s&&s.border.sqrMagnitude>0)i.type=Image.Type.Sliced;return i;}
    static TMP_Text Txt(Transform p,string n,string value,TMP_FontAsset f,float size,Rect r,TextAlignmentOptions align){GameObject g=GO(n,p);Apply(g.GetComponent<RectTransform>(),r);TextMeshProUGUI t=g.AddComponent<TextMeshProUGUI>();t.text=value;t.font=f;t.fontSize=size;t.fontSizeMin=12;t.fontSizeMax=size;t.enableAutoSizing=true;t.alignment=align;t.color=Color.white;t.raycastTarget=false;return t;}
    static Button Btn(Transform p,string n,Sprite s,Rect r,TMP_FontAsset f,string label){Image i=Img(p,n,s,Color.white,r);i.raycastTarget=true;Button b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;Txt(i.transform,"Text",label,f,25,R(0,0,1,1),TextAlignmentOptions.Center);return b;}
    static Rect R(float x1,float y1,float x2,float y2){return new Rect(x1,y1,x2-x1,y2-y1);}
    static void Apply(RectTransform t,Rect r,float pad=0){t.anchorMin=new Vector2(r.xMin,r.yMin);t.anchorMax=new Vector2(r.xMax,r.yMax);t.offsetMin=new Vector2(pad,pad);t.offsetMax=new Vector2(-pad,-pad);t.localScale=Vector3.one;}
    static Sprite S(string p)=>AssetDatabase.LoadAssetAtPath<Sprite>(p);
    static T Get<T>(GameObject g)where T:Component{return g.GetComponent<T>()??g.AddComponent<T>();}
    static void Obj(Object o,string name,Object value){SerializedObject s=new SerializedObject(o);SerializedProperty p=s.FindProperty(name);if(p!=null){p.objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}}
    static void Arr<T>(Object o,string name,T[] values)where T:Object{SerializedObject s=new SerializedObject(o);SerializedProperty p=s.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
    static void Definitions(Object o,object[][] values)
    {
        SerializedObject s=new SerializedObject(o);SerializedProperty p=s.FindProperty("definitions");p.arraySize=values.Length;
        for(int i=0;i<values.Length;i++){SerializedProperty e=p.GetArrayElementAtIndex(i);e.FindPropertyRelative("id").enumValueIndex=(int)(DailyMissionId)values[i][0];e.FindPropertyRelative("title").stringValue=(string)values[i][1];e.FindPropertyRelative("description").stringValue=(string)values[i][2];e.FindPropertyRelative("target").intValue=(int)values[i][3];e.FindPropertyRelative("rewardCash").intValue=(int)values[i][4];e.FindPropertyRelative("rewardGems").intValue=(int)values[i][5];e.FindPropertyRelative("missionIcon").objectReferenceValue=(Sprite)values[i][6];e.FindPropertyRelative("rewardIcon").objectReferenceValue=(Sprite)values[i][7];}
        s.ApplyModifiedPropertiesWithoutUndo();
    }
    static GameObject Find(string n){foreach(GameObject g in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g.name==n)return g;return null;}
    static GameObject Path(string p){string[] a=p.Split('/');GameObject g=Find(a[0]);Transform t=g?g.transform:null;for(int i=1;i<a.Length&&t;i++)t=t.Find(a[i]);return t?t.gameObject:null;}
    static Transform Inside(Transform root,string n){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    static T In<T>(Transform root,string n)where T:Component{Transform t=Inside(root,n);return t?t.GetComponent<T>():null;}
    static void Kill(Transform t){if(t)Object.DestroyImmediate(t.gameObject);}
    static void Dirty(Object o){if(!o)return;EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
}
#endif
