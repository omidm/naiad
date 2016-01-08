#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

import sys
import os
import subprocess
import time

import config

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
import ec2

def run_experiment(worker_ips, worker_p_ips):

  assert(len(worker_ips) == config.WORKER_INSTANCE_NUM);
  assert(len(worker_p_ips) == config.WORKER_INSTANCE_NUM);

  worker_num = config.WORKER_INSTANCE_NUM * config.WORKER_PER_INSTANCE 

  host_list = ''
  for idx in range(0, config.WORKER_INSTANCE_NUM):
    p_ip = worker_p_ips[idx]
    for w in range(0, config.WORKER_PER_INSTANCE):
      host_list += p_ip + ':'
      host_list += str(config.FIRST_PORT + idx * config.WORKER_PER_INSTANCE + w)
      host_list += ' '

  for idx in range(0, config.WORKER_INSTANCE_NUM):
    ip = worker_ips[idx]
    p_ip = worker_p_ips[idx]
    for w in range(0, config.WORKER_PER_INSTANCE):
      pid = idx * config.WORKER_PER_INSTANCE + w
      run_worker(worker_num, ip, p_ip, pid, host_list);


def run_worker(worker_num, worker_ip, worker_p_ip, pid, host_list):
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
  worker_command += ' &> ' + str(pid) + '_' + config.STD_OUT_LOG

  subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + worker_ip, worker_command])

  print '** Worker ' + str(pid + 1) + ' Launched: ' + worker_ip


def terminate_experiment(controller_ip, worker_ips):

  assert(len(worker_ips) == config.WORKER_INSTANCE_NUM);
  for ip in worker_ips:
    terminate_worker(ip);


def terminate_worker(worker_ip):
  worker_command =  'killall -v mono;'

  subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + worker_ip, worker_command])

  print '** Worker Terminated: ' + worker_ip


def  test_nodes(node_ips):
  command  = 'cd ' + config.EC2_NIMBUS_ROOT + config.REL_WORKER_PATH + ';'
  command += 'pwd;'

  for ip in node_ips:
    subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + ip, command])

    print '** Node Tested: ' + ip


def collect_output_data(controller_ip, worker_ips):

  subprocess.call(['rm', '-rf', config.OUTPUT_PATH])
  subprocess.call(['mkdir', '-p', config.OUTPUT_PATH])

  subprocess.Popen(['scp', '-r', '-i', config.PRIVATE_KEY,
      '-o', 'UserKnownHostsFile=/dev/null',
      '-o', 'StrictHostKeyChecking=no',
      'ubuntu@' + controller_ip + ':' + config.EC2_NIMBUS_ROOT +
      config.REL_CONTROLLER_PATH + config.STD_OUT_LOG,
      config.OUTPUT_PATH])

  for ip in worker_ips:
    subprocess.Popen(['scp', '-r', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + ip + ':' + config.EC2_NIMBUS_ROOT +
        config.REL_WORKER_PATH  + '*_' + config.STD_OUT_LOG,
        config.OUTPUT_PATH])

def clean_output_data(worker_ips):

  worker_path = config.EC2_NIMBUS_ROOT + config.REL_WORKER_PATH;
  worker_command  = 'rm -rf ' + worker_path + '*_' + config.STD_OUT_LOG + ';'
  for ip in worker_ips:
    subprocess.Popen(['ssh', '-i', config.PRIVATE_KEY,
        '-o', 'UserKnownHostsFile=/dev/null',
        '-o', 'StrictHostKeyChecking=no',
        'ubuntu@' + ip, worker_command])
  
    print '** Worker Cleaned: ' + ip

