set name=%random%
copy home.txt build%name%.txt
"D:\Program Files\Notepad++\Notepad++" build%name%.txt
echo build%name%.txt >> map.txt