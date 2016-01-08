#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

# ssh -i ~/.ssh/omidm-sing-key-pair-us-west-2.pem -o UserKnownHostsFile=/dev/null -o StrictHostKeyChecking=no ubuntu@<ip>

# EC2 configurations
# US West (Oregon) Region
EC2_LOCATION                    = 'us-west-2'
UBUNTU_AMI                      = 'ami-fa9cf1ca'
NAIAD_AMI                       = 'ami-ea3c278b'
KEY_NAME                        = 'omidm-sing-key-pair-us-west-2'
SECURITY_GROUP                  = 'nimbus_sg_uswest2'
WORKER_INSTANCE_TYPE            = 'c3.2xlarge'
PLACEMENT                       = 'us-west-2c' # None
PLACEMENT_GROUP                 = 'nimbus-cluster' # None
PRIVATE_KEY                     = '/home/omidm/.ssh/' + KEY_NAME + '.pem'

# US West (Northern California) Region
# EC2_LOCATION = 'us-west-1'
# UBUNTU_AMI = 'ami-660c3023'
# KEY_NAME = 'omidm-sing-key-pair-us-west-1'
# SECURITY_GROUP = 'nimbus_sg_uswest1'


# simulation configurations
WORKER_INSTANCE_NUM             = 25
WORKER_PER_INSTANCE             = 1
WORKER_THREAD_NUM               = 8
DIMENSION                       = 10
ITERATION_NUM                   = 30
PARTITION_NUM                   = 2000
SAMPLE_NUM_M                    = 100
RUN_WITH_TASKSET                = False
WORKER_TASKSET                  = '0-1,4-5'
# WORKER_TASKSET                = '0-3,8-11'
FIRST_PORT                      = 2101

# logging configurations
STD_OUT_LOG                     = 'ec2_log.txt'
OUTPUT_PATH                     = 'output/'


# Build and Path configuration
EC2_NAIAD_ROOT                  = '~/cloud/src/naiad/'
REL_WORKER_PATH                 = 'extended/logistic-regression/'
WORKER_EXE                      = 'LogisticRegression.exe'



