using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;

namespace KeyOverlay
{
    internal class FadingTexture : IDisposable
    {
        uint width;
        uint height;
        byte[] pixels;

        Texture texture;
        Sprite sprite;

        public Sprite GetSprite()
            => sprite;

        public FadingTexture(Color backgroundColor, uint windowWidth, float ratioY)
        {
            width = windowWidth;
            height = (uint)(512 * ratioY);

            pixels = new byte[width * height * 4];

            Color color = backgroundColor;
            for (uint y = 0; y < height; y++)
            {
                byte alpha = (byte)((1f - (y / (float)height)) * 255f);
                color.A = alpha;

                int pixelOffset = (int)(width * y * 4);
                for (uint x = 0; x < width; x++)
                {
                    pixels[pixelOffset++] = color.R;
                    pixels[pixelOffset++] = color.G;
                    pixels[pixelOffset++] = color.B;
                    pixels[pixelOffset++] = color.A;
                }
            }

            texture = new Texture(width, height);
            texture.Update(pixels);
            sprite = new Sprite(texture);
        }

        public void Dispose()
        {
            sprite.Dispose();
            texture.Dispose();
        }
    }
}
