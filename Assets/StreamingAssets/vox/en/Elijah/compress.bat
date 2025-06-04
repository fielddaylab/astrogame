@echo off

set root=%cd%
for /d /r %%s in (*) do (
    cd %%s
    
    set processDir="%%s"
    echo processing: %processDir%
    
    for %%a in (*.mp3) do (
        ffmpeg -i "%%a" -ab 80k "temp-%%a"
        del "%%a"
        ren "temp-%%a" "%%a"
    )
    
    cd %root%
)