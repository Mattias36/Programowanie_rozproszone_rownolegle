using System;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        string serverIp = "10.104.33.219"; // ten sam co w serwerze
        int port = 2040;

        using (TcpClient client = new TcpClient())
        {
            client.Connect(serverIp, port);
            Console.WriteLine("Połączono z serwerem.");
            using (NetworkStream stream = client.GetStream())
            {
                // 1. odbierz dane
                int height = ReadInt(stream);
                int width = ReadInt(stream);
                Console.WriteLine($"Odebrano obraz {width}x{height}");
                byte[] imgData = ReadExact(stream, height * width);

                // 2. przetwórz (Sobel)
                byte[] processed = Sobel(imgData, width, height);
                Console.WriteLine("Przetwarzanie zakończone.");

                // 3. odeślij
                WriteInt(stream, height);
                WriteInt(stream, width);
                stream.Write(processed, 0, processed.Length);
                Console.WriteLine("Wysłano wynik do serwera.");
            }
        }

        Console.WriteLine("Klient zakończył pracę.");
    }

    // --- sieciówka ---
    static int ReadInt(NetworkStream stream)
    {
        byte[] buf = ReadExact(stream, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(buf);
        return BitConverter.ToInt32(buf, 0);
    }

    static void WriteInt(NetworkStream stream, int value)
    {
        byte[] buf = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(buf);
        stream.Write(buf, 0, 4);
    }

    static byte[] ReadExact(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                throw new Exception("Połączenie przerwane");
            offset += read;
        }
        return buffer;
    }

    // --- Sobel ---
    static byte[] Sobel(byte[] data, int width, int height)
    {
        // najpierw do 2D
        int[,] src = new int[height, width];
        int idx = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                src[y, x] = data[idx++];

        int[,] gxK = new int[3, 3]
        {
            { -1, 0, 1 },
            { -2, 0, 2 },
            { -1, 0, 1 }
        };
        int[,] gyK = new int[3, 3]
        {
            { -1, -2, -1 },
            {  0,  0,  0 },
            {  1,  2,  1 }
        };

        byte[] output = new byte[width * height];

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int gx = 0;
                int gy = 0;

                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int pixel = src[y + ky, x + kx];
                        gx += pixel * gxK[ky + 1, kx + 1];
                        gy += pixel * gyK[ky + 1, kx + 1];
                    }
                }

                double g = Math.Sqrt(gx * gx + gy * gy);
                if (g > 255) g = 255;
                if (g < 0) g = 0;

                output[y * width + x] = (byte)g;
            }
        }

        // brzegi zostawimy 0
        return output;
    }
}