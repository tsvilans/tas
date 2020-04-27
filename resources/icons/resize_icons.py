import sys
import os 
from os import listdir
from os.path import isfile, join
from subprocess import call

folder = os.path.dirname(os.path.realpath(__file__))
onlyfiles = [f for f in listdir(folder) if isfile(join(folder, f)) and f.endswith('.png')]



for f in onlyfiles:
	if f.endswith('_24x24.png'): continue
	msg = "magick \"" + f + "\" -resize 24x24 " + f.split('.')[0] + "_24x24.png"
	print (msg)
	call (msg, shell=False)

print (onlyfiles)
