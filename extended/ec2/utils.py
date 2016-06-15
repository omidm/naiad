#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

import sys
import os
import subprocess
import time

import config
import ec2

# Logging configurations
STD_OUT_LOG                = 'ec2_log.txt'
OUTPUT_PATH                = 'output/'


# Path configuration
NAIAD_ROOT                 = '~/cloud/src/naiad/'




LR_REL_WORKER_PATH  = 'extended/logistic-regression/Release/'
LR_WORKER_EXE       = 'LogisticRegression.exe'
LR_APP_OPTIONS      = ' '
LR_APP_OPTIONS     += ' ' + str(config.DIMENSION)
LR_APP_OPTIONS     += ' ' + str(config.ITERATION_NUM)
LR_APP_OPTIONS     += ' ' + str(config.PARTITION_NUM)
LR_APP_OPTIONS     += ' ' + str(config.SAMPLE_NUM_M)
LR_APP_OPTIONS     += ' ' + str(config.WORKER_INSTANCE_NUM * config.WORKER_PER_INSTANCE)
LR_APP_OPTIONS     += ' ' + str(config.SPIN_WAIT_US)


KM_REL_WORKER_PATH  = 'extended/k-means/Release/'
KM_WORKER_EXE       = 'KMeans.exe'
KM_APP_OPTIONS      = ' '
KM_APP_OPTIONS     += ' ' + str(config.DIMENSION)
KM_APP_OPTIONS     += ' ' + str(config.CLUSTER_NUM)
KM_APP_OPTIONS     += ' ' + str(config.ITERATION_NUM)
KM_APP_OPTIONS     += ' ' + str(config.PARTITION_NUM)
KM_APP_OPTIONS     += ' ' + str(config.SAMPLE_NUM_M)
KM_APP_OPTIONS     += ' ' + str(config.WORKER_INSTANCE_NUM * config.WORKER_PER_INSTANCE)
KM_APP_OPTIONS     += ' ' + str(config.SPIN_WAIT_US)




if (config.APPLICATION == 'lr'):
  REL_WORKER_PATH = LR_REL_WORKER_PATH
  WORKER_EXE      = LR_WORKER_EXE
  APP_OPTIONS     = LR_APP_OPTIONS
elif (config.APPLICATION == 'k-means'):
  REL_WORKER_PATH = KM_REL_WORKER_PATH
  WORKER_EXE      = KM_WORKER_EXE
  APP_OPTIONS     = KM_APP_OPTIONS
else:
  print "ERROR: Unknown application: " + config.APPLICATION
  exit(0)



def start_experiment(worker_dnss, worker_p_dnss):

  assert(len(worker_dnss) >= config.WORKER_INSTANCE_NUM);
  assert(len(worker_p_dnss) >= config.WORKER_INSTANCE_NUM);

  worker_num = config.WORKER_INSTANCE_NUM * config.WORKER_PER_INSTANCE 

  host_list = ''
  for idx in range(0, config.WORKER_INSTANCE_NUM):
    p_dns = worker_p_dnss[idx]
    for w in range(0, config.WORKER_PER_INSTANCE):
      host_list += p_dns + ':'
      host_list += str(config.FIRST_PORT + idx * config.WORKER_PER_INSTANCE + w)
      host_list += ' '

  for idx in range(0, config.WORKER_INSTANCE_NUM):
    dns = worker_dnss[idx]
    for w in range(0, config.WORKER_PER_INSTANCE):
      pid = idx * config.WORKER_PER_INSTANCE + w
      start_worker(worker_num, dns, pid, host_list);


def start_worker(worker_num, worker_dns, pid, host_list):
  worker_command =  'cd ' + NAIAD_ROOT + REL_WORKER_PATH + ';'
  if config.RUN_WITH_TASKSET:
    worker_command += 'taskset -c ' + config.WORKER_TASKSET + ' mono ' + WORKER_EXE 
  else:
    worker_command += 'mono ' + WORKER_EXE
  worker_command += ' -n ' + str(worker_num)
  worker_command += ' -p ' + str(pid)
  worker_command += ' -t ' + str(config.WORKER_THREAD_NUM)
  worker_command += ' -h ' + host_list
  worker_command += ' --inlineserializer'
  worker_command += ' ' + APP_OPTIONS
  worker_command += ' &> ' + str(pid) + '_' + STD_OUT_LOG

  subprocess.Popen(['ssh', '-q', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + worker_dns, worker_command])

  print '** Worker ' + str(pid + 1) + ' Launched: ' + worker_dns


def stop_experiment(worker_dnss):

  for dns in worker_dnss:
    stop_worker(dns);


def stop_worker(worker_dns):
  worker_command =  'killall -v mono;'

  subprocess.Popen(['ssh', '-q', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + worker_dns, worker_command])

  print '** Worker Terminated: ' + worker_dns


def  test_nodes(node_dnss):
  command  = 'cd ' + NAIAD_ROOT + REL_WORKER_PATH + ';'
  command += 'pwd;'

  for dns in node_dnss:
    subprocess.Popen(['ssh', '-q', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + dns, command])

    print '** Node Tested: ' + dns


def collect_logs(worker_dnss):

  subprocess.call(['rm', '-rf', OUTPUT_PATH])
  subprocess.call(['mkdir', '-p', OUTPUT_PATH])

  for dns in worker_dnss:
    subprocess.Popen(['scp', '-q', '-r', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + dns + ':' + NAIAD_ROOT +
        REL_WORKER_PATH  + '*_' + STD_OUT_LOG,
        OUTPUT_PATH])

def clean_logs(worker_dnss):

  worker_path = NAIAD_ROOT + REL_WORKER_PATH;
  worker_command  = 'rm -rf ' + worker_path + '*_' + STD_OUT_LOG + ';'
  for dns in worker_dnss:
    subprocess.Popen(['ssh', '-q', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + dns, worker_command])
  
    print '** Worker Cleaned: ' + dns


