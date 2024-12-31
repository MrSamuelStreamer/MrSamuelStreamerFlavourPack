using System;
using System.IO;
using RimWorld;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace MSSFP;

public class PawnGraphicUtils
{
    public static string SaveDataPath =>
        Path.Combine(GenFilePaths.ConfigFolderPath, "Haunts");

    public static Texture2D LoadTexture(string texturePath)
    {
        if (!File.Exists(texturePath))
        {
            return null;
        }

        byte[] fileData = File.ReadAllBytes(texturePath);
        Texture2D tex = new Texture2D(256, 256);
        tex.LoadImage(fileData);
        return tex;
    }
    public static string SavePawnTexture(Pawn pawn, Action<bool, string> onComplete = null)
    {
        RenderTexture tex = PortraitsCache.Get(pawn, new Vector2(175f, 175f), Rot4.South, new Vector3(0f, 0f, 0.1f), 0.9f, healthStateOverride: PawnHealthState.Mobile);
        Guid guid = Guid.NewGuid();

        if(!Directory.Exists(SaveDataPath)) Directory.CreateDirectory(SaveDataPath);

        string path = Path.Combine(SaveDataPath, $"{guid}.png");
        SaveTextureToFile(tex, path, 256, 256, (sucess, p) =>
        {
            ModLog.Log($"Saved texture to {p}? {sucess}");
            onComplete?.Invoke(sucess, p);
        });
        return $"{guid}.png";
    }

    public static void SaveTextureToFile(Texture source,
        string filePath,
        int width,
        int height,
        Action<bool, string> done = null)
    {
        // check that the input we're getting is something we can handle:
        if (source is not (Texture2D or RenderTexture))
        {
            done?.Invoke(false, filePath);
            return;
        }

        // use the original texture size in case the input is negative:
        if (width < 0 || height < 0)
        {
            width = source.width;
            height = source.height;
        }

        // resize the original image:
        RenderTexture resizeRT = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(source, resizeRT);

        // create a native array to receive data from the GPU:
        NativeArray<byte> narray = new(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        // request the texture data back from the GPU:
        AsyncGPUReadbackRequest request = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, (AsyncGPUReadbackRequest request) =>
        {
            // if the readback was successful, encode and write the results to disk
            if (!request.hasError)
            {
                try
                {
                    NativeArray<byte> encoded;
                    encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint) width, (uint) height);
                    System.IO.File.WriteAllBytes(filePath, encoded.ToArray());
                    encoded.Dispose();
                }
                catch (Exception e)
                {
                    ModLog.Error("Error saving texture", e);
                    narray.Dispose();
                    done?.Invoke(false, filePath);
                }
            }

            narray.Dispose();
            done?.Invoke(!request.hasError, filePath);
        });
    }
}
