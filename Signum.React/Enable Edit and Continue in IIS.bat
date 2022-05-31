SET "APPCMD=%systemroot%\system32\inetsrv\AppCmd.exe"
FOR /F "TOKENS=*" %%f IN ('%APPCMD% list apppool /text:name') DO %APPCMD% set apppool "%%~f" /+environmentVariables.add[@start,name='COMPLUS_ForceEnC',value='1']
PAUSE