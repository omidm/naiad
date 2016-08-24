
---------------------------------------------
Author: Omid Mashayekhi <omidm@stanford.edu>
---------------------------------------------

This README file is prepared to give directives on writing, compiling and
running your own Naiad program. If you are looking to install and compile Naiad
library on Ubuntu, please read the README-UBUNTU.txtx in the parent folder.
There will also be some directives on running naiad in a distributed way, for
example a cluster of EC2 nodes or on your local machine.

For more information you can take a look at following tutorials:
    1. http://microsoftresearch.github.io/Naiad/ (has Naiad SDK)
    2. http://www.cambridgeplus.net/tutorials/2015/NaiadTutorial_2015/index.html

A copy of the useful documents can be found in the docs/ subdirectory:

    docs/naiad-tutorial.html
    docs/first-program.html


-------------------------------------------------------------------------------
How to write, compile and run your first Naiad program
-------------------------------------------------------------------------------
1. Create an empty directory, and write a .cs file which defines the dataflow
and input/output. Note that there should be class with a "static Main" method
for the entry point of the program.

3. To use Naiad classes and definitions, you need to incluse those in your
program. After compiling the Naiad Library (calling sh build_mono.sh in the
    parent directory) copy the necessary generated .dll files to your folder.
Some useful files cou;d be found in the following:
    <parent-dir>/Naiad/bin/Debug/Microsoft.Research.Naiad.dll
    <parent-dir>/Examples/bin/Debug/Microsoft.Research.Naiad.Lindi.dll
    <parent-dir>/Examples/bin/Debug/Microsoft.Research.Naiad.GraphLINQ.dll

4. You need to compile the program while referencing the libraries:
    $ dmcs <program>.cs /reference:Microsoft.Research.Naiad.dll
        - for each extra .dll file add /reference:<file.dll> statement.

5. Run the prgram using Mono:
    $ mono <program>.exe <naiad-options> <program-arguments-if-any>


- You can take a look at the first-naiad-program folder for the example program.
It has README and Makefile file on how to compile and run the code.

- Useful programs:
    <>parent-dir>/Examples/GraphLINQ/PageRank.cs


-------------------------------------------------------------------------------
How to activate assertions in mono Mono
-------------------------------------------------------------------------------

1. Debug.Assert is annotated with [ConditionalAttribute("DEBUG")]. This means
that all invocations are removed by the compiler unless the DEBUG preprocessor
symbol is defined. So while compiling with dmcs also pass the -d:DEBUG option.

    $ dmcs -d:DEBUG <program>.cs ...

2. Mono does not show a dialog box like Microsoft's .NET implementation when an
assertion is hit. You need to set a TraceListener, e.g.

    $ export MONO_TRACE_LISTENER=Console.Error
    $ mono <program>.exe

3. For an example, refer to the Makefile in k-means.


-------------------------------------------------------------------------------
How to run Naiad in distributed mode
-------------------------------------------------------------------------------

To run in the distributed mode leverage naiad command line options.

Naiad options:
	--procid,-p	Process ID (default 0)
	--threads,-t	Number of threads (default 1)
	--numprocs,-n	Number of processes (default 1)
	--hosts,-h	List of hostnames in the form host:port
	--local		Shorthand for -h localhost:2101 localhost:2102 ... localhost:2100+numprocs
  --inlineserializer pass when running with mono in distributed mode.

Runs single-process by default, optionally set -t for number of threads
To run multiprocess, specify -p, -n, and -h
   Example for 2 machines, M1 and M2, using TCP port 2101:
      M1> NaiadProgram.exe -p 0 -n 2 -h M1:2101 M2:2101
      M2> NaiadProgram.exe -p 1 -n 2 -h M1:2101 M2:2101
To run multiprocess on one machine, specify -p, -n and --local for each process

** NOTE: the hostname should be in the form of dns not ip address.

** NOTE: In the distributed mode when running with mono also pass the
"--inlineserializer" options for non-primitive data types (e.g. List<float>).
Naiad has a bug for serialization/deserialization when running under mono. 

** NOTE: All naiad processes should have the same -t option, so that all of them
have the same threads.

Example of running logistic regression program over two local nodes: 
    $ mono LogisticRegression.exe -n 2 --local -p 0 --inlineserializer <logistic-regression-args>
    $ mono LogisticRegression.exe -n 2 --local -p 1 --inlineserializer <logistic-regression-args>


-------------------------------------------------------------------------------
Partition mapping in Naiad
-------------------------------------------------------------------------------

Running operations such as GroupBy or CoGroupBy requires a mapping from each
sample to an integer. Each integer is mapped to a thread on a worker. In Naiad
terminology a worker is a "process",  and a thread is a "core". There are as
many physical replication of the computation flow as the number of threads in
the system (<-t arg> x <-n arg> ).

The mapping is as follows, where "TN" is the thread num and WN is the worker num:

0, ..., TN-1, TN, ..., 2xTN-1, ... , (WN-1)xTN, ... WNxTN-1

<-  P0   ->   <-  P1   ->             <-      Pn-1       ->

and the same pattern repeats to integers after WNxTN-1. The general rule is that
for an integer I, it will be mapped to the following worker and core:

Worker process ID -> (I / TN) % WN
Thread Physical ID -> I % TN


-------------------------------------------------------------------------------
How to activate Naiad logging
-------------------------------------------------------------------------------

To activate all logging levels in Naiad, change the following line in the
file "Naiad/Logging.cs":

     -        private static LoggingLevel logLevel = LoggingLevel.Error;
     +        private static LoggingLevel logLevel = LoggingLevel.Debug;

Look in to this file to find other options for different logging level. You
need to recompile the Naiad library, make clean, and rebuild the applications.
Note that the make clean for the applications is necessary.

You can also add logging directives any other place you want. For example to
chase down the runtime overhead for the OSDI'16 rebuttal I changed the sources a
reflected in "rebuttal-debug.diff". Again, don't forget to rebuild the Naiad
library, make clean the application and rebuilding the applications.


