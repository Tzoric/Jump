using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SilverVisualCaptureCommand
{
    private const string ScenePath="Assets/Scenes/SilverLevel1_SilverLode.unity";

    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        Camera camera=Object.FindFirstObjectByType<Camera>();
        if(camera==null) throw new MissingReferenceException("Silver scene has no camera.");

        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","VisualQA"));
        Directory.CreateDirectory(output);
        CaptureView(camera,output,"Silver_00_WholeLevel",Vector2.zero,39f,1920,1080);
        CaptureView(camera,output,"Silver_01_Entrance",new Vector2(-46f,-27f),6.4f,1280,720);
        CaptureView(camera,output,"Silver_02_LeftClimb",new Vector2(-44f,1f),6.4f,1280,720);
        CaptureView(camera,output,"Silver_03_UpperGlide",new Vector2(-28f,19f),6.4f,1280,720);
        CaptureView(camera,output,"Silver_04_CentralRoute",new Vector2(3f,4f),6.4f,1280,720);
        CaptureView(camera,output,"Silver_05_FinalChute",new Vector2(45f,14f),6.4f,1280,720);
        CaptureView(camera,output,"Silver_06_ExitAndSecret",new Vector2(41f,-27f),6.4f,1280,720);
        Debug.Log($"SILVER VISUAL QA CAPTURED: {output}");
    }

    private static void CaptureView(Camera camera,string output,string name,Vector2 position,
        float orthographicSize,int width,int height)
    {
        camera.transform.position=new Vector3(position.x,position.y,-10f);
        camera.orthographicSize=orthographicSize;
        RenderTexture target=new(width,height,24,RenderTextureFormat.ARGB32);
        Texture2D image=new(width,height,TextureFormat.RGBA32,false);
        RenderTexture previous=RenderTexture.active;
        RenderTexture previousTarget=camera.targetTexture;
        try
        {
            camera.targetTexture=target;
            RenderTexture.active=target;
            camera.Render();
            image.ReadPixels(new Rect(0f,0f,width,height),0,0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output,$"{name}.png"),image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;
            RenderTexture.active=previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }
    }
}
