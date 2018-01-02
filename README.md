# FFSVersionCleaner
Dieses Kommandozeilen programm soll versionierte Dateien bereinigen. 
Das Programm FreeFileSync bietet die Option, dass gelöschte Dateien in ein Versions-Verzeichnis weggeschrieben werden.
Diese Dateien bekommen dann ein gewisses Format:
*.DATEIENDUNG YYYY-MM-DD HH24MISS.DATEIENDUNG
z.B. Z:\VersioningC\Tools\Readme.txt 2017-12-20 000902.txt
Leider versioniert FreeFileSync ohne Grenzen.
Eine Outlookdatei ändert sich mit jedem Aufruf von Outlook. 
Hat diese Datei eine Größe von ca. 1,5GB muss man die Festplatte häufig manuell bereinigen, oder die Platte läuft über.
Diese Programm löscht automatisch die ältesten Versionen einer Datei und lässt nur die neuesten Versionen unversehrt...
Erster Parameter der Kommandozeile ist das Verzeichniss z.B.: "Z:\VersioningC"
Zweiter Parameter der Kommandozeile gibt an, wieviele alte Dateien stehen bleiben sollen. Z.B.: "4"
