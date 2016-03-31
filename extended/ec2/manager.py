#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

import sys
import os
import time
import subprocess
import argparse

import ec2
import utils
import config


parser = argparse.ArgumentParser(description='Nimbus EC2 Manager.')
parser.add_argument(
    "-l", "--launch",
    dest="launch",
    action="store_true",
    help="launch ec2 instances")
parser.add_argument(
    "-t", "--terminate",
    dest="terminate",
    action="store_true",
    help="terminate ec2 instances")
parser.add_argument(
    "-m", "--monitor",
    dest="monitor",
    action="store_true",
    help="monitor ec2 instances")
parser.add_argument(
    "-s", "--start",
    dest="start",
    action="store_true",
    help="start the experiment")
parser.add_argument(
    "-e", "--end",
    dest="end",
    action="store_true",
    help="end the experiment")
parser.add_argument(
    "-d", "--download",
    dest="download",
    action="store_true",
    help="download the logs")
parser.add_argument(
    "-c", "--clean",
    dest="clean",
    action="store_true",
    help="clean the logs")
parser.add_argument(
    "-p", "--print",
    dest="printdns",
    action="store_true",
    help="print controller and workers dns")
parser.add_argument(
    "-w", "--wake_up",
    dest="wakeup",
    action="store_true",
    help="ssh test in to nodes for testing")

parser.add_argument(
    "-pdns", "--use_private",
    dest="useprivate",
    action="store_true",
    help="if specified will use the private dns for inter node communications")

args = parser.parse_args()


if (args.monitor):
  ec2.wait_for_instances_to_start(
      config.EC2_LOCATION,
      config.WORKER_INSTANCE_NUM,
      placement_group=config.PLACEMENT_GROUP);

elif (args.launch):
  ans = raw_input("Are you sure you want to launch {} ec2 instances? (Enter 'yes' to proceed): ".format(config.WORKER_INSTANCE_NUM))
  if (ans != 'yes'):
    print "Aborted"
    exit(0)

  print "Launching the instances ..."
  ec2.run_instances(
      config.EC2_LOCATION,
      config.NAIAD_AMI,
      config.WORKER_INSTANCE_NUM,
      config.KEY_NAME,
      config.SECURITY_GROUP,
      config.PLACEMENT,
      config.PLACEMENT_GROUP,
      config.WORKER_INSTANCE_TYPE);

elif (args.terminate):
  ans = raw_input("Are you sure you want to terminate all ec2 instances? (Enter 'yes' to proceed): ")
  if (ans != 'yes'):
    print "Aborted"
    exit(0)

  print "Terminating the instances ..."
  ec2.terminate_instances(
      config.EC2_LOCATION,
      placement_group=config.PLACEMENT_GROUP);

elif (args.start or args.end or args.download or args.clean or args.printdns or args.wakeup):

  dns_names = ec2.get_dns_names(
      config.EC2_LOCATION,
      placement_group=config.PLACEMENT_GROUP);
  
  worker_dnss = list(dns_names["public"])
  if (not args.useprivate):
    worker_p_dnss = list(worker_dnss)
  else:
    worker_p_dnss = list(dns_names["private"])

  if (args.printdns):
    print "Worker DNSs:           " + str(worker_dnss)
    print "Worker Private DNSs:   " + str(worker_p_dnss)
 
  if (args.wakeup):
    utils.test_nodes(worker_dnss)
   
  if (args.start):
    utils.start_experiment(worker_dnss, worker_p_dnss)
  
  if(args.download):
    utils.collect_logs(worker_dnss)
  
  if (args.end):
    utils.stop_experiment(worker_dnss)

  if (args.clean):
    utils.clean_logs(worker_dnss)
  
else :
  print "\n** Provide an action to perform!\n"
  print parser.print_help();

