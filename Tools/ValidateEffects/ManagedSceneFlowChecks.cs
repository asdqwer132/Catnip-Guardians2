using System;
using UnityEngine;

public static partial class Runner
{
    private static void RunSceneFlowChecks()
    {
        Check("legacy aliases preserve independent battle and item destinations", () => {
            True(GameSceneRoutes.Resolve("GameScene") == GameSceneRoutes.General);
            True(GameSceneRoutes.Resolve("GameBattleScene") == GameSceneRoutes.Battle);
            True(GameSceneRoutes.Resolve("Assets/Scenes/GameItemScene.unity") == GameSceneRoutes.Item);
        });
        Check("default editor play uses only the selected scene", () => {
            foreach (string name in new[] { GameSceneRoutes.Battle, GameSceneRoutes.Item, GameSceneRoutes.Main, GameSceneRoutes.Story, GameSceneRoutes.Upgrade, GameSceneRoutes.General, GameSceneRoutes.Core })
            { True(SceneStartupPolicy.EditorEntry(name, ScenePlayMode.CurrentSceneOnly) == name); True(SceneStartupPolicy.EditorCoreTarget(name, ScenePlayMode.CurrentSceneOnly) == ""); }
        });
        Check("core-assisted tests return to the exact scene without aggregation", () => {
            foreach (string name in new[] { GameSceneRoutes.Battle, GameSceneRoutes.Item, GameSceneRoutes.Upgrade })
            {
                True(SceneStartupPolicy.EditorEntry(name, ScenePlayMode.CurrentSceneWithCore) == GameSceneRoutes.Core);
                string target=SceneStartupPolicy.EditorCoreTarget(name, ScenePlayMode.CurrentSceneWithCore);
                True(target == name); True(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General,true,true,true,target)==name);
            }
        });
        Check("explicit core start uses the configured destination", () => {
            True(SceneStartupPolicy.EditorCoreTarget(GameSceneRoutes.Battle,ScenePlayMode.CoreStartupScene)=="");
            foreach(string destination in new[] {GameSceneRoutes.General,GameSceneRoutes.Main,GameSceneRoutes.Battle,GameSceneRoutes.Item})
                True(SceneStartupPolicy.CoreDestination(destination,true,true,true,"")==destination);
        });
        Check("core added to another active scene does not redirect by default", () => {
            True(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General,true,false,true,"")=="");
            True(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General,true,false,false,"")==GameSceneRoutes.General);
        });
        Check("disabled core startup remains idle but explicit editor tests still load", () => {
            True(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General,false,true,true,"")=="");
            True(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General,false,true,true,GameSceneRoutes.Battle)==GameSceneRoutes.Battle);
        });
        Check("core startup rejects recursive and empty destinations", () => {
            True(SceneStartupPolicy.CoreDestination(GameSceneRoutes.Core,true,true,true,"")=="");
            True(SceneStartupPolicy.CoreDestination(null,true,true,true,"")=="");
            True(SceneStartupPolicy.EditorCoreTarget(GameSceneRoutes.Core,ScenePlayMode.CurrentSceneWithCore)=="");
        });
        Check("normal scene links and empty input resolve safely", () => {
            True(GameSceneRoutes.Resolve(" Main ") == GameSceneRoutes.Main);
            True(GameSceneRoutes.Resolve("Assets/Scenes/UpgradeScene.unity") == GameSceneRoutes.Upgrade);
            True(GameSceneRoutes.Resolve(null) == ""); True(GameSceneRoutes.Resolve("  ") == "");
            True(GameSceneRoutes.Resolve("CoreScene") == GameSceneRoutes.Core);
        });
        Check("default split occupies exactly two halves", () => {
            GameViewportLayout.Calculate(GameScreenLayout.SideBySide, .5f, true, 0, out var b, out var i);
            Eq(b.xMin,0); Eq(b.width,.5f); Eq(i.xMin,.5f); Eq(i.width,.5f); Eq(b.height,1); Eq(i.height,1);
        });
        Check("screen gap preserves requested usable battle share", () => {
            GameViewportLayout.Calculate(GameScreenLayout.SideBySide, .7f, true, .04f, out var b, out var i);
            Eq(b.width,.672f); Eq(i.width,.288f); Eq(i.xMin-b.xMax,.04f); Eq(i.xMax,1);
        });
        Check("reversing sides preserves battle share", () => {
            GameViewportLayout.Calculate(GameScreenLayout.SideBySide, .7f, false, .02f, out var b, out var i);
            Eq(i.xMin,0); Eq(b.xMax,1); Eq(b.width,.686f); Eq(b.xMin-i.xMax,.02f);
        });
        Check("vertical first view starts at the top", () => {
            GameViewportLayout.Calculate(GameScreenLayout.TopAndBottom, .6f, true, 0, out var b, out var i);
            Eq(b.yMin,.4f); Eq(b.yMax,1); Eq(i.yMin,0); Eq(i.yMax,b.yMin);
        });
        Check("vertical reversed view preserves battle area", () => {
            GameViewportLayout.Calculate(GameScreenLayout.TopAndBottom, .6f, false, 0, out var b, out var i);
            Eq(b.height,.6f); Eq(b.yMin,0); Eq(i.yMin,b.yMax); Eq(i.yMax,1);
        });
        Check("single views never leave an overlapping visible camera", () => {
            GameViewportLayout.Calculate(GameScreenLayout.BattleOnly, .5f, true, 0, out var b, out var i);
            Eq(b.width,1); Eq(b.height,1); Eq(i.width,0);
            GameViewportLayout.Calculate(GameScreenLayout.ItemOnly, .5f, true, 0, out b, out i);
            Eq(i.width,1); Eq(i.height,1); Eq(b.width,0);
        });
        Check("invalid serialized split values remain finite and visible", () => {
            foreach (float ratio in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity, -10f, 10f })
            foreach (float gap in new[] { float.NaN, -1f, 1f })
            {
                GameViewportLayout.Calculate(GameScreenLayout.SideBySide, ratio, false, gap, out var b, out var i);
                True(!float.IsNaN(b.width) && b.width > 0 && i.width > 0);
                True(b.xMin >= 0 && i.xMin >= 0 && b.xMax <= 1.0001f && i.xMax <= 1.0001f);
            }
        });
    }
}
