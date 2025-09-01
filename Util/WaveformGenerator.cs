//using UnityEngine;
//using System.Threading.Tasks;

//public static class WaveformGenerator
//{
//    // Bu ana fonksiyondur. Ses klibini, resmin boyutlarını ve rengini alıp
//    // bize dalga formunu içeren bir Texture2D döndürür.
//    public static async Task<Texture2D> GenerateWaveformTexture(AudioClip audioClip, int width, int height, Color color)
//    {
//        if (audioClip == null) return null;

//        // Arka planda çalışacak bir görev oluşturuyoruz ki Unity donmasın
//        return await Task.Run(() =>
//        {
//            // Ses verisini çek
//            float[] samples = new float[audioClip.samples * audioClip.channels];
//            audioClip.GetData(samples, 0);

//            // Çizim için bir doku (texture) oluştur
//            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
//            Color[] blank = new Color[width * height];
//            for (int i = 0; i < blank.Length; i++)
//            {
//                blank[i] = Color.clear; // Başlangıçta tamamen şeffaf
//            }
//            texture.SetPixels(blank);

//            int packSize = (audioClip.samples / width) + 1;
//            float[] waveform = new float[width];

//            // Ses verisini analiz et ve her piksel sütunu için en yüksek sesi bul
//            for (int w = 0; w < width; w++)
//            {
//                int start = w * packSize;
//                int end = start + packSize;
//                float max = 0f;

//                for (int i = start; i < end && i < samples.Length; i++)
//                {
//                    float val = Mathf.Abs(samples[i]);
//                    if (val > max)
//                    {
//                        max = val;
//                    }
//                }
//                waveform[w] = max;
//            }

//            // Analiz edilen veriyi resme çiz
//            for (int x = 0; x < width; x++)
//            {
//                for (int y = 0; y < height; y++)
//                {
//                    // Dalga formunun o anki yüksekliğiyle karşılaştır
//                    if (y < waveform[x] * height)
//                    {
//                        // Alt ve üst simetrik çizgileri çiz
//                        int centerY = height / 2;
//                        int pixelIndexUp = (centerY + y) * width + x;
//                        int pixelIndexDown = (centerY - y) * width + x;

//                        if (pixelIndexUp < blank.Length) blank[pixelIndexUp] = color;
//                        if (pixelIndexDown >= 0 && pixelIndexDown < blank.Length) blank[pixelIndexDown] = color;
//                    }
//                }
//            }
//            texture.SetPixels(blank);
//            // Not: texture.Apply() ana thread'de çağrılmalı

//            return texture;
//        });
//    }
//}