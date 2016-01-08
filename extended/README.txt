
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
    2. naiad-tutorial.html in the current directory


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


** NOTE: In the distributed mode when running with mono also pass the
"--inlineserializer" options for non-primitive data types (e.g. List<float>).
Naiad has a bug for serialization/deserialization when running under mono. 


Example of running logistic regression program over two local nodes: 
    $ mono LogisticRegression.exe -n 2 --local -p 0 --inlineserializer <logistic-regression-args>
    $ mono LogisticRegression.exe -n 2 --local -p 1 --inlineserializer <logistic-regression-args>

