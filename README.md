Usage
=====
git-get will download a users public repositories, starred repositories and gists to the directory it is executed from. git-get was written in C#, the mono runtime is needed to execute it on OSX / Linux. I suggest setting up an alia for mono git-get as just git-get.
```
<mono> git-get all <username>
<mono> git-get star <username>
<mono> git-get gist <username>
<mono> git-get repo <username>
<mono> git-get bitbucket <username>
```

Installation (OSX and Linux)
==========================
Download and install the mono runtime
```
http://www.go-mono.com/mono-downloads/download.html
```
Download git-get.exe from this repo and move it to ```/usr/bin/```
```
https://github.com/gszauer/git-get/raw/master/git-get.exe
```
Set up a system wide alias, to access from anywhere
```
alias git-get='mono /usr/bin/git-get.exe'
```

Installation (Windows)
======================
This executable was only tested on OSX / Linux, i assume Win32 works.
Download git-get.exe from this repo and move it to C:\Windows\
```
https://github.com/gszauer/git-get/raw/master/git-get.exe
```
