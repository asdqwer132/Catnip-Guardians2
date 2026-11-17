using UnityEngine;
public static class SceneViewportInput
{
    public static Rect ScreenRect(Camera camera)
    { var view=SceneViewportCamera.For(camera);return view!=null && view.IsReady ? view.ScreenRect : camera.pixelRect; }
    public static Vector2 ToRenderPoint(Camera camera,Vector2 screen)
    { var view=SceneViewportCamera.For(camera);return view!=null && view.IsReady ? view.ToRenderPoint(screen) : screen; }
    public static Ray ScreenPointToRay(Camera camera,Vector2 screen) => camera.ScreenPointToRay(ToRenderPoint(camera,screen));
    public static Vector3 ScreenToWorldPoint(Camera camera,Vector2 screen) => camera.ScreenToWorldPoint(ToRenderPoint(camera,screen));
    public static Vector3 WorldToScreenPoint(Camera camera,Vector3 world)
    { var point=camera.WorldToScreenPoint(world);var view=SceneViewportCamera.For(camera);return view!=null && view.IsReady ? view.ToScreenPoint(point) : point; }
}
