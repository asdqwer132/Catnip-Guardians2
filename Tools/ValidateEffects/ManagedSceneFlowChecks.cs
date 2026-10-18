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
        Check("requested scene ratios stay 60:40 and 15:85 at landscape portrait and ultrawide resolutions", () => {
            foreach (var pixels in new[] { new Vector2(1920,1080), new Vector2(1024,768), new Vector2(1080,1920), new Vector2(2560,1080), new Vector2(800,600) })
            {
                GameViewportLayout.Calculate(GameScreenLayout.SideBySide,.6f,true,0,.15f,out var b,out var i);
                Eq(b.width*pixels.x/pixels.x,.6f); Eq(i.width*pixels.x/pixels.x,.4f);
                Eq(b.yMax*pixels.y/pixels.y,.85f); Eq(i.yMax,b.yMax); Eq(b.xMax,i.xMin); Eq(i.xMax,1);
            }
        });
        Check("item scene uses an 80 by 80 percent game region and nonoverlapping outer panels", () => {
            SceneUILayout.ItemRegions(new Vector2(.8f,.8f),.5f,out var game,out var series,out var materials,out var recipe);
            Eq(game.width,.8f); Eq(game.height,.8f); Eq(series.yMin,game.yMax);
            Eq(materials.xMin,game.xMax); Eq(recipe.xMin,game.xMax); Eq(recipe.yMax,materials.yMin);
            Eq(materials.yMax,game.yMax); Eq(series.yMax,1); Eq(series.width,1);
            Eq(game.width*game.height+series.width*series.height+materials.width*materials.height+recipe.width*recipe.height,1);
        });
        Check("item region configuration rejects invalid shares without holes or negative rectangles", () => {
            foreach (float value in new[] {float.NaN,float.PositiveInfinity,float.NegativeInfinity,-5f,0f,.8f,10f})
            {
                SceneUILayout.ItemRegions(new Vector2(value,value),value,out var g,out var s,out var m,out var r);
                True(g.width>0 && g.height>0 && s.height>0 && m.width>0 && m.height>0 && r.height>0);
                Eq(g.width*g.height+s.width*s.height+m.width*m.height+r.width*r.height,1);
            }
        });
        Check("camera canvas scale uses its local pixel size and supports different aspect ratios", () => {
            Eq(SceneUILayout.CanvasScale(new Vector2(960,1147.5f),new Vector2(960,1147.5f),.5f),1);
            Eq(SceneUILayout.CanvasScale(new Vector2(480,573.75f),new Vector2(960,1147.5f),.5f),.5f);
            Eq(SceneUILayout.CanvasScale(new Vector2(960,573.75f),new Vector2(960,1147.5f),0),1);
            Eq(SceneUILayout.CanvasScale(new Vector2(960,573.75f),new Vector2(960,1147.5f),1),.5f);
        });
        Check("shared UI profile keeps the authored full-display width scale at different aspect ratios", () => {
            var reference = new Vector2(2400,600);
            foreach (var pixels in new[] {new Vector2(2400,1350), new Vector2(1920,1080), new Vector2(1024,768), new Vector2(1080,1920)})
                Eq(SceneUIScaleProfile.CalculateScale(pixels,reference,UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,0),pixels.x/2400f);
        });
        Check("shared UI profile supports balanced expand and shrink policies", () => {
            var pixels = new Vector2(1200,1200); var reference = new Vector2(2400,600);
            Eq(SceneUIScaleProfile.CalculateScale(pixels,reference,UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,.5f),1);
            Eq(SceneUIScaleProfile.CalculateScale(pixels,reference,UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand,0),.5f);
            Eq(SceneUIScaleProfile.CalculateScale(pixels,reference,UnityEngine.UI.CanvasScaler.ScreenMatchMode.Shrink,0),2);
        });
        Check("shared UI height policy keeps frame size when only display width changes", () => {
            var reference = new Vector2(2400,1440);
            foreach(float width in new[] {800f,1200f,1920f,2560f,3440f})
                Eq(SceneUIScaleProfile.CalculateScale(new Vector2(width,1080),reference,UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,1),.75f);
        });
        Check("shared UI profile invalid dimensions cannot create NaN or infinite scale", () => {
            foreach(float value in new[] {float.NaN,float.PositiveInfinity,float.NegativeInfinity,0f,-1f})
            {
                float scale = SceneUIScaleProfile.CalculateScale(new Vector2(value,value),new Vector2(value,value),UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,value);
                True(!float.IsNaN(scale) && !float.IsInfinity(scale) && scale>0);
            }
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
        Check("top navigation excludes both horizontal views without changing the battle share", () => {
            GameViewportLayout.Calculate(GameScreenLayout.SideBySide, .7f, true, .04f, .12f, out var b, out var i);
            Eq(b.width,.672f); Eq(i.width,.288f); Eq(b.yMax,.88f); Eq(i.yMax,.88f);
            Eq(b.yMin,0); Eq(i.yMin,0);
        });
        Check("top navigation fits reversed vertical views and their gap inside the remaining height", () => {
            GameViewportLayout.Calculate(GameScreenLayout.TopAndBottom, .6f, false, .1f, .2f, out var b, out var i);
            Eq(b.height,.432f); Eq(i.height,.288f); Eq(i.yMin-b.yMax,.08f); Eq(i.yMax,.8f);
        });
        Check("single camera modes keep the navigation space and disable the other view", () => {
            GameViewportLayout.Calculate(GameScreenLayout.BattleOnly, .5f, true, 0, .2f, out var b, out var i);
            Eq(b.width,1); Eq(b.height,.8f); Eq(i.width,0); Eq(i.height,0);
            GameViewportLayout.Calculate(GameScreenLayout.ItemOnly, .5f, true, 0, .2f, out b, out i);
            Eq(i.width,1); Eq(i.height,.8f); Eq(b.width,0); Eq(b.height,0);
        });
        Check("navigation inset sanitization leaves usable finite views and zero restores the old layout", () => {
            foreach (GameScreenLayout mode in Enum.GetValues(typeof(GameScreenLayout)))
            foreach (float inset in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f, .5f, 2f })
            {
                GameViewportLayout.Calculate(mode, .6f, false, .02f, inset, out var b, out var i);
                True(!float.IsNaN(b.height) && !float.IsNaN(i.height));
                True(b.height >= 0 && i.height >= 0 && b.yMax <= 1 && i.yMax <= 1);
                True(b.height >= .01f || i.height >= .01f);
            }
            GameViewportLayout.Calculate(GameScreenLayout.TopAndBottom, .6f, true, .02f, out var originalBattle, out var originalItem);
            GameViewportLayout.Calculate(GameScreenLayout.TopAndBottom, .6f, true, .02f, 0f, out var battle, out var item);
            Eq(originalBattle.yMin,battle.yMin); Eq(originalBattle.height,battle.height);
            Eq(originalItem.yMin,item.yMin); Eq(originalItem.height,item.height);
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
