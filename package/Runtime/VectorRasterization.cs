using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        static Color SampleGradient(GradientStop[] stops, float u)
        {
            int stop;
            for (stop = 0; stop < stops.Length; stop++)
            {
                if (u < stops[stop].stopPercentage)
                    break;
            }
            if (stop >= stops.Length)
                return stops[stops.Length - 1].color;
            if (stop == 0)
                return stops[0].color;

            float percentageRange = stops[stop].stopPercentage - stops[stop - 1].stopPercentage;
            if (percentageRange > Epsilon)
            {
                float blend = (u - stops[stop - 1].stopPercentage) / percentageRange;
                return Color.LerpUnclamped(stops[stop - 1].color, stops[stop].color, blend);
            }
            else return stops[stop - 1].color;
        }

        static Vector2 RayUnitCircleFirstHit(Vector2 rayStart, Vector2 rayDir)
        {
            float tca = Vector2.Dot(-rayStart, rayDir);
            float d2 = Vector2.Dot(rayStart, rayStart) - tca * tca;
            System.Diagnostics.Debug.Assert(d2 <= 1.0f);
            float thc = Mathf.Sqrt(1.0f - d2);
            // solutions for t if the ray intersects
            float t0 = tca - thc;
            float t1 = tca + thc;
            float t = Mathf.Min(t0, t1);
            if (t < 0.0f)
                t = Mathf.Max(t0, t1);
            System.Diagnostics.Debug.Assert(t >= 0);
            return rayStart + rayDir * t;
        }

        static float RadialAddress(Vector2 uv, Vector2 focus)
        {
            uv = (uv - new Vector2(0.5f, 0.5f)) * 2.0f;
            //focus = (focus - new Vector2(0.5f, 0.5f)) * 2.0f;
            var pointOnPerimiter = RayUnitCircleFirstHit(focus, (uv - focus).normalized);

            //return (uv - focus).magnitude / (pointOnPerimiter - focus).magnitude;
            // This is faster
            Vector2 diff = pointOnPerimiter - focus;
            if (Mathf.Abs(diff.x) > Epsilon)
                return (uv.x - focus.x) / diff.x;
            if (Mathf.Abs(diff.y) > Epsilon)
                return (uv.y - focus.y) / diff.y;
            return 0.0f;
        }

        static Color32[] RasterizeGradient(GradientFill gradient, int width, int height)
        {
            Color32[] pixels = new Color32[width * height];

            if (gradient.type == GradientFillType.Linear)
            {
                int pixIndex = 0;
                for (int x = 0; x < width; x++)
                    pixels[pixIndex++] = SampleGradient(gradient.stops, x / (float)(width - 1));
                for (int y = 1; y < height; y++)
                {
                    Array.Copy(pixels, 0, pixels, pixIndex, width);
                    pixIndex += width;
                }
            }
            else if (gradient.type == GradientFillType.Radial)
            {
                int pixIndex = 0;
                for (int y = 0; y < height; y++)
                {
                    float v = y / ((float)height - 1);
                    for (int x = 0; x < width; x++)
                    {
                        float u = x / ((float)width - 1);
                        pixels[pixIndex++] = SampleGradient(gradient.stops, RadialAddress(new Vector2(u, 1.0f - v), gradient.radialFocus));
                    }
                }
            }

            return pixels;
        }

        static Color32[] RasterizeGradientStripe(GradientFill gradient, int width)
        {
            Color32[] pixels = new Color32[width];
            for (int x = 0; x < width; ++x)
            {
                float u = x / ((float)width - 1);
                pixels[x] = SampleGradient(gradient.stops, u);
            }
            return pixels;
        }

        struct PackRectItem
        {
            public Vector2 position;
            public bool rotated;
        }
        static List<PackRectItem> PackRects(IList<Vector2> sizes, out Vector2 atlasDims)
        {
            var pack = new List<PackRectItem>(sizes.Count);
            atlasDims = new Vector2(1024, 1024);
            var maxPos = Vector2.zero;
            var curPos = Vector2.zero;
            float curColThickness = 0.0f;
            foreach (var s in sizes)
            {
                if (atlasDims.y < curPos.y + s.y)
                {
                    if (atlasDims.y < s.y)
                        atlasDims.y = s.y;
                    if (curPos.y != 0)
                        curPos.x += curColThickness;
                    curPos.y = 0;
                    curColThickness = s.x;
                }
                curColThickness = Mathf.Max(curColThickness, s.x);
                pack.Add(new PackRectItem() { position = curPos });
                maxPos = Vector2.Max(maxPos, curPos + s);
                curPos.y += s.y;
            }
            atlasDims = maxPos;
            return pack;
        }

        static void BlitRawTexture(RawTexture src, RawTexture dest, int destX, int destY, bool rotate)
        {
            if (rotate)
            {
                for (int y = 0; y < src.height; y++)
                {
                    int srcRowIndex = y * src.width;
                    int destColumnIndex = destY * dest.width + destX + y;
                    for (int x = 0; x < src.width; x++)
                    {
                        int srcIndex = srcRowIndex + x;
                        int destIndex = destColumnIndex + x * dest.width;
                        dest.rgba[destIndex] = src.rgba[srcIndex];
                    }
                }
            }
            else
            {
                for (int y = 0; y < src.height; y++)
                    Array.Copy(src.rgba, y * src.width, dest.rgba, (destY + y) * dest.width + destX, src.width);
            }
        }

        static void WriteRawInt2Packed(RawTexture dest, int v0, int v1, int destX, int destY)
        {
            byte r = (byte)(v0/255);
            byte g = (byte)(v0-r*255);
            byte b = (byte)(v1/255);
            byte a = (byte)(v1-b*255);
            int offset = destY * dest.width + destX;
            dest.rgba[offset] = new Color32(r, g, b, a);
        }

        static void WriteRawFloat4Packed(RawTexture dest, float f0, float f1, float f2, float f3, int destX, int destY)
        {
            byte r = (byte)(f0*255.0f+0.5f);
            byte g = (byte)(f1*255.0f+0.5f);
            byte b = (byte)(f2*255.0f+0.5f);
            byte a = (byte)(f3*255.0f+0.5f);
            int offset = destY * dest.width + destX;
            dest.rgba[offset] = new Color32(r, g, b, a);
        }
    }
}
