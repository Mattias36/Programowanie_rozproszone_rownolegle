// See https://aka.ms/new-console-template for more information
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Semaphore
{
    class Program
    {

        static int DepositValue = 2000;
        static SemaphoreSlim semaphoreDeposite = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaphoreMagazine = new SemaphoreSlim(1, 1);
        static object lockObject = new object();
        const int TruckCapacity = 200;
        static void Main(string[] args)
        {
            Console.WriteLine("Start symulacji...");
            int minerCount = 5;
            Task[] miners = new Task[minerCount];
            for (int i = 0; i < minerCount; i++)
            {
                int id = i + 1;
                miners[i] = Task.Run(() => StartWork(id));
            }
            // czekanie na zakończenie
            Task.WaitAll(miners);
            Console.WriteLine("Koniec symulacji. Cały węgiel został wydobyty.");
        }

        static void StartWork(int minerID)
        {
            while (true)
            {
                int actualCoalValue = 0;
                //1. Wydobycie węgla

                // czekanie na wolne miejsce przy złożu 
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

                        // ładowanie ciężarówki
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

                        Console.WriteLine($"Górnik {minerID} wydobył {actualCoalValue} jedn. węgla. Pozostało w złożu: {DepositValue}");
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
                Console.WriteLine($"Górnik {minerID} transportuje węgiel do magazynu...");
                Thread.Sleep(10000);

                //3.Rozładowanie
                semaphoreMagazine.Wait();
                try
                {
                    Console.WriteLine($"Górnik {minerID} rozładowuje węgiel...");
                    Thread.Sleep(actualCoalValue * 10);
                }
                finally
                {
                    semaphoreMagazine.Release();
                }
                // 4.Powrót do złoża 
            }
            Console.WriteLine($"GÓRNIK {minerID} ZAKOŃCZYŁ PRACĘ.");
        }
    }
}