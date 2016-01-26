#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

import sys
import os
import time
import subprocess
import argparse

import utils
import config

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
import ec2

action_help = "action to do: "
action_help += "\np: just print the ip addresses"
action_help += "\nw: test the workers to see if they ar awake"
action_help += "\nr: run the experiment"
action_help += "\nc: collect the results"
action_help += "\nt: terminate and clean"
action_help += "\ns: start ec2 instances"
action_help += "\nm: monitor ec2 instances"
action_help += "\nk: terminate ec2 instances"

parser = argparse.ArgumentParser(description='Process log files.')
parser.add_argument(
    "-a", "--action",
    dest="action",
    default='p',
    help=action_help)
parser.add_argument(
    "-pp", "--use_private",
    dest="useprivate",
    action="store_true",
    help="if specified will use the private ips for inter node communications")

args = parser.parse_args()


if args.action == 's':
  ec2.run_instances(
      config.EC2_LOCATION,
      config.NAIAD_AMI,
      config.WORKER_INSTANCE_NUM,
      config.KEY_NAME,
      config.SECURITY_GROUP,
      config.PLACEMENT,
      config.PLACEMENT_GROUP,
      config.WORKER_INSTANCE_TYPE);

elif args.action == 'k':
  ec2.terminate_instances(
      config.EC2_LOCATION,
      placement_group=config.PLACEMENT_GROUP);

elif args.action == 'm':
  ec2.wait_for_instances_to_start(
      config.EC2_LOCATION,
      config.WORKER_INSTANCE_NUM,
      placement_group=config.PLACEMENT_GROUP);

else:

  ip_addresses = ec2.get_ip_addresses(
      config.EC2_LOCATION,
      placement_group=config.PLACEMENT_GROUP);
  
  if args.action == 'p':
    print ip_addresses
    exit(0)

  dns_names = ec2.get_dns_names(
      config.EC2_LOCATION,
      placement_group=config.PLACEMENT_GROUP);
  
  worker_dnss = list(dns_names["public"])
  if (not args.useprivate):
    worker_p_dnss = list(worker_dnss)
  else:
    worker_p_dnss = list(dns_names["private"])

  if args.action == 'w':
    utils.test_nodes(worker_dnss)
   
  elif args.action == 'r':
    utils.run_experiment(worker_dnss, worker_p_dnss)
  
  elif args.action == 'c':
    utils.collect_output_data(worker_dnss)
  
  elif args.action == 't':
    utils.terminate_experiment(worker_dnss)
    utils.clean_output_data(worker_dnss)
  
  else :
    print "Unknown action: " + args.action

