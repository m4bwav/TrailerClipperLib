# TrailerClipperLib
A library and console app for batch removing trailers from video files.  
This library and console app uses nuget package for project MediaToolKit: https://github.com/AydinAdn/MediaToolkit

Standard Usage - Remove trailer:   
TClipper &lt;path> &lt;trailer_length_in_milliseconds>   
&lt;path> can be a single file or a directory   

Options:  
﻿-h - Displays this help  
-o &lt;output_filename> - outputs the configuration of the clipping to file, so that the configuration can be used again.  
-c, -config &lt;config_filepath> - reads the clipping parameters from a file, rather than from the command-line   
-i, -intro &lt;intro_length_in_milliseconds> - This options allows you to trim an intro rather than a trailer.  Can be used  with or without the trailer trimming functionality  
-cf, -consoleoff - Turns off logging output to console  
-d - delete the original files that are trimmed, off by default  
-m, -multi - Use multi-tasking, experimental, generally does not increase the speed of the trimming, but may show slight improvement  
-a, -allfiles - Process files that don't have a valid file extension as well as those that do.  Off by default  


"MediaToolkit is licensed under the [MIT license](https://github.com/AydinAdn/MediaToolkit/blob/master/LICENSE.md)  
MediaToolkit uses [FFmpeg](http://ffmpeg.org), a multimedia framework which is licensed under the [LGPLv2.1 license](http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html), its source can be downloaded from [here](https://github.com/AydinAdn/MediaToolkit/tree/master/FFMpeg%20src)"
