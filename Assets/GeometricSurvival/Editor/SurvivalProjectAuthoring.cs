using System;
using System.IO;
using GeometricSurvival;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only authoring utility. It writes normal Unity assets once, never runs on Play.
public static class SurvivalProjectAuthoring
{
    private const string Root = "Assets/GeometricSurvival";
    private static readonly Color Green = new Color(.3f, 1, .58f);
    private static readonly Color Dark = new Color(.025f, .035f, .065f);
    private static readonly Color Muted = new Color(.52f, .63f, .72f);
    private static Font font;
    private static Sprite square, triangle, circle, diamond;
    private static GameConfig config;
    private static EnemyActor[] enemies;
    private static ProjectileActor[] projectiles;
    private static PlayerController playerPrefab;
    private static GameObject exitPrefab;

    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring.");
        foreach (string folder in new[] { "Art", "Data", "Prefabs", "Scenes" }) Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        square = CreateSprite("Square", 4, 45);
        diamond = CreateSprite("Diamond", 4, 0);
        triangle = CreateSprite("Triangle", 3, 90);
        circle = CreateSprite("Circle", 64, 0);
        config = Asset<GameConfig>("MatchConfig");
        EditorUtility.SetDirty(config);
        CreatePrefabs();
        CreateMenu(false);
        CreateGame();
        CreateMenu(true);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(Root + "/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene(Root + "/Scenes/Game.unity", true),
            new EditorBuildSettingsScene(Root + "/Scenes/Results.unity", true)
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Root + "/Scenes/MainMenu.unity");
        Validate();
        Debug.Log("SURVIVAL_AUTHORING_OK: scenes, prefab instances, Canvas and data assets saved.");
    }

    private static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Root + "/Data/" + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        return asset;
    }

    private static Sprite CreateSprite(string name, int sides, float startAngle)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var vertices = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = (startAngle + i * 360f / sides) * Mathf.Deg2Rad;
            vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .47f;
        }
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2((x+.5f)/size-.5f, (y+.5f)/size-.5f);
            bool inside = true;
            for (int i = 0; i < sides; i++)
            {
                Vector2 a = vertices[i], b = vertices[(i+1)%sides];
                if ((b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x) < 0) { inside = false; break; }
            }
            texture.SetPixel(x,y,inside ? Color.white : Color.clear);
        }
        texture.Apply();
        string path = Root + "/Art/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject Shape(string name, Sprite sprite, Color color, Vector2 size, Transform parent = null, int order = 0)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
        return go;
    }
    private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(UnityEngine.Object target, string property, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(target);
        var field = serialized.FindProperty(property); field.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject SavePrefab(GameObject go, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(go); return prefab;
    }
    private static Rigidbody2D PhysicsBody(GameObject go, bool dynamic)
    {
        var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.gravityScale = 0; rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.useFullKinematicContacts = true; return rb;
    }
    private static void CreatePrefabs()
    {
        var player = new GameObject("Player");
        PhysicsBody(player, true);
        player.AddComponent<CircleCollider2D>().radius = .38f;
        var visual = Shape("Body - green diamond", diamond, Green, Vector2.one * 1.1f, player.transform, 4);
        var pivot = new GameObject("AimPivot").transform; pivot.SetParent(player.transform, false);
        var muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = new Vector3(.65f,0,0);
        Shape("Aim marker", circle, Green, Vector2.one * .13f, muzzle, 4);
        var pc = player.AddComponent<PlayerController>(); Set(pc,"config",config); Set(pc,"body",visual.GetComponent<SpriteRenderer>());
        var wc = player.AddComponent<WeaponController>(); Set(wc,"muzzle",muzzle); Set(wc,"aimPivot",pivot);

        enemies = new EnemyActor[3];
        string[] names = { "Square", "Triangle", "Circle" };
        Sprite[] sprites = { square, triangle, circle };
        Color[] colors = { new Color(1,.3f,.38f), new Color(1,.7f,.2f), new Color(.48f,.55f,1) };
        float[] health = { 110,55,25 }, damage = { 28,15,7 }, speed = { 1.15f,2.05f,3.25f }, radius = { .52f,.43f,.32f };
        for (int i = 0; i < 3; i++)
        {
            var def = Asset<EnemyDefinition>(names[i]); def.displayName = names[i]; def.health = health[i]; def.damage = damage[i]; def.speed = speed[i]; def.points = 30-i*10; EditorUtility.SetDirty(def);
            var go = new GameObject("Enemy_"+names[i]); go.layer = 8; PhysicsBody(go,false);
            var col = go.AddComponent<CircleCollider2D>(); col.radius = radius[i]; col.isTrigger = true;
            Shape("Body",sprites[i],colors[i],Vector2.one * radius[i] * 2,go.transform,2);
            var actor = go.AddComponent<EnemyActor>(); Set(actor,"definition",def);
            enemies[i] = SavePrefab(go,"Enemy_"+names[i]).GetComponent<EnemyActor>();
        }
        var effectGo = Shape("Explosion",circle,new Color(1,.65f,.2f,.5f),Vector2.one);
        var effect = effectGo.AddComponent<ExplosionEffect>(); Set(effect,"visual",effectGo.GetComponent<SpriteRenderer>());
        var explosion = SavePrefab(effectGo,"Explosion").GetComponent<ExplosionEffect>();
        projectiles = new ProjectileActor[3];
        string[] weaponNames = { "Explosive", "Piercing", "Heavy" };
        string[] labels = { "EXPLOSIVA", "PERFORANTE", "POTENTE" };
        float[] damages = { 45,18,55 }, cooldowns = { .65f,.18f,.24f };
        Color[] bulletColors = { new Color(1,.65f,.2f), Color.cyan, Green };
        for (int i=0;i<3;i++)
        {
            var def = Asset<WeaponDefinition>(weaponNames[i]); def.displayName=labels[i]; def.impact=(ImpactKind)i; def.damage=damages[i]; def.cooldown=cooldowns[i]; def.penetration=i==1?5:1; EditorUtility.SetDirty(def);
            var go = new GameObject("Bullet_"+weaponNames[i]); PhysicsBody(go,false);
            var col = go.AddComponent<CircleCollider2D>(); col.radius=.12f; col.isTrigger=true;
            Shape("Body",circle,bulletColors[i],Vector2.one*.25f,go.transform,5);
            var actor = go.AddComponent<ProjectileActor>(); Set(actor,"definition",def); Set(actor,"explosionPrefab",explosion);
            projectiles[i]=SavePrefab(go,"Bullet_"+weaponNames[i]).GetComponent<ProjectileActor>();
        }
        SetArray(wc,"projectilePrefabs",projectiles);
        playerPrefab=SavePrefab(player,"Player").GetComponent<PlayerController>();
        var exitGo = new GameObject("EscapeExit");
        var exitCollider=exitGo.AddComponent<CircleCollider2D>(); exitCollider.radius=1.1f; exitCollider.isTrigger=true;
        Shape("Portal",circle,new Color(.1f,.55f,.35f),Vector2.one*2.2f,exitGo.transform,1);
        Shape("Core",diamond,Green,Vector2.one*.9f,exitGo.transform,2);
        exitGo.AddComponent<EscapeExit>(); exitPrefab=SavePrefab(exitGo,"EscapeExit");
    }

    private static Camera Camera()
    {
        var go=new GameObject("Main Camera"); go.tag="MainCamera";
        var camera=go.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=12.5f;
        camera.backgroundColor=Dark; camera.clearFlags=CameraClearFlags.SolidColor;
        camera.transform.position=new Vector3(0,0,-10); go.AddComponent<AudioListener>(); return camera;
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
    }
    private static RectTransform Canvas()
    {
        var go=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,800); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        return go.GetComponent<RectTransform>();
    }
    private static Image Panel(string name,Transform parent,Vector2 point,Vector2 size,Color color)
    { var r=Rect(name,parent,point,size); var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return image; }
    private static Text Label(string name,Transform parent,string value,Vector2 point,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        var r=Rect(name,parent,point,size); var text=r.gameObject.AddComponent<Text>(); text.font=font; text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=alignment; text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; return text;
    }
    private static Button Button(string name,Transform parent,string value,Vector2 point,Vector2 size,bool primary=false)
    {
        var image=Panel(name,parent,point,size,primary ? new Color(.14f,.5f,.33f):new Color(.075f,.115f,.17f)); image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors; colors.highlightedColor=new Color(.72f,1,.85f); colors.pressedColor=new Color(.4f,.65f,.55f); button.colors=colors;
        Label("Label",image.transform,value,Vector2.zero,size,20,Color.white); return button;
    }
    private static void CreateMenu(bool results)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Camera(); var canvas=Canvas();
        Panel("Background",canvas,Vector2.zero,new Vector2(2400,1600),Dark);
        Panel("Left accent",canvas,new Vector2(-500,0),new Vector2(3,560),Green);
        var menu=new GameObject("Scene Navigation").AddComponent<SceneMenu>();
        Label("Eyebrow",canvas,"S O B R E V I V Í   A   L A   H O R D A",new Vector2(0,290),new Vector2(950,35),14,Muted);
        var title=Label("Title",canvas,results ? "FIN DE LA PARTIDA" : "GEOMETRIC\nSURVIVAL",new Vector2(0,180),new Vector2(950,160),results?46:58,Green); title.fontStyle=FontStyle.Bold;
        var best=Label("High Score",canvas,"RÉCORD PERSONAL   0000",new Vector2(0,65),new Vector2(800,45),20,Color.white); Set(menu,"bestLabel",best);
        if (results)
        {
            Set(menu,"resultTitle",title);
            Set(menu,"resultScore",Label("Score",canvas,"PUNTUACIÓN   0",new Vector2(0,0),new Vector2(800,45),26,Color.white));
            Set(menu,"resultKills",Label("Kills",canvas,"ENEMIGOS ELIMINADOS   0",new Vector2(0,-50),new Vector2(800,40),20,Muted));
        }
        else Label("Objective",canvas,"Sumá 500 puntos. Llegá a la salida en 15 segundos.\nCuando el tiempo termina, comienza la furia.",new Vector2(0,-15),new Vector2(850,70),20,Muted);
        var play=Button("Play Button",canvas,results?"VOLVER A JUGAR":"JUGAR",new Vector2(0,-130),new Vector2(360,60),true);
        UnityEventTools.AddPersistentListener(play.onClick,menu.Play);
        var secondary=Button(results?"Menu Button":"Quit Button",canvas,results?"VOLVER AL MENÚ":"SALIR",new Vector2(0,-205),new Vector2(360,54));
        if (results) UnityEventTools.AddPersistentListener(secondary.onClick,menu.MainMenu);
        else UnityEventTools.AddPersistentListener(secondary.onClick,menu.Quit);
        Label("Controls",canvas,"WASD / FLECHAS   Moverte     •     MOUSE + CLIC   Disparar\n1  Explosiva     2  Perforante     3  Potente",new Vector2(0,-310),new Vector2(1000,65),16,Muted);
        // Visible scene objects provide a small preview of the actual prefabs' shapes.
        Shape("Decor - Player",diamond,Green,Vector2.one*1.2f).transform.position=new Vector3(-12,0,0);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"/Scenes/"+(results?"Results":"MainMenu")+".unity");
    }
    private static void CreateGame()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=Camera(); camera.orthographicSize=15.5f; Set(camera.gameObject.AddComponent<ArenaCamera>(),"config",config);
        var arena=new GameObject("Arena").transform;
        Shape("Floor",square,new Color(.035f,.055f,.085f),new Vector2(52,31),arena,-10);
        for (int x=-16;x<=16;x+=2) Shape("Grid vertical "+x,square,new Color(.065f,.09f,.12f),new Vector2(.025f,30),arena,-9).transform.position=new Vector3(x,0,0);
        for (int y=-10;y<=10;y+=2) Shape("Grid horizontal "+y,square,new Color(.065f,.09f,.12f),new Vector2(52,.025f),arena,-9).transform.position=new Vector3(0,y,0);
        Shape("North boundary",square,Muted,new Vector2(51.2f,.09f),arena,-8).transform.position=new Vector3(0,10,0);
        Shape("South boundary",square,Muted,new Vector2(51.2f,.09f),arena,-8).transform.position=new Vector3(0,-10,0);
        Shape("West boundary",square,Muted,new Vector2(.09f,30),arena,-8).transform.position=new Vector3(-17,0,0);
        Shape("East boundary",square,Muted,new Vector2(.09f,30),arena,-8).transform.position=new Vector3(17,0,0);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject);
        var pc=player.GetComponent<PlayerController>(); var wc=player.GetComponent<WeaponController>();
        var match=new GameObject("Match Controller").AddComponent<MatchController>();
        var spawner=new GameObject("Enemy Spawner").AddComponent<EnemySpawner>();
        var exit=(GameObject)PrefabUtility.InstantiatePrefab(exitPrefab); exit.transform.position=new Vector3(14,0,0);
        Set(exit.GetComponent<EscapeExit>(),"match",match); exit.SetActive(false);
        Set(match,"config",config); Set(match,"player",pc); Set(match,"escapeExit",exit);
        Set(spawner,"config",config); Set(spawner,"player",pc); Set(spawner,"match",match); SetArray(spawner,"enemyPrefabs",enemies);
        Set(wc,"aimCamera",camera); Set(wc,"spawner",spawner);
        CreateHud(pc,wc,match);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"/Scenes/Game.unity");
    }
    private static void CreateHud(PlayerController player,WeaponController weapons,MatchController match)
    {
        var canvas=Canvas();
        var top=Panel("Top Bar",canvas,new Vector2(0,335),new Vector2(1280,130),new Color(.025f,.035f,.065f,.96f)).rectTransform;
        var score=Label("Score",top,"PUNTOS  0 / 500",new Vector2(-430,25),new Vector2(320,42),23,Green,TextAnchor.MiddleLeft);
        var kills=Label("Kills",top,"ELIMINADOS  0",new Vector2(430,25),new Vector2(320,42),23,Color.white,TextAnchor.MiddleRight);
        var status=Label("Escape countdown",top,"ALCANZÁ LA META PARA ABRIR LA SALIDA",new Vector2(100,-20),new Vector2(700,36),17,Muted);
        var healthRoot=Panel("Health Bar",top,new Vector2(-475,-15),new Vector2(230,10),new Color(.12f,.18f,.23f)).rectTransform;
        var fill=Panel("Fill",healthRoot,Vector2.zero,new Vector2(230,10),Green).rectTransform;
        fill.anchorMin=Vector2.zero; fill.anchorMax=Vector2.one; fill.offsetMin=fill.offsetMax=Vector2.zero;
        var slider=healthRoot.gameObject.AddComponent<Slider>(); slider.interactable=false; slider.transition=Selectable.Transition.None; slider.fillRect=fill; slider.minValue=0; slider.maxValue=1; slider.value=1;
        var health=Label("Health",top,"VIDA  100",new Vector2(-475,-43),new Vector2(230,28),14,Muted,TextAnchor.MiddleLeft);
        var bottom=Panel("Weapons Bar",canvas,new Vector2(0,-340),new Vector2(1280,120),new Color(.025f,.035f,.065f,.96f)).rectTransform;
        string[] names={"1   EXPLOSIVA","2   PERFORANTE","3   POTENTE"}; string[] descriptions={"Daño en área","Atraviesa hasta 5 enemigos","Z: deshacer cambio de arma"};
        var highlights=new UnityEngine.Object[3];
        for (int i=0;i<3;i++)
        {
            var button=Button("Weapon "+(i+1),bottom,names[i],new Vector2((i-1)*280,12),new Vector2(260,48));
            UnityEventTools.AddIntPersistentListener(button.onClick,weapons.Select,i); highlights[i]=button.GetComponent<Image>();
            Label("Description "+(i+1),bottom,descriptions[i],new Vector2((i-1)*280,-31),new Vector2(270,25),14,Muted);
        }
        var hud=canvas.gameObject.AddComponent<GameHud>(); Set(hud,"match",match); Set(hud,"player",player); Set(hud,"weapons",weapons);
        Set(hud,"scoreLabel",score); Set(hud,"killsLabel",kills); Set(hud,"statusLabel",status); Set(hud,"healthLabel",health); Set(hud,"healthBar",slider); SetArray(hud,"weaponHighlights",highlights);
    }

    public static void Validate()
    {
        string report="";
        foreach (string sceneName in new[] { "MainMenu","Game","Results" })
        {
            var scene=EditorSceneManager.OpenScene(Root+"/Scenes/"+sceneName+".unity");
            int objects=0,buttons=0;
            foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                objects++;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
                foreach (var component in t.GetComponents<MonoBehaviour>())
                {
                    if (!component) continue;
                    var serialized=new SerializedObject(component); var field=serialized.GetIterator();
                    while (field.NextVisible(true))
                        if (field.propertyType==SerializedPropertyType.ObjectReference && field.name!="m_Script" && field.objectReferenceInstanceIDValue!=0 && !field.objectReferenceValue)
                            throw new Exception("Broken reference: "+t.name+"."+field.name);
                }
                var button=t.GetComponent<Button>();
                if (button) { buttons++; if (button.onClick.GetPersistentEventCount()==0) throw new Exception("Unwired button: "+t.name); }
            }
            if (!UnityEngine.Object.FindFirstObjectByType<Canvas>()) throw new Exception("Missing Canvas: "+sceneName);
            if (sceneName=="Game")
            {
                if (!UnityEngine.Object.FindFirstObjectByType<PlayerController>()) throw new Exception("Missing player scene instance");
                if (!UnityEngine.Object.FindFirstObjectByType<MatchController>()) throw new Exception("Missing match controller");
            }
            report+=sceneName+": "+objects+" authored objects, "+buttons+" wired buttons\n";
        }
        foreach (string path in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"}))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(path));
            if (!prefab) throw new Exception("Invalid prefab");
        }
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/asset-validation.txt",report);
        EditorSceneManager.OpenScene(Root+"/Scenes/MainMenu.unity");
        Debug.Log("SURVIVAL_VALIDATION_OK\n"+report);
    }
}
