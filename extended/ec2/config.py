#!/usr/bin/env python

# Author: Omid Mashayekhi <omidm@stanford.edu>

# ssh -i ~/.ssh/omidm-sing-key-pair-us-west-2.pem -o UserKnownHostsFile=/dev/null -o StrictHostKeyChecking=no ubuntu@<ip>

# EC2 configurations
# US West (Oregon) Region
EC2_LOCATION                    = 'us-west-2'
UBUNTU_AMI                      = 'ami-fa9cf1ca'
NAIAD_AMI                       = 'ami-8869b8e8'
KEY_NAME                        = 'omidm-sing-key-pair-us-west-2'
SECURITY_GROUP                  = 'nimbus_sg_uswest2'
WORKER_INSTANCE_TYPE            = 'c3.2xlarge'
PLACEMENT                       = 'us-west-2c' # None
PLACEMENT_GROUP                 = 'nimbus-cluster' # None
PRIVATE_KEY                     = '/home/omidm/.ssh/' + KEY_NAME + '.pem'
WORKER_INSTANCE_NUM             = 100

# Naiad configurations
WORKER_PER_INSTANCE             = 1
WORKER_THREAD_NUM               = 8
APPLICATION                     = 'lr' # 'lr' 'stencil-1d' 'k-means'
FIRST_PORT                      = 5800
RUN_WITH_TASKSET                = False
WORKER_TASKSET                  = '0-1,4-5' # '0-3,8-11'

# Simulation configurations
# lr and k-means
DIMENSION                       = 10
CLUSTER_NUM                     = 2
ITERATION_NUM                   = 30
PARTITION_PER_CORE              = 10
PARTITION_NUM                   = WORKER_INSTANCE_NUM * WORKER_PER_INSTANCE * WORKER_THREAD_NUM * PARTITION_PER_CORE # 8000
SAMPLE_NUM_M                    = 544 # PARTITION_NUM / 1000000.0
SPIN_WAIT_US                    = 0 # 4000 * (100 / (WORKER_INSTANCE_NUM * WORKER_PER_INSTANCE))



