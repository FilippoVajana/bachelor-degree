using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SARLib.SAREnvironment;
using System.IO;
using System.Threading;

namespace GridPlanner
{
    public class Program
    {        
        public static void Main(string[] args)
        {
            //APP DIRECTORIES 
            string APP_DATA_ROOT = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data")).FullName;
            string APP_ENVS_ROOT = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data", "Environments")).FullName;

            //SIMULATOR
            SARSimulator.MissionSimulator SIMULATOR = new SARSimulator.MissionSimulator(APP_ENVS_ROOT);


            //PROVA CREAZIONE/CANCELLAZIONE TASK

            ////sorgente segnale timeout simulazione
            //CancellationTokenSource cancTokenSource = new CancellationTokenSource(4000);
            //List<Task> taskPool = new List<Task>(4);

            ////creazione pool
            //for (int i = 0; i < 4; i++)
            //{
            //    taskPool.Add(new Task(() => { SimulateWorkload(cancTokenSource.Token); }));
            //}

            ////avvio dei task
            //foreach (var t in taskPool)
            //{
            //    t.Start();
            //}

            //Task.WaitAll(taskPool.ToArray());

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();
        }

        private static void SimulateWorkload(CancellationToken cancToken)
        {
            while (true)
            {
                if (cancToken.IsCancellationRequested == false)
                {
                    Console.WriteLine($"{DateTime.Now}\t" +
                        $"ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(100);
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now}\t" +
                        $"ThreadId: {Thread.CurrentThread.ManagedThreadId}\t" +
                        $"Cancellation Request");
                    return;
                }
            }
        }
    }
}
