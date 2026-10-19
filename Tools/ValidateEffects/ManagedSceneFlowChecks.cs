using System;
using UnityEngine;

public static partial class Runner
{
    private static void RunSceneFlowChecks()
    {
        Check("scene carrier resolves independently declared keys by name and type", () => {
            SceneDataCarrier.Clear();
            var sender=new SceneDataKey<int>(" shared-count ");
            var receiver=new SceneDataKey<int>("shared-count");
            SceneDataCarrier.Put(sender,7);
            True(SceneDataCarrier.TryGet(receiver,out int value) && value==7);
            True(SceneDataCarrier.TryGet(receiver,out value) && value==7);
            True(SceneDataCarrier.Count==1); SceneDataCarrier.Clear();
        });
        Check("scene carrier isolates keys with equal names but different data types", () => {
            SceneDataCarrier.Clear();
            var number=new SceneDataKey<int>("payload"); var text=new SceneDataKey<string>("payload");
            SceneDataCarrier.Put(number,3); SceneDataCarrier.Put(text,"hello");
            True(SceneDataCarrier.TryGet(number,out int a) && a==3);
            True(SceneDataCarrier.TryGet(text,out string b) && b=="hello");
            True(!SceneDataCarrier.TryGet(new SceneDataKey<float>("payload"),out float missing) && missing==0);
            True(SceneDataCarrier.Count==2); SceneDataCarrier.Clear();
        });
        Check("scene carrier overwrites one key and take consumes it exactly once", () => {
            SceneDataCarrier.Clear(); var key=new SceneDataKey<int>("once");
            SceneDataCarrier.Put(key,1); SceneDataCarrier.Put(key,9);
            True(SceneDataCarrier.Count==1);
            True(SceneDataCarrier.TryTake(key,out int value) && value==9);
            True(!SceneDataCarrier.TryTake(key,out value) && value==0);
            True(!SceneDataCarrier.TryGet(key,out value));
        });
        Check("scene carrier preserves explicit null and reference payloads without cloning", () => {
            SceneDataCarrier.Clear(); var key=new SceneDataKey<System.Collections.Generic.List<int>>("items");
            var items=new System.Collections.Generic.List<int> {1}; SceneDataCarrier.Put(key,items);
            True(SceneDataCarrier.TryGet(key,out var received) && ReferenceEquals(received,items));
            received.Add(2); True(items.Count==2);
            SceneDataCarrier.Put(key,null);
            True(SceneDataCarrier.TryGet(key,out received) && received==null);
            True(SceneDataCarrier.TryTake(key,out received) && received==null);
            True(!SceneDataCarrier.TryGet(key,out received));
        });
        Check("scene carrier remove is scoped and clear removes all remaining payloads", () => {
            SceneDataCarrier.Clear(); var first=new SceneDataKey<int>("first"); var second=new SceneDataKey<int>("second");
            SceneDataCarrier.Put(first,1); SceneDataCarrier.Put(second,2);
            True(SceneDataCarrier.Remove(first) && !SceneDataCarrier.Remove(first));
            True(SceneDataCarrier.TryGet(second,out int value) && value==2);
            SceneDataCarrier.Clear(); True(SceneDataCarrier.Count==0 && !SceneDataCarrier.TryGet(second,out value));
        });
        Check("scene carrier rejects invalid key names and null key objects", () => {
            bool rejected=false;
            try { new SceneDataKey<int>(" "); } catch(ArgumentException) { rejected=true; }
            True(rejected); rejected=false;
            try { SceneDataCarrier.Put<int>(null,1); } catch(ArgumentNullException) { rejected=true; }
            True(rejected); True(SceneDataCarrier.Count==0);
        });
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
        Check("shared screen background cover and contain preserve image aspect", () => {
            var image = new Vector2(1600,900);
            foreach (var screen in new[] {new Vector2(1920,1080),new Vector2(1080,1920),new Vector2(3440,1440)})
            {
                var cover = SceneScreenPresentationMath.BackgroundUvScale(image,screen,ScreenBackgroundFit.Cover);
                var contain = SceneScreenPresentationMath.BackgroundUvScale(image,screen,ScreenBackgroundFit.Contain);
                True(cover.x<=1 && cover.y<=1 && (cover.x==1 || cover.y==1));
                True(contain.x>=1 && contain.y>=1 && (contain.x==1 || contain.y==1));
                Eq(cover.x*image.x/(cover.y*image.y),screen.x/screen.y);
                Eq(contain.x*image.x/(contain.y*image.y),screen.x/screen.y);
            }
            var stretch = SceneScreenPresentationMath.BackgroundUvScale(image,new Vector2(800,1200),ScreenBackgroundFit.Stretch);
            Eq(stretch.x,1); Eq(stretch.y,1);
        });
        Check("rounded battle input excludes clipped corners and follows output resize", () => {
            foreach (float screenHeight in new[] {720f,1440f,2160f})
            {
                var view = new Rect(0,0,screenHeight*.6f,screenHeight*.85f);
                float radius = SceneScreenPresentationMath.CornerRadius(48,screenHeight,1440,view.size);
                Eq(radius,48*screenHeight/1440);
                True(SceneScreenPresentationMath.Contains(view,view.center,SceneViewShape.RoundedRectangle,radius));
                True(!SceneScreenPresentationMath.Contains(view,new Vector2(1,1),SceneViewShape.RoundedRectangle,radius));
                True(SceneScreenPresentationMath.Contains(view,new Vector2(radius,radius),SceneViewShape.RoundedRectangle,radius));
                True(SceneScreenPresentationMath.Contains(view,new Vector2(1,1),SceneViewShape.Rectangle,radius));
                True(!SceneScreenPresentationMath.Contains(view,new Vector2(-1,1),SceneViewShape.Rectangle,radius));
                True(!SceneScreenPresentationMath.Contains(view,new Vector2(1,1),SceneViewShape.Ellipse,radius));
                True(SceneScreenPresentationMath.Contains(view,view.center,SceneViewShape.Ellipse,radius));
            }
            Eq(SceneScreenPresentationMath.CornerRadius(10000,1440,1440,new Vector2(100,300)),50);
        });
        Check("each scene padding stays inside its original split at every aspect ratio", () => {
            var padding=new SceneViewportPadding {left=24,right=40,top=16,bottom=32};
            foreach(var screen in new[] {new Vector2(1920,1080),new Vector2(1080,1920),new Vector2(3440,1440)})
            {
                GameViewportLayout.Calculate(GameScreenLayout.SideBySide,.6f,true,0,.1f,out var battle,out var item);
                foreach(var cell in new[] {battle,item,new Rect(0,.9f,1,.1f)})
                {
                    float scale=screen.y/1440;
                    var actual=padding.Inset(cell,screen,scale);
                    Eq(actual.xMin-cell.xMin,24*scale/screen.x);
                    Eq(cell.xMax-actual.xMax,40*scale/screen.x);
                    Eq(actual.yMin-cell.yMin,32*scale/screen.y);
                    Eq(cell.yMax-actual.yMax,16*scale/screen.y);
                    True(actual.xMin>=cell.xMin && actual.xMax<=cell.xMax && actual.yMin>=cell.yMin && actual.yMax<=cell.yMax);
                }
                Eq(battle.width/item.width,1.5f); Eq(battle.height,.9f);
            }
        });
        Check("zero padding preserves cells and excessive padding leaves at least one pixel", () => {
            var screen=new Vector2(1200,900); var cell=new Rect(.6f,0,.4f,.9f);
            var same=new SceneViewportPadding().Inset(cell,screen,1);
            Eq(same.x,cell.x); Eq(same.y,cell.y); Eq(same.width,cell.width); Eq(same.height,cell.height);
            var huge=new SceneViewportPadding {left=10000,right=20000,top=20000,bottom=10000}.Inset(cell,screen,1);
            True(huge.xMin>=cell.xMin && huge.xMax<=cell.xMax && huge.yMin>=cell.yMin && huge.yMax<=cell.yMax);
            Eq(huge.width,1/screen.x); Eq(huge.height,1/screen.y);
            var hidden=new Rect(0,0,0,0);
            Eq(new SceneViewportPadding {left=20}.Inset(hidden,screen,1).width,0);
        });
        Check("invalid padding cannot create nonfinite or negative viewports", () => {
            foreach(float value in new[] {float.NaN,float.PositiveInfinity,float.NegativeInfinity,-10f,10000f,float.MaxValue})
            {
                var pad=new SceneViewportPadding {left=value,right=value,top=value,bottom=value};
                var view=pad.Inset(new Rect(0,0,.4f,.9f),new Vector2(1920,1080),2);
                True(!float.IsNaN(view.width) && !float.IsInfinity(view.width) && view.width>0 && view.height>0);
            }
        });
        Check("image mask fitting stretches covers or contains without wrapping outside pixels", () => {
            var image=new Vector2(512,512); var view=new Vector2(1200,600);
            var stretch=SceneScreenPresentationMath.MaskUvScale(image,view,SceneMaskFit.Stretch);
            Eq(stretch.x,1); Eq(stretch.y,1);
            var cover=SceneScreenPresentationMath.MaskUvScale(image,view,SceneMaskFit.Cover);
            True(SceneScreenPresentationMath.TryMaskUv(Vector2.zero,cover,out var uv)); Eq(uv.x,0); Eq(uv.y,.25f);
            var contain=SceneScreenPresentationMath.MaskUvScale(image,view,SceneMaskFit.Contain);
            True(!SceneScreenPresentationMath.TryMaskUv(Vector2.zero,contain,out uv));
            True(SceneScreenPresentationMath.TryMaskUv(new Vector2(.5f,.5f),contain,out uv)); Eq(uv.x,.5f); Eq(uv.y,.5f);
        });
        Check("image mask input uses linear clamped alpha and preserves transparent holes", () => {
            byte zero=0,one=255;
            var pixels=new[] {zero,one,zero,one};
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(pixels,2,2,new Vector2(.25f,.5f)),0);
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(pixels,2,2,new Vector2(.75f,.5f)),1);
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(pixels,2,2,new Vector2(.5f,.5f)),.5f);
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(pixels,2,2,Vector2.zero),0);
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(pixels,2,2,Vector2.one),1);
            var hole=new[] {one,one,one,one,zero,one,one,one,one};
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(hole,3,3,new Vector2(.5f,.5f)),0);
            Eq(SceneScreenPresentationMath.SampleMaskAlpha(hole,3,3,Vector2.one),1);
        });
        Check("closed frame fills transparent lenses but keeps the exterior hidden", () => {
            var alpha=new byte[81];
            for(int y=2;y<=6;y++) for(int x=2;x<=6;x++)
                if(x==2 || x==6 || y==2 || y==6) alpha[y*9+x]=255;
            var mask=SceneScreenPresentationMath.FillFrameWindows(alpha,9,9,.5f);
            True(mask[4*9+4]==255 && alpha[4*9+4]==0);
            True(mask[0]==0 && mask[8*9+8]==0 && mask[4*9+2]==255);
            alpha[2*9+4]=0; // 열린 테두리는 창으로 오인하면 안 됩니다.
            mask=SceneScreenPresentationMath.FillFrameWindows(alpha,9,9,.5f);
            True(mask[4*9+4]==0);
        });
        Check("one glasses frame supports two separate lenses without exposing the space between them", () => {
            var alpha=new byte[11*7];
            for(int y=1;y<=5;y++) for(int x=1;x<=9;x++)
                if((x<=4 && (x==1 || x==4 || y==1 || y==5)) || (x>=6 && (x==6 || x==9 || y==1 || y==5))) alpha[y*11+x]=255;
            var mask=SceneScreenPresentationMath.FillFrameWindows(alpha,11,7,.5f);
            True(mask[3*11+2]==255 && mask[3*11+7]==255 && mask[3*11+5]==0);
        });
        Check("frame aspect and pivot follow UI Image instead of stretching circles", () => {
            var rect=SceneScreenPresentationMath.PreserveFrameAspect(new Rect(-200,-100,400,200),new Vector2(64,64),new Vector2(.5f,.5f));
            Eq(rect.width,200); Eq(rect.height,200); Eq(rect.x,-100); Eq(rect.y,-100);
            rect=SceneScreenPresentationMath.PreserveFrameAspect(new Rect(0,0,400,200),new Vector2(64,64),Vector2.zero);
            Eq(rect.x,0); Eq(rect.width,200);
        });
        Check("frame screen map follows resize and rotation while keeping the same lens UVs", () => {
            foreach(var corners in new[] {
                new[] {new Vector2(.1f,.2f),new Vector2(.4f,.2f),new Vector2(.1f,.7f)},
                new[] {new Vector2(.6f,.1f),new Vector2(.6f,.5f),new Vector2(.2f,.1f)}})
            {
                True(SceneScreenPresentationMath.FrameScreenMap(corners[0],corners[1],corners[2],out var x,out var y));
                var uv=SceneScreenPresentationMath.FrameUv(corners[0]+(corners[1]-corners[0])*.3f+(corners[2]-corners[0])*.8f,x,y);
                Eq(uv.x,.3f); Eq(uv.y,.8f);
            }
            True(!SceneScreenPresentationMath.FrameScreenMap(Vector2.zero,Vector2.zero,Vector2.zero,out var a,out var b));
        });
        Check("nine sliced frame mapping keeps source borders fixed while its center grows", () => {
            foreach(float width in new[] {200f,400f,800f})
            {
                var source=new Vector4(.25f,.25f,.25f,.25f);
                var dest=new Vector4(8/width,.04f,8/width,.04f);
                var uv=SceneScreenPresentationMath.SlicedFrameUv(new Vector2(4/width,.5f),dest,source);
                Eq(uv.x,.125f); Eq(uv.y,.5f);
                uv=SceneScreenPresentationMath.SlicedFrameUv(new Vector2(1-4/width,.5f),dest,source);
                Eq(uv.x,.875f);
            }
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
