using System;
using System.IO;
using GeometricSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only integration checks against the saved scenes and prefab assets.
[InitializeOnLoad]
public static class SurvivalGameplayChecks
{
    private const string Key = "SurvivalGameplayChecks.Running";
    private const string Root = "Assets/GeometricSurvival/";
    private static int stage;
    private static double deadline;
    private static EnemyActor enemy;
    private static ProjectileActor heavy;
    private static string report = "";
    static SurvivalGameplayChecks() { EditorApplication.update += Tick; }

    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first.");
        EditorSceneManager.OpenScene(Root+"Scenes/Game.unity");
        SessionState.SetBool(Key,true);
        SessionState.SetInt(Key+".Best",RunSession.Best);
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        report += "PASS: " + name + "\n";
    }
    private static T Find<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindFirstObjectByType<T>(); }
    private static void Next(int next, double seconds = .2) { stage=next; deadline=EditorApplication.timeSinceStartup+seconds; }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<deadline) return;
        try
        {
            var match=Find<MatchController>();
            switch (stage)
            {
                case 0:
                    if (!match || match.Status==null) return;
                    Find<EnemySpawner>().enabled=false;
                    Check(Find<PlayerController>().Health==100,"Player starts with full health");
                    Check(!match.CanEscape && !match.Enraged,"Initial survival phase");
                    var circle=AssetDatabase.LoadAssetAtPath<EnemyActor>(Root+"Prefabs/Enemy_Circle.prefab");
                    enemy=UnityEngine.Object.Instantiate(circle);
                    enemy.Spawn(new Vector2(2,0),Find<PlayerController>(),match,e=>e.gameObject.SetActive(false));
                    enemy.enabled=false;
                    heavy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ProjectileActor>(Root+"Prefabs/Bullet_Heavy.prefab"));
                    heavy.Fire(new Vector2(.6f,0),Vector2.right,Find<EnemySpawner>(),p=>p.gameObject.SetActive(false));
                    Next(1,.35); break;
                case 1:
                    Check(!enemy.Alive && match.Score>=10 && match.Kills>=1,"Heavy projectile hits a collider and awards points");
                    var square=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyActor>(Root+"Prefabs/Enemy_Square.prefab"));
                    bool returned=false;
                    square.Spawn(new Vector2(5,0),Find<PlayerController>(),match,e=>{ returned=true; e.gameObject.SetActive(false); });
                    new DirectImpact().Apply(heavy,square,Vector2.zero);
                    Check(square.Alive,"Square survives one heavy hit");
                    square.TakeDamage(55);
                    Check(returned,"Dead enemy returns through its pool callback");
                    var factory=new EnemyFactory(new[] { AssetDatabase.LoadAssetAtPath<EnemyActor>(Root+"Prefabs/Enemy_Circle.prefab") });
                    bool factoryReleased=false;
                    var first=factory.CreateRandom(new Vector2(12,0),Find<PlayerController>(),match,e=>factoryReleased=true);
                    int firstId=first.SpawnId;
                    first.TakeDamage(1000);
                    var reused=factory.CreateRandom(new Vector2(12,0),Find<PlayerController>(),match,e=>{});
                    Check(factoryReleased && first==reused && reused.Alive && reused.SpawnId!=firstId,"Factory initializes enemies and reuses the pool safely");
                    reused.enabled=false;
                    var player=Find<PlayerController>(); player.TakeDamage(7); float hp=player.Health; player.TakeDamage(28);
                    Check(hp==93 && player.Health==93,"Contact damage grants temporary invulnerability");
                    var weapon=Find<WeaponController>(); weapon.Select(0); Check(weapon.Selected==0,"Explosive weapon selection"); weapon.Select(1); Check(weapon.Selected==1,"Piercing weapon selection");
                    weapon.UndoWeaponChange(); Check(weapon.Selected==0,"Command undo restores weapon memento");
                    weapon.UndoWeaponChange(); Check(weapon.Selected==2,"Command history restores the original weapon");
                    int beforeQueue=match.Score;
                    match.EnemyKilled(match.Config.targetScore);
                    Check(match.Score==beforeQueue,"Enemy kill is queued instead of applied immediately");
                    Next(2); break;
                case 2:
                    Check(match.CanEscape && match.EscapeRemaining>14 && match.EscapeRemaining<=15,"Goal opens exit with a 15 second countdown");
                    match.EscapeRemaining=.01f; Next(3); break;
                case 3:
                    Check(match.Enraged && !match.CanEscape && match.SpeedMultiplier>1 && match.DamageMultiplier>1,"Countdown closes exit and activates aggression");
                    Next(4,.75); break;
                case 4:
                    Find<PlayerController>().TakeDamage(1000); Next(5); break;
                case 5:
                    Check(SceneManager.GetActiveScene().name=="Results" && !RunSession.Escaped && RunSession.Kills>=2,"Defeat loads results with score and kills");
                    GameObject.Find("Menu Button").GetComponent<Button>().onClick.Invoke(); Next(6); break;
                case 6:
                    Check(SceneManager.GetActiveScene().name=="MainMenu","Results button returns to main menu");
                    GameObject.Find("Play Button").GetComponent<Button>().onClick.Invoke(); Next(7); break;
                case 7:
                    Check(SceneManager.GetActiveScene().name=="Game" && match.Score==0 && match.Kills==0,"Menu button starts a fresh run");
                    Find<EnemySpawner>().enabled=false; match.EnemyKilled(match.Config.targetScore); Next(8); break;
                case 8:
                    match.Escape(); Next(9); break;
                case 9:
                    Check(SceneManager.GetActiveScene().name=="Results" && RunSession.Escaped,"Escape ends the run with victory");
                    GameObject.Find("Play Button").GetComponent<Button>().onClick.Invoke(); Next(10); break;
                case 10:
                    Check(SceneManager.GetActiveScene().name=="Game" && match.Score==0,"Replay button resets the run");
                    SessionState.SetBool(Key,false); Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/gameplay-validation.txt",report);
                    RestoreBest(); Debug.Log("SURVIVAL_GAMEPLAY_CHECKS_OK\n"+report); EditorApplication.isPlaying=false; break;
            }
        }
        catch (Exception error)
        {
            SessionState.SetBool(Key,false); File.WriteAllText("Logs/gameplay-validation.txt",report+"FAIL: "+error);
            RestoreBest(); Debug.LogException(error); EditorApplication.isPlaying=false;
        }
    }
    private static void RestoreBest()
    { PlayerPrefs.SetInt("GeometricSurvival.HighScore",SessionState.GetInt(Key+".Best",0)); PlayerPrefs.Save(); }
}
