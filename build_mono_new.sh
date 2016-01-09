#!/bin/bash

if [ "$1" = "-debug" ]
then
  # To build in Debug mode. Binaries will be placed in $PROJ/bin/Debug.
  echo "**** Building Naiad in Debug mode"
  xbuild /p:Configuration="MonoDebug"
else
  # To build in Release mode. Binaries will be placed in $PROJ/bin/Release.
  echo "**** Building Naiad in Release mode"
  xbuild /p:Configuration="MonoRelease"
fi

