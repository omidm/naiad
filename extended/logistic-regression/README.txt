
---------------------------------------------
Author: Omid Mashayekhi <omidm@stanford.edu>
---------------------------------------------

Logistic regression program, applying the logistic regression algorithm over a
collection of input labeled samples. Samples are randomly generated but
potentially you could change the code to read in samples from input files.
Workers could be multiple threaded, so set the number of threads to the number
of cores available at the worker, using the -t option. Also, you could have
more than one partition per core. 

Usage:
  LogisticRegression.exe <dimension>
                         <iteration_num>
                         <partition_num>
                         <sample_num in million>
                         <worker_num>
                         <spin_wait in us>

If spin_wait is not zero the gradient phase is replaced by an exact busy loop.

To run N, T-threaded workers locally, luanch workers using the following command
where PID is replaced by a unique integer from 0 to N-1 for each worker.

    $ mono LogisticRegression.exe -n N --local -p PID -t T --inlineserializer <lr-arguments>

Makefile options:
    $ make             to compile
    $ make run         to run an example over single node
    $ make run-dist    to run an example over two nodes on your local machine
    $ make run-spin    to run an example over single node and replace the
                       gradient operation with an spin wait
    $ make clean       to clean generated binary

Default make is for Release mode, you can add "debug" prefix to generate the
Debug mode, e.g. "make debug" or "make debug-run"



** NOTE: take a look at the files in "progressive-files/" folder. There are few
different versions of the code that I have written. Specifically, the
"LogisticRegression-Migration.cs" one has some good points on migrating samples
between node through changing the key mappings.
