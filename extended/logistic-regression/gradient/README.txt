
---------------------------------------------
Author: Omid Mashayekhi <omidm@stanford.edu>
---------------------------------------------

The gradient operation in Logistic regression program on a single node. This
code is written to measure the bottomline speed of the program in C#.

Usage:
  Gradient.exe <dimension>
               <iteration_num>
               <sample_num in million>

To compile:
    $ make

To run an example over single node:
    $ make run

Default make is for Release mode, you can add "debug" prefix to generate the
Debug mode, e.g. "make debug" or "make debug-run"

