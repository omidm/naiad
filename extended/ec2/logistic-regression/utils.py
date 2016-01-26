#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

import sys
import os
import subprocess
import time

import config

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
import ec2

def run_experiment(worker_dnss, worker_p_dnss):

  assert(len(worker_dnss) == config.WORKER_INSTANCE_NUM);
  assert(len(worker_p_dnss) == config.WORKER_INSTANCE_NUM);

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
      run_worker(worker_num, dns, pid, host_list);


def run_worker(worker_num, worker_dns, pid, host_list):
  worker_command =  'cd ' + config.EC2_NAIAD_ROOT + config.REL_WORKER_PATH + ';'
  if config.RUN_WITH_TASKSET:
    worker_command += 'taskset -c ' + config.WORKER_TASKSET + ' mono ' + config.WORKER_EXE 
  else:
    worker_command += 'mono ' + config.WORKER_EXE
  worker_command += ' -n ' + str(worker_num)
  worker_command += ' -p ' + str(pid)
  worker_command += ' -t ' + str(config.WORKER_THREAD_NUM)
  worker_command += ' -h ' + host_list
  worker_command += ' --inlineserializer'
  worker_command += ' ' + str(config.DIMENSION)
  worker_command += ' ' + str(config.ITERATION_NUM)
  worker_command += ' ' + str(config.PARTITION_NUM)
  worker_command += ' ' + str(config.SAMPLE_NUM_M)
  worker_command += ' ' + str(worker_num)
  worker_command += ' ' + str(config.SPIN_WAIT_US)
  worker_command += ' &> ' + str(pid) + '_' + config.STD_OUT_LOG

  subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + worker_dns, worker_command])

  print '** Worker ' + str(pid + 1) + ' Launched: ' + worker_dns


def terminate_experiment(worker_dnss):

  assert(len(worker_dnss) == config.WORKER_INSTANCE_NUM);
  for dns in worker_dnss:
    terminate_worker(dns);


def terminate_worker(worker_dns):
  worker_command =  'killall -v mono;'

  subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + worker_dns, worker_command])

  print '** Worker Terminated: ' + worker_dns


def  test_nodes(node_dnss):
  command  = 'cd ' + config.EC2_NAIAD_ROOT + config.REL_WORKER_PATH + ';'
  command += 'pwd;'

  for dns in node_dnss:
    subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + dns, command])

    print '** Node Tested: ' + dns


def collect_output_data(worker_dnss):

  subprocess.call(['rm', '-rf', config.OUTPUT_PATH])
  subprocess.call(['mkdir', '-p', config.OUTPUT_PATH])

  for dns in worker_dnss:
    subprocess.Popen(['scp', '-r', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + dns + ':' + config.EC2_NAIAD_ROOT +
        config.REL_WORKER_PATH  + '*_' + config.STD_OUT_LOG,
        config.OUTPUT_PATH])

def clean_output_data(worker_dnss):

  worker_path = config.EC2_NAIAD_ROOT + config.REL_WORKER_PATH;
  worker_command  = 'rm -rf ' + worker_path + '*_' + config.STD_OUT_LOG + ';'
  for dns in worker_dnss:
    subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + dns, worker_command])
  
    print '** Worker Cleaned: ' + dns

