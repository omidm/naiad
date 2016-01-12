#!/usr/bin/python

# ./get_summary.py [directory_name] [number of workers]

import sys
import argparse

def parse_line(line):
    items = line.split()
    assert(len(items) == 6);
    return float(items[1]),  float(items[3]), float(items[5])


## Parse the command line arguments ##
parser = argparse.ArgumentParser(description='Process log files.')
parser.add_argument(
    "-d", "--dir_path",
    dest="dirpath",
    default="output/",
    help="input dorectory path")
parser.add_argument(
    "-wn", "--worker_num",
    dest="workernum",
    default=25,
    help="worker num")
parser.add_argument(
    "-ti", "--truncate_index",
    dest="truncateindex",
    default=0,
    help="truncate index to ignore the initial iterations")
parser.add_argument(
    "-v", "--verbose",
    dest="collapse",
    action="store_true",
    help="print per iteration stats as well")


args = parser.parse_args()

D  = args.dirpath 
N  = int(args.workernum) 
TI = int(args.truncateindex)





total_loop_sum     = 0
total_gradient_sum = 0
iter_nums = []
    
print '--------------------------------------------------------------------------------------'
print '   worker     iter    gradient        total'
print '--------------------------------------------------------------------------------------'

for pid in range(0, N):
  f = open('{}/{}_ec2_log.txt'.format(D, pid), 'r')

  iter_num     = 0
  loop_sum     = 0
  gradient_sum = 0

  idx = 0
  for line in f:
    if "Loop" in line:
      idx += 1
      iter_idx, gradient, loop = parse_line(line)
      loop     /= 1000
      gradient /= 1000
      assert(idx == iter_idx)
      if (args.collapse):
        print '          {:8.0f} {:8.3f}        {:8.3f}'.format(idx, gradient, loop)
      if idx <= TI:
        continue
      iter_num     += 1
      loop_sum     += loop
      gradient_sum += gradient

  iter_nums.append(iter_num)
  total_loop_sum     += loop_sum/iter_num
  total_gradient_sum += gradient_sum/iter_num

  print '{:8.0f}: {:8.0f} {:8.3f}        {:8.3f}'.format(pid+1, iter_num, gradient_sum/iter_num, loop_sum/iter_num)

print '--------------------------------------------------------------------------------------'
print ' Average: {:8.0f} {:8.3f}        {:8.3f}'.format(iter_nums[0], total_gradient_sum/N, total_loop_sum/N)
print '--------------------------------------------------------------------------------------'
