#!/bin/sh

if [ -z ${2+x} ]; then
    exit 1;
fi

echo Parameters: $*

TARGET=../../../Bin64/Plugins/Local/
mkdir $TARGET

echo Deploying CLIENT plugin binary:

echo From "$1" to "$TARGET"
cp "$1" "$TARGET"
