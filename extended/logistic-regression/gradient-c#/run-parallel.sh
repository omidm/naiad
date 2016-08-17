#!/usr/bin/env bash

#
#  Copyright 2013 Stanford University.
#  All rights reserved.
# 
#  Redistribution and use in source and binary forms, with or without
#  modification, are permitted provided that the following conditions
#  are met:
# 
#  - Redistributions of source code must retain the above copyright
#    notice, this list of conditions and the following disclaimer.
# 
#  - Redistributions in binary form must reproduce the above copyright
#    notice, this list of conditions and the following disclaimer in the
#    documentation and/or other materials provided with the
#    distribution.
# 
#  - Neither the name of the copyright holders nor the names of
#    its contributors may be used to endorse or promote products derived
#    from this software without specific prior written permission.
# 
#  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
#  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
#  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
#  FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL
#  THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
#  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
#  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
#  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
#  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
#  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
#  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
#  OF THE POSSIBILITY OF SUCH DAMAGE.
#

# Checks controller and worker basic functionalities against stencil 1D
# application.

# Author: Omid Mashayekhi <omidm@stanford.edu>

# **************************
# Text Reset
RCol='\x1B[0m'    
# Regular           
Bla='\x1B[0;90m';
Red='\x1B[0;91m';
Gre='\x1B[0;92m';
Yel='\x1B[0;93m';
Blu='\x1B[0;94m';
Pur='\x1B[0;95m';
Cya='\x1B[0;96m';
Whi='\x1B[0;97m';
# **************************



echo -e "${Cya}This script launches one process per each core to compute Gradient in parallel ${RCol}"
echo -e "${Cya}to emulate the resource sharing among threads at Naiad worker (called process). ${RCol}"

core_num=$(grep -c ^processor /proc/cpuinfo)
if ! [ -z ${core_num} ]; then
  echo -e "${Gre}Detected ${core_num} cores ...${RCol}"
else
  echo -e "${Red}Could not detect number of cores ...${RCol}"
fi

trap 'kill $(jobs -p)' EXIT

echo -e "${Cya}Launching ${core_num} parallel processes each on a separate core ...${RCol}"

for i in `seq 0 $((${core_num}-1))`; do
echo -e "${Cya}Launching on core $i ... ${RCol}"
  if [ "$i" != "$((${core_num}-1))" ]; then
    taskset -c $i mono Release/Gradient.exe 10 50 0.125 > /dev/null &
  else
    echo -e "${Cya}Output of core $i : ${RCol}"
    taskset -c $i mono Release/Gradient.exe 10 50 0.125
  fi
done


