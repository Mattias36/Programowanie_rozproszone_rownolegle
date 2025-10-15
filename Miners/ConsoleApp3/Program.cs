// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Semaphore
{
    class Program
    {
        static volatile int DepositValue;
        static volatile int MagazineValue;
        static SemaphoreSlim semaphoreDeposite = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaphoreMagazine = new SemaphoreSlim(1, 1);
        static object lockObject = new object();

        const int InitialDeposit = 2000;
        const int TruckCapacity = 200;
        const int MaxMiners = 6;

        static void Main(string[] args)
        {
            Console.WriteLine("--- Rozpoczynanie pomiarów wydajności ---");
            double baseTime = 0.0;

            for (int minerCount = 1; minerCount <= MaxMiners; minerCount++)
            {
                // resetowanie stanu symulacji przed każdym przebiegiem
                DepositValue = InitialDeposit;
                MagazineValue = 0;

                // pomiar czasu
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Uruchomienie właściwej symulacji
                Task[] miners = new Task[minerCount];
                for (int i = 0; i < minerCount; i++)
                {
                    miners[i] = Task.Run(() => StartWork());
                }
                Task.WaitAll(miners);

                stopwatch.Stop();
                double elapsedTime = stopwatch.Elapsed.TotalSeconds;

                // Obliczenia i wyświetlanie wyników
                if (minerCount == 1)
                {
                    baseTime = elapsedTime;
                }

                double speedup = baseTime / elapsedTime;
                double efficiency = speedup / minerCount;

                Console.WriteLine(
                    $"liczba górników: {minerCount}, " +
                    $"czas: {elapsedTime:F2} s, " +
                    $"przyśpieszenie: {speedup:F2}, " +
                    $"efektywność: {efficiency:F2}"
                );
            }

            Console.WriteLine("\n--- Koniec pomiarów ---");
        }

        static void StartWork()
        {
            while (true)
            {
                int actualCoalValue = 0;

                // czekanie na wolne miejsce przy złożu
                semaphoreDeposite.Wait();
                try
                {
                    lock (lockObject)
                    {
                        if (DepositValue <= 0)
                        {
                            break;
                        }

                        // Wydobycie
                        for (int i = 0; i < TruckCapacity; i++)
                        {
                            if (DepositValue > 0)
                            {
                                DepositValue--;
                                actualCoalValue++;
                                Thread.Sleep(10);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    semaphoreDeposite.Release();
                }

                if (actualCoalValue == 0)
                {
                    break;
                }

                // Transport
                Thread.Sleep(1000);

                // Czekanie na magazyn
                semaphoreMagazine.Wait();
                try
                {
                    // Rozładunek
                    Thread.Sleep(actualCoalValue * 10);
                    lock (lockObject)
                    {
                        MagazineValue += actualCoalValue;
                    }
                }
                finally
                {
                    semaphoreMagazine.Release();
                }

                // Powrót
                Thread.Sleep(1000);
            }
        }
    }
}

