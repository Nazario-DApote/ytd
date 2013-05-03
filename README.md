_ytd_  
---

Command line tool that allows to download YouTube videos and playlists.  

YouTubeDownloader.NET version 1.1.1.0  
Allows to download videos from YouTube  
Copyright &copy; Nazario D'Apote 2013  
*****

Usage: ytd_net [OPTIONS]+ &lt;url&gt;  
  
  <url>                      the YouTube playlist url or video url.  
  
Options:  
    -p, --playlist=VALUE    (get all video data from a playlist link and save them to VALUE)  
    --p1                    (get only video urls from a playlist link and save them to VALUE)  
    -d, --download          (download the video url and save to output.)  
    -l, --list              (consider the <url> parameter as a list of links.)  
    -o, --output=VALUE      (specify the output file name.)  
    -w, --width=VALUE       (specify the preferred video width.)  
    -s, --size=VALUE        (specify the preferred video file size.)  
    -e, --ext=VALUE         (specify the preferred video file extension.)  
    -v, --verbose           (show verbose message.)  
    -h, -?, --help          (print the help.)  
