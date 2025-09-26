@echo off

magick mogrify -resize "1024x1024>" -verbose *.png
pngquant *.png --ext .png --verbose -f --skip-if-larger