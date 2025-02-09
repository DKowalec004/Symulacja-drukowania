using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

class Program
{
    static string doWydrukuPath = "do_wydruku.txt";
    static string historiaWydrukowPath = "historia_wydrukow.txt";
    static bool isRunning = true;
    static object queueLock = new object();
    static object fileLock = new object();
    static Queue<string> printQueue = new Queue<string>();

    static void Main(string[] args)
    {
        // Wczytanie istniejących zadań wydruku z pliku do pamięci
        LoadPrintQueueFromFile();

        // Uruchomienie osobnego wątku obsługującego wydruki
        Thread printingThread = new Thread(HandlePrintingQueue);
        printingThread.Start();

        // Główny wątek obsługujący menu użytkownika
        while (isRunning)
        {
            ShowMenu();
        }

        Console.WriteLine("Czekanie na zakończenie bieżącego wydruku");
        printingThread.Join();
    }

    static void LoadPrintQueueFromFile()
    {
        lock (fileLock)
        {
            if (File.Exists(doWydrukuPath))
            {
                string[] lines = File.ReadAllLines(doWydrukuPath);
                for (int i = 1; i < lines.Length; i++)
                {
                    printQueue.Enqueue(lines[i]);
                }
            }
        }
    }

    static void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine("MENU:");
        Console.WriteLine("1. Nowy wydruk");
        Console.WriteLine("2. Wydruki do odebrania");
        Console.WriteLine("3. Przeglądaj kolejkę wydruku");
        Console.WriteLine("4. Przeglądaj historię wydruków");
        Console.WriteLine("5. Zakończ");
        Console.Write("Wybierz opcję: ");
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                HandleNewPrint();
                break;
            case "2":
                RetrievePrints();
                break;
            case "3":
                DisplayFile(doWydrukuPath);
                break;
            case "4":
                DisplayFile(historiaWydrukowPath);
                break;
            case "5":
                isRunning = false;
                break;
            default:
                Console.WriteLine("Niepoprawny wybór. Spróbuj ponownie.");
                Thread.Sleep(2000);
                break;
        }
    }

    static void HandleNewPrint()
    {
        string owner = "";
        int pages = 0;
        bool isValid = false;
        Console.Clear();

        while (string.IsNullOrWhiteSpace(owner))
        {
            Console.Write("Podaj swoją nazwę: ");
            owner = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(owner))
            {
                Console.WriteLine("Nazwa nie może być pusta. Spróbuj ponownie.");
            }
        }

        while (!isValid)
        {
            Console.Write("Podaj ilość stron do wydrukowania: ");
            string input = Console.ReadLine();

            try
            {
                pages = int.Parse(input);
                if (pages <= 0)
                {
                    Console.WriteLine("Ilość stron musi być większa niż 0. Spróbuj ponownie.");
                }
                else
                {
                    isValid = true;
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Nieprawidłowy format. Wprowadź liczbę całkowitą.");
            }
            catch (OverflowException)
            {
                Console.WriteLine("Wprowadzona liczba jest zbyt duża lub zbyt mała.");
            }
        }

        Console.Write("Podaj tryb wydruku (normalny, szybki, dokladny): ");
        string mode = Console.ReadLine();
        if (mode != "normalny" && mode != "szybki" && mode != "dokladny")
        {
            Console.WriteLine("Wprowadzona nazwa trybu nie istnieje, zostanie zastosowany tryb normalny");
        }

        // Dodanie zadania do kolejki
        lock (queueLock)
        {
            string entry = pages + ";" + mode + ";" + DateTime.Now.ToString("dd.MM.yyyy") + ";" + owner;

            printQueue.Enqueue(entry);

            lock (fileLock)
            {
                if (!File.Exists(doWydrukuPath))
                {
                    File.WriteAllText(doWydrukuPath, "ilosc_stron;tryb;data_przyjecia;wlasciciel_wydruku\n");
                }
                File.AppendAllText(doWydrukuPath, entry + "\n");
            }
        }

        Console.WriteLine("Dokumenty przekazane do wydruku.");
        Thread.Sleep(2000);
    }

    static void HandlePrintingQueue()
    {
        while (isRunning)
        {
            string printJob = null;

            // Pobranie zadania z kolejki
            lock (queueLock)
            {
                if (printQueue.Count > 0)
                {
                    printJob = printQueue.Dequeue();
                }
            }

            if (printJob != null)
            {
                string[] jobDetails = printJob.Split(';');
                int pages = int.Parse(jobDetails[0]);
                string mode = jobDetails[1];
                int delay = mode == "normalny" ? 5 : mode == "dokladny" ? 10 : 5;

                // Symulacja czasu potrzebnego na wydrukowanie stron
                for (int i = 0; i < pages; i++)
                {
                    Thread.Sleep(delay * 1000);
                }

                lock (fileLock)
                {
                    // Aktualizacja historii wydruków
                    if (!File.Exists(historiaWydrukowPath))
                    {
                        File.WriteAllText(historiaWydrukowPath, "id_wydruku;ilosc_stron;tryb;data_przyjecia;wlasciciel_wydruku;czy_odebrany\n");
                    }

                    string[] historyLines = File.ReadAllLines(historiaWydrukowPath);
                    int nextId = 1;
                    if (historyLines.Length > 1)
                    {
                        string lastLine = historyLines[historyLines.Length - 1];
                        string[] lastFields = lastLine.Split(';');
                        nextId = int.Parse(lastFields[0]) + 1;
                    }

                    string completedEntry = nextId + ";" + printJob + ";nie";
                    File.AppendAllText(historiaWydrukowPath, completedEntry + "\n");

                    // Aktualizacja pliku z kolejką
                    string[] lines = File.ReadAllLines(doWydrukuPath);
                    string[] newLines = new string[lines.Length - 1];
                    Array.Copy(lines, newLines, lines.Length - 1);
                    File.WriteAllLines(doWydrukuPath, newLines);
                }
            }

            Thread.Sleep(1000);
        }
    }

    static void RetrievePrints()
    {
        Console.Clear();
        string owner = "";

        while (string.IsNullOrWhiteSpace(owner))
        {
            Console.Write("Podaj swoją nazwę: ");
            owner = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(owner))
            {
                Console.WriteLine("Nazwa nie może być pusta. Spróbuj ponownie.");
            }
        }

        lock (fileLock)
        {
            if (!File.Exists(historiaWydrukowPath))
            {
                Console.WriteLine("Brak wydruków w historii.");
                Thread.Sleep(2000);
                return;
            }

            string[] lines = File.ReadAllLines(historiaWydrukowPath);
            string header = lines[0];
            StringBuilder updatedHistory = new StringBuilder();

            int totalPages = 0;

            foreach (string line in lines)
            {
                string[] fields = line.Split(';');
                if (fields.Length < 6) continue;

                if (fields[4] == owner && fields[5] == "nie")
                {
                    totalPages += int.Parse(fields[1]);
                    fields[5] = "tak";
                }

                updatedHistory.AppendLine(string.Join(";", fields));
            }

            File.WriteAllText(historiaWydrukowPath, updatedHistory.ToString());

            if (totalPages == 0)
            {
                Console.WriteLine(owner + " nie posiada druków do odbioru");
            }
            else
            {
                Console.WriteLine($"Proszę oto {totalPages} stron dokumentów.");
            }
            Thread.Sleep(3000);
        }
    }

    static void DisplayFile(string filePath)
    {
        Console.Clear();
        if (File.Exists(filePath))
        {
            string content = File.ReadAllText(filePath);
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine($"Brak pliku {filePath}.");
        }

        Console.WriteLine("\nNaciśnij Enter, aby wrócić do menu.");
        Console.ReadLine();
    }
}
