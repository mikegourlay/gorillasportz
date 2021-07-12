$wc = New-Object System.Net.WebClient
$wc.DownloadFile("http://golf.gorillasportz.net/login/ping", "$PSScriptRoot\alive.txt")