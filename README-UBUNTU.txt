
---------------------------------------------
Author: Omid Mashayekhi <omidm@stanford.edu>
---------------------------------------------

This README file is prepared to give directives on installing and running Naiad
on Ubuntu. It is tested on Ubuntu 12.04 but the process should work for later
versions of the Ubuntu as well. To run C# with in the Microsoft's .NET
framework on Ubuntu you need to use Mono. In the following, you will find
instructions on installing Mono as well.

The "extended" directory, has additional instructions and examples added by
me to show how to write and run customized Naiad programs, as well as running
the distributed version of the program on multiple nodes (e.g. EC2 nodes).


-------------------------------------------------------------------------------
How to get, compile, and run Naiad
-------------------------------------------------------------------------------
1. First you need to install Mono (> 4.2), see instructions below.

2. Clone the Naiad git repository from GitHub. Note that the latest release of
Naiad does not work on Ubuntu, so you need to checkout v0.4.2 release. This
directory already contain the correct and complete version of files that work
on Ubuntu 12.04.

    $ git clone https://github.com/MicrosoftResearch/Naiad.git
    $ cd Naiad
    $ git checkout tags/v0.4.2

3. Build the Naiad library and examples. By the default it build in the Debug
mode. To build in the Release mode, simply open and modify the build_mono.sh
file accordingly.

    $ sh ./build_mono.sh

4. There are a few available examples that you can run immediately.

    $ cd Examples/bin/Debug/
    $ mono Examples.exe wordcount

To explore more examples and Naiad running options, simply run Example.exe with
no arguments to get the help printed out.

    $ mono Examples.exe

    

-------------------------------------------------------------------------------
How to install latest version of Mono
-------------------------------------------------------------------------------

Refer to the website: http://www.mono-project.com/download/#download-lin

1. un-install any available older version of the mono.

    $ sudo apt-get purge libmono* cli-common mono-runtime
    $ (you may need to clean up file /etc/apt/sources.list.d/mono-xamarin.list if existed)
    $ sudo apt-get -f install (might not be necessary)
    $ sudo apt-get autoremove


2. Download and install latest version of Mono (>4.2), from Mono project website:
      http://www.mono-project.com/download/#download-lin
   Note the specific instructions for Ubuntu 12.04 on the website. For your
   convenience, here are the detailed steps you need to take for Ubuntu 12.04.
   For the latest Version of the Ubuntu, refer to the website.

    $ sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
    $ echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
    $ echo "deb http://download.mono-project.com/repo/debian wheezy-libtiff-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
    $ sudo apt-get update

    $ sudo apt-get install mono-devel
    $ sudo apt-get install mono-complete
    $ sudo apt-get install referenceassemblies-pcl 
    $ sudo apt-get install ca-certificates-mono

Note: Maybe versions older than 4.2 would work as well, but it has not been
tested. To get the older version through apt-get use following commands: 

    $ sudo apt-get install mono-devel
    $ sudo apt-get install mono-complete
    $ sudo apt-get install mono-xbuild

However, if you install the older versio, you need to first completely remove
it before installing the newer version as instructed above!


