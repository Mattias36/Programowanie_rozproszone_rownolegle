// See https://aka.ms/new-console-template for more information
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Semaphore
{
    class Program
    {
        static int DepositValue = 2000;
        static int MagazineValue = 0;
        static SemaphoreSlim semaphoreDeposite = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaphoreMagazine = new SemaphoreSlim(1, 1);
        static object lockObject = new object();
        static readonly object consoleLock = new object();
        const int TruckCapacity = 200;
        static string[] minerStatuses;
        static bool simulationRunning = true;
        static void Main(string[] args)
        {
            Console.Clear();
            int minerCount = 5;
            Task[] miners = new Task[minerCount];
            minerStatuses = new string[minerCount];
            for (int i = 0; i < minerCount; i++)
            {
                minerStatuses[i] = "Inicjalizacja";
            }
            // czekanie na zakończenie
            Task displayTask = Task.Run(() => UpdateDisplay());

            // rozpoczęcie pracy
            for (int i = 0; i < minerCount; i++)
            {
                int id = i;
                miners[i] = Task.Run(() => StartWork(id));
            }
            Task.WaitAll(miners);
            simulationRunning = false;
            displayTask.Wait();

            Console.SetCursorPosition(0, minerCount + 4);
            Console.WriteLine("Koniec symulacji. Cały węgiel został wydobyty.");
        }

        static void UpdateStatus(int minerIndex, string status)
        {
            lock (lockObject)
            {
                minerStatuses[minerIndex] = status;
            }
        }
        static void StartWork(int minerIndex)
        {
            int minerID = minerIndex + 1;

            while (true)
            {
                int actualCoalValue = 0;
                //1. Wydobycie węgla

                // czekanie na wolne miejsce przy złożu 
                UpdateStatus(minerIndex, "Czeka na miejsce przy zlozu");
                semaphoreDeposite.Wait();


                try
                {
                    // wejście do sekcji krytycznej
                    lock (lockObject)
                    {
                        if (DepositValue <= 0)
                        {
                            break;
                        }
                        UpdateStatus(minerIndex, "Wydobywa wegiel...");

                        // wydobycie
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

                // przerwanie w przypadku braku wydobycia czegokolwiek
                if (actualCoalValue == 0)
                {
                    break;
                }

                // 2.Transport
                UpdateStatus(minerIndex, "Transportuje do magazynu");
                Thread.Sleep(1000);

                //3.Rozładowanie
                UpdateStatus(minerIndex, "Czeka na miejsce w magazynie");
                semaphoreMagazine.Wait();
                try
                {
                    UpdateStatus(minerIndex, "Rozladowuje wegiel");
                    Thread.Sleep(actualCoalValue * 10);
                    lock (lockObject)
                    {
                        MagazineValue += actualCoalValue; // Dodawanie węgla do magazynu
                    }
                }
                finally
                {
                    semaphoreMagazine.Release();
                }
                // 4.Powrót do złoża 
                UpdateStatus(minerIndex, "Wraca po nowy ladunek");
                Thread.Sleep(1000);
            }

            UpdateStatus(minerIndex, "Zakonczyl prace.");
        }

        static void UpdateDisplay()
        {
            while (simulationRunning)
            {
                lock (consoleLock)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("--- SYMULACJA ---".PadRight(50));

                    // lockObject, aby odczytać spójne dane
                    lock (lockObject)
                    {
                        Console.WriteLine($"Stan zloza:   {DepositValue} jedn.".PadRight(50));
                        Console.WriteLine($"Stan magazynu: {MagazineValue} jedn.".PadRight(50));
                    }

                    Console.WriteLine("".PadRight(50, '-'));

                    // wyświetlanie statusów wszystkich górników
                    for (int i = 0; i < minerStatuses.Length; i++)
                    {
                        Console.SetCursorPosition(0, i + 4);
                        string status = $"Gornik {i + 1}: {minerStatuses[i]}";
                        Console.WriteLine(status.PadRight(50));
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}