
---------------------------------------------
Author: Omid Mashayekhi <omidm@stanford.edu>
---------------------------------------------

The gradient operation in Logistic regression program on a single node written
in C#. This code is written to measure the bottomline speed of the program
without including the naiad runtime overhead.

Usage:
  Gradient.exe <dimension>
               <iteration_num>
               <sample_num in million>

Makefile options:
    $ make             to compile
    $ make run         to run an example over single node
    $ make clean       to clean generated binary

Default make is for Release mode, you can add "debug" prefix to generate the
Debug mode, e.g. "make debug" or "make debug-run"

