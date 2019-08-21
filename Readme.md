# AutoSubsync

## About
This is a simple windows service developed in order to automate the process of synchronizing downloaded subtitles using Subsync.

[https://github.com/smacke/subsync]

Can be setup to listen to media library folder, and automatically synchronize downloaded subtitles with matching video files in the same folder. Works good with Plex.

## Instructions:
1) Download and install Subsync. Ensure that the subsync folder is setup in path environment variable.
2) Build this project.
3) Run install.bat (as administrator) and follow instructions.

The service can be administered through services.msc after installation, i.e to make it run on computer startup. Logs can be accessed through the event viewer in Windows (eventvwr.msc).