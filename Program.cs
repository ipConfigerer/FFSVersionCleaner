using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FFSVersionCleaner
{
    class Program
    {
        // Dieses Kommandozeilen programm soll versionierte Dateien bereinigen. Das Programm FreeFileSync bietet die Option,
        // dass gelöschte Dateien in ein Versions-Verzeichnis weggeschrieben werden.
        // Diese Dateien bekommen dann ein gewisses Format:
        // *.DATEIENDUNG YYYY-MM-DD HH24MISS.DATEIENDUNG
        // z.B. Z:\Folder\SubFolder\ReadMe.txt 2017-12-20 000902.txt
        // Leider versioniert FreeFileSync ohne Grenzen.
        // Eine Outlookdatei ändert sich mit jedem Aufruf von Outlook. 
        // Hat diese Datei eine Größe von ca. 1,5GB muss man die Festplatte häufig manuell bereinigen, oder die Platte läuft über.
        // Diese Programm löscht automatisch die ältesten Versionen einer Datei und lässt nur die neuesten Versionen unversehrt...
        // Erster Parameter der Kommandozeile ist das Verzeichniss z.B.: "Z:\VersioningC"
        // Zweiter Parameter der Kommandozeile gibt an, wieviele alte Dateien stehen bleiben sollen. Z.B.: "4"
        //
        // Coole Konfiguration wäre eine Parameter-Key-Value-List:
        // size:5-8, size:100-5, default:3, type:docx-10, type:jpg-2
        // --> Dateien bis 5 MB Größe werden maximal 8 mal behalten
        // --> Dateien > 5 MB bis 100 MB werden maximal 5 mal behalten
        // --> Dateien größer 100 MB werden 3 mal behalten
        // --> Dateien mit der Endung docx werden egal welche Größe 10 mal behalten
        // --> Dateien mit der Endung jpg werden egal welche Größe 2 mal behalten
        static void Main(string[] args)
        {
            try
            {
                if (args[0] == "?" || args[0].ToUpper() == "HELP" || args[0].ToUpper() == "-HELP" )
                {
                    Console.WriteLine("Dieses Programm bereinigt versionierte Dateien von FreeFileSync");
                    Console.WriteLine("Vorraussetzung ist, dass in den Synchronisationseinstellungen von FFS mittels F8");
                    Console.WriteLine("als Verhalten beim Löschen von Dateien \"Versionierung\" ausgewählt ist.");
                    Console.WriteLine("Desweiteren muss als Namenskonvention \"Zeitstempel\" ausgewählt sein.");
                    Console.WriteLine("Wenn dies der Fall ist können mit diesem Programm ältere Versionen einer Datei gelöscht werden.");
                    Console.WriteLine("Sie können angeben, wieviele Versionen sie behalten möchten (zwischen 2 und 500)");
                    Console.WriteLine("Geben Sie bitte als ersten Parameter den zu bereinigenden Ordner an");
                    Console.WriteLine("und als zweiten Parameter die Anzahl der maximal zu behaltenden Versionen");
                    Console.WriteLine("z.B.: FFSVersionCleaner \"C:\\Ordner\\Unter Ordner\\\" \"4\"");
                    Console.WriteLine("In obigem Beispiel wird sowohl in \"C:\\Ordner\\Unter Ordner\\\",");
                    Console.WriteLine("als auch rekursiv in allen Unterverzeichnissen gesucht.");
                    return;
                }
                if (args.Count() < 2)
                {
                    Console.WriteLine("Geben Sie bitte als ersten Parameter den zu bereinigenden Ordner an");
                    Console.WriteLine("und als zweiten Parameter die Anzahl der maximal zu behaltenden Versionen");
                    Console.WriteLine("z.B.: FFSVersionCleaner \"C:\\Ordner\\Unter Ordner\\\" \"4\"");
                    return;
                }
                if (Directory.Exists(args[0]) == false)
                {
                    Console.WriteLine("Der erste Parameter \"{0}\" ist kein zugängliches Verzeichniss!", args[0]);
                    Console.WriteLine("Geben Sie bitte als ersten Parameter den zu bereinigenden Ordner an");
                    Console.WriteLine("und als zweiten Parameter die Anzahl der maximal zu behaltenden Versionen");
                    Console.WriteLine("z.B.: FFSVersionCleaner \"C:\\Ordner\\Unter Ordner\\\" \"4\"");
                    return;
                }
                int nAnzahl = 0;
                if (int.TryParse(args[1], out nAnzahl) == false)
                {
                    Console.WriteLine("Der zweite Parameter \"{0}\" ist keine ganze Zahl!", args[1]);
                    Console.WriteLine("Geben Sie bitte als ersten Parameter den zu bereinigenden Ordner an");
                    Console.WriteLine("und als zweiten Parameter die Anzahl der maximal zu behaltenden Versionen");
                    Console.WriteLine("z.B.: FFSVersionCleaner \"C:\\Ordner\\Unter Ordner\\\" \"4\"");
                    return;
                }
                if (nAnzahl > 500 || nAnzahl < 2)
                {
                    Console.WriteLine("Der zweite Parameter \"{0}\" darf nicht größer als 500 und kleiner als 2 sein!", args[1]);
                    Console.WriteLine("Geben Sie bitte als ersten Parameter den zu bereinigenden Ordner an");
                    Console.WriteLine("und als zweiten Parameter die Anzahl der maximal zu behaltenden Versionen");
                    Console.WriteLine("z.B.: FFSVersionCleaner \"C:\\Ordner\\Unter Ordner\\\" \"4\"");
                    return;
                }

                var files = from file in Directory.EnumerateFiles(args[0], "*.*", SearchOption.AllDirectories).OrderByDescending(filename => filename)
                            select new
                            {
                                File = file
                            };
                string[] saFileNames = new string[501];
                string[] saHelper = new string[501];
                int nLoop = 0;
                int nParLoop = 0;
                int nDeleted = 0;
                bool bIdentisch = false;

                foreach (var f in files)
                {
                    /* 
                    In der Schleife könnte folgendes eingelesen werden:

                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-20 000902.txt
                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-20 000447.txt
                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-18 142506.txt
                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-11 190859.txt
                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-10 225912.txt
                    Z:\Folder\SubFolder2\SubSubFolder\File.exe 2017-12-22 195550.exe
                    Z:\Folder\SubFolder2\SubSubFolder\ReadMe.txt 2017-12-08 131643.txt
                    Z:\Folder\SubFolder2\SubSubFolder\ReadMe.txt 2017-12-07 124449.txt

                    Der allgemeine Dateiaufbau ist: *.DATEIENDUNG YYYY-MM-DD HH24MISS.DATEIENDUNG
                    Ich möchte nun die 3 neuesten Dateien stehen lassen und die älteren Versionen Löschen.
                    Im obigen Beispiel würden also die Dateien 
                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-11 190859.txt und 
                    Z:\Folder\SubFolder\ReadMe.txt 2017-12-10 225912.txt gelöscht werden...

                    Idee:
                    Array der Größe 4

                    In [0] kommt die erst gelesene Datei rein: 2017-12-20 000902 (Datei aus dem ersten Schleifendurchlauf)
                    In [1] 2017-12-20 000447 (Datei aus dem zweiten Schleifendurchlauf)
                    In [2] 2017-12-18 142506 (Datei aus dem dritten Schleifendurchlauf)                   
                    In [3] 2017-12-11 190859 (Datei aus dem vierten Schleifendurchlauf)
                    
                    I) 
                    Ab dem Moment, wo das Array mindestens 4 Elemente hat, prüfe ich, 
                    ob in allen Elementen mit Ausnahme des Zeitstempels die anderen Zeichen genau gleich sind.
                    Wenn nein --> kopiere [1] in [0], 
                                  kopiere [2] in [1], 
                                  kopiere [3] in [2] und 
                                  kopiere das aktuelle Element in [3] 
                                  Lese die nächste Datei in [3] ein und gehe zu I)
                    Wenn ja   --> lösche physikalisch die Datei aus [3]
                                  Lese die nächste Datei in [3] ein und gehe zu I)
                    */

                    if (nLoop > nAnzahl-1)
                    {
                        //hier jetzt in der Zeichenkette von rechts ausgehend nach einem . suchen
                        //ist der gefunden, "schneide" ich den datumsanteil raus...
                        nParLoop = 0;
                        while (++nParLoop < nAnzahl)
                        {
                            saHelper[nParLoop-1] = RemoveDate(saFileNames[nParLoop-1]);
                        }
                        saHelper[nParLoop-1] = RemoveDate(f.File);

                        //...und überprüfe, ob dann alle Arrayinhalte identisch sind...
                        nParLoop = 0;
                        bIdentisch = true;
                        while (++nParLoop < nAnzahl)
                        {
                            if (saHelper[0] != saHelper[nParLoop])
                            {
                                bIdentisch = false;
                                break;
                            }
                        }

                        if (bIdentisch==true)
                        {
                            saFileNames[nAnzahl-1] = f.File;
                            Console.WriteLine("Deleting File {0}\t", f.File);
                            File.Delete(f.File);
                            nDeleted++;
                        }
                        else
                        {
                            nParLoop = 0;
                            while (++nParLoop < nAnzahl)
                            {
                                saFileNames[nParLoop-1] = saFileNames[nParLoop];
                            }
                            saFileNames[nParLoop-1] = f.File;
                        }
                    }
                    else
                        saFileNames[nLoop] = f.File;

                    nLoop++;
                    
                }
                Console.WriteLine("{0} files found. {1} files deleted.", nLoop.ToString(), nDeleted.ToString() );
            }
            catch (UnauthorizedAccessException UAEx)
            {
                Console.WriteLine(UAEx.Message);
            }
            catch (PathTooLongException PathEx)
            {
                Console.WriteLine(PathEx.Message);
            }
        }
        static string RemoveDate( string p_sName)
        {
            try
            {
                string sRet = p_sName;
                int lastLocation = sRet.LastIndexOf(".");
                if (lastLocation==-1)
                {
                    lastLocation = sRet.Length;
                }
                sRet = sRet.Substring(0, (lastLocation-18));

                return sRet;
            }
            catch
            {
                Console.WriteLine("Error in function RemoveDate");
                return "";
            }

        }
    }
}
